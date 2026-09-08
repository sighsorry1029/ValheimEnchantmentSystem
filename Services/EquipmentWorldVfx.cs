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

        ZDO zdo = visEquipment.m_nview?.m_zdo;
        RefreshEquipmentInstance(visEquipment.m_leftItemInstance, zdo, "VES_leftitemColor", false);
        RefreshEquipmentInstance(visEquipment.m_rightItemInstance, zdo, "VES_rightitemColor", false);
        RefreshEquipmentInstance(visEquipment.m_leftBackItemInstance, zdo, "VES_leftbackitemColor", false);
        RefreshEquipmentInstance(visEquipment.m_rightBackItemInstance, zdo, "VES_rightbackitemColor", false);
        RefreshEquipmentInstances(visEquipment.m_chestItemInstances, zdo, "VES_chestitemColor", true);
        RefreshEquipmentInstances(visEquipment.m_legItemInstances, zdo, "VES_legsitemColor", true);
        RefreshEquipmentInstances(visEquipment.m_shoulderItemInstances, zdo, "VES_shoulderitemColor", true);
        RefreshEquipmentInstance(visEquipment.m_helmetItemInstance, zdo, "VES_helmetitemColor", true);
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

        if (itemStand.m_nview == null || !itemStand.m_nview.IsValid() || !itemStand.m_visualItem)
        {
            if (itemStand.m_visualItem)
            {
                Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem);
            }

            return;
        }

        ZDO zdo = itemStand.m_nview.GetZDO();
        if (zdo == null)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem);
            return;
        }

        string itemPrefab = zdo.GetString(ZDOVars.s_item);
        if (string.IsNullOrEmpty(itemPrefab))
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem);
            return;
        }

        GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(itemPrefab) : null;
        if (!prefab && ZNetScene.instance)
        {
            prefab = ZNetScene.instance.GetPrefab(itemPrefab);
        }

        if (!prefab)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem);
            return;
        }

        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
        if (itemDrop == null)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem);
            return;
        }

        ItemDrop.ItemData itemData = itemDrop.m_itemData.Clone();
        ItemDrop.LoadFromZDO(itemData, zdo);
        if (itemData.Data()?.Get<Enchantment_Core.Enchanted>() is not { level: > 0 } en)
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem, Enchantment_VFX.IsArmorVisual(itemData));
            return;
        }

        bool isArmor = Enchantment_VFX.IsArmorVisual(itemData);
        if (!Enchantment_VFX.IsEffectEnabledForClass(isArmor))
        {
            Enchantment_VFX.DisableMeshEffect(itemStand.m_visualItem, isArmor);
            return;
        }

        string color = SyncedData.GetColor(itemPrefab, en.level, out int variant, false);
        Enchantment_VFX.AttachMeshEffect(itemStand.m_visualItem, color.ToColorAlpha(), variant, isArmor);
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

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
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

            if (!localPlayer!.m_nview || !localPlayer.m_nview.IsValid() || !localPlayer.m_nview.IsOwner())
            {
                return;
            }

            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_leftitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_leftItem, out int leftVariant), leftVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_rightitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_rightItem, out int rightVariant), rightVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_leftbackitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_hiddenLeftItem, out int leftBackVariant), leftBackVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_rightbackitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_hiddenRightItem, out int rightBackVariant), rightBackVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_chestitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_chestItem, out int chestVariant), chestVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_legsitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_legItem, out int legsVariant), legsVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_helmetitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_helmetItem, out int helmetVariant), helmetVariant);
            Enchantment_VFX.InsertColor(localPlayer.m_nview.m_zdo, "VES_shoulderitemColor", Enchantment_VFX.GetItemEnchantmentColor(localPlayer.m_shoulderItem, out int shoulderVariant), shoulderVariant);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetLeftHandEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentLeftItemHash != hash;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetRightHandEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentRightItemHash != hash;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetBackEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetBackEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int leftItem, int rightItem, int leftVariant, out bool __state)
        {
            __state = __instance.m_currentLeftBackItemHash != leftItem || __instance.m_currentRightBackItemHash != rightItem;
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetChestEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentChestItemHash != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetLegEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentLegItemHash != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetShoulderEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentShoulderItemHash != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
    [ClientOnlyPatch]
    private static class VisEquipment_SetHelmetEquipped_Patch
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int hash, out bool __state)
        {
            __state = __instance.m_currentHelmetItemHash != hash && Enchantment_VFX.IsArmorVfxEnabled();
        }

        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance, bool __state)
        {
            if (!__state || !__instance.m_nview || __instance.m_nview.m_zdo == null)
            {
                return;
            }

            ScheduleVisEquipmentRefresh(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    [ClientOnlyPatch]
    private static class ItemDrop_Start_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance)
        {
            RefreshItemDropVisual(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.SetVisualItem))]
    [ClientOnlyPatch]
    private static class ItemStand_SetVisualItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemStand __instance, out bool __state, string itemName, int variant)
        {
            __state = __instance.m_visualName != itemName || __instance.m_visualVariant != variant;
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
