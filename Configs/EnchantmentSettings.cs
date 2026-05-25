namespace kg.ValheimEnchantmentSystem.Configs;

public static class EnchantmentSettings
{
    private static bool IsBound;
    private static readonly Dictionary<char, ConfigEntry<int>> EnchantSkillExpByTier = new();

    public static ConfigEntry<int> SafetyLevel { get; private set; } = null!;
    public static ConfigEntry<bool> DropEnchantmentOnUpgrade { get; private set; } = null!;
    public static ConfigEntry<SyncedData.ItemDesctructionTypeEnum> ItemFailureType { get; private set; } = null!;
    public static ConfigEntry<int> FailedEnchantLevelDecrease { get; private set; } = null!;
    public static ConfigEntry<bool> BlessedScrollsPreventBreak { get; private set; } = null!;
    public static ConfigEntry<int> BlessedScrollsAdditionalChance { get; private set; } = null!;
    public static ConfigEntry<bool> AllowJewelcraftingMirrorCopyEnchant { get; private set; } = null!;
    public static ConfigEntry<float> AdditionalEnchantmentChancePerLevel { get; private set; } = null!;
    public static ConfigEntry<float> FailedEnchantSkillExpMultiplier { get; private set; } = null!;
    public static ConfigEntry<int> EnchantmentNotificationMinLevel { get; private set; } = null!;
    public static ConfigEntry<bool> EnchantmentEnableNotifications { get; private set; } = null!;

    public static void Bind()
    {
        if (IsBound)
        {
            return;
        }

        SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
            "The level below which failed enchantments stay at the current level. Set to 0 to disable.");
        DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false,
            "Drop enchantment on item upgrade.");
        ItemFailureType = ValheimEnchantmentSystem.config("Enchantment", "ItemFailureType", SyncedData.ItemDesctructionTypeEnum.LevelDecrease,
            "LevelDecrease will remove FailedEnchantLevelDecrease levels on fail, Destroy will destroy item on fail, Combined will use yaml destroy chance and success chance, CombinedEasy will keep or decrease level and never destroy");
        FailedEnchantLevelDecrease = ValheimEnchantmentSystem.config("Enchantment", "FailedEnchantLevelDecrease", 1,
            new ConfigDescription(
                "Number of enchantment levels removed when a failed enchantment causes a level decrease. Set to 20 to effectively clear normal enchantments on failure without destroying the item.",
                new AcceptableValueRange<int>(1, 100)));
        BlessedScrollsPreventBreak = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsPreventBreak", true,
            "Blessed enchant scrolls prevent negative outcomes on failed enchant attempts. If set to false enchanting chance is increased instead.");
        BlessedScrollsAdditionalChance = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsAdditionalChance", 25,
            "Enchanting chance added when using blessed enchant scrolls if the option to prevent breaking of an item in case of failed enchant is set to false.");
        AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment", "AllowJewelcraftingMirrorCopyEnchant", false,
            "Allow jewelcrafting to copy enchantment from one item to another using mirror.");
        AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.07f,
            "Additional enchantment chance per level of Enchantment skill.");
        FailedEnchantSkillExpMultiplier = ValheimEnchantmentSystem.config("Enchantment", "FailedEnchantSkillExpMultiplier", 0.5f,
            new ConfigDescription(
                "Multiplier applied to enchant skill EXP when an enchant attempt fails. Failure EXP = Success EXP × multiplier.",
                new AcceptableValueRange<float>(0f, 2f)));
        foreach (char tier in EnchantmentTierCatalog.AllTiers)
        {
            EnchantSkillExpByTier[tier] = ValheimEnchantmentSystem.config(
                "Enchantment",
                $"Enchant Skill EXP {tier}",
                EnchantmentTierCatalog.GetDefaultEnchantSkillExp(tier),
                new ConfigDescription(
                    $"Skill EXP granted when enchanting with a tier {tier} enchant scroll.",
                    new AcceptableValueRange<int>(0, 100)));
        }
        EnchantmentEnableNotifications = ValheimEnchantmentSystem.config("Notifications", "EnchantmentEnableNotifications", true,
            "Enable enchantment notifications.");
        EnchantmentNotificationMinLevel = ValheimEnchantmentSystem.config("Notifications", "EnchantmentNotificationMinLevel", 6,
            "The minimum level of enchantment to show notification.");

        IsBound = true;
    }

    public static bool TryGetConfiguredEnchantSkillExp(char tier, out int exp)
    {
        exp = 0;
        if (!EnchantSkillExpByTier.TryGetValue(char.ToUpperInvariant(tier), out ConfigEntry<int> config))
        {
            return false;
        }

        exp = config.Value;
        return true;
    }
}
