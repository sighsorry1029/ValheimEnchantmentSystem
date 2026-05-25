using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload(VES_Autoload.Priority.Normal)]
internal static class VfxInstanceRegistry
{
    private static readonly Dictionary<int, VisEquipment> VisEquipments = new();
    private static readonly Dictionary<int, ItemDrop> ItemDrops = new();
    private static readonly Dictionary<int, ItemStand> ItemStands = new();
    private static readonly Dictionary<int, ArmorStand> ArmorStands = new();
    private static bool _initialized;

    private static void OnInit()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        EngineEvents.MainMenuAwake += OnMainMenuAwake;
        EngineEvents.WorldAwake += ClearAll;
    }

    public static void Register(VisEquipment? visEquipment)
    {
        RegisterInstance(VisEquipments, visEquipment);
    }

    public static void Register(ItemDrop? itemDrop)
    {
        RegisterInstance(ItemDrops, itemDrop);
    }

    public static void Register(ItemStand? itemStand)
    {
        RegisterInstance(ItemStands, itemStand);
    }

    public static void Register(ArmorStand? armorStand)
    {
        RegisterInstance(ArmorStands, armorStand);
    }

    public static void Unregister(VisEquipment? visEquipment)
    {
        UnregisterInstance(VisEquipments, visEquipment);
    }

    public static void Unregister(ItemDrop? itemDrop)
    {
        UnregisterInstance(ItemDrops, itemDrop);
    }

    public static void Unregister(ItemStand? itemStand)
    {
        UnregisterInstance(ItemStands, itemStand);
    }

    public static void Unregister(ArmorStand? armorStand)
    {
        UnregisterInstance(ArmorStands, armorStand);
    }

    public static IEnumerable<VisEquipment> EnumerateVisEquipments()
    {
        return EnumerateLiveInstances(VisEquipments);
    }

    public static IEnumerable<ItemDrop> EnumerateItemDrops()
    {
        return EnumerateLiveInstances(ItemDrops);
    }

    public static IEnumerable<ItemStand> EnumerateItemStands()
    {
        return EnumerateLiveInstances(ItemStands);
    }

    public static IEnumerable<ArmorStand> EnumerateArmorStands()
    {
        return EnumerateLiveInstances(ArmorStands);
    }

    public static void ClearAll()
    {
        VisEquipments.Clear();
        ItemDrops.Clear();
        ItemStands.Clear();
        ArmorStands.Clear();
    }

    private static void RegisterInstance<T>(Dictionary<int, T> instances, T? instance) where T : Component
    {
        if (!instance)
        {
            return;
        }

        instances[instance.GetInstanceID()] = instance;
    }

    private static void UnregisterInstance<T>(Dictionary<int, T> instances, T? instance) where T : Component
    {
        if (!instance)
        {
            return;
        }

        instances.Remove(instance.GetInstanceID());
    }

    private static IEnumerable<T> EnumerateLiveInstances<T>(Dictionary<int, T> instances) where T : Component
    {
        foreach (KeyValuePair<int, T> entry in instances.ToArray())
        {
            T instance = entry.Value;
            if (!instance)
            {
                instances.Remove(entry.Key);
                continue;
            }

            if (!instance.gameObject.activeInHierarchy)
            {
                continue;
            }

            yield return instance;
        }
    }

    private static void OnMainMenuAwake(FejdStartup _)
    {
        ClearAll();
    }

    private static MethodInfo? FindDeclaredLifecycleMethod(Type componentType, string methodName)
    {
        return componentType.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null,
            Type.EmptyTypes,
            null);
    }

    private static MethodBase? FindUnregisterLifecycleMethod(Type componentType)
    {
        return FindDeclaredLifecycleMethod(componentType, "OnDestroy") ??
               FindDeclaredLifecycleMethod(componentType, "OnDisable");
    }

    [HarmonyPatch(typeof(VisEquipment), "Awake")]
    [ClientOnlyPatch]
    private static class VisEquipment_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            Register(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemStand), "Awake")]
    [ClientOnlyPatch]
    private static class ItemStand_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemStand __instance)
        {
            Register(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    [ClientOnlyPatch]
    private static class ItemDrop_Start_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance)
        {
            Register(__instance);
        }
    }

    [HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.Awake))]
    [ClientOnlyPatch]
    private static class ArmorStand_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ArmorStand __instance)
        {
            Register(__instance);
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class VisEquipment_Unregister_Patch
    {
        private static readonly MethodBase? LifecycleMethod = FindUnregisterLifecycleMethod(typeof(VisEquipment));

        [UsedImplicitly]
        private static MethodBase? TargetMethod()
        {
            return LifecycleMethod;
        }

        [UsedImplicitly]
        private static bool Prepare()
        {
            return LifecycleMethod != null;
        }

        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance)
        {
            Unregister(__instance);
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ItemDrop_Unregister_Patch
    {
        private static readonly MethodBase? LifecycleMethod = FindUnregisterLifecycleMethod(typeof(ItemDrop));

        [UsedImplicitly]
        private static MethodBase? TargetMethod()
        {
            return LifecycleMethod;
        }

        [UsedImplicitly]
        private static bool Prepare()
        {
            return LifecycleMethod != null;
        }

        [UsedImplicitly]
        private static void Prefix(ItemDrop __instance)
        {
            Unregister(__instance);
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ItemStand_Unregister_Patch
    {
        private static readonly MethodBase? LifecycleMethod = FindUnregisterLifecycleMethod(typeof(ItemStand));

        [UsedImplicitly]
        private static MethodBase? TargetMethod()
        {
            return LifecycleMethod;
        }

        [UsedImplicitly]
        private static bool Prepare()
        {
            return LifecycleMethod != null;
        }

        [UsedImplicitly]
        private static void Prefix(ItemStand __instance)
        {
            Unregister(__instance);
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ArmorStand_Unregister_Patch
    {
        private static readonly MethodBase? LifecycleMethod = FindUnregisterLifecycleMethod(typeof(ArmorStand));

        [UsedImplicitly]
        private static MethodBase? TargetMethod()
        {
            return LifecycleMethod;
        }

        [UsedImplicitly]
        private static bool Prepare()
        {
            return LifecycleMethod != null;
        }

        [UsedImplicitly]
        private static void Prefix(ArmorStand __instance)
        {
            Unregister(__instance);
        }
    }

}
