using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

internal static class EngineEvents
{
    public static event Action<Inventory>? InventoryChanged;
    public static event Action<Player>? EquipmentChanged;
    public static event Action? InventoryGuiShown;
    public static event Action<FejdStartup>? MainMenuAwake;
    public static event Action? WorldAwake;

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    [ClientOnlyPatch]
    private static class Inventory_Changed_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Inventory __instance)
        {
            InventoryChanged?.Invoke(__instance);
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class Humanoid_EquipState_Patch
    {
        [UsedImplicitly]
        private static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
            yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
        }

        [UsedImplicitly]
        private static void Postfix(Humanoid __instance)
        {
            if (__instance is Player player)
            {
                EquipmentChanged?.Invoke(player);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [ClientOnlyPatch]
    private static class InventoryGui_Show_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            InventoryGuiShown?.Invoke();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    [ClientOnlyPatch]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            MainMenuAwake?.Invoke(__instance);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [ClientOnlyPatch]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            WorldAwake?.Invoke();
        }
    }
}
