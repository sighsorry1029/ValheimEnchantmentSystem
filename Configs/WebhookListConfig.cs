namespace kg.ValheimEnchantmentSystem.Configs;

internal static class WebhookListConfig
{
    internal static IReadOnlyList<string> ParseTargets(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw!
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(target => target.Trim())
            .Where(target => target.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
