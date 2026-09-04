using System.Reflection;

namespace kg.ValheimEnchantmentSystem.Configs;

[Flags]
internal enum ConfigRefreshScope
{
    None = 0,
    Ui = 1,
    LocalPlayerState = 2,
    EquipmentVfx = 4
}

internal static class ConfigRefreshCoordinator
{
    private static readonly MethodInfo HumanoidSetupEquipmentMethod = AccessTools.Method(typeof(Humanoid), "SetupEquipment");
    private static readonly MethodInfo PlayerUpdateStatsMethod = AccessTools.Method(typeof(Player), "UpdateStats", new[] { typeof(float) });

    private static ConfigRefreshScope PendingRefreshScope;
    private static bool RefreshScheduled;
    private static bool IsInitialized;

    public static void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        RegisterSettingRefreshHandlers();
        RegisterSyncedValueRefreshHandlers();
        IsInitialized = true;
    }

    private static void RegisterSettingRefreshHandlers()
    {
        EnchantmentSettings.SafetyLevel.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
        EnchantmentSettings.ItemFailureType.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
        EnchantmentSettings.FailedEnchantLevelDecrease.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
        EnchantmentSettings.BlessedScrollsPreventBreak.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
        EnchantmentSettings.BlessedScrollsAdditionalChance.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
        EnchantmentSettings.AdditionalEnchantmentChancePerLevel.SettingChanged += (_, _) => RequestRefresh(ConfigRefreshScope.Ui);
    }

    private static void RegisterSyncedValueRefreshHandlers()
    {
        SyncedData.Synced_EnchantmentChances_Weapons.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui);
        SyncedData.Synced_EnchantmentChances_Armor.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui);
        SyncedData.Synced_EnchantmentStats_Weapons.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui | ConfigRefreshScope.LocalPlayerState);
        SyncedData.Synced_EnchantmentStats_Armor.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui | ConfigRefreshScope.LocalPlayerState);
        SyncedData.Synced_EnchantmentColors.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui | ConfigRefreshScope.EquipmentVfx);
        SyncedData.Synced_EnchantmentReqs.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui);
        SyncedData.Overrides_EnchantmentChances.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui);
        SyncedData.Overrides_EnchantmentStats.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui | ConfigRefreshScope.LocalPlayerState);
        SyncedData.Overrides_EnchantmentColors.ValueChanged += () => RequestRefresh(ConfigRefreshScope.Ui | ConfigRefreshScope.EquipmentVfx);
    }

    private static void RequestRefresh(ConfigRefreshScope scope)
    {
        PendingRefreshScope |= scope;
        if (RefreshScheduled)
        {
            return;
        }

        if (ValheimEnchantmentSystem._thistype == null)
        {
            FlushRefresh();
            return;
        }

        RefreshScheduled = true;
        ValheimEnchantmentSystem._thistype.DelayedInvoke(FlushRefresh, 1);
    }

    private static void FlushRefresh()
    {
        ConfigRefreshScope scope = PendingRefreshScope;
        PendingRefreshScope = ConfigRefreshScope.None;
        RefreshScheduled = false;

        if (scope == ConfigRefreshScope.None)
        {
            return;
        }

        if ((scope & ConfigRefreshScope.LocalPlayerState) != 0)
        {
            RefreshLocalPlayerState();
        }

        if ((scope & ConfigRefreshScope.Ui) != 0)
        {
            Enchantment_VFX.UpdateGrid();
        }

        if ((scope & ConfigRefreshScope.EquipmentVfx) != 0)
        {
            Enchantment_VFX.RefreshSceneVisuals();
        }

        if (PendingRefreshScope != ConfigRefreshScope.None)
        {
            RequestRefresh(ConfigRefreshScope.None);
        }
    }

    private static void RefreshLocalPlayerState()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        if (!Player.m_localPlayer)
        {
            return;
        }

        try
        {
            HumanoidSetupEquipmentMethod?.Invoke(Player.m_localPlayer, null);
        }
        catch (Exception ex)
        {
            Utils.print($"Failed to refresh local equipment state: {ex.Message}", ConsoleColor.Yellow);
        }

        try
        {
            PlayerUpdateStatsMethod?.Invoke(Player.m_localPlayer, new object[] { 0f });
        }
        catch (Exception ex)
        {
            Utils.print($"Failed to refresh local player stats: {ex.Message}", ConsoleColor.Yellow);
        }

        foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.GetAllItems())
        {
            float maxDurability = item.GetMaxDurability();
            if (maxDurability <= 0f)
            {
                continue;
            }

            if (item.m_durability > maxDurability)
            {
                item.m_durability = maxDurability;
            }
        }

        Player.m_localPlayer.m_inventory.Changed();
    }
}
