namespace kg.ValheimEnchantmentSystem.UI;

internal static class EnchantmentAnimationTiming
{
    internal const float DefaultDuration = 1f;

    internal static float NormalizeDuration(float value)
    {
        if (float.IsNaN(value)) return DefaultDuration;
        return Math.Max(0f, Math.Min(1f, value));
    }

    internal static float GetProgress(float remaining, float duration)
    {
        float seconds = NormalizeDuration(duration);
        if (seconds <= 0f) return 1f;
        if (float.IsNaN(remaining)) return 0f;
        return Math.Max(0f, Math.Min(1f, 1f - remaining / seconds));
    }
}
