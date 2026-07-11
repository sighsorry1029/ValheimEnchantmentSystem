using ItemDataManager;
using System.Globalization;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.UI;

namespace kg.ValheimEnchantmentSystem;

public readonly struct EnchantmentAttemptSummary
{
    public readonly double FinalChancePercent;
    public readonly string ChanceBreakdownText;
    public readonly string FailureBreakdownText;

    public EnchantmentAttemptSummary(double finalChancePercent, string chanceBreakdownText, string failureBreakdownText)
    {
        FinalChancePercent = finalChancePercent;
        ChanceBreakdownText = chanceBreakdownText;
        FailureBreakdownText = failureBreakdownText;
    }
}

public sealed class EnchantmentPreview
{
    public ItemDrop.ItemData Item = null!;
    public string ItemDisplayName = string.Empty;
    public string ItemLabel = string.Empty;
    public string BlockedMessage = string.Empty;
    public Sprite RequirementIcon = null!;
    public bool UseBlessedScroll;
    public bool CanSelect;
    public bool CanAttempt;
    public bool IsMaxedOut;
    public bool HasRequirementDefinition;
    public bool HasRequiredItems;
    public Color TrailColor = new(1f, 1f, 1f, 0.8f);
    public string RequirementStatusText = string.Empty;
    public EnchantmentAttemptSummary AttemptSummary;
    public string NextStatsTooltipText = string.Empty;
}

public static class EnchantmentPreviewService
{
    private static readonly Color DefaultTrailColor = new(1f, 1f, 1f, 0.8f);

    public static EnchantmentPreview GetPreview(ItemDrop.ItemData item, bool useBlessedScroll)
    {
        EnchantmentPreview preview = new()
        {
            Item = item,
            UseBlessedScroll = useBlessedScroll,
            ItemDisplayName = item?.m_shared?.m_name?.Localize() ?? string.Empty,
            RequirementStatusText = "$enchantment_noenchantitems".Localize()
        };

        Enchantment_Core.Enchanted enchantment = item?.Data().Get<Enchantment_Core.Enchanted>();
        int currentLevel = enchantment?.level ?? 0;
        string itemPrefabName = EnchantmentDomainHelper.ResolveItemPrefabName(item);
        string displayColor = ResolveDisplayColor(itemPrefabName, currentLevel);
        preview.ItemLabel = BuildItemLabel(preview.ItemDisplayName, currentLevel, displayColor);
        preview.TrailColor = currentLevel == 0 ? DefaultTrailColor : displayColor.ToColorAlpha();

        if (item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return preview;
        }

        if (SyncedData.GetReqs(itemPrefabName) == null)
        {
            preview.BlockedMessage = "$enchantment_cannotbe".Localize();
            return preview;
        }

        if (!SyncedData.IsLevelEnchantable(itemPrefabName, currentLevel, item.IsWeapon()))
        {
            preview.CanSelect = true;
            preview.IsMaxedOut = true;
            preview.BlockedMessage = "$enchantment_maxedout".Localize();
            preview.RequirementStatusText = preview.BlockedMessage;
            return preview;
        }

        preview.CanSelect = true;
        preview.AttemptSummary = BuildAttemptSummary(item, itemPrefabName, currentLevel, useBlessedScroll);
        PopulateRequirementPreview(preview, itemPrefabName);
        PopulateNextStatsPreview(preview, itemPrefabName, currentLevel);
        preview.CanAttempt = preview.CanSelect && preview.HasRequirementDefinition && preview.HasRequiredItems;
        return preview;
    }

    private static void PopulateRequirementPreview(EnchantmentPreview preview, string itemPrefabName)
    {
        if (preview.Item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return;
        }

        SyncedData.EnchantmentReqs reqs = SyncedData.GetReqs(itemPrefabName);
        if (reqs == null)
        {
            return;
        }

        SyncedData.SingleReq selectedRequirement = preview.UseBlessedScroll ? reqs.blessed_enchant_prefab : reqs.enchant_prefab;
        if (selectedRequirement == null || !selectedRequirement.IsValid())
        {
            return;
        }

        GameObject requirementPrefab = ZNetScene.instance?.GetPrefab(selectedRequirement.prefab);
        if (requirementPrefab?.GetComponent<ItemDrop>() is not { } requirementDrop)
        {
            return;
        }

        preview.HasRequirementDefinition = true;
        preview.RequirementIcon = requirementDrop.m_itemData.GetIcon();
        string requirementDisplayName = requirementDrop.m_itemData.m_shared.m_name.Localize();
        int requirementNeededCount = 1;
        int requirementCount = Utils.CustomCountItemsNoLevel(selectedRequirement.prefab);
        preview.HasRequiredItems = requirementCount >= requirementNeededCount;
        preview.RequirementStatusText = BuildRequirementStatusText(requirementDisplayName, requirementCount, requirementNeededCount);
    }

    private static EnchantmentAttemptSummary BuildAttemptSummary(ItemDrop.ItemData item, string itemPrefabName, int currentLevel, bool useBlessedScroll)
    {
        if (item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return default;
        }

        EnchantmentRulePreview rulePreview = EnchantmentRules.BuildPreview(
            item,
            itemPrefabName,
            currentLevel,
            Player.m_localPlayer,
            useBlessedScroll,
            SyncedData.BlessedScrollsPreventBreak.Value);

        double baseChancePercent = RoundChance(rulePreview.BaseChance);
        double skillBonusChancePercent = RoundChance(rulePreview.SkillBonus);
        double blessBonusChancePercent = RoundChance(rulePreview.BlessBonus);
        double finalChancePercent = RoundChance(rulePreview.FinalChance);
        double destroyChancePercent = RoundChance(rulePreview.DestroyChance);

        return new EnchantmentAttemptSummary(
            finalChancePercent,
            BuildChanceBreakdownText(baseChancePercent, skillBonusChancePercent, blessBonusChancePercent, finalChancePercent),
            BuildFailureBreakdownText(finalChancePercent, destroyChancePercent, rulePreview.CanBreak, rulePreview.BlessPreventsBreak, rulePreview.FailureType));
    }

    private static void PopulateNextStatsPreview(EnchantmentPreview preview, string itemPrefabName, int currentLevel)
    {
        if (preview.Item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return;
        }

        bool isWeapon = preview.Item.IsWeapon();
        SyncedData.Stat_Data currentStats = SyncedData.GetStatIncrease(itemPrefabName, currentLevel, isWeapon);
        SyncedData.Stat_Data nextStats = SyncedData.GetStatIncrease(itemPrefabName, currentLevel + 1, isWeapon);
        string transitionDescription = EnchantmentStatFormatter.BuildTransitionDescription(currentStats, nextStats);
        if (string.IsNullOrWhiteSpace(transitionDescription))
        {
            return;
        }

        preview.NextStatsTooltipText = $"<color=orange>+{currentLevel} > +{currentLevel + 1}</color>\n{transitionDescription}";
    }

    private static string ResolveDisplayColor(string itemPrefabName, int currentLevel)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return "white";
        }

        return SyncedData.GetColor(itemPrefabName, currentLevel, out _, true).IncreaseColorLight();
    }

    private static string BuildItemLabel(string itemDisplayName, int currentLevel, string displayColor)
    {
        return $"{itemDisplayName} (<color={displayColor}>+{currentLevel}</color>)";
    }

    private static string BuildRequirementStatusText(string requirementDisplayName, int requirementCount, int requirementNeededCount)
    {
        return $"{("$enchantment_rule_material".Localize())}: {requirementDisplayName} {requirementCount}/{requirementNeededCount}";
    }

    private static string BuildChanceBreakdownText(double baseChancePercent, double skillBonusChancePercent, double blessBonusChancePercent, double finalChancePercent)
    {
        List<string> breakdown = new()
        {
            $"{FormatPercent(baseChancePercent)}%"
        };

        if (skillBonusChancePercent > 0d)
        {
            breakdown.Add($"{("$enchantment_rule_skill".Localize())}{FormatPercent(skillBonusChancePercent)}%");
        }

        if (blessBonusChancePercent > 0d)
        {
            breakdown.Add($"{("$enchantment_rule_bless".Localize())}{FormatPercent(blessBonusChancePercent)}%");
        }

        return $"{FormatPercent(finalChancePercent)}% ({string.Join(" + ", breakdown)})";
    }

    private static string BuildFailureBreakdownText(double finalChancePercent, double destroyChancePercent, bool canBreak, bool blessPreventsBreak, SyncedData.ItemDesctructionTypeEnum failureType)
    {
        List<string> parts = new();
        double failureChance = 100d - finalChancePercent;
        int levelDecrease = Mathf.Clamp(SyncedData.FailedEnchantLevelDecrease.Value, 1, 100);
        if (RoundChance(failureChance) <= 0d)
        {
            return string.Empty;
        }

        if (blessPreventsBreak || !canBreak)
        {
            AppendFailureChancePart(parts, "stay", failureChance);
            return string.Join("\n", parts);
        }

        switch (failureType)
        {
            case SyncedData.ItemDesctructionTypeEnum.Destroy:
                AppendFailureChancePart(parts, "break", failureChance);
                break;
            case SyncedData.ItemDesctructionTypeEnum.Combined:
                AppendFailureChancePart(parts, "decrease", failureChance * ((100d - destroyChancePercent) / 100d), levelDecrease);
                AppendFailureChancePart(parts, "break", failureChance * (destroyChancePercent / 100d));
                break;
            case SyncedData.ItemDesctructionTypeEnum.CombinedEasy:
                AppendFailureChancePart(parts, "stay", failureChance * ((100d - destroyChancePercent) / 100d));
                AppendFailureChancePart(parts, "decrease", failureChance * (destroyChancePercent / 100d), levelDecrease);
                break;
            default:
                AppendFailureChancePart(parts, "decrease", failureChance, levelDecrease);
                break;
        }

        return string.Join("\n", parts);
    }

    private static double RoundChance(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string FormatPercent(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static void AppendFailureChancePart(List<string> parts, string label, double value, int levelDecrease = 0)
    {
        double rounded = RoundChance(value);
        if (rounded <= 0d)
        {
            return;
        }

        string outcomeLabel = ResolveFailureOutcomeLabel(label);
        if (label == "decrease" && levelDecrease > 0)
        {
            outcomeLabel = $"{outcomeLabel} -{levelDecrease}";
        }

        parts.Add($"{FormatPercent(rounded)}% ({outcomeLabel})");
    }

    private static string ResolveFailureOutcomeLabel(string label)
    {
        return label switch
        {
            "stay" => "$enchantment_preview_stay".Localize(),
            "decrease" => "$enchantment_preview_decrease".Localize(),
            "break" => "$enchantment_preview_break".Localize(),
            _ => label
        };
    }
}
