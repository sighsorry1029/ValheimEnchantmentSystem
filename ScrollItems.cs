using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

[VES_Autoload(VES_Autoload.Priority.Last, "OnInit", typeof(VES_UI), typeof(Enchantment_Skill))]
public static class ScrollItems
{
    [UsedImplicitly]
    private static void OnInit()
    {
        ScrollStationBootstrap.Initialize();
        ScrollCombineService.Initialize();
        ScrollContentBootstrap.Initialize();
        EngineEvents.InventoryChanged += inventory => ScrollCombineService.RequestRefresh(inventory);
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
    [ClientOnlyPatch]
    public static class InventoryGrid_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            ScrollCombineService.AttachCombineOutline(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateInventory))]
    [ClientOnlyPatch]
    private static class InventoryGrid_UpdateInventory_Patch
    {
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance, Inventory inventory)
        {
            ScrollCombineService.BindInventoryGrid(__instance, inventory);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [ClientOnlyPatch]
    private static class InventoryGrid_UpdateGui_Patch
    {
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            ScrollCombineService.TryFlushDirtyRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.OnRightClick))]
    [ClientOnlyPatch]
    private static class InventoryGrid_OnRightClick_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance, UIInputHandler element)
        {
            ScrollCombineService.TryCombineAt(__instance, element);
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    [ClientOnlyPatch]
    public static class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, ref string __result)
        {
            ScrollCombineService.AppendCombineTooltip(item, crafting, ref __result);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem), typeof(ItemDrop.ItemData), typeof(int), typeof(Vector3), typeof(Quaternion))]
    private static class ItemDrop_DropItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData item)
        {
            ScrollRegistry.FixDropPrefab(item);
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __result)
        {
            if (__result != null && !__result.gameObject.activeSelf)
            {
                __result.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Save))]
    private static class Inventory_Save_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Inventory __instance)
        {
            ScrollRegistry.FixInventoryPrefabs(__instance);
        }
    }
}
