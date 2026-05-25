using BepInEx.Bootstrap;
using BepInEx.Logging;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

internal static class PatchRegistry
{
    private sealed class PatchDescriptor
    {
        public readonly string Name;
        public readonly Type RootType;
        public readonly IReadOnlyList<Type> PatchTypes;
        public readonly string[] RequiredPlugins;

        public PatchDescriptor(Type rootType, IReadOnlyList<Type> patchTypes, string[] requiredPlugins)
        {
            RootType = rootType;
            PatchTypes = patchTypes;
            RequiredPlugins = requiredPlugins ?? Array.Empty<string>();
            Name = rootType.FullName ?? rootType.Name;
        }
    }

    public static void Apply(Harmony harmony, ManualLogSource logger, bool isServerRuntime, ISet<Type> failedAutoloadTypes)
    {
        HashSet<Type> seenPatchTypes = new();
        int patchedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        foreach (PatchDescriptor descriptor in DiscoverPatchDescriptors())
        {
            if (ValheimEnchantmentSystem.IsTypeBlockedByAutoloadFailure(descriptor.RootType, failedAutoloadTypes))
            {
                skippedCount += descriptor.PatchTypes.Count;
                continue;
            }

            string[] missingPlugins = descriptor.RequiredPlugins
                .Where(pluginGuid => !Chainloader.PluginInfos.ContainsKey(pluginGuid))
                .ToArray();
            if (missingPlugins.Length > 0)
            {
                skippedCount += descriptor.PatchTypes.Count;
                logger.LogDebug($"Skipped Harmony patch root '{descriptor.Name}': missing plugin(s) {string.Join(", ", missingPlugins)}");
                continue;
            }

            foreach (Type patchType in descriptor.PatchTypes)
            {
                if (!seenPatchTypes.Add(patchType))
                {
                    continue;
                }

                if (isServerRuntime && patchType.GetCustomAttribute<ClientOnlyPatch>() != null)
                {
                    skippedCount++;
                    continue;
                }

                if (!isServerRuntime && patchType.GetCustomAttribute<ServerOnlyPatch>() != null)
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    patchedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogWarning($"Skipped Harmony patch '{patchType.FullName}' in root '{descriptor.Name}': {ex.Message}");
                    logger.LogDebug(ex);
                }
            }
        }

        logger.LogInfo($"Harmony patch discovery applied. patched={patchedCount}, skipped={skippedCount}, failed={failedCount}");
    }

    private static IEnumerable<PatchDescriptor> DiscoverPatchDescriptors()
    {
        IEnumerable<Type> rootTypes = ValheimEnchantmentSystem.GetAssemblyTypes()
            .Select(ValheimEnchantmentSystem.GetTopLevelDeclaringType)
            .Distinct()
            .OrderBy(rootType => rootType.FullName, StringComparer.Ordinal);

        foreach (Type rootType in rootTypes)
        {
            List<Type> patchTypes = EnumerateTypeTree(rootType)
                .Where(type => type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();
            if (patchTypes.Count == 0)
            {
                continue;
            }

            string[] requiredPlugins = rootType.GetCustomAttributes(typeof(VES_RequiresPlugin), false)
                .Cast<VES_RequiresPlugin>()
                .Select(attribute => attribute.PluginGuid)
                .Where(pluginGuid => !string.IsNullOrWhiteSpace(pluginGuid))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            yield return new PatchDescriptor(rootType, patchTypes, requiredPlugins);
        }
    }

    private static IEnumerable<Type> EnumerateTypeTree(Type type)
    {
        yield return type;

        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (Type child in EnumerateTypeTree(nestedType))
            {
                yield return child;
            }
        }
    }
}
