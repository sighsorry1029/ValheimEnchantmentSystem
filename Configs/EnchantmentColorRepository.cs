namespace kg.ValheimEnchantmentSystem.Configs;

internal static class EnchantmentColorRepository
{
    private static readonly Dictionary<string, Dictionary<int, SyncedData.VFX_Data>> OptimizedOverrides = new();
    private static bool IsInitialized;

    public static void Initialize()
    {
        EnsureSubscriptions();
        OptimizeOverridesCache();
    }

    public static void LoadAuthoritativeData()
    {
        EnchantmentYamlConfigSupport.InitializeYamlConfig(
            EnchantmentConfigPaths.ColorsYaml,
            Defaults.YAML_Colors,
            SyncedData.Synced_EnchantmentColors,
            "enchantment colors");

        SyncedData.Overrides_EnchantmentColors.Value = new List<SyncedData.OverrideColors>();
        _ = TryReloadOverrides();
        OptimizeOverridesCache();
    }

    public static bool TryReloadColors()
    {
        return EnchantmentYamlConfigSupport.TryReloadYamlConfig(
            EnchantmentConfigPaths.ColorsYaml,
            SyncedData.Synced_EnchantmentColors,
            "enchantment colors");
    }

    public static bool TryReloadOverrides()
    {
        if (!EnchantmentYamlConfigSupport.TryReadYamlListDirectory(EnchantmentConfigPaths.OverrideColorsDirectory, out List<SyncedData.OverrideColors> result, out string error))
        {
            Utils.print($"Skipped reload for enchantment color overrides: {error}", ConsoleColor.Red);
            return false;
        }

        SyncedData.Overrides_EnchantmentColors.Value = result;
        return true;
    }

    public static string GetColor(string dropPrefab, int level, out int variant, bool trimAlpha, string defaultValue = "#00000000")
    {
        variant = 0;
        if (level == 0)
        {
            return SanitizeColor(defaultValue, defaultValue, trimAlpha);
        }

        if (dropPrefab != null &&
            OptimizedOverrides.TryGetValue(dropPrefab, out Dictionary<int, SyncedData.VFX_Data> overridden) &&
            overridden.TryGetValue(level, out SyncedData.VFX_Data overrideVfxData))
        {
            string result = SanitizeColor(overrideVfxData.color, defaultValue, trimAlpha);
            variant = NormalizeVariantIndex(overrideVfxData.variant);
            return result;
        }

        if (SyncedData.Synced_EnchantmentColors.Value.TryGetValue(level, out SyncedData.VFX_Data vfxData))
        {
            string result = SanitizeColor(vfxData.color, defaultValue, trimAlpha);
            variant = NormalizeVariantIndex(vfxData.variant);
            return result;
        }

        return SanitizeColor(defaultValue, defaultValue, trimAlpha);
    }

    private static void EnsureSubscriptions()
    {
        if (IsInitialized)
        {
            return;
        }

        SyncedData.Overrides_EnchantmentColors.ValueChanged += OptimizeOverridesCache;
        IsInitialized = true;
    }

    private static void OptimizeOverridesCache()
    {
        OptimizedOverrides.Clear();
        foreach (SyncedData.OverrideColors colors in SyncedData.Overrides_EnchantmentColors.Value)
        {
            foreach (string entry in colors.Items)
            {
                OptimizedOverrides[entry] = colors.Colors;
            }
        }
    }

    private static string SanitizeColor(string color, string defaultValue, bool trimAlpha)
    {
        string fallback = string.IsNullOrWhiteSpace(defaultValue) ? "#00000000" : defaultValue;
        string source = string.IsNullOrWhiteSpace(color) ? fallback : color.Trim();
        if (!source.StartsWith("#", StringComparison.Ordinal))
        {
            source = "#" + source;
        }

        int requiredLength = trimAlpha ? 7 : 9;
        if (source.Length < requiredLength)
        {
            source = fallback;
            if (!source.StartsWith("#", StringComparison.Ordinal))
            {
                source = "#" + source;
            }

            if (source.Length < requiredLength)
            {
                source = trimAlpha ? "#000000" : "#00000000";
            }
        }

        return trimAlpha ? source.Substring(0, 7) : source;
    }

    private static int NormalizeVariantIndex(int variant)
    {
        if (Enchantment_VFX.VFXs.Count == 0)
        {
            return 0;
        }

        int normalized = variant <= 0 ? 0 : variant - 1;
        return Mathf.Clamp(normalized, 0, Enchantment_VFX.VFXs.Count - 1);
    }
}
