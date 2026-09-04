using System;

namespace kg.ValheimEnchantmentSystem;

internal static class EnchantmentSkillExperience
{
    internal static float CalculateSuccessfulAttemptExp(
        int currentLevel,
        double baseChance,
        double finalChance,
        float baseExp,
        float expPerLevel,
        float difficultyBonus)
    {
        if (currentLevel < 0 || !IsFinite(baseChance) || !IsFinite(finalChance) || finalChance <= 0d ||
            !IsFinite(baseExp) || !IsFinite(expPerLevel) || !IsFinite(difficultyBonus))
        {
            return 0f;
        }

        double difficulty = 1d - Math.Min(100d, Math.Max(0d, baseChance)) / 100d;
        // Current level is target level - 1, captured before any success or level decrease.
        double exp = Math.Max(0d, baseExp)
                     + Math.Max(0d, expPerLevel) * currentLevel
                     + Math.Max(0d, difficultyBonus) * difficulty;
        return (float)Math.Min(float.MaxValue, exp);
    }

    internal static float CalculateGrantedExp(float successfulAttemptExp, bool success, float failedMultiplier)
    {
        if (!IsFinite(successfulAttemptExp) || successfulAttemptExp <= 0f)
        {
            return 0f;
        }

        if (success)
        {
            return successfulAttemptExp;
        }

        if (!IsFinite(failedMultiplier))
        {
            return 0f;
        }

        double multiplier = Math.Min(2d, Math.Max(0d, failedMultiplier));
        return (float)Math.Min(float.MaxValue, successfulAttemptExp * multiplier);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
