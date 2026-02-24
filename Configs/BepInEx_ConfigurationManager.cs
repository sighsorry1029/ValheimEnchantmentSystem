using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Configs;

[VES_Autoload]
public static class BepInEx_ConfigurationManager
{
    private static Type activator;

    [UsedImplicitly]
    private static void OnInit()
    {
        // Add a safe-mode toggle to avoid interfering with other mods' ConfigurationManager UI
        var safeMode = ValheimEnchantmentSystem.ClientConfig(
            "Compatibility",
            "ConfigurationManager - Safe Mode",
            true,
            "When enabled, VES will not patch ConfigurationManager internals to avoid any potential conflicts with other mods. Disable only if you need legacy integration.");
        if (safeMode.Value) return;

        Type find = Type.GetType("ConfigurationManager.SettingSearcher, ConfigurationManager");
        if (find == null) return;
        MethodInfo method = AccessTools.Method(find, "GetPluginConfig");
        if (method == null) return;
        activator = Type.GetType("ConfigurationManager.ConfigSettingEntry, ConfigurationManager");
        if (activator == null) return;
        ValheimEnchantmentSystem.Harmony.Patch(method, postfix: new HarmonyMethod(typeof(BepInEx_ConfigurationManager), nameof(Modify)));
    }

    private static void Modify(BaseUnityPlugin plugin, ref IEnumerable<object> __result)
    {
        if (plugin != ValheimEnchantmentSystem._thistype) return;
        __result = plugin.Config.Select(kvp => Activator.CreateInstance(activator, kvp.Value, plugin));
    }
}