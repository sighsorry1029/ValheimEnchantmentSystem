using LocalizationManager;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class LocalizationBootstrap
{
    public static void Initialize()
    {
        Localizer.Load();
    }
}
