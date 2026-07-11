namespace kg.ValheimEnchantmentSystem.Configs;

public static class EnchantmentConfigPaths
{
    private static string RootDirectory = string.Empty;

    public static string ChancesWeaponsYaml { get; private set; } = string.Empty;
    public static string ChancesArmorYaml { get; private set; } = string.Empty;
    public static string StatsWeaponsYaml { get; private set; } = string.Empty;
    public static string StatsArmorYaml { get; private set; } = string.Empty;
    public static string ColorsYaml { get; private set; } = string.Empty;
    public static string RequirementsYaml { get; private set; } = string.Empty;
    public static string ResourceMapYaml { get; private set; } = string.Empty;
    public static string OverrideChancesDirectory { get; private set; } = string.Empty;
    public static string OverrideStatsDirectory { get; private set; } = string.Empty;
    public static string OverrideColorsDirectory { get; private set; } = string.Empty;
    public static string AdditionalRequirementsDirectory { get; private set; } = string.Empty;

    public static void Initialize()
    {
        string configFolder = Path.GetFullPath(ValheimEnchantmentSystem.ConfigFolder);
        if (string.Equals(RootDirectory, configFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RootDirectory = configFolder;
        StatsWeaponsYaml = Path.Combine(configFolder, "EnchantmentStats_Weapons.yml");
        StatsArmorYaml = Path.Combine(configFolder, "EnchantmentStats_Armor.yml");
        ColorsYaml = Path.Combine(configFolder, "EnchantmentColors.yml");
        RequirementsYaml = Path.Combine(configFolder, "EnchantmentReqs.yml");
        ResourceMapYaml = Path.Combine(configFolder, "resourcemap.yml");
        ChancesWeaponsYaml = Path.Combine(configFolder, "EnchantmentChances_Weapons.yml");
        ChancesArmorYaml = Path.Combine(configFolder, "EnchantmentChances_Armor.yml");
        AdditionalRequirementsDirectory = Path.Combine(configFolder, "AdditionalEnchantmentReqs");
        OverrideChancesDirectory = Path.Combine(configFolder, "AdditionalOverrides_EnchantmentChances");
        OverrideStatsDirectory = Path.Combine(configFolder, "AdditionalOverrides_EnchantmentStats");
        OverrideColorsDirectory = Path.Combine(configFolder, "AdditionalOverrides_EnchantmentColors");

        EnsureDirectory(AdditionalRequirementsDirectory);
        EnsureDirectory(OverrideChancesDirectory);
        EnsureDirectory(OverrideStatsDirectory);
        EnsureDirectory(OverrideColorsDirectory);

        EnsureFile(ChancesWeaponsYaml, Defaults.YAML_Chances_Weapons);
        EnsureFile(ChancesArmorYaml, Defaults.YAML_Chances_Armor);
        EnsureFile(StatsWeaponsYaml, Defaults.YAML_Stats_Weapons);
        EnsureFile(StatsArmorYaml, Defaults.YAML_Stats_Armor);
        EnsureFile(ColorsYaml, Defaults.YAML_Colors);
        EnsureFile(RequirementsYaml, Defaults.YAML_Reqs);
        EnsureFile(ResourceMapYaml, ResourceMapRequirementResolver.DefaultYaml);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void EnsureFile(string path, string contents)
    {
        if (!File.Exists(path))
        {
            path.WriteFile(contents);
        }
    }
}
