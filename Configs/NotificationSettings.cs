namespace kg.ValheimEnchantmentSystem.Configs;

internal static class NotificationSettings
{
    internal const int DefaultMinimumLevel = 6;

    internal static ConfigDescription CreateWebhookMinimumLevelDescription()
    {
        return ConfigurationManagerDisplay.Description(
            "Minimum resulting enchantment level for Discord webhook delivery. Only the server's value is used; it is not synchronized to clients. Does not restrict in-game notifications or event forwarding. For destruction, the pre-destruction level is used. Set to 0 to include all levels; leave webhook URLs empty to disable webhook delivery.",
            ConfigurationManagerDisplay.General,
            850,
            "Webhook Minimum Enchant Level",
            new AcceptableValueRange<int>(0, 500),
            showRangeAsPercent: false);
    }

    internal static ConfigDescription CreateNotificationMinimumLevelDescription()
    {
        return ConfigurationManagerDisplay.Description(
            "Minimum resulting enchantment level for in-game notifications on this client only. Not synchronized with the server. Does not affect other players, event forwarding, or webhook delivery. For destruction, the pre-destruction level is used. Set to 0 to include all levels; Notifications - Filter = None hides all in-game notifications. The host can set this independently from Webhook Minimum Enchant Level.",
            ConfigurationManagerDisplay.Client,
            910,
            "Notification Minimum Enchant Level",
            new AcceptableValueRange<int>(0, 100),
            showRangeAsPercent: false);
    }
}
