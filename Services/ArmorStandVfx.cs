using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

internal static class ArmorStandVfx
{
    private static readonly MethodInfo ArmorStandUpdateVisualMethod = AccessTools.Method(typeof(ArmorStand), "UpdateVisual");
    private static readonly HashSet<int> ArmorStandBatchRefreshes = new();

    private static void RefreshArmorStandSlotVisual(ArmorStand armorStand, ZDO armorStandZdo, ZDO visEquipmentZdo, int slotIndex, bool writeMetadata)
    {
        if (armorStand.m_slots == null || slotIndex < 0 || slotIndex >= armorStand.m_slots.Count)
        {
            return;
        }

        if (!Enchantment_VFX.TryGetVisSlotColorKey(armorStand.m_slots[slotIndex].m_slot, out string colorKey, out _))
        {
            return;
        }

        string itemName = armorStand.m_slots[slotIndex].m_visualName;
        if (string.IsNullOrEmpty(itemName))
        {
            if (writeMetadata)
            {
                Enchantment_VFX.InsertColor(visEquipmentZdo, colorKey, string.Empty, 0);
            }

            return;
        }

        GameObject itemPrefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(itemName) : null;
        if (!itemPrefab)
        {
            return;
        }

        ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
        if (itemDrop == null)
        {
            return;
        }

        ItemDrop.ItemData itemData = itemDrop.m_itemData.Clone();
        if (armorStandZdo != null)
        {
            ItemDrop.LoadFromZDO(slotIndex, itemData, armorStandZdo);
        }
        else
        {
            itemData.m_variant = armorStand.m_slots[slotIndex].m_visualVariant;
        }

        string color = Enchantment_VFX.GetItemEnchantmentColor(itemData, out int variant);
        if (writeMetadata)
        {
            Enchantment_VFX.InsertColor(visEquipmentZdo, colorKey, color, variant);
        }
    }

    public static void RefreshArmorStandVisuals(ArmorStand armorStand)
    {
        if (armorStand == null || armorStand.m_visEquipment == null)
        {
            return;
        }

        ZDO visEquipmentZdo = armorStand.m_visEquipment.m_nview?.GetZDO();
        if (visEquipmentZdo == null)
        {
            return;
        }

        ZDO armorStandZdo = armorStand.m_nview?.GetZDO();
        bool writeMetadata = armorStand.m_visEquipment.m_nview != null &&
                             armorStand.m_visEquipment.m_nview.IsValid() &&
                             armorStand.m_visEquipment.m_nview.IsOwner();
        if (armorStand.m_slots != null)
        {
            for (int i = 0; i < armorStand.m_slots.Count; ++i)
            {
                RefreshArmorStandSlotVisual(armorStand, armorStandZdo, visEquipmentZdo, i, writeMetadata);
            }
        }

        EquipmentWorldVfx.RefreshVisEquipment(armorStand.m_visEquipment);
    }

    public static void RefreshAllArmorStandVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        foreach (ArmorStand armorStand in VfxInstanceRegistry.EnumerateArmorStands())
        {
            RefreshArmorStandVisuals(armorStand);
        }
    }

    private static void ForceInitialArmorStandRefresh(ArmorStand armorStand)
    {
        if (armorStand == null || armorStand.m_nview == null || !armorStand.m_nview.IsValid())
        {
            return;
        }

        try
        {
            ArmorStandUpdateVisualMethod?.Invoke(armorStand, null);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(ArmorStand), "UpdateVisual")]
    [ClientOnlyPatch]
    private static class ArmorStand_UpdateVisual_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ArmorStand __instance)
        {
            if (__instance == null)
            {
                return;
            }

            ArmorStandBatchRefreshes.Add(__instance.GetInstanceID());
        }

        [UsedImplicitly]
        private static void Postfix(ArmorStand __instance)
        {
            if (__instance == null)
            {
                return;
            }

            ArmorStandBatchRefreshes.Remove(__instance.GetInstanceID());
            RefreshArmorStandVisuals(__instance);
        }
    }

    [HarmonyPatch(typeof(ArmorStand), "SetVisualItem", new[] { typeof(int), typeof(string), typeof(int) })]
    [ClientOnlyPatch]
    private static class ArmorStand_SetVisualItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ArmorStand __instance, int index)
        {
            if (__instance != null && ArmorStandBatchRefreshes.Contains(__instance.GetInstanceID()))
            {
                return;
            }

            RefreshArmorStandVisuals(__instance);
        }
    }

    [HarmonyPatch(typeof(ArmorStand), nameof(ArmorStand.Awake))]
    [ClientOnlyPatch]
    private static class ArmorStand_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ArmorStand __instance)
        {
            if (ValheimEnchantmentSystem._thistype == null || __instance == null)
            {
                return;
            }

            ValheimEnchantmentSystem._thistype.DelayedInvoke(() => ForceInitialArmorStandRefresh(__instance), 1);
            ValheimEnchantmentSystem._thistype.DelayedInvoke(() => ForceInitialArmorStandRefresh(__instance), 30);
        }
    }
}
