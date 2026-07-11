namespace kg.ValheimEnchantmentSystem.Configs;

public static class EnchantmentSettings
{
    private static bool IsBound;
    private static bool NotificationSettingsBound;
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
            Display("The level below which failed enchantments stay at the current level. Set to 0 to disable.", ConfigurationManagerDisplay.Enchantment, "Safety Level", 990));
        DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false,
            Display("Drop enchantment on item upgrade.", ConfigurationManagerDisplay.Enchantment, "Drop Enchantment On Upgrade", 800));
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
        AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment", "AllowJewelcraftingMirrorCopyEnchant", false,
            Display("Allow jewelcrafting to copy enchantment from one item to another using mirror.", ConfigurationManagerDisplay.Enchantment, "Allow Jewelcrafting Mirror Copy Enchant", 790));
        AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.07f,
            Display("Additional enchantment chance per level of Enchantment skill.", ConfigurationManagerDisplay.Skill, "Enchant Chance Per Skill Level", 980));
        FailedEnchantSkillExpMultiplier = ValheimEnchantmentSystem.config("Enchantment", "FailedEnchantSkillExpMultiplier", 0.5f,
            Display(
                "Multiplier applied to enchant skill EXP when an enchant attempt fails. Failure EXP = Success EXP x multiplier.",
                ConfigurationManagerDisplay.Skill,
                "Failed Enchant Skill EXP Multiplier",
                970,
                new AcceptableValueRange<float>(0f, 2f)));
        int enchantSkillExpOrder = 900;
        foreach (char tier in EnchantmentTierCatalog.AllTiers)
        {
            EnchantSkillExpByTier[tier] = ValheimEnchantmentSystem.config(
                "Enchantment",
                $"Enchant Skill EXP {tier}",
                EnchantmentTierCatalog.GetDefaultEnchantSkillExp(tier),
                Display(
                    $"Skill EXP granted when enchanting with a tier {tier} enchant scroll.",
                    ConfigurationManagerDisplay.Skill,
                    $"Enchant Skill EXP {tier}",
                    enchantSkillExpOrder,
                    new AcceptableValueRange<int>(0, 100)));
            enchantSkillExpOrder -= 10;
        }
        IsBound = true;
    }

    internal static void BindNotificationSettings()
    {
        if (NotificationSettingsBound)
        {
            return;
        }

        EnchantmentEnableNotifications = ValheimEnchantmentSystem.config("Notifications", "EnchantmentEnableNotifications", true,
            Display("Enable enchantment notifications.", ConfigurationManagerDisplay.Notifications, "Enable Enchantment Notifications", 1000));
        EnchantmentNotificationMinLevel = ValheimEnchantmentSystem.config("Notifications", "EnchantmentNotificationMinLevel", 6,
            Display("The minimum level of enchantment to show notification.", ConfigurationManagerDisplay.Notifications, "Notification Minimum Enchant Level", 990));
        NotificationSettingsBound = true;
    }

    private static ConfigDescription Display(
        string description,
        string category,
        string displayName,
        int order,
        AcceptableValueBase? acceptableValues = null)
    {
        return ConfigurationManagerDisplay.Description(description, category, order, displayName, acceptableValues);
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
