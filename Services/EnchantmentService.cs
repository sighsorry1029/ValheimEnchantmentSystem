using ItemDataManager;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.UI;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem;

public sealed class EnchantmentResult
{
    public Enchantment_Core.Enchanted Enchantment = null!;
    public Player Player = null!;
    public string ItemPrefabName = string.Empty;
    public string Message = string.Empty;
    public int PreviousLevel;
    public int CurrentLevel;
    public float SkillExpGranted;
    public bool Success;
    public bool Destroyed;
    public bool ConsumedRequirement;
    public bool LevelChanged;
    public Notifications_UI.NotificationItemResult? NotificationType;
}

internal enum EnchantmentOutcome
{
    Success,
    LevelDecrease,
    Destroyed,
    NoChange
}

internal readonly struct EnchantmentDecision
{
    public readonly EnchantmentOutcome Outcome;
    public readonly int NewLevel;
    public readonly Notifications_UI.NotificationItemResult NotificationType;

    public EnchantmentDecision(EnchantmentOutcome outcome, int newLevel, Notifications_UI.NotificationItemResult notificationType)
    {
        Outcome = outcome;
        NewLevel = newLevel;
        NotificationType = notificationType;
    }
}

internal readonly struct EnchantmentRulePreview
{
    public readonly double BaseChance;
    public readonly double SkillBonus;
    public readonly double BlessBonus;
    public readonly double FinalChance;
    public readonly double DestroyChance;
    public readonly bool CanBreak;
    public readonly bool BlessPreventsBreak;
    public readonly SyncedData.ItemDesctructionTypeEnum FailureType;

    public EnchantmentRulePreview(
        double baseChance,
        double skillBonus,
        double blessBonus,
        double finalChance,
        double destroyChance,
        bool canBreak,
        bool blessPreventsBreak,
        SyncedData.ItemDesctructionTypeEnum failureType)
    {
        BaseChance = baseChance;
        SkillBonus = skillBonus;
        BlessBonus = blessBonus;
        FinalChance = finalChance;
        DestroyChance = destroyChance;
        CanBreak = canBreak;
        BlessPreventsBreak = blessPreventsBreak;
        FailureType = failureType;
    }
}

public static class EnchantmentService
{
    public static EnchantmentResult Execute(Enchantment_Core.Enchanted enchantment, Player player, bool useBlessedScroll, bool blessedScrollPreventsBreak)
    {
        EnchantmentResult result = CreateResult(enchantment, player);
        if (enchantment?.Item == null || player == null)
        {
            result.Message = "$enchantment_cannotbe".Localize();
            return result;
        }

        ItemDrop.ItemData item = enchantment.Item;
        if (!EnchantmentDomainHelper.CanEnchant(item, enchantment.level, result.ItemPrefabName))
        {
            result.Message = "$enchantment_cannotbe".Localize();
            return result;
        }

        if (!TryResolveRequirement(item, result.ItemPrefabName, useBlessedScroll, out SyncedData.EnchantmentReqs reqs, out GameObject requirementPrefab))
        {
            result.Message = "$enchantment_nomaterials".Localize();
            return result;
        }

        if (!TryConsumeRequirement(requirementPrefab.name))
        {
            result.Message = "$enchantment_nomaterials".Localize();
            return result;
        }

        result.ConsumedRequirement = true;
        float successfulAttemptSkillExp = GetSkillExp(reqs);

        EnchantmentDecision decision = EnchantmentRules.Decide(enchantment, player, useBlessedScroll, blessedScrollPreventsBreak);
        ApplyDecision(enchantment, player, decision, result);
        result.SkillExpGranted = GetGrantedSkillExp(successfulAttemptSkillExp, result.Success);
        result.Message = BuildMessage(decision.Outcome, item.m_shared.m_name.Localize(), result.PreviousLevel, result.CurrentLevel);
        return result;
    }

    private static EnchantmentResult CreateResult(Enchantment_Core.Enchanted enchantment, Player player)
    {
        int previousLevel = enchantment?.level ?? 0;
        return new EnchantmentResult
        {
            Enchantment = enchantment,
            Player = player,
            ItemPrefabName = EnchantmentDomainHelper.ResolveItemPrefabName(enchantment?.Item),
            PreviousLevel = previousLevel,
            CurrentLevel = previousLevel
        };
    }

    private static bool TryResolveRequirement(ItemDrop.ItemData item, string itemPrefabName, bool useBlessedScroll, out SyncedData.EnchantmentReqs reqs, out GameObject requirementPrefab)
    {
        reqs = null!;
        requirementPrefab = null!;

        if (item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return false;
        }

        reqs = SyncedData.GetReqs(itemPrefabName);
        if (reqs == null)
        {
            return false;
        }

        SyncedData.SingleReq selectedRequirement = useBlessedScroll ? reqs.blessed_enchant_prefab : reqs.enchant_prefab;
        if (selectedRequirement == null || !selectedRequirement.IsValid())
        {
            return false;
        }

        requirementPrefab = ZNetScene.instance?.GetPrefab(selectedRequirement.prefab);
        return requirementPrefab != null;
    }

    private static bool TryConsumeRequirement(string requirementPrefabName)
    {
        if (string.IsNullOrWhiteSpace(requirementPrefabName))
        {
            return false;
        }

        if (Utils.CustomCountItemsNoLevel(requirementPrefabName) < 1)
        {
            return false;
        }

        Utils.CustomRemoveItemsNoLevel(requirementPrefabName, 1);
        return true;
    }

    private static float GetSkillExp(SyncedData.EnchantmentReqs reqs)
    {
        if (reqs?.enchant_prefab == null || !reqs.enchant_prefab.IsValid())
        {
            return 0;
        }

        return EnchantmentTierCatalog.GetEnchantSkillExp(reqs.enchant_prefab.prefab);
    }

    private static float GetGrantedSkillExp(float successfulAttemptSkillExp, bool success)
    {
        if (successfulAttemptSkillExp <= 0f)
        {
            return 0f;
        }

        if (success)
        {
            return successfulAttemptSkillExp;
        }

        float multiplier = Mathf.Clamp(SyncedData.FailedEnchantSkillExpMultiplier.Value, 0f, 2f);
        return successfulAttemptSkillExp * multiplier;
    }

    private static void ApplyDecision(Enchantment_Core.Enchanted enchantment, Player player, EnchantmentDecision decision, EnchantmentResult result)
    {
        result.Success = decision.Outcome == EnchantmentOutcome.Success;
        result.Destroyed = decision.Outcome == EnchantmentOutcome.Destroyed;
        result.NotificationType = decision.NotificationType;

        if (decision.Outcome == EnchantmentOutcome.Destroyed)
        {
            if (player != null)
            {
                player.UnequipItem(enchantment.Item);
                player.m_inventory.RemoveItem(enchantment.Item);
            }

            result.CurrentLevel = result.PreviousLevel;
            return;
        }

        enchantment.level = decision.NewLevel;
        enchantment.Save();
        result.CurrentLevel = enchantment.level;
        result.LevelChanged = result.CurrentLevel != result.PreviousLevel;
    }

    private static string BuildMessage(EnchantmentOutcome outcome, string itemName, int previousLevel, int currentLevel)
    {
        return outcome switch
        {
            EnchantmentOutcome.Success => "$enchantment_success".Localize(itemName, previousLevel.ToString(), currentLevel.ToString()),
            EnchantmentOutcome.LevelDecrease => "$enchantment_fail_leveldown".Localize(itemName, previousLevel.ToString(), currentLevel.ToString()),
            EnchantmentOutcome.Destroyed => "$enchantment_fail_destroyed".Localize(itemName),
            _ => "$enchantment_fail_nochange".Localize(itemName, currentLevel.ToString())
        };
    }

}

internal static class EnchantmentRules
{
    public static EnchantmentRulePreview BuildPreview(ItemDrop.ItemData item, string itemPrefabName, int currentLevel, Player player, bool useBlessedScroll, bool blessedScrollPreventsBreak)
    {
        SyncedData.ItemDesctructionTypeEnum failureType = SyncedData.ItemFailureType.Value;
        if (item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return new EnchantmentRulePreview(0d, 0d, 0d, 0d, 0d, false, false, failureType);
        }

        SyncedData.Chance_Data chanceData = SyncedData.GetEnchantmentChance(itemPrefabName, currentLevel, item.IsWeapon());
        double baseChance = chanceData.success;
        double skillBonus = EnchantmentSkillBonusService.GetAdditionalEnchantmentChance(player);
        double blessBonus = useBlessedScroll && !blessedScrollPreventsBreak ? SyncedData.BlessedScrollsAdditionalChance.Value : 0d;
        double finalChance = NormalizePercentChance(baseChance + skillBonus + blessBonus);
        bool blessPreventsBreak = useBlessedScroll && blessedScrollPreventsBreak && SyncedData.SafetyLevel.Value <= currentLevel;
        bool canBreak = SyncedData.SafetyLevel.Value <= currentLevel && !blessPreventsBreak;
        double destroyChance = canBreak ? NormalizePercentChance(ResolveDestroyChance(chanceData, useBlessedScroll, failureType)) : 0d;

        return new EnchantmentRulePreview(baseChance, skillBonus, blessBonus, finalChance, destroyChance, canBreak, blessPreventsBreak, failureType);
    }

    public static EnchantmentDecision Decide(Enchantment_Core.Enchanted enchantment, Player player, bool useBlessedScroll, bool blessedScrollPreventsBreak)
    {
        int currentLevel = enchantment.level;
        EnchantmentRulePreview preview = BuildPreview(
            enchantment.Item,
            EnchantmentDomainHelper.ResolveItemPrefabName(enchantment.Item),
            currentLevel,
            player,
            useBlessedScroll,
            blessedScrollPreventsBreak);
        if (RollPercent(preview.FinalChance))
        {
            return new EnchantmentDecision(EnchantmentOutcome.Success, currentLevel + 1, Notifications_UI.NotificationItemResult.Success);
        }

        if (!preview.CanBreak)
        {
            return new EnchantmentDecision(EnchantmentOutcome.NoChange, currentLevel, Notifications_UI.NotificationItemResult.LevelDecrease);
        }

        return DecideFailure(
            preview,
            currentLevel,
            Random.Range(0f, 100f),
            Mathf.Clamp(SyncedData.FailedEnchantLevelDecrease.Value, 1, 100));
    }

    internal static EnchantmentDecision DecideFailure(EnchantmentRulePreview preview, int currentLevel, double destroyRoll, int levelDecrease)
    {
        bool destroy = IsRollSuccessful(preview.DestroyChance, destroyRoll);
        return preview.FailureType switch
        {
            SyncedData.ItemDesctructionTypeEnum.Destroy => new EnchantmentDecision(EnchantmentOutcome.Destroyed, currentLevel, Notifications_UI.NotificationItemResult.Destroyed),
            SyncedData.ItemDesctructionTypeEnum.Combined => destroy
                ? new EnchantmentDecision(EnchantmentOutcome.Destroyed, currentLevel, Notifications_UI.NotificationItemResult.Destroyed)
                : CreateLevelDecreaseDecision(currentLevel, levelDecrease),
            SyncedData.ItemDesctructionTypeEnum.CombinedEasy => destroy
                ? CreateLevelDecreaseDecision(currentLevel, levelDecrease)
                : new EnchantmentDecision(EnchantmentOutcome.NoChange, currentLevel, Notifications_UI.NotificationItemResult.LevelDecrease),
            _ => CreateLevelDecreaseDecision(currentLevel, levelDecrease)
        };
    }

    private static EnchantmentDecision CreateLevelDecreaseDecision(int currentLevel, int levelDecrease)
    {
        int decrease = Math.Min(100, Math.Max(1, levelDecrease));
        return new EnchantmentDecision(
            EnchantmentOutcome.LevelDecrease,
            Math.Max(0, currentLevel - decrease),
            Notifications_UI.NotificationItemResult.LevelDecrease);
    }

    private static double ResolveDestroyChance(SyncedData.Chance_Data chanceData, bool useBlessedScroll, SyncedData.ItemDesctructionTypeEnum failureType)
    {
        return failureType switch
        {
            SyncedData.ItemDesctructionTypeEnum.Destroy => 100d,
            SyncedData.ItemDesctructionTypeEnum.Combined => Math.Min(100d, Math.Max(0d, chanceData.destroy - (useBlessedScroll ? SyncedData.BlessedScrollsAdditionalChance.Value : 0))),
            SyncedData.ItemDesctructionTypeEnum.CombinedEasy => Math.Min(100d, Math.Max(0d, chanceData.destroy - (useBlessedScroll ? SyncedData.BlessedScrollsAdditionalChance.Value : 0))),
            _ => 0d
        };
    }

    internal static double NormalizePercentChance(double chance)
    {
        return Math.Round(Math.Min(100d, Math.Max(0d, chance)), 2, MidpointRounding.AwayFromZero);
    }

    private static bool RollPercent(double chance)
    {
        return IsRollSuccessful(chance, Random.Range(0f, 100f));
    }

    internal static bool IsRollSuccessful(double chance, double roll)
    {
        double normalizedChance = NormalizePercentChance(chance);
        if (normalizedChance <= 0d)
        {
            return false;
        }

        if (normalizedChance >= 100d)
        {
            return true;
        }

        return roll < normalizedChance;
    }
}

public static class EnchantmentSideEffects
{
    public static void ApplyAttemptResult(EnchantmentResult result)
    {
        if (result == null)
        {
            return;
        }

        Enchantment_VFX.UpdateGrid();

        if (result.LevelChanged && !result.Destroyed)
        {
            if (ValheimEnchantmentSystem._thistype != null && result.Enchantment?.Item != null)
            {
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(result.Enchantment.Item));
            }
        }

        if (result.SkillExpGranted > 0)
        {
            Utils.IncreaseSkillEXP(Enchantment_Skill.SkillType_Enchantment, result.SkillExpGranted);
        }

        if (result.NotificationType.HasValue &&
            SyncedData.EnchantmentEnableNotifications.Value &&
            SyncedData.EnchantmentNotificationMinLevel.Value <= result.CurrentLevel)
        {
            string playerName = result.Player != null ? result.Player.GetPlayerName() : "No Name";
            Notifications_UI.AddNotification(playerName, result.ItemPrefabName, (int)result.NotificationType.Value, result.PreviousLevel, result.CurrentLevel);
        }
    }

    public static void HandleUpgrade(Enchantment_Core.Enchanted enchantment)
    {
        if (ValheimEnchantmentSystem._thistype == null || enchantment == null)
        {
            return;
        }

        ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
        {
            if (SyncedData.DropEnchantmentOnUpgrade.Value)
            {
                enchantment.Item?.Data().Remove<Enchantment_Core.Enchanted>();
                Enchantment_VFX.UpdateGrid();
                return;
            }

            Enchantment_VFX.UpdateGrid();
        }, 1);
    }

    public static void ApplyStateChanged(Enchantment_Core.Enchanted enchantment, bool refreshEquipment)
    {
        if (enchantment == null)
        {
            return;
        }

        Enchantment_VFX.UpdateGrid();

        if (refreshEquipment && ValheimEnchantmentSystem._thistype != null && enchantment.Item != null)
        {
            ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(enchantment.Item));
        }
    }
}
