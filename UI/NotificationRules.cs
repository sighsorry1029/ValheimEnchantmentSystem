using System;

namespace kg.ValheimEnchantmentSystem.UI;

internal static class NotificationRules
{
    internal const float DisplayDurationSeconds = 5f;

    internal static bool MeetsMinimumLevel(int resultLevel, int minimumLevel) =>
        resultLevel >= Math.Max(0, minimumLevel);

    internal static bool ShouldShowNotification(int type, int resultLevel, int minimumLevel, Notifications_UI.Filter filter)
    {
        if (!MeetsMinimumLevel(resultLevel, minimumLevel))
        {
            return false;
        }

        return (Notifications_UI.NotificationItemResult)type switch
        {
            Notifications_UI.NotificationItemResult.Success => (filter & Notifications_UI.Filter.Success) != 0,
            Notifications_UI.NotificationItemResult.LevelDecrease or Notifications_UI.NotificationItemResult.Destroyed =>
                (filter & Notifications_UI.Filter.Fail) != 0,
            _ => false
        };
    }
}
