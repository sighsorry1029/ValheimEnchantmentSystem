namespace kg.ValheimEnchantmentSystem.Configs;

internal static class EnchantmentStatRepository
{
    private static readonly Dictionary<string, Dictionary<int, SyncedData.Stat_Data>> OptimizedOverrides = new();
    private static bool IsInitialized;

    public static void Initialize()
    {
        EnsureSubscriptions();
        OptimizeOverridesCache();
    }

    public static void LoadAuthoritativeData()
    {
        EnchantmentYamlConfigSupport.InitializeYamlConfig(
            EnchantmentConfigPaths.StatsWeaponsYaml,
            Defaults.YAML_Stats_Weapons,
            SyncedData.Synced_EnchantmentStats_Weapons,
            "weapon enchant stats");
        EnchantmentYamlConfigSupport.InitializeYamlConfig(
            EnchantmentConfigPaths.StatsArmorYaml,
            Defaults.YAML_Stats_Armor,
            SyncedData.Synced_EnchantmentStats_Armor,
            "armor enchant stats");

        SyncedData.Overrides_EnchantmentStats.Value = new List<SyncedData.OverrideStats>();
        _ = TryReloadOverrides();
        OptimizeOverridesCache();
    }

    public static bool TryReloadWeaponStats()
    {
        return EnchantmentYamlConfigSupport.TryReloadYamlConfig(
            EnchantmentConfigPaths.StatsWeaponsYaml,
            SyncedData.Synced_EnchantmentStats_Weapons,
            "weapon enchant stats");
    }

    public static bool TryReloadArmorStats()
    {
        return EnchantmentYamlConfigSupport.TryReloadYamlConfig(
            EnchantmentConfigPaths.StatsArmorYaml,
            SyncedData.Synced_EnchantmentStats_Armor,
            "armor enchant stats");
    }

    public static bool TryReloadOverrides()
    {
        if (!EnchantmentYamlConfigSupport.TryReadYamlListDirectory(EnchantmentConfigPaths.OverrideStatsDirectory, out List<SyncedData.OverrideStats> result, out string error))
        {
            Utils.print($"Skipped reload for enchantment stat overrides: {error}", ConsoleColor.Red);
            return false;
        }

        SyncedData.Overrides_EnchantmentStats.Value = result;
        return true;
    }

    public static SyncedData.Stat_Data GetStatIncrease(Enchantment_Core.Enchanted enchantment)
    {
        if (enchantment.level == 0)
        {
            return null;
        }

        string dropPrefab = enchantment.Item.m_dropPrefab?.name;
        if (dropPrefab != null &&
            OptimizedOverrides.TryGetValue(dropPrefab, out Dictionary<int, SyncedData.Stat_Data> overridden) &&
            overridden.TryGetValue(enchantment.level, out SyncedData.Stat_Data overrideStat))
        {
            return overrideStat;
        }

        Dictionary<int, SyncedData.Stat_Data> target = enchantment.Item.IsWeapon()
            ? SyncedData.Synced_EnchantmentStats_Weapons.Value
            : SyncedData.Synced_EnchantmentStats_Armor.Value;
        return target.TryGetValue(enchantment.level, out SyncedData.Stat_Data increase) ? increase : null;
    }

    public static SyncedData.Stat_Data GetStatIncrease(string dropPrefab, int level, bool isWeapon)
    {
        if (level <= 0 || string.IsNullOrWhiteSpace(dropPrefab))
        {
            return null;
        }

        if (OptimizedOverrides.TryGetValue(dropPrefab, out Dictionary<int, SyncedData.Stat_Data> overridden) &&
            overridden.TryGetValue(level, out SyncedData.Stat_Data overrideStat))
        {
            return overrideStat;
        }

        Dictionary<int, SyncedData.Stat_Data> target = isWeapon
            ? SyncedData.Synced_EnchantmentStats_Weapons.Value
            : SyncedData.Synced_EnchantmentStats_Armor.Value;
        return target.TryGetValue(level, out SyncedData.Stat_Data increase) ? increase : null;
    }

    public static bool HasStatIncrease(string dropPrefab, int level, bool isWeapon)
    {
        if (level <= 0 || string.IsNullOrWhiteSpace(dropPrefab))
        {
            return false;
        }

        if (OptimizedOverrides.TryGetValue(dropPrefab, out Dictionary<int, SyncedData.Stat_Data> overridden) &&
            overridden.ContainsKey(level))
        {
            return true;
        }

        Dictionary<int, SyncedData.Stat_Data> target = isWeapon
            ? SyncedData.Synced_EnchantmentStats_Weapons.Value
            : SyncedData.Synced_EnchantmentStats_Armor.Value;
        return target.ContainsKey(level);
    }

    private static void EnsureSubscriptions()
    {
        if (IsInitialized)
        {
            return;
        }

        SyncedData.Overrides_EnchantmentStats.ValueChanged += OptimizeOverridesCache;
        IsInitialized = true;
    }

    private static void OptimizeOverridesCache()
    {
        OptimizedOverrides.Clear();
        foreach (SyncedData.OverrideStats stats in SyncedData.Overrides_EnchantmentStats.Value)
        {
            foreach (string entry in stats.Items)
            {
                OptimizedOverrides[entry] = stats.Stats;
            }
        }
    }
}
