namespace kg.ValheimEnchantmentSystem;

internal static class EnchantmentMaterialRules
{
    internal static bool Matches(string? actualPrefab, string requiredPrefab, int stack) =>
        stack > 0 && !string.IsNullOrEmpty(requiredPrefab) &&
        string.Equals(actualPrefab, requiredPrefab, StringComparison.Ordinal);

    internal static int AvailableInContainer(int count, bool leaveOne) =>
        (int)Math.Max(0L, (long)count - (leaveOne ? 1L : 0L));

    internal static int AddCounts(int first, int second) =>
        (int)Math.Min(int.MaxValue, (long)Math.Max(0, first) + Math.Max(0, second));

    internal static bool TryConsumeOne(Func<bool> consumeInventory,
        Func<IEnumerable<Func<bool>>> containerConsumers)
    {
        if (consumeInventory()) return true;
        foreach (Func<bool> consume in containerConsumers())
            if (consume()) return true;
        return false;
    }
}
