namespace kg.ValheimEnchantmentSystem.Configs;

public static class EnchantmentSettings
{
    private static bool IsBound;

    public static ConfigEntry<int> SafetyLevel { get; private set; } = null!;
    public static ConfigEntry<SyncedData.ItemDesctructionTypeEnum> ItemFailureType { get; private set; } = null!;
    public static ConfigEntry<int> FailedEnchantLevelDecrease { get; private set; } = null!;
    public static ConfigEntry<bool> BlessedScrollsPreventBreak { get; private set; } = null!;
    public static ConfigEntry<int> BlessedScrollsAdditionalChance { get; private set; } = null!;
    public static ConfigEntry<float> AdditionalEnchantmentChancePerLevel { get; private set; } = null!;
    public static ConfigEntry<float> FailedEnchantSkillExpMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> EnchantSkillExpBase { get; private set; } = null!;
    public static ConfigEntry<float> EnchantSkillExpPerLevel { get; private set; } = null!;
    public static ConfigEntry<float> EnchantSkillExpDifficultyBonus { get; private set; } = null!;

    public static void Bind()
    {
        if (IsBound)
        {
            return;
        }

        SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
            Display("Prevents level loss and item destruction on failure while the item's CURRENT enchantment level is below this value. Example with 3: failing +2 -> +3 keeps +2, but failing +3 -> +4 follows Item Failure Type. This does not guarantee a minimum retained level. Set to 0 to disable this protection. Blessed scroll protection is unaffected.", ConfigurationManagerDisplay.Enchantment, "Safety Level", 990));
        ItemFailureType = ValheimEnchantmentSystem.config("Enchantment", "ItemFailureType", SyncedData.ItemDesctructionTypeEnum.LevelDecrease,
            Display("LevelDecrease will remove FailedEnchantLevelDecrease levels on fail, Destroy will destroy item on fail, Combined will use yaml destroy chance and success chance, CombinedEasy will keep or decrease level and never destroy", ConfigurationManagerDisplay.Enchantment, "Item Failure Type", 1000));
        FailedEnchantLevelDecrease = ValheimEnchantmentSystem.config("Enchantment", "FailedEnchantLevelDecrease", 1,
            Display(
                "Number of enchantment levels removed when a failed enchantment causes a level decrease. Set to 20 to effectively clear normal enchantments on failure without destroying the item.",
                ConfigurationManagerDisplay.Enchantment,
                "Failed Enchant Level Decrease",
                980,
                new AcceptableValueRange<int>(1, 100)));
        BlessedScrollsPreventBreak = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsPreventBreak", true,
            Display("Blessed enchant scrolls prevent negative outcomes on failed enchant attempts. If set to false enchanting chance is increased instead.", ConfigurationManagerDisplay.Enchantment, "Blessed Scrolls Prevent Break", 900));
        BlessedScrollsAdditionalChance = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsAdditionalChance", 25,
            Display("Enchanting chance added when using blessed enchant scrolls if the option to prevent breaking of an item in case of failed enchant is set to false.", ConfigurationManagerDisplay.Enchantment, "Blessed Scrolls Additional Chance", 890));
        AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.07f,
            Display("Additional enchantment chance per level of Enchantment skill.", ConfigurationManagerDisplay.Skill, "Enchant Chance Per Skill Level", 980));
        FailedEnchantSkillExpMultiplier = ValheimEnchantmentSystem.config("Enchantment", "FailedEnchantSkillExpMultiplier", 0.5f,
            Display(
                "Multiplier applied to enchant skill EXP when an enchant attempt fails. Failure EXP = Success EXP x multiplier.",
                ConfigurationManagerDisplay.Skill,
                "Failed Enchant Skill EXP Multiplier",
                970,
                new AcceptableValueRange<float>(0f, 2f)));
        EnchantSkillExpBase = ValheimEnchantmentSystem.config("Enchantment", "Enchant Skill EXP Base", 2f,
            Display(
                "Base EXP for successful enchantments, independent of scroll tier. Success EXP = Base + Per Level x (target enchant level - 1) + Difficulty Bonus x (1 - base success chance / 100). Uses the item level before the attempt and the YAML chance before skill or blessed bonuses. Attempts with 0% final success chance grant no EXP. Failure EXP uses Failed Enchant Skill EXP Multiplier; Skill Gain Factor applies to both outcomes. Set all three EXP values to 0 to disable enchantment EXP.",
                ConfigurationManagerDisplay.Skill,
                "Enchant Skill EXP Base",
                960,
                new AcceptableValueRange<float>(0f, 100f),
                showRangeAsPercent: false));
        EnchantSkillExpPerLevel = ValheimEnchantmentSystem.config("Enchantment", "Enchant Skill EXP Per Level", 0.5f,
            Display(
                "Success EXP added per existing enchantment level before the attempt. For +9 to +10, this value is multiplied by 9. Applies equally to all scroll tiers, including blessed scrolls.",
                ConfigurationManagerDisplay.Skill,
                "Enchant Skill EXP Per Level",
                950,
                new AcceptableValueRange<float>(0f, 10f),
                showRangeAsPercent: false));
        EnchantSkillExpDifficultyBonus = ValheimEnchantmentSystem.config("Enchantment", "Enchant Skill EXP Difficulty Bonus", 4f,
            Display(
                "Maximum extra success EXP from difficulty: this value x (1 - base success chance / 100). Uses the item's YAML chance, including overrides, clamped to 0-100% before skill or blessed bonuses. A 100% base chance adds no bonus; a 0% base chance adds the full bonus, but EXP is only granted if the final success chance is above 0%.",
                ConfigurationManagerDisplay.Skill,
                "Enchant Skill EXP Difficulty Bonus",
                940,
                new AcceptableValueRange<float>(0f, 100f),
                showRangeAsPercent: false));
        IsBound = true;
    }

    private static ConfigDescription Display(
        string description,
        string category,
        string displayName,
        int order,
        AcceptableValueBase? acceptableValues = null,
        bool? showRangeAsPercent = null)
    {
        return ConfigurationManagerDisplay.Description(description, category, order, displayName, acceptableValues, showRangeAsPercent);
    }
}
