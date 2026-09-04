using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx.Configuration;
using kg.ValheimEnchantmentSystem;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Integrations;
using kg.ValheimEnchantmentSystem.Items_Structures;
using kg.ValheimEnchantmentSystem.UI;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class Program
{
    private static int _failures;

    private static int Main()
    {
        TestChanceNormalization();
        TestChanceBoundaries();
        TestSkillChance();
        TestEnchantmentSkillExperienceFormula();
        TestEnchantmentSkillExperienceBoundaries();
        TestEnchantmentSkillExperienceOutcomes();
        TestFailureDecisions();
        TestConfigurationManagerDisplayMetadata();
        TestConfigurationManagerCategoryOrdering();
        TestWebhookListConfig();
        TestNotificationThresholds();
        TestNotificationFilters();
        TestNotificationMinimumLevelMetadata();
        TestResourceMapTierSelection();
        TestSkillScrollPrefabNames();
        TestSkillScrollExpSliders();
        TestScrollCombineModes();
        TestScrollCombineModeMetadata();
        TestExpandWorldDataReloadMethodSelection();
        TestPanelPositionMath();
        TestEnchantmentAnimationTiming();
        TestEnchantmentKeyboardShortcut();
        TestEnchantmentGamepadShortcutState();
        TestEnchantmentMaterialRules();

        if (_failures == 0)
        {
            Console.WriteLine("All enchantment rule tests passed.");
            return 0;
        }

        Console.Error.WriteLine($"{_failures} enchantment rule test(s) failed.");
        return 1;
    }

    private static void TestEnchantmentMaterialRules()
    {
        True(EnchantmentMaterialRules.Matches("ScrollF", "ScrollF", 1), "material matches exact prefab");
        False(EnchantmentMaterialRules.Matches("ScrollF", "BlessedScrollF", 1), "normal and blessed scrolls stay separate");
        False(EnchantmentMaterialRules.Matches("scrollf", "ScrollF", 1), "prefab matching preserves case");
        False(EnchantmentMaterialRules.Matches(null, "ScrollF", 1), "missing prefab is not a material");
        False(EnchantmentMaterialRules.Matches("ScrollF", "ScrollF", 0), "empty stack is not a material");
        Equal(0, EnchantmentMaterialRules.AvailableInContainer(-1, true), "negative stock is not available");
        Equal(0, EnchantmentMaterialRules.AvailableInContainer(0, false), "empty chest has no materials");
        Equal(1, EnchantmentMaterialRules.AvailableInContainer(1, false), "last scroll can be used when leave one is off");
        Equal(0, EnchantmentMaterialRules.AvailableInContainer(1, true), "leave one reserves the final scroll");
        Equal(1, EnchantmentMaterialRules.AvailableInContainer(2, true), "leave one makes only the excess available");
        Equal(int.MaxValue - 1, EnchantmentMaterialRules.AvailableInContainer(int.MaxValue, true), "large stock preserves reserve");
        Equal(4, EnchantmentMaterialRules.AddCounts(1, 3), "own and chest materials add together");
        Equal(3, EnchantmentMaterialRules.AddCounts(-4, 3), "negative counts cannot reduce available stock");
        Equal(int.MaxValue, EnchantmentMaterialRules.AddCounts(int.MaxValue, 1), "large totals saturate instead of overflow");

        True(EnchantmentMaterialRules.TryConsumeOne(() => true,
                () => throw new Exception("containers must not be queried when the inventory supplies the scroll")),
            "own inventory has priority without touching optional integration");
        var calls = new List<string>();
        True(EnchantmentMaterialRules.TryConsumeOne(() => { calls.Add("inventory"); return false; },
            () => new Func<bool>[]
            {
                () => { calls.Add("unavailable"); return false; },
                () => { calls.Add("chest"); return true; },
                () => throw new Exception("a second material must not be consumed")
            }), "first eligible chest supplies exactly one material");
        Equal("inventory,unavailable,chest", string.Join(",", calls), "consumption follows inventory-first source order");
        False(EnchantmentMaterialRules.TryConsumeOne(() => false, () => Array.Empty<Func<bool>>()),
            "missing optional integration and missing local material fail cleanly");
        False(EnchantmentMaterialRules.TryConsumeOne(() => false, () => new Func<bool>[] { () => false, () => false }),
            "stock disappearing at execution time does not permit enchanting");
        bool uncertainStopped = false;
        try
        {
            EnchantmentMaterialRules.TryConsumeOne(() => false, () => new Func<bool>[]
            {
                () => throw new InvalidOperationException("uncertain removal"),
                () => throw new Exception("must not retry another chest")
            });
        }
        catch (InvalidOperationException) { uncertainStopped = true; }
        True(uncertainStopped, "uncertain mutations stop instead of retrying and consuming twice");
    }

    private static void TestEnchantmentKeyboardShortcut()
    {
        var pressed = new HashSet<string> { "Y" };
        var held = new HashSet<string> { "Y", "W" };
        True(EnchantmentShortcutRules.IsPressed("Y", "None", Array.Empty<string>(), pressed.Contains, held.Contains),
            "Y activates even when an unrelated movement key is held");
        False(EnchantmentShortcutRules.IsPressed("Y", "None", new[] { "Control" }, pressed.Contains, held.Contains),
            "configured modifier must be held");
        held.Add("Control");
        True(EnchantmentShortcutRules.IsPressed("Y", "None", new[] { "Control" }, pressed.Contains, held.Contains),
            "configured modifier and main key activate together");
        False(EnchantmentShortcutRules.IsPressed("Y", "None", new[] { "Control", "Shift" }, pressed.Contains, held.Contains),
            "all configured modifiers are required");
        held.Add("Shift");
        True(EnchantmentShortcutRules.IsPressed("Y", "None", new[] { "Control", "Shift" }, pressed.Contains, held.Contains),
            "multiple configured modifiers are supported");
        False(EnchantmentShortcutRules.IsPressed("U", "None", Array.Empty<string>(), pressed.Contains, held.Contains),
            "the configured main key must be pressed");
        pressed.Clear();
        False(EnchantmentShortcutRules.IsPressed("Y", "None", Array.Empty<string>(), pressed.Contains, held.Contains),
            "holding Y does not repeatedly toggle the panel");
        False(EnchantmentShortcutRules.IsPressed("None", "None", Array.Empty<string>(),
                _ => throw new Exception("disabled key should not query input"), held.Contains),
            "None disables keyboard activation without querying input");

        True(EnchantmentShortcutRules.IsConfigurationManager("sighsorry.ConfigManager", "ConfigManager"),
            "installed ConfigManager is recognized for keyboard input blocking");
        True(EnchantmentShortcutRules.IsConfigurationManager("com.bepis.bepinex.configurationmanager", "Settings"),
            "configuration manager GUID is recognized");
        True(EnchantmentShortcutRules.IsConfigurationManager("custom.plugin", "Configuration Manager"),
            "configuration manager display name is recognized");
        True(EnchantmentShortcutRules.IsConfigurationManager("SighSorry.CONFIGMANAGER", "Settings"),
            "configuration manager detection is case insensitive");
        False(EnchantmentShortcutRules.IsConfigurationManager("other.plugin", "Other Settings"),
            "unrelated plugins are not treated as configuration manager");
    }

    private static void TestEnchantmentGamepadShortcutState()
    {
        var shortcut = new EnchantmentGamepadShortcutState();
        False(shortcut.Observe(true, true, true), "initially held gamepad chord is ignored");
        False(shortcut.Observe(false, true, true), "initialization waits for the modifier to be released too");
        False(shortcut.Observe(true, true, true), "repressing the main button before initial full release is ignored");
        False(shortcut.Observe(false, false, true), "initial neutral state arms the shortcut without activating it");

        False(shortcut.Observe(false, true, true), "holding only the modifier does not activate");
        True(shortcut.Observe(true, true, true), "main button rising edge with held modifier activates");
        False(shortcut.Observe(true, true, true), "repeated fixed or dynamic polls do not duplicate activation");
        False(shortcut.Observe(true, true, true), "held chord does not activate on subsequent polls");
        False(shortcut.Observe(false, true, true), "releasing the main button does not activate");
        True(shortcut.Observe(true, true, true), "main button can be repressed while the modifier stays held");

        False(shortcut.Observe(false, false, true), "releasing both buttons does not activate");
        True(shortcut.Observe(true, true, true), "modifier and main button pressed in the same poll activate");
        False(shortcut.Observe(false, false, true), "full release after simultaneous chord does not activate");
        False(shortcut.Observe(true, false, true), "main button without required modifier does not activate");
        False(shortcut.Observe(true, true, true), "pressing modifier after an already held main button does not activate");
        False(shortcut.Observe(true, false, true), "releasing modifier while main stays held does not activate");
        False(shortcut.Observe(true, true, true), "repressing modifier while main stays held still does not activate");
        False(shortcut.Observe(false, true, true), "main button release resets its edge independently");
        True(shortcut.Observe(true, true, true), "new main button press activates after the previously invalid chord");

        shortcut.RequireRelease();
        False(shortcut.Observe(true, true, true), "rebinding suppresses an already held chord");
        False(shortcut.Observe(true, false, true), "rebinding waits for the main button to be released too");
        False(shortcut.Observe(false, true, true), "rebinding requires both buttons released at the same time");
        False(shortcut.Observe(true, true, true), "partial releases cannot rearm a rebound shortcut");
        False(shortcut.Observe(false, false, true), "full release rearms a rebound shortcut without activation");
        True(shortcut.Observe(true, true, true), "a rebound shortcut activates after full release and a fresh press");

        var singleButton = new EnchantmentGamepadShortcutState();
        False(singleButton.Observe(false, false, false), "single-button shortcut also starts from a neutral state");
        True(singleButton.Observe(true, false, false), "main button activates when no modifier is configured");
        False(singleButton.Observe(true, false, false), "single-button shortcut does not repeat while held");
        False(singleButton.Observe(false, true, false), "unrequired modifier does not activate without main button");
        True(singleButton.Observe(true, true, false), "unrequired held modifier does not block the main button");

        // The caller evaluates UI blockers after Observe. Dropping an observed press must not queue it.
        var blockedShortcut = new EnchantmentGamepadShortcutState();
        blockedShortcut.Observe(false, false, true);
        bool observedWhileBlocked = blockedShortcut.Observe(true, true, true);
        True(observedWhileBlocked, "a blocked UI can observe and discard the initial press");
        False(blockedShortcut.Observe(true, true, true), "removing a UI blocker while the chord stays held does not replay the press");
        False(blockedShortcut.Observe(false, true, true), "release after a blocked press does not activate");
        True(blockedShortcut.Observe(true, true, true), "a fresh press after clearing the blocker activates");
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

    private static void TestEnchantmentSkillExperienceFormula()
    {
        (int currentLevel, double baseChance, double finalChance, float expected)[] examples =
        {
            (0, 90d, 90d, 2.4f),
            (4, 74d, 74d, 5.04f),
            (9, 50d, 50d, 8.5f),
            (14, 25d, 25d, 12f),
            (19, 0d, 3.5d, 15.5f)
        };
        foreach (var example in examples)
        {
            Near(example.expected, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(
                    example.currentLevel, example.baseChance, example.finalChance, 2f, 0.5f, 4f), 0.0001f,
                $"default EXP for +{example.currentLevel} to +{example.currentLevel + 1}");
        }

        foreach (double finalChance in new[] { 25d, 28.5d, 50d, 100d })
        {
            Near(12f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(14, 25d, finalChance, 2f, 0.5f, 4f),
                0.0001f, $"skill or blessed bonus changing final chance to {finalChance} does not reduce EXP");
        }

        Near(13.06f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(14, 17d, 20.5d, 3f, 0.6f, 2f),
            0.0001f, "custom YAML base chance and configurable formula coefficients determine EXP");
        Equal(2f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(0, 100d, 100d, 2f, 0.5f, 4f),
            "first enchantment at 100 percent grants only base EXP");
        Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(19, 0d, 3.5d, 0f, 0f, 0f),
            "setting all formula coefficients to zero disables enchantment EXP");
        Equal(6f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(8, 25d, 25d, 0f, 0f, 8f),
            "difficulty bonus can be used independently");
        Equal(4f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(8, 25d, 25d, 0f, 0.5f, 0f),
            "level bonus can be used independently");
        Equal(3f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(8, 25d, 25d, 3f, 0f, 0f),
            "base EXP can be used independently");
    }

    private static void TestEnchantmentSkillExperienceBoundaries()
    {
        foreach (double finalChance in new[] { 0d, -0d, -1d, -double.MaxValue, double.NaN,
                     double.NegativeInfinity, double.PositiveInfinity })
        {
            Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(19, 0d, finalChance, 2f, 0.5f, 4f),
                $"impossible or invalid final chance {finalChance} grants no EXP");
        }
        Equal(15.5f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(19, 0d, 3.5d, 2f, 0.5f, 4f),
            "zero base chance with a positive skill bonus remains eligible for EXP");
        Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 0d, 2f, 0.5f, 4f),
            "zero final chance suppresses EXP even when the base chance is positive");
        Equal(15.5f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(19, 0d, 25d, 2f, 0.5f, 4f),
            "zero base chance with a positive blessed bonus remains eligible for EXP");
        Equal(15.5f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(19, 0d, double.Epsilon, 2f, 0.5f, 4f),
            "smallest positive final chance is not treated as zero");
        Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(-1, 90d, 90d, 2f, 0.5f, 4f),
            "negative current enchantment level is invalid");
        Equal(6f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(0, -double.MaxValue, 5d, 2f, 0.5f, 4f),
            "finite base chance below zero clamps difficulty to one");
        Equal(2f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(0, double.MaxValue, 100d, 2f, 0.5f, 4f),
            "finite base chance above 100 clamps difficulty to zero");

        foreach (double baseChance in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, baseChance, 50d, 2f, 0.5f, 4f),
                $"invalid base chance {baseChance} grants no EXP");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            for (int index = 0; index < 3; index++)
            {
                float[] coefficients = { 2f, 0.5f, 4f };
                coefficients[index] = invalid;
                Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(
                        9, 50d, 50d, coefficients[0], coefficients[1], coefficients[2]),
                    $"nonfinite EXP coefficient {index} ({invalid}) grants no EXP");
            }
        }
        Equal(6.5f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 50d, -2f, 0.5f, 4f),
            "negative base EXP is sanitized independently");
        Equal(4f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 50d, 2f, -0.5f, 4f),
            "negative level coefficient is sanitized independently");
        Equal(6.5f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 50d, 2f, 0.5f, -4f),
            "negative difficulty coefficient is sanitized independently");
        Equal(0f, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 50d,
                -float.MaxValue, -float.MaxValue, -float.MaxValue),
            "all negative coefficients produce zero EXP");
        Equal(float.MaxValue, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(
                int.MaxValue, 0d, 1d, float.MaxValue, float.MaxValue, float.MaxValue),
            "formula overflow is capped to finite float maximum");
        Equal(float.MaxValue, EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(0, 100d, 100d,
                float.MaxValue, 0f, 0f),
            "finite float maximum base EXP is preserved");
    }

    private static void TestEnchantmentSkillExperienceOutcomes()
    {
        float beforeAttemptExp = EnchantmentSkillExperience.CalculateSuccessfulAttemptExp(9, 50d, 53.5d, 2f, 0.5f, 4f);
        foreach (EnchantmentOutcome outcome in Enum.GetValues(typeof(EnchantmentOutcome)))
        {
            bool success = outcome == EnchantmentOutcome.Success;
            Equal(success ? 8.5f : 4.25f,
                EnchantmentSkillExperience.CalculateGrantedExp(beforeAttemptExp, success, 0.5f),
                $"{outcome} uses the original attempt EXP before any level change or destruction");
        }

        Equal(0f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, 0f),
            "zero failure multiplier disables failure EXP");
        Equal(0f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, -1f),
            "negative failure multiplier clamps to zero");
        Equal(8.5f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, 1f),
            "failure multiplier one grants full attempt EXP");
        Equal(17f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, 2f),
            "failure multiplier two grants twice the attempt EXP");
        Equal(17f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, float.MaxValue),
            "failure multiplier above two clamps to two");
        foreach (float multiplier in new[] { 0f, -1f, 2f, float.MaxValue, float.NaN,
                     float.NegativeInfinity, float.PositiveInfinity })
        {
            Equal(8.5f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, true, multiplier),
                $"success ignores failure multiplier {multiplier}");
        }
        foreach (float multiplier in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        {
            Equal(0f, EnchantmentSkillExperience.CalculateGrantedExp(8.5f, false, multiplier),
                $"nonfinite failure multiplier {multiplier} grants no failure EXP");
        }
        foreach (float reward in new[] { 0f, -0f, -1f, -float.MaxValue, float.NaN,
                     float.NegativeInfinity, float.PositiveInfinity })
        {
            foreach (bool success in new[] { false, true })
            {
                Equal(0f, EnchantmentSkillExperience.CalculateGrantedExp(reward, success, 0.5f),
                    $"invalid or disabled reward {reward} grants no EXP on success={success}");
            }
        }
        Equal(float.Epsilon, EnchantmentSkillExperience.CalculateGrantedExp(float.Epsilon, true, 0.5f),
            "smallest positive successful reward is preserved");
        Equal(float.MaxValue, EnchantmentSkillExperience.CalculateGrantedExp(float.MaxValue, true, 0.5f),
            "largest finite successful reward is preserved");
        Equal(float.MaxValue, EnchantmentSkillExperience.CalculateGrantedExp(float.MaxValue, false, 2f),
            "failure multiplier cannot overflow the granted reward");
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
        Equal<object>(4, tagType.GetField("CategoryOrder")?.GetValue(displayTag),
            "configuration manager skill category has an explicit priority");
        Equal("Display Name", tagType.GetField("DispName")?.GetValue(displayTag),
            "configuration manager display name");
        Equal(321, tagType.GetField("Order")?.GetValue(displayTag),
            "configuration manager order");
        True(tagType.GetField("ShowRangeAsPercent") != null,
            "configuration manager percent display metadata field is available");
        Equal<object>(null, tagType.GetField("ShowRangeAsPercent")?.GetValue(displayTag),
            "configuration manager percent display defaults to unspecified");
        True(tagType.GetField("Browsable") != null,
            "configuration manager hidden-entry metadata field is available");
        Equal<object>(null, tagType.GetField("Browsable")?.GetValue(displayTag),
            "configuration manager entry visibility defaults to unspecified");
        Equal("1 - General", ConfigurationManagerDisplay.General,
            "configuration manager general category");
        Equal("2 - Client", ConfigurationManagerDisplay.Client,
            "configuration manager client category");
        Equal("3 - Enchantment", ConfigurationManagerDisplay.Enchantment,
            "configuration manager enchantment category");
        Equal("4 - Skill", ConfigurationManagerDisplay.Skill,
            "configuration manager skill category");
        Equal("5 - Scrolls", ConfigurationManagerDisplay.Scrolls,
            "configuration manager scroll category");
        Equal("6 - Biome Tiers", ConfigurationManagerDisplay.BiomeTiers,
            "configuration manager biome tier category");
        Equal("7 - Scroll Recipes", ConfigurationManagerDisplay.ScrollRecipes,
            "configuration manager scroll recipe category");
        True(ReferenceEquals(source.AcceptableValues, displayed.AcceptableValues),
            "configuration manager metadata preserves acceptable values");

        AcceptableValueRange<float> durationRange = new(0f, 1f);
        ConfigDescription durationSource = new("Duration in seconds", durationRange, preservedTag);
        ConfigDescription[] durationDescriptions =
        {
            ConfigurationManagerDisplay.WithDisplay(
                durationSource,
                ConfigurationManagerDisplay.Client,
                100,
                "Animation Duration",
                showRangeAsPercent: false),
            ConfigurationManagerDisplay.Description(
                "Duration in seconds",
                ConfigurationManagerDisplay.Client,
                100,
                "Animation Duration",
                durationRange,
                showRangeAsPercent: false)
        };
        for (int index = 0; index < durationDescriptions.Length; index++)
        {
            ConfigDescription durationDescription = durationDescriptions[index];
            object durationTag = Array.Find(durationDescription.Tags,
                tag => tag.GetType().Name == "ConfigurationManagerAttributes");
            if (durationTag == null)
            {
                Fail($"configuration manager duration metadata {index}", "display tag was not found");
                continue;
            }

            Equal<object>(false, durationTag.GetType().GetField("ShowRangeAsPercent")?.GetValue(durationTag),
                $"configuration manager duration metadata {index} preserves explicit false");
            True(ReferenceEquals(durationRange, durationDescription.AcceptableValues),
                $"configuration manager duration metadata {index} preserves the float range");
            True(durationDescription.AcceptableValues is AcceptableValueRange<float>,
                $"configuration manager duration metadata {index} retains a float slider");
        }

        True(Array.Exists(durationDescriptions[0].Tags, tag => ReferenceEquals(tag, preservedTag)),
            "configuration manager seconds slider preserves existing tags");
        Equal(0f, durationRange.MinValue, "animation duration slider starts at zero seconds");
        Equal(1f, durationRange.MaxValue, "animation duration slider ends at one second");

        ConfigDescription[] hiddenDescriptions =
        {
            ConfigurationManagerDisplay.WithDisplay(source, ConfigurationManagerDisplay.Client, 50,
                "Hidden Offset", browsable: false),
            ConfigurationManagerDisplay.Description("Hidden offset", ConfigurationManagerDisplay.Client, 50,
                "Hidden Offset", source.AcceptableValues, browsable: false)
        };
        for (int index = 0; index < hiddenDescriptions.Length; index++)
        {
            ConfigDescription hidden = hiddenDescriptions[index];
            object hiddenTag = Array.Find(hidden.Tags,
                tag => tag.GetType().Name == "ConfigurationManagerAttributes");
            if (hiddenTag == null)
            {
                Fail($"hidden config metadata {index}", "display tag was not found");
                continue;
            }

            Equal<object>(false, hiddenTag.GetType().GetField("Browsable")?.GetValue(hiddenTag),
                $"hidden config metadata {index} preserves explicit false");
            Equal<object>(null, hiddenTag.GetType().GetField("ShowRangeAsPercent")?.GetValue(hiddenTag),
                $"hidden config metadata {index} does not change percent display");
            True(ReferenceEquals(source.AcceptableValues, hidden.AcceptableValues),
                $"hidden config metadata {index} preserves acceptable values");
        }
        True(Array.Exists(hiddenDescriptions[0].Tags, tag => ReferenceEquals(tag, preservedTag)),
            "hidden config metadata preserves existing tags");
    }

    private static void TestNotificationThresholds()
    {
        Equal(5f, NotificationRules.DisplayDurationSeconds, "notifications have a fixed five-second duration");
        Equal(6, NotificationSettings.DefaultMinimumLevel, "both notification thresholds default to level six");
        False(NotificationRules.MeetsMinimumLevel(5, 6), "notification result below threshold is excluded");
        True(NotificationRules.MeetsMinimumLevel(6, 6), "notification threshold is inclusive");
        True(NotificationRules.MeetsMinimumLevel(7, 6), "notification result above threshold is included");
        True(NotificationRules.MeetsMinimumLevel(0, 0), "zero threshold includes level zero");
        False(NotificationRules.MeetsMinimumLevel(-1, 0), "negative result level is excluded");
        True(NotificationRules.MeetsMinimumLevel(0, -1), "negative threshold clamps to zero");
        False(NotificationRules.MeetsMinimumLevel(-1, int.MinValue),
            "extreme negative threshold cannot allow a negative result");
        False(NotificationRules.MeetsMinimumLevel(499, 500), "maximum configured threshold excludes level 499");
        True(NotificationRules.MeetsMinimumLevel(500, 500), "maximum configured threshold includes level 500");

        const int resultLevel = 8;
        const int success = (int)Notifications_UI.NotificationItemResult.Success;
        False(NotificationRules.MeetsMinimumLevel(resultLevel, 15),
            "server webhook threshold 15 excludes result level eight");
        True(NotificationRules.ShouldShowNotification(success, resultLevel, 6, Notifications_UI.Filter.Success),
            "client threshold six can display an event excluded from server webhooks");
        True(NotificationRules.MeetsMinimumLevel(resultLevel, 6),
            "server webhook threshold six includes result level eight");
        False(NotificationRules.ShouldShowNotification(success, resultLevel, 15, Notifications_UI.Filter.Success),
            "client threshold 15 can hide an event sent to server webhooks");
        True(NotificationRules.MeetsMinimumLevel(resultLevel, 6),
            "local notification rejection does not modify webhook eligibility");
    }

    private static void TestNotificationFilters()
    {
        const int successType = (int)Notifications_UI.NotificationItemResult.Success;
        const int decreaseType = (int)Notifications_UI.NotificationItemResult.LevelDecrease;
        const int destroyedType = (int)Notifications_UI.NotificationItemResult.Destroyed;
        const int noneFilter = (int)Notifications_UI.Filter.None;
        const int successFilter = (int)Notifications_UI.Filter.Success;
        const int failFilter = (int)Notifications_UI.Filter.Fail;
        int[] filters =
        {
            noneFilter,
            successFilter,
            failFilter,
            successFilter | failFilter
        };
        // Keep the pure-rule harness independent of Unity's outer UI type metadata.
        // Enum constants are checked through real calls, without reflection or enum boxing.
        foreach (int result in new[] { successType, decreaseType, destroyedType })
        {
            int required = result == successType ? successFilter : failFilter;
            foreach (int filter in filters)
            {
                bool expected = (filter & required) == required;
                False(NotificationRules.ShouldShowNotification(result, 5, 6, (Notifications_UI.Filter)filter),
                    $"{result} below threshold stays hidden with filter {filter}");
                Equal(expected, NotificationRules.ShouldShowNotification(result, 6, 6, (Notifications_UI.Filter)filter),
                    $"{result} at threshold follows filter {filter}");
                Equal(expected, NotificationRules.ShouldShowNotification(result, 7, 6, (Notifications_UI.Filter)filter),
                    $"{result} above threshold follows filter {filter}");
                False(NotificationRules.ShouldShowNotification(result, 8, 6, (Notifications_UI.Filter)4),
                    $"{result} does not match unrelated filter bits");
            }
        }

        foreach (int invalidType in new[] { int.MinValue, -1, 3, int.MaxValue })
        {
            False(NotificationRules.ShouldShowNotification(invalidType, 8, 6,
                    Notifications_UI.Filter.Success | Notifications_UI.Filter.Fail),
                $"unknown notification type {invalidType} is hidden");
            False(NotificationRules.ShouldShowNotification(invalidType, 8, 6, (Notifications_UI.Filter)(-1)),
                $"unknown notification type {invalidType} is hidden even with all filter bits");
        }
    }

    private static void TestConfigurationManagerCategoryOrdering()
    {
        string[] expected =
        {
            ConfigurationManagerDisplay.General,
            ConfigurationManagerDisplay.Client,
            ConfigurationManagerDisplay.Enchantment,
            ConfigurationManagerDisplay.Skill,
            ConfigurationManagerDisplay.Scrolls,
            ConfigurationManagerDisplay.BiomeTiers,
            ConfigurationManagerDisplay.ScrollRecipes
        };
        // Reproduce the reported registration order, deliberately varying per-setting priorities.
        int[] registrationOrder = { 0, 2, 3, 4, 5, 1, 6 };
        var entries = registrationOrder.SelectMany(index => new[] { 10, 1001 }.Select(order =>
        {
            ConfigDescription description = ConfigurationManagerDisplay.Description(
                "description", expected[index], order, "Option " + order);
            object tag = Array.Find(description.Tags,
                value => value.GetType().Name == "ConfigurationManagerAttributes");
            var type = tag.GetType();
            int? priority = (int?)type.GetField("CategoryOrder")?.GetValue(tag);
            Equal<int?>(7 - index, priority, expected[index] + " explicit category priority");
            Equal<object>(order, type.GetField("Order")?.GetValue(tag),
                expected[index] + " preserves option ordering");
            return (category: expected[index], priority: priority ?? 0, order);
        })).ToArray();

        // ConfigManager groups by category, sorts by maximum CategoryOrder descending,
        // and sorts settings inside each group separately by Order descending.
        var sorted = entries.GroupBy(entry => entry.category)
            .OrderByDescending(group => group.Max(entry => entry.priority)).ToArray();
        Equal(string.Join("|", expected), string.Join("|", sorted.Select(group => group.Key)),
            "numbered categories override the original registration order");
        foreach (var group in sorted)
        {
            Equal("1001,10", string.Join(",", group.OrderByDescending(entry => entry.order).Select(entry => entry.order)),
                group.Key + " keeps its internal descending option order");
        }

        var filtered = entries.Where(entry => entry.category == ConfigurationManagerDisplay.Client ||
                                             entry.category == ConfigurationManagerDisplay.Enchantment)
            .GroupBy(entry => entry.category)
            .OrderByDescending(group => group.Max(entry => entry.priority));
        Equal(ConfigurationManagerDisplay.Client + "|" + ConfigurationManagerDisplay.Enchantment,
            string.Join("|", filtered.Select(group => group.Key)),
            "filtering settings still places Client before Enchantment");

        ConfigDescription unrelated = ConfigurationManagerDisplay.Description("description", "Other Skill", null);
        object unrelatedTag = Array.Find(unrelated.Tags,
            value => value.GetType().Name == "ConfigurationManagerAttributes");
        Equal<object>(null, unrelatedTag.GetType().GetField("CategoryOrder")?.GetValue(unrelatedTag),
            "unrelated categories retain their unspecified priority");
    }

    private static void TestNotificationMinimumLevelMetadata()
    {
        (ConfigDescription description, string category, string displayName, int order, int maximum)[] settings =
        {
            (NotificationSettings.CreateWebhookMinimumLevelDescription(), ConfigurationManagerDisplay.General,
                "Webhook Minimum Enchant Level", 850, 500),
            (NotificationSettings.CreateNotificationMinimumLevelDescription(), ConfigurationManagerDisplay.Client,
                "Notification Minimum Enchant Level", 910, 100)
        };
        foreach (var setting in settings)
        {
            if (setting.description.AcceptableValues is not AcceptableValueRange<int> range)
            {
                Fail(setting.displayName, "expected an integer range");
                continue;
            }

            Equal(0, range.MinValue, $"{setting.displayName} minimum");
            Equal(setting.maximum, range.MaxValue, $"{setting.displayName} maximum");
            Equal<object>(6, range.Clamp(6), $"{setting.displayName} default remains within range");
            Equal<object>(0, range.Clamp(-1), $"{setting.displayName} negative value clamps to zero");
            Equal<object>(setting.maximum, range.Clamp(setting.maximum + 1), $"{setting.displayName} value above maximum clamps");
            Equal<object>(setting.maximum, range.Clamp(int.MaxValue), $"{setting.displayName} large value clamps");

            object tag = Array.Find(setting.description.Tags,
                value => value.GetType().Name == "ConfigurationManagerAttributes");
            if (tag == null)
            {
                Fail(setting.displayName, "missing display metadata");
                continue;
            }

            Type type = tag.GetType();
            Equal<object>(setting.category, type.GetField("Category")?.GetValue(tag),
                $"{setting.displayName} category");
            Equal<object>(setting.displayName, type.GetField("DispName")?.GetValue(tag),
                $"{setting.displayName} display name");
            Equal<object>(setting.order, type.GetField("Order")?.GetValue(tag),
                $"{setting.displayName} display order");
            Equal<object>(false, type.GetField("ShowRangeAsPercent")?.GetValue(tag),
                $"{setting.displayName} displays levels instead of percentages");
        }
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

    private static void TestSkillScrollExpSliders()
    {
        int order = 600;
        foreach (char tier in "FEDCBAS")
        {
            ConfigDescription description = SkillScrollService.CreateSkillScrollExpDescription(tier, order);
            if (description.AcceptableValues is not AcceptableValueRange<int> range)
            {
                Fail($"skill scroll EXP {tier} slider", "expected an integer range");
                continue;
            }

            Equal(0, range.MinValue, $"skill scroll EXP {tier} minimum");
            Equal(100, range.MaxValue, $"skill scroll EXP {tier} maximum");
            Equal<object>(34, range.Clamp(34), $"skill scroll EXP {tier} in-range values stay unchanged");
            Equal<object>(0, range.Clamp(-1), $"skill scroll EXP {tier} negative values clamp to zero");
            Equal<object>(100, range.Clamp(101), $"skill scroll EXP {tier} values above 100 clamp");

            object tag = Array.Find(description.Tags, value => value.GetType().Name == "ConfigurationManagerAttributes");
            if (tag == null)
            {
                Fail($"skill scroll EXP {tier} slider", "missing display metadata");
                continue;
            }
            Type type = tag.GetType();
            Equal<object>(ConfigurationManagerDisplay.Skill, type.GetField("Category")?.GetValue(tag),
                $"skill scroll EXP {tier} remains in Skill section");
            Equal<object>($"Skill Scroll EXP {tier}", type.GetField("DispName")?.GetValue(tag),
                $"skill scroll EXP {tier} display name");
            Equal<object>(order, type.GetField("Order")?.GetValue(tag), $"skill scroll EXP {tier} order");
            Equal<object>(false, type.GetField("ShowRangeAsPercent")?.GetValue(tag),
                $"skill scroll EXP {tier} displays EXP rather than percent");
            order -= 10;
        }
    }

    private static void TestScrollCombineModes()
    {
        Type modeType = typeof(ScrollCombineService.CombineMode);
        Equal("Off,Row3,Cross5", string.Join(",", Enum.GetNames(modeType)),
            "combine dropdown contains only the unified modes in display order");

        (ScrollCombineService.CombineMode mode, string label, bool enabled, string markup)[] cases =
        {
            (ScrollCombineService.CombineMode.Off, "Off", false, ""),
            (ScrollCombineService.CombineMode.Row3, "Three in a row", true, "<color=yellow><b>-</b></color>"),
            (ScrollCombineService.CombineMode.Cross5, "Five in a cross", true, "<color=yellow><b>+</b></color>")
        };
        foreach (var example in cases)
        {
            var field = modeType.GetField(example.mode.ToString());
            var attributes = field?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes == null || attributes.Length != 1)
            {
                Fail($"combine mode {example.mode} label", "expected one display description");
            }
            else
            {
                Equal(example.label, ((DescriptionAttribute)attributes[0]).Description,
                    $"combine mode {example.mode} dropdown label");
            }

            Equal(example.enabled, ScrollCombineService.IsCombineEnabled(example.mode),
                $"combine mode {example.mode} availability");
            Equal(example.markup, ScrollCombineService.GetCombineShapeMarkup(example.mode),
                $"combine mode {example.mode} tooltip shape");
            Equal(example.mode, (ScrollCombineService.CombineMode)Enum.Parse(modeType, example.mode.ToString()),
                $"combine mode {example.mode} is a valid cfg value");
        }

        foreach (int invalid in new[] { int.MinValue, -1, 3, int.MaxValue })
        {
            var mode = (ScrollCombineService.CombineMode)invalid;
            False(ScrollCombineService.IsCombineEnabled(mode),
                $"undefined combine mode {invalid} disables combining");
            Equal("", ScrollCombineService.GetCombineShapeMarkup(mode),
                $"undefined combine mode {invalid} has no tooltip shape");
        }
    }

    private static void TestScrollCombineModeMetadata()
    {
        ConfigDescription description = ScrollCombineService.CreateCombineModeDescription();
        Equal<object>(null, description.AcceptableValues,
            "combine mode uses an enum dropdown rather than a numeric range");

        object tag = Array.Find(description.Tags, value => value.GetType().Name == "ConfigurationManagerAttributes");
        if (tag == null)
        {
            Fail("combine mode metadata", "missing display metadata");
            return;
        }

        Type type = tag.GetType();
        Equal<object>(ConfigurationManagerDisplay.Scrolls, type.GetField("Category")?.GetValue(tag),
            "combine mode remains in Scrolls section");
        Equal<object>("Combine Mode", type.GetField("DispName")?.GetValue(tag),
            "combine mode replaces the two previous display entries");
        Equal<object>(940, type.GetField("Order")?.GetValue(tag),
            "combine mode preserves its position after scroll drop settings");
    }

    private static void TestExpandWorldDataReloadMethodSelection()
    {
        AssertMethodSignatures(
            typeof(CurrentExpandWorldBiomeManager),
            new[] { "NamesFromFile:0:closed", "ReadConfigs:0:closed", "FromSetting:1:closed", "SetNames:1:closed" },
            "current Expand World Data reload hooks");
        AssertMethodSignatures(
            typeof(UnsupportedExpandWorldBiomeManager),
            Array.Empty<string>(),
            "unsupported Expand World Data reload hooks");
    }

    private static void TestPanelPositionMath()
    {
        Equal(0f, PanelPositionMath.SanitizeOffset(float.NaN), "NaN panel offset resets to zero");
        Equal(0f, PanelPositionMath.SanitizeOffset(float.PositiveInfinity), "positive infinite panel offset resets to zero");
        Equal(0f, PanelPositionMath.SanitizeOffset(float.NegativeInfinity), "negative infinite panel offset resets to zero");
        Equal(123.5f, PanelPositionMath.SanitizeOffset(123.5f), "positive finite panel offset is preserved");
        Equal(-123.5f, PanelPositionMath.SanitizeOffset(-123.5f), "negative finite panel offset is preserved");
        Equal(float.MaxValue, PanelPositionMath.SanitizeOffset(float.MaxValue), "large finite panel offset is not arbitrarily capped");

        Equal(0f, PanelPositionMath.ClampOffset(0f, -100f, 100f, -80f, 80f, -500f, 500f),
            "default panel position stays unchanged when it fits");
        Equal(125f, PanelPositionMath.ClampOffset(125f, -100f, 100f, -80f, 80f, -500f, 500f),
            "in-bounds positive offset stays unchanged");
        Equal(-125f, PanelPositionMath.ClampOffset(-125f, -100f, 100f, -80f, 80f, -500f, 500f),
            "in-bounds negative offset stays unchanged");
        Equal(400f, PanelPositionMath.ClampOffset(900f, -100f, 100f, -80f, 80f, -500f, 500f),
            "positive offset clamps the panel right edge");
        Equal(-400f, PanelPositionMath.ClampOffset(-900f, -100f, 100f, -80f, 80f, -500f, 500f),
            "negative offset clamps the panel left edge");
        Equal(400f, PanelPositionMath.ClampOffset(400f, -100f, 100f, -80f, 80f, -500f, 500f),
            "exact positive boundary is preserved");
        Equal(-400f, PanelPositionMath.ClampOffset(-400f, -100f, 100f, -80f, 80f, -500f, 500f),
            "exact negative boundary is preserved");
        Equal(0f, PanelPositionMath.ClampOffset(100f, -500f, 500f, -80f, 80f, -500f, 500f),
            "panel exactly as wide as viewport stays fully visible");

        Equal(420f, PanelPositionMath.ClampOffset(900f, -600f, 600f, -80f, 80f, -500f, 500f),
            "oversized panel clamps by the handle right edge");
        Equal(-420f, PanelPositionMath.ClampOffset(-900f, -600f, 600f, -80f, 80f, -500f, 500f),
            "oversized panel clamps by the handle left edge");
        Equal(50f, PanelPositionMath.ClampOffset(50f, -600f, 600f, -80f, 80f, -500f, 500f),
            "oversized panel allows movement while its handle remains reachable");
        Equal(-50f, PanelPositionMath.ClampOffset(900f, -900f, 900f, -600f, 700f, -500f, 500f),
            "oversized off-center handle is centered regardless of requested offset");
        Equal(0f, PanelPositionMath.ClampOffset(-900f, -900f, 900f, -700f, 700f, -500f, 500f),
            "oversized centered handle stays centered for negative requests");

        Equal(40f, PanelPositionMath.ClampOffset(100f, 700f, 900f, 710f, 890f, 20f, 940f),
            "off-center authored panel uses its actual right edge");
        Equal(-680f, PanelPositionMath.ClampOffset(-900f, 700f, 900f, 710f, 890f, 20f, 940f),
            "off-center authored panel uses its actual left edge");
        Equal(-20f, PanelPositionMath.ClampOffset(0f, -300f, 1100f, 900f, 960f, 20f, 940f),
            "oversized panel brings an off-screen handle back into view");
        Equal(200f, PanelPositionMath.ClampOffset(350f, -100f, 100f, -80f, 80f, -300f, 300f),
            "smaller viewport temporarily limits a saved offset");
        Equal(350f, PanelPositionMath.ClampOffset(350f, -100f, 100f, -80f, 80f, -500f, 500f),
            "larger viewport restores the same original saved offset");

        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Equal(0f, PanelPositionMath.ClampOffset(invalid, -100f, 100f, -80f, 80f, -500f, 500f),
                $"nonfinite requested offset {invalid} safely uses the default");
            for (int index = 0; index < 6; index++)
            {
                float[] bounds = { -100f, 100f, -80f, 80f, -500f, 500f };
                bounds[index] = invalid;
                Equal(0f, PanelPositionMath.ClampOffset(125f, bounds[0], bounds[1], bounds[2], bounds[3], bounds[4], bounds[5]),
                    $"nonfinite bound {index} ({invalid}) safely returns zero");
            }
        }

        Equal(0f, PanelPositionMath.ClampOffset(125f, 100f, -100f, -80f, 80f, -500f, 500f),
            "reversed panel bounds safely return zero");
        Equal(0f, PanelPositionMath.ClampOffset(125f, -100f, 100f, 80f, -80f, -500f, 500f),
            "reversed handle bounds safely return zero");
        Equal(0f, PanelPositionMath.ClampOffset(125f, -100f, 100f, -80f, 80f, 500f, -500f),
            "reversed viewport bounds safely return zero");
        Equal(float.MaxValue, PanelPositionMath.ClampOffset(0f, -float.MaxValue, -float.MaxValue,
                -float.MaxValue, -float.MaxValue, float.MaxValue, float.MaxValue),
            "extreme positive translation remains representable and finite");
        Equal(-float.MaxValue, PanelPositionMath.ClampOffset(0f, float.MaxValue, float.MaxValue,
                float.MaxValue, float.MaxValue, -float.MaxValue, -float.MaxValue),
            "extreme negative translation remains representable and finite");
    }

    private static void TestEnchantmentAnimationTiming()
    {
        Equal(1f, EnchantmentAnimationTiming.DefaultDuration, "animation duration defaults to one second");
        Equal(0f, EnchantmentAnimationTiming.NormalizeDuration(0f), "zero duration enables instant enchantment");
        Equal(0f, EnchantmentAnimationTiming.NormalizeDuration(-0f), "negative zero duration enables instant enchantment");
        Equal(0f, EnchantmentAnimationTiming.NormalizeDuration(-0.5f), "negative duration clamps to zero");
        Equal(0f, EnchantmentAnimationTiming.NormalizeDuration(-float.MaxValue), "large negative duration clamps to zero");
        Equal(0f, EnchantmentAnimationTiming.NormalizeDuration(float.NegativeInfinity), "negative infinite duration clamps to zero");
        Equal(float.Epsilon, EnchantmentAnimationTiming.NormalizeDuration(float.Epsilon), "smallest positive duration is preserved");
        Equal(0.25f, EnchantmentAnimationTiming.NormalizeDuration(0.25f), "fractional duration is preserved");
        Equal(1f, EnchantmentAnimationTiming.NormalizeDuration(1f), "one-second duration is preserved");
        Equal(1f, EnchantmentAnimationTiming.NormalizeDuration(3f), "previous integer duration above one clamps to one second");
        Equal(1f, EnchantmentAnimationTiming.NormalizeDuration(float.MaxValue), "large positive duration clamps to one second");
        Equal(1f, EnchantmentAnimationTiming.NormalizeDuration(float.PositiveInfinity), "positive infinite duration clamps to one second");
        Equal(1f, EnchantmentAnimationTiming.NormalizeDuration(float.NaN), "NaN duration uses the one-second default");

        Equal(1f, EnchantmentAnimationTiming.GetProgress(0f, 0f), "instant animation is complete without division by zero");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(1f, 0f), "instant animation ignores positive remaining time");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(float.NaN, 0f), "instant animation ignores NaN remaining time");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(float.PositiveInfinity, 0f), "instant animation ignores infinite remaining time");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(1f, -1f), "negative duration normalizes to an instant animation");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(1f, 1f), "animation starts with zero progress");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(0f, 1f), "animation completes with no remaining time");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(-float.Epsilon, 1f), "elapsed animation clamps to full progress");
        Equal(1f, EnchantmentAnimationTiming.GetProgress(float.NegativeInfinity, 1f), "negative infinite remaining time clamps to full progress");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(2f, 1f), "remaining time above duration clamps to zero progress");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(float.PositiveInfinity, 1f), "positive infinite remaining time clamps to zero progress");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(float.NaN, 1f), "NaN remaining time safely returns zero progress");
        Near(0.5f, EnchantmentAnimationTiming.GetProgress(0.25f, 0.5f), 0.0001f,
            "fractional duration uses fractional remaining time");
        Near(0.75f, EnchantmentAnimationTiming.GetProgress(0.05f, 0.2f), 0.0001f,
            "fractional duration computes normalized progress");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(float.Epsilon, float.Epsilon),
            "smallest positive duration starts with finite zero progress");
        Equal(0f, EnchantmentAnimationTiming.GetProgress(float.MaxValue, float.Epsilon),
            "extreme remaining-time ratio safely clamps to zero progress");
        Near(0.5f, EnchantmentAnimationTiming.GetProgress(0.5f, 3f), 0.0001f,
            "progress uses the normalized previous integer duration");
        Near(0.5f, EnchantmentAnimationTiming.GetProgress(0.5f, float.NaN), 0.0001f,
            "progress uses the default for NaN duration");
        Near(0.5f, EnchantmentAnimationTiming.GetProgress(0.5f, float.PositiveInfinity), 0.0001f,
            "progress uses one second for positive infinite duration");

        float[] boundaries =
        {
            float.NegativeInfinity, -float.MaxValue, -1f, -float.Epsilon, -0f,
            0f, float.Epsilon, 0.25f, 0.5f, 1f, 3f, float.MaxValue, float.PositiveInfinity, float.NaN
        };
        foreach (float duration in boundaries)
        {
            float normalized = EnchantmentAnimationTiming.NormalizeDuration(duration);
            True(!float.IsNaN(normalized) && !float.IsInfinity(normalized) && normalized >= 0f && normalized <= 1f,
                $"normalized duration {duration} is finite and within slider bounds");
            foreach (float remaining in boundaries)
            {
                float progress = EnchantmentAnimationTiming.GetProgress(remaining, duration);
                True(!float.IsNaN(progress) && !float.IsInfinity(progress) && progress >= 0f && progress <= 1f,
                    $"progress is finite and normalized for remaining {remaining}, duration {duration}");
            }
        }
    }

    private static void AssertMethodSignatures(Type managerType, IReadOnlyList<string> expectedSignatures, string name)
    {
        List<string> actualSignatures = new();
        foreach (var method in ExpandWorldDataIntegration.FindBiomeReloadTargetMethods(managerType))
        {
            actualSignatures.Add($"{method.Name}:{method.GetParameters().Length}:{(method.ContainsGenericParameters ? "generic" : "closed")}");
        }

        Equal(string.Join(",", expectedSignatures), string.Join(",", actualSignatures), name);
    }

    private sealed class CurrentExpandWorldBiomeManager
    {
        public static void NamesFromFile() { }
        private static void ReadConfigs() { }
        private static void ReadConfigs(string yaml) { }
        internal static void FromSetting(string yaml) { }
        internal static void FromSetting<T>(T yaml) { }
        public static void SetNames(object names) { }
        public static int SetNames() => 0;
        public static void Unrelated() { }
    }

    private sealed class UnsupportedExpandWorldBiomeManager
    {
        public static void Unrelated() { }
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
