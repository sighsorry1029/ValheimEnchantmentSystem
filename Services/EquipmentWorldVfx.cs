using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

internal static class EquipmentWorldVfx
{
    private static readonly HashSet<int> ScheduledVisEquipmentRefreshes = new();

    private static void RefreshEquipmentInstance(GameObject itemInstance, ZDO zdo, string colorKey, bool isArmor)
    {
        if (!itemInstance)
        {
            return;
        }

        string color = zdo?.GetString(colorKey) ?? string.Empty;
        int variant = zdo?.GetInt(colorKey + "_variant") ?? 0;
        if (!Enchantment_VFX.IsEffectEnabledForClass(isArmor) || string.IsNullOrEmpty(color))
        {
            Enchantment_VFX.DisableMeshEffect(itemInstance, isArmor);
            return;
        }

        Enchantment_VFX.AttachMeshEffect(itemInstance, color.ToColorAlpha(), variant, isArmor);
    }

    private static void RefreshEquipmentInstances(IEnumerable<GameObject> itemInstances, ZDO zdo, string colorKey, bool isArmor)
    {
        if (itemInstances == null)
        {
            return;
        }

        foreach (GameObject itemInstance in itemInstances)
        {
            RefreshEquipmentInstance(itemInstance, zdo, colorKey, isArmor);
        }
    }

    private static void ScheduleVisEquipmentRefresh(VisEquipment visEquipment)
    {
        if (!visEquipment)
        {
            return;
        }

        if (ValheimEnchantmentSystem._thistype == null)
        {
            RefreshVisEquipment(visEquipment);
            return;
        }

        int instanceId = visEquipment.GetInstanceID();
        if (!ScheduledVisEquipmentRefreshes.Add(instanceId))
        {
            return;
        }

        ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
        {
            ScheduledVisEquipmentRefreshes.Remove(instanceId);
            if (!visEquipment)
            {
                return;
            }

            RefreshVisEquipment(visEquipment);
        }, 1);
    }

    internal static void RefreshVisEquipment(VisEquipment visEquipment)
    {
        if (!visEquipment)
        {
            return;
        }

        ZDO zdo = visEquipment.VES_m_nview()?.GetZDO();
        RefreshEquipmentInstance(visEquipment.VES_m_leftItemInstance(), zdo, "VES_leftitemColor", false);
        RefreshEquipmentInstance(visEquipment.VES_m_rightItemInstance(), zdo, "VES_rightitemColor", false);
        RefreshEquipmentInstance(visEquipment.VES_m_leftBackItemInstance(), zdo, "VES_leftbackitemColor", false);
        RefreshEquipmentInstance(visEquipment.VES_m_rightBackItemInstance(), zdo, "VES_rightbackitemColor", false);
        RefreshEquipmentInstances(visEquipment.VES_m_chestItemInstances(), zdo, "VES_chestitemColor", true);
        RefreshEquipmentInstances(visEquipment.VES_m_legItemInstances(), zdo, "VES_legsitemColor", true);
        RefreshEquipmentInstances(visEquipment.VES_m_shoulderItemInstances(), zdo, "VES_shoulderitemColor", true);
        RefreshEquipmentInstance(visEquipment.VES_m_helmetItemInstance(), zdo, "VES_helmetitemColor", true);
    }

    public static void RefreshItemDropVisual(ItemDrop itemDrop)
    {
        if (itemDrop == null)
        {
            return;
        }

        if (itemDrop.m_itemData?.Data()?.Get<Enchantment_Core.Enchanted>() is not { level: > 0 } en)
        {
            Enchantment_VFX.DisableMeshEffect(itemDrop.gameObject, Enchantment_VFX.IsArmorVisual(itemDrop.m_itemData));
            return;
        }

        bool isArmor = Enchantment_VFX.IsArmorVisual(itemDrop.m_itemData);
        if (!Enchantment_VFX.IsEffectEnabledForClass(isArmor))
        {
            Enchantment_VFX.DisableMeshEffect(itemDrop.gameObject, isArmor);
            return;
        }

        string prefabName = global::Utils.GetPrefabName(itemDrop.gameObject);
        string color = SyncedData.GetColor(prefabName, en.level, out int variant, false);
        Enchantment_VFX.AttachMeshEffect(itemDrop.gameObject, color.ToColorAlpha(), variant, isArmor);
    }

    public static void RefreshItemStandVisual(ItemStand itemStand)
    {
        if (itemStand == null)
        {
            return;
        }

        if (itemStand.VES_m_nview() == null || !itemStand.VES_m_nview().IsValid() || !itemStand.VES_m_visualItem())
        {
            if (itemStand.VES_m_visualItem())
            {
                Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem());
            }

            return;
        }

        ZDO zdo = itemStand.VES_m_nview().GetZDO();
        if (zdo == null)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem());
            return;
        }

        int itemPrefab = zdo.GetInt(ZDOVars.s_item);
        if (itemPrefab == 0)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem());
            return;
        }

        GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(itemPrefab) : null;
        if (!prefab && ZNetScene.instance)
        {
            prefab = ZNetScene.instance.GetPrefab(itemPrefab);
        }

        if (!prefab)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem());
            return;
        }

        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
        if (itemDrop == null)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem());
            return;
        }

        ItemDrop.ItemData itemData = itemDrop.m_itemData.Clone();
        ItemDrop.LoadFromZDO(itemData, zdo);
        if (itemData.Data()?.Get<Enchantment_Core.Enchanted>() is not { level: > 0 } en)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem(), Enchantment_VFX.IsArmorVisual(itemData));
            return;
        }

        bool isArmor = Enchantment_VFX.IsArmorVisual(itemData);
        if (!Enchantment_VFX.IsEffectEnabledForClass(isArmor))
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.VES_m_visualItem(), isArmor);
            return;
        }

        string color = SyncedData.GetColor(prefab.name, en.level, out int variant, false);
        Enchantment_VFX.AttachMeshEffect(itemStand.VES_m_visualItem(), color.ToColorAlpha(), variant, isArmor);
    }

    public static void RefreshEquipmentVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        foreach (VisEquipment visEquipment in VfxInstanceRegistry.EnumerateVisEquipments())
        {
            if (visEquipment.GetComponentInParent<ArmorStand>() != null)
            {
                continue;
            }

            RefreshVisEquipment(visEquipment);
        }
    }

    public static void RefreshWorldItemVisuals()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        foreach (ItemDrop itemDrop in VfxInstanceRegistry.EnumerateItemDrops())
        {
            RefreshItemDropVisual(itemDrop);
        }

        foreach (ItemStand itemStand in VfxInstanceRegistry.EnumerateItemStands())
        {
            RefreshItemStandVisual(itemStand);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "SetupVisEquipment")]
    [ClientOnlyPatch]
    [HarmonyPriority(-10000)]
    private static class Humanoid_SetupVisEquipment_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(-6000)]
        private static void Postfix(Humanoid __instance, VisEquipment visEq, bool isRagdoll)
        {
            Player localPlayer = ZNetScene.instance ? Player.m_localPlayer : __instance as Player;
            if (__instance != localPlayer || isRagdoll || !ZNetScene.instance)
            {
                return;
            }

            if (!localPlayer!.VES_m_nview() || !localPlayer.VES_m_nview().IsValid() || !localPlayer.VES_m_nview().IsOwner())
            {
                return;
            }

            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_leftitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_leftItem(), out int leftVariant), leftVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_rightitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_rightItem(), out int rightVariant), rightVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_leftbackitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_hiddenLeftItem(), out int leftBackVariant), leftBackVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_rightbackitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_hiddenRightItem(), out int rightBackVariant), rightBackVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_chestitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_chestItem(), out int chestVariant), chestVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_legsitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_legItem(), out int legsVariant), legsVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_helmetitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_helmetItem(), out int helmetVariant), helmetVariant);
            Enchantment_VFX.InsertColor(localPlayer.VES_m_nview().GetZDO(), "VES_shoulderitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.VES_m_shoulderItem(), out int shoulderVariant), shoulderVariant);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetLeftHandEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetLeftHandEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentLeftItemHash() != hash;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetRightHandEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetRightHandEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentRightItemHash() != hash;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetBackEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetBackEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int leftItem, int rightItem, int leftVariant, out bool __state)
        {
            __state = __instance.VES_m_currentLeftBackItemHash() != leftItem || __instance.VES_m_currentRightBackItemHash() != rightItem;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetChestEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetChestEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentChestItemHash() != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetLegEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetLegEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentLegItemHash() != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetShoulderEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetShoulderEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentShoulderItemHash() != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetHelmetEquipped")]
    [ClientOnlyPatch]
    private static class VisEquipment_SetHelmetEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.VES_m_currentHelmetItemHash() != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.VES_m_nview() || __instance.VES_m_nview().GetZDO() == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "Start")]
    [ClientOnlyPatch]
    private static class ItemDrop_Start_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance)
        {
            RefreshItemDropVisual(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemStand), "SetVisualItem")]
    [ClientOnlyPatch]
    private static class ItemStand_SetVisualItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemStand __instance, out bool __state, int itemHash, int variant, int orientation, int ___m_visualHash, int ___m_orientation)
        {
            __state = ___m_visualHash != itemHash || __instance.VES_m_visualVariant() != variant || ___m_orientation != orientation;
        }

        [UsedImplicitly]
        private static void Postfix(ItemStand __instance, bool __state)
        {
            if (!__state)
            {
                return;
            }

            RefreshItemStandVisual(__instance);
        }
    }
}
