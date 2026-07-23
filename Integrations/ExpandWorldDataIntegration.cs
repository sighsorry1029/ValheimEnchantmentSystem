using BepInEx.Bootstrap;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Integrations;

[VES_RequiresPlugin("expand_world_data")]
public sealed class ExpandWorldDataIntegration : IOptionalIntegration, IBiomeCatalogSource
{
    public static readonly ExpandWorldDataIntegration Instance = new();

    private const string ExpandWorldDataGuid = "expand_world_data";
    private const string ExpandWorldBiomeManagerTypeName = "ExpandWorldData.BiomeManager";
    private const string ExpandWorldBiomeToDisplayNameFieldName = "BiomeToDisplayName";
    private IReadOnlyList<MethodBase>? _biomeReloadTargetMethods;

    public string Name => "ExpandWorldData";

    public event Action? Changed;

    public bool IsAvailable()
    {
        return Chainloader.PluginInfos.ContainsKey(ExpandWorldDataGuid);
    }

    public void Initialize()
    {
    }

    public bool TryGetCustomBiomes(out List<CustomBiomeDefinition> customBiomes)
    {
        customBiomes = new List<CustomBiomeDefinition>();
        if (!IsAvailable())
        {
            return false;
        }

        Type biomeManagerType = AccessTools.TypeByName(ExpandWorldBiomeManagerTypeName);
        if (biomeManagerType == null)
        {
            Utils.print("ExpandWorldData biome manager type was not found while refreshing custom biome tiers.", ConsoleColor.Yellow);
            return false;
        }

        FieldInfo biomeToDisplayNameField = AccessTools.Field(biomeManagerType, ExpandWorldBiomeToDisplayNameFieldName);
        if (biomeToDisplayNameField == null)
        {
            Utils.print("ExpandWorldData biome name map field was not found while refreshing custom biome tiers.", ConsoleColor.Yellow);
            return false;
        }

        object rawMap;
        try
        {
            rawMap = biomeToDisplayNameField.GetValue(null);
        }
        catch (Exception ex)
        {
            Utils.print($"Failed to read ExpandWorldData biome map: {ex.Message}", ConsoleColor.Yellow);
            return false;
        }

        if (rawMap is not IEnumerable entries)
        {
            return false;
        }

        foreach (object entry in entries)
        {
            if (!TryGetBiomeMapEntry(entry, out Heightmap.Biome biome, out string identifier))
            {
                continue;
            }

            if (biome == Heightmap.Biome.None || string.Equals(identifier, "none", StringComparison.OrdinalIgnoreCase) || EnchantmentTierCatalog.IsBuiltInBiome(biome))
            {
                continue;
            }

            customBiomes.Add(new CustomBiomeDefinition(biome, identifier));
        }

        return true;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    private static bool TryGetBiomeMapEntry(object entry, out Heightmap.Biome biome, out string identifier)
    {
        biome = Heightmap.Biome.None;
        identifier = string.Empty;
        if (entry == null)
        {
            return false;
        }

        Type entryType = entry.GetType();
        PropertyInfo keyProperty = AccessTools.Property(entryType, "Key");
        PropertyInfo valueProperty = AccessTools.Property(entryType, "Value");
        if (keyProperty == null || valueProperty == null)
        {
            return false;
        }

        object keyValue = keyProperty.GetValue(entry);
        object value = valueProperty.GetValue(entry);
        if (keyValue == null || value is not string rawIdentifier)
        {
            return false;
        }

        biome = (Heightmap.Biome)Convert.ToInt32(keyValue);
        identifier = rawIdentifier.Trim();
        return identifier.Length > 0;
    }

    private IReadOnlyList<MethodBase> GetBiomeReloadTargetMethods(bool logMissingHooks)
    {
        if (_biomeReloadTargetMethods is { } cachedMethods)
        {
            return cachedMethods;
        }

        if (!IsAvailable())
        {
            return Array.Empty<MethodBase>();
        }

        Type biomeManagerType = AccessTools.TypeByName(ExpandWorldBiomeManagerTypeName);
        if (biomeManagerType == null)
        {
            if (logMissingHooks)
            {
                Utils.print("ExpandWorldData biome manager type was not found while preparing biome reload hooks.", ConsoleColor.Yellow);
            }

            return Array.Empty<MethodBase>();
        }

        IReadOnlyList<MethodBase> methods = _biomeReloadTargetMethods = FindBiomeReloadTargetMethods(biomeManagerType);

        if (methods.Count == 0 && logMissingHooks)
        {
            Utils.print("ExpandWorldData is installed, but no known biome reload hook was found. Skipping biome reload patch.", ConsoleColor.Yellow);
        }

        return methods;
    }

    internal static IReadOnlyList<MethodBase> FindBiomeReloadTargetMethods(Type biomeManagerType)
    {
        MethodInfo[] declaredMethods = biomeManagerType.GetMethods(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly);
        List<MethodBase> methods = new();

        AddMethodIfFound(methods, FindVoidMethod(declaredMethods, "NamesFromFile", 0));
        AddMethodIfFound(methods, FindVoidMethod(declaredMethods, "ReadConfigs", 0));
        AddMethodIfFound(methods, FindVoidMethod(declaredMethods, "FromSetting", 1, typeof(string)));
        AddMethodIfFound(methods, FindVoidMethod(declaredMethods, "SetNames", 1));

        return methods.ToArray();
    }

    private static MethodInfo? FindVoidMethod(MethodInfo[] methods, string name, int parameterCount, Type? firstParameterType = null)
    {
        foreach (MethodInfo method in methods)
        {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal) ||
                method.ReturnType != typeof(void) ||
                method.ContainsGenericParameters)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != parameterCount)
                continue;
            if (firstParameterType != null && parameters[0].ParameterType != firstParameterType)
                continue;

            return method;
        }

        return null;
    }

    private static void AddMethodIfFound(ICollection<MethodBase> methods, MethodInfo? method)
    {
        if (method != null && !methods.Contains(method))
        {
            methods.Add(method);
        }
    }

    [HarmonyPatch]
    private static class BiomeReloadPatch
    {
        [UsedImplicitly]
        private static bool Prepare()
        {
            return Instance.GetBiomeReloadTargetMethods(logMissingHooks: true).Count > 0;
        }

        [UsedImplicitly]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return Instance.GetBiomeReloadTargetMethods(logMissingHooks: false);
        }

        [UsedImplicitly]
        private static void Postfix()
        {
            Instance.NotifyChanged();
        }
    }
}
