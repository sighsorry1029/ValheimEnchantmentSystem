namespace kg.ValheimEnchantmentSystem.Configs;

internal static class EnchantmentChanceRepository
{
    private static readonly Dictionary<string, Dictionary<int, SyncedData.Chance_Data>> OptimizedOverrides = new();
    private static bool IsInitialized;

    public static void Initialize()
    {
        EnsureSubscriptions();
        OptimizeOverridesCache();
    }

    public static void LoadAuthoritativeData()
    {
        EnchantmentYamlConfigSupport.InitializeYamlConfig(
            EnchantmentConfigPaths.ChancesWeaponsYaml,
            Defaults.YAML_Chances_Weapons,
            SyncedData.Synced_EnchantmentChances_Weapons,
            "weapon enchant chances");
        EnchantmentYamlConfigSupport.InitializeYamlConfig(
            EnchantmentConfigPaths.ChancesArmorYaml,
            Defaults.YAML_Chances_Armor,
            SyncedData.Synced_EnchantmentChances_Armor,
            "armor enchant chances");

        SyncedData.Overrides_EnchantmentChances.Value = new List<SyncedData.OverrideChances>();
        _ = TryReloadOverrides();
        OptimizeOverridesCache();
    }

    public static bool TryReloadWeaponChances()
    {
        return EnchantmentYamlConfigSupport.TryReloadYamlConfig(
            EnchantmentConfigPaths.ChancesWeaponsYaml,
            SyncedData.Synced_EnchantmentChances_Weapons,
            "weapon enchant chances");
    }

    public static bool TryReloadArmorChances()
    {
        return EnchantmentYamlConfigSupport.TryReloadYamlConfig(
            EnchantmentConfigPaths.ChancesArmorYaml,
            SyncedData.Synced_EnchantmentChances_Armor,
            "armor enchant chances");
    }

    public static bool TryReloadOverrides()
    {
        if (!EnchantmentYamlConfigSupport.TryReadYamlListDirectory(EnchantmentConfigPaths.OverrideChancesDirectory, out List<SyncedData.OverrideChances> result, out string error))
        {
            Utils.print($"Skipped reload for enchantment chance overrides: {error}", ConsoleColor.Red);
            return false;
        }

        SyncedData.Overrides_EnchantmentChances.Value = result;
        return true;
    }

    public static SyncedData.Chance_Data GetEnchantmentChance(string dropPrefab, int level, bool isWeapon)
    {
        if (TryGetDefinedEnchantmentChance(dropPrefab, level, isWeapon, out SyncedData.Chance_Data chance))
        {
            return chance;
        }

        return new SyncedData.Chance_Data { success = 0 };
    }

    public static bool IsLevelEnchantable(string dropPrefab, int level, bool isWeapon)
    {
        return TryGetDefinedEnchantmentChance(dropPrefab, level, isWeapon, out SyncedData.Chance_Data chance) &&
               chance.success >= 0 &&
               chance.destroy >= 0;
    }

    private static bool TryGetDefinedEnchantmentChance(string dropPrefab, int level, bool isWeapon, out SyncedData.Chance_Data chance)
    {
        chance = null;
        if (dropPrefab != null &&
            OptimizedOverrides.TryGetValue(dropPrefab, out Dictionary<int, SyncedData.Chance_Data> overridden) &&
            overridden.TryGetValue(level, out SyncedData.Chance_Data overrideChance))
        {
            chance = overrideChance;
            return true;
        }

        Dictionary<int, SyncedData.Chance_Data> target = isWeapon
            ? SyncedData.Synced_EnchantmentChances_Weapons.Value
            : SyncedData.Synced_EnchantmentChances_Armor.Value;
        return target.TryGetValue(level, out chance);
    }

    private static void EnsureSubscriptions()
    {
        if (IsInitialized)
        {
            return;
        }

        SyncedData.Overrides_EnchantmentChances.ValueChanged += OptimizeOverridesCache;
        IsInitialized = true;
    }

    private static void OptimizeOverridesCache()
    {
        OptimizedOverrides.Clear();
        foreach (SyncedData.OverrideChances chance in SyncedData.Overrides_EnchantmentChances.Value)
        {
            foreach (string entry in chance.Items)
            {
                OptimizedOverrides[entry] = chance.Chances;
            }
        }
    }
}
