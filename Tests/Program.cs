using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using kg.ValheimEnchantmentSystem;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Items_Structures;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class Program
{
    private static int _failures;

    private static int Main()
    {
        TestChanceNormalization();
        TestChanceBoundaries();
        TestSkillChance();
        TestFailureDecisions();
        TestConfigurationManagerDisplayMetadata();
        TestWebhookListConfig();
        TestResourceMapTierSelection();
        TestSkillScrollPrefabNames();

        if (_failures == 0)
        {
            Console.WriteLine("All enchantment rule tests passed.");
            return 0;
        }

        Console.Error.WriteLine($"{_failures} enchantment rule test(s) failed.");
        return 1;
    }

    private static void TestChanceNormalization()
    {
        Equal(0d, EnchantmentRules.NormalizePercentChance(-1d), "negative chance clamps to zero");
        Equal(100d, EnchantmentRules.NormalizePercentChance(101d), "chance clamps to 100");
        Equal(81.23d, EnchantmentRules.NormalizePercentChance(81.234d), "chance rounds down to two decimals");
        Equal(81.24d, EnchantmentRules.NormalizePercentChance(81.236d), "chance rounds up to two decimals");
    }

    private static void TestChanceBoundaries()
    {
        False(EnchantmentRules.IsRollSuccessful(0d, 0d), "zero chance always fails");
        True(EnchantmentRules.IsRollSuccessful(100d, 99.999d), "100 chance always succeeds");
        True(EnchantmentRules.IsRollSuccessful(81d, 80.999d), "roll below chance succeeds");
        False(EnchantmentRules.IsRollSuccessful(81d, 81d), "roll equal to chance fails");
    }

    private static void TestSkillChance()
    {
        Near(7f, EnchantmentSkillBonusService.CalculateAdditionalEnchantmentChance(100f, 0.07f), 0.0001f,
            "100 skill levels add exactly seven percentage points");
    }

    private static void TestFailureDecisions()
    {
        EnchantmentRulePreview combined = Preview(SyncedData.ItemDesctructionTypeEnum.Combined, 25d);
        Decision(EnchantmentOutcome.Destroyed, 20, EnchantmentRules.DecideFailure(combined, 20, 24.99d, 10),
            "Combined destroys below destroy chance");
        Decision(EnchantmentOutcome.LevelDecrease, 10, EnchantmentRules.DecideFailure(combined, 20, 25d, 10),
            "Combined decreases at destroy chance boundary");

        EnchantmentRulePreview combinedEasy = Preview(SyncedData.ItemDesctructionTypeEnum.CombinedEasy, 25d);
        Decision(EnchantmentOutcome.LevelDecrease, 10, EnchantmentRules.DecideFailure(combinedEasy, 20, 24.99d, 10),
            "CombinedEasy decreases below destroy chance");
        Decision(EnchantmentOutcome.NoChange, 20, EnchantmentRules.DecideFailure(combinedEasy, 20, 25d, 10),
            "CombinedEasy keeps level at destroy chance boundary");

        EnchantmentRulePreview decrease = Preview(SyncedData.ItemDesctructionTypeEnum.LevelDecrease, 0d);
        Decision(EnchantmentOutcome.LevelDecrease, 0, EnchantmentRules.DecideFailure(decrease, 5, 99d, 10),
            "level decrease clamps at zero");

        EnchantmentRulePreview destroy = Preview(SyncedData.ItemDesctructionTypeEnum.Destroy, 0d);
        Decision(EnchantmentOutcome.Destroyed, 20, EnchantmentRules.DecideFailure(destroy, 20, 99d, 10),
            "Destroy failure type always destroys");
    }

    private static void TestConfigurationManagerDisplayMetadata()
    {
        object preservedTag = new();
        ConfigDescription source = new(
            "description",
            new AcceptableValueRange<int>(0, 10),
            preservedTag);
        ConfigDescription displayed = ConfigurationManagerDisplay.WithDisplay(
            source,
            ConfigurationManagerDisplay.Skill,
            321,
            "Display Name");

        True(Array.Exists(displayed.Tags, tag => ReferenceEquals(tag, preservedTag)),
            "configuration manager metadata preserves existing tags");

        object displayTag = Array.Find(displayed.Tags, tag =>
            tag.GetType().GetField("Category") != null &&
            tag.GetType().GetField("DispName") != null &&
            tag.GetType().GetField("Order") != null);
        if (displayTag == null)
        {
            Fail("configuration manager metadata", "display tag was not found");
            return;
        }

        Type tagType = displayTag.GetType();
        Equal("ConfigurationManagerAttributes", tagType.Name,
            "configuration manager recognizes the metadata tag type");
        Equal(ConfigurationManagerDisplay.Skill, tagType.GetField("Category")?.GetValue(displayTag),
            "configuration manager category");
        Equal("Display Name", tagType.GetField("DispName")?.GetValue(displayTag),
            "configuration manager display name");
        Equal(321, tagType.GetField("Order")?.GetValue(displayTag),
            "configuration manager order");
        Equal("1 - General", ConfigurationManagerDisplay.General,
            "configuration manager general category");
        Equal("2 - Enchantment", ConfigurationManagerDisplay.Enchantment,
            "configuration manager enchantment category");
        Equal("3 - Skill", ConfigurationManagerDisplay.Skill,
            "configuration manager skill category");
        Equal("4 - Scrolls", ConfigurationManagerDisplay.Scrolls,
            "configuration manager scroll category");
        Equal("5 - Biome Tiers", ConfigurationManagerDisplay.BiomeTiers,
            "configuration manager biome tier category");
        Equal("6 - Notifications", ConfigurationManagerDisplay.Notifications,
            "configuration manager notification category");
        Equal("7 - Client", ConfigurationManagerDisplay.Client,
            "configuration manager client category");
        Equal("8 - Scroll Recipes", ConfigurationManagerDisplay.ScrollRecipes,
            "configuration manager scroll recipe category");
        True(ReferenceEquals(source.AcceptableValues, displayed.AcceptableValues),
            "configuration manager metadata preserves acceptable values");
    }

    private static void TestWebhookListConfig()
    {
        Equal(0, WebhookListConfig.ParseTargets(null).Count, "null webhook list is empty");
        Equal(0, WebhookListConfig.ParseTargets("   ").Count, "blank webhook list is empty");

        const string first = "https://discord.com/api/webhooks/1/Token?wait=true&thread_id=2#fragment";
        const string second = "https://example.com/hook:8443/path;punctuation=value?enabled=true&mode=all#result";
        IReadOnlyList<string> targets = WebhookListConfig.ParseTargets(
            $"  {first}  , , {first.ToUpperInvariant()}, {second}  ");
        Equal(2, targets.Count, "webhook targets remove case-insensitive duplicates");
        Equal(first, targets[0], "webhook target trims surrounding whitespace");
        Equal(second, targets[1], "webhook URL punctuation is preserved");
    }

    private static void TestResourceMapTierSelection()
    {
        True(ResourceMapRequirementResolver.TryParseResourceMap(
                ResourceMapRequirementResolver.DefaultYaml,
                out ResourceMapDocument document,
                out string parseError),
            "default resource map parses: " + parseError);
        ResourceMapTier deepNorth = document.ResourceMap![document.ResourceMap.Count - 1];
        Equal("DeepNorth", deepNorth.Biome, "default resource map ends with DeepNorth");
        Equal(0, deepNorth.Materials.Count, "DeepNorth resource map tier is intentionally empty");

        List<string> warnings = new();
        Dictionary<string, ResourceTierAssignment> tiers = ResourceMapRequirementResolver.BuildResourceTierMap(
            document,
            ResolveDefaultResourceBiomeTier,
            warnings);

        Equal(0, warnings.Count, "default resource map has valid biome tiers");
        Equal('F', tiers[ResourceMapRequirementResolver.NormalizeResourceToken("Wood")].ScrollTier,
            "Meadows material maps to F");
        Equal('C', tiers[ResourceMapRequirementResolver.NormalizeResourceToken("BlackMetal")].ScrollTier,
            "Plains material maps to C");
        Equal(0, tiers[ResourceMapRequirementResolver.NormalizeResourceToken("Resin")].Rank,
            "duplicate material keeps its first resource map tier");

        const string customBiomeYaml = """
resourceMap:
- biome: Meadows
  materials:
  - Wood
- biome: JirocFlatWorld
  materials:
  - JirocOre
""";
        True(ResourceMapRequirementResolver.TryParseResourceMap(
                customBiomeYaml,
                out ResourceMapDocument customBiomeDocument,
                out string customBiomeParseError),
            "custom biome resource map parses: " + customBiomeParseError);
        List<string> customBiomeWarnings = new();
        Dictionary<string, ResourceTierAssignment> customBiomeTiers = ResourceMapRequirementResolver.BuildResourceTierMap(
            customBiomeDocument,
            biome => biome == "JirocFlatWorld" ? 'B' : ResolveDefaultResourceBiomeTier(biome),
            customBiomeWarnings);
        Equal(0, customBiomeWarnings.Count, "registered custom biome resource map has no warnings");
        ResourceTierAssignment customOreTier = customBiomeTiers[ResourceMapRequirementResolver.NormalizeResourceToken("JirocOre")];
        Equal(1, customOreTier.Rank, "custom biome keeps resource map ordering");
        Equal('B', customOreTier.ScrollTier, "custom biome uses its configured scroll tier");

        True(ResourceMapRequirementResolver.TrySelectHighestTier(
                new[]
                {
                    tiers[ResourceMapRequirementResolver.NormalizeResourceToken("Wood")],
                    tiers[ResourceMapRequirementResolver.NormalizeResourceToken("BlackMetal")]
                },
                out ResourceTierAssignment highest),
            "mapped recipe materials select a tier");
        Equal('C', highest.ScrollTier, "highest mapped recipe material wins");

        True(ResourceMapRequirementResolver.TrySelectMappedRecipeTier(
                new ResourceTierAssignment?[]
                {
                    null,
                    tiers[ResourceMapRequirementResolver.NormalizeResourceToken("BlackMetal")]
                },
                out ResourceTierAssignment partiallyMapped),
            "one mapped material is enough for automatic assignment");
        Equal('C', partiallyMapped.ScrollTier, "unmapped recipe materials are ignored");
        False(ResourceMapRequirementResolver.TrySelectMappedRecipeTier(
                new ResourceTierAssignment?[] { null, null },
                out _),
            "all recipe materials unmapped skips automatic assignment");

        False(ResourceMapRequirementResolver.TryParseResourceMap(
                "resourceMap:\n  - biome: [",
                out _,
                out _),
            "invalid resource map is rejected");
        False(ResourceMapRequirementResolver.TryParseResourceMap(
                "resources: []",
                out _,
                out _),
            "missing resourceMap root is rejected");
    }

    private static void TestSkillScrollPrefabNames()
    {
        foreach (char tier in "FEDCBAS")
        {
            True(SkillScrollService.TryGetSkillScrollTier($"kg_EnchantSkillScroll_{tier}", out char parsedTier),
                $"canonical tier {tier} skill scroll name is accepted");
            Equal(tier, parsedTier, $"canonical tier {tier} skill scroll name returns its tier");
        }

        False(SkillScrollService.TryGetSkillScrollTier("kg_EnchantSkillScroll_", out _),
            "skill scroll name without a tier is rejected");
        False(SkillScrollService.TryGetSkillScrollTier("kg_EnchantSkillScroll_fake_S", out _),
            "skill scroll name with an extra suffix is rejected");
        False(SkillScrollService.TryGetSkillScrollTier("kg_EnchantSkillScroll_X", out _),
            "skill scroll name with an invalid tier is rejected");
        False(SkillScrollService.TryGetSkillScrollTier("kg_EnchantSkillScroll_s", out _),
            "skill scroll name with a lowercase tier is rejected");
        False(SkillScrollService.TryGetSkillScrollTier("kg_EnchantScroll_Weapon_S", out _),
            "non-skill scroll name is rejected");
    }

    private static char? ResolveDefaultResourceBiomeTier(string biome)
    {
        return biome switch
        {
            "Meadows" => 'F',
            "BlackForest" => 'F',
            "Swamp" => 'E',
            "Ocean" => 'E',
            "Mountain" => 'D',
            "Plains" => 'C',
            "Mistlands" => 'B',
            "AshLands" => 'A',
            "DeepNorth" => 'S',
            _ => null
        };
    }

    private static EnchantmentRulePreview Preview(SyncedData.ItemDesctructionTypeEnum failureType, double destroyChance) =>
        new(0d, 0d, 0d, 0d, destroyChance, true, false, failureType);

    private static void Decision(EnchantmentOutcome expectedOutcome, int expectedLevel, EnchantmentDecision actual, string name)
    {
        Equal(expectedOutcome, actual.Outcome, name + " outcome");
        Equal(expectedLevel, actual.NewLevel, name + " level");
    }

    private static void True(bool condition, string name)
    {
        if (!condition) Fail(name, "expected true");
    }

    private static void False(bool condition, string name)
    {
        if (condition) Fail(name, "expected false");
    }

    private static void Near(float expected, float actual, float tolerance, string name)
    {
        if (Math.Abs(expected - actual) > tolerance) Fail(name, $"expected {expected}, got {actual}");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual)) Fail(name, $"expected {expected}, got {actual}");
    }

    private static void Fail(string name, string detail)
    {
        _failures++;
        Console.Error.WriteLine($"FAIL: {name}: {detail}");
    }
}
