using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Configs;

[VES_Autoload]
public static class BepInEx_ConfigurationManager
{
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("VES.CMCompat");
    private static readonly BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const int NormalizeCategoriesOrder = -900001;

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private static ConfigEntry<bool> _normalizeConfigManagerCategories = null!;
    private static bool _warnedMissingConfigManager;

    [UsedImplicitly]
    private static void OnInit()
    {
        _normalizeConfigManagerCategories = ValheimEnchantmentSystem.ClientConfig(
            "Compatibility",
            "Normalize ConfigurationManager Categories",
            true,
            new ConfigDescription(
                "When enabled, category tags like [GB_*] or $token are normalized to section names to prevent split sections in ConfigurationManager UI.",
                null,
                new ConfigurationManagerAttributes { Order = NormalizeCategoriesOrder }));
        Harmony harmony = new("kg.ValheimEnchantmentSystem.CMCompat");
        harmony.Patch(
            AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Start)),
            postfix: new HarmonyMethod(typeof(BepInEx_ConfigurationManager), nameof(AfterFejdStartupStart)));

        Type? configurationManagerType = AccessTools.TypeByName("ConfigurationManager.ConfigurationManager");
        MethodInfo? buildSettingList = configurationManagerType == null ? null : AccessTools.Method(configurationManagerType, "BuildSettingList");
        if (buildSettingList != null)
        {
            harmony.Patch(
                buildSettingList,
                prefix: new HarmonyMethod(typeof(BepInEx_ConfigurationManager), nameof(BeforeBuildSettingList)));
        }
        else
        {
            _warnedMissingConfigManager = true;
        }
    }

    [UsedImplicitly]
    private static void AfterFejdStartupStart()
    {
        if (!_normalizeConfigManagerCategories.Value)
        {
            return;
        }

        int changed = NormalizeAllConfigManagerCategories();
        if (changed > 0)
        {
            Log.LogInfo($"Normalized ConfigurationManager category tags. changed={changed}");
        }
    }

    [UsedImplicitly]
    private static void BeforeBuildSettingList()
    {
        if (!_normalizeConfigManagerCategories.Value)
        {
            return;
        }

        int changed = NormalizeAllConfigManagerCategories();
        if (changed > 0)
        {
            Log.LogInfo($"Normalized ConfigurationManager category tags before BuildSettingList. changed={changed}");
        }
    }

    private static int NormalizeAllConfigManagerCategories()
    {
        int changed = 0;
        foreach (PluginInfo pluginInfo in Chainloader.PluginInfos.Values)
        {
            if (pluginInfo.Instance is not BaseUnityPlugin plugin)
            {
                continue;
            }

            foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> kvp in plugin.Config)
            {
                ConfigEntryBase entry = kvp.Value;
                string section = entry.Definition.Section;
                object[] tags = entry.Description?.Tags ?? Array.Empty<object>();
                foreach (object tag in tags)
                {
                    if (tag == null)
                    {
                        continue;
                    }

                    string? category = TryGetCategory(tag);
                    if (!NeedsNormalization(category, section))
                    {
                        continue;
                    }

                    if (TrySetCategory(tag, section))
                    {
                        changed++;
                    }
                }
            }
        }

        if (changed == 0 && _warnedMissingConfigManager)
        {
            _warnedMissingConfigManager = false;
            Log.LogDebug("ConfigurationManager type not detected; category normalization hook is inactive until CM is loaded.");
        }

        return changed;
    }

    private static bool NeedsNormalization(string? category, string section)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        string trimmed = category.Trim();
        if (string.Equals(trimmed, section, StringComparison.Ordinal))
        {
            return false;
        }

        bool bracketToken = trimmed.Length >= 3 && trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal);
        bool localizeToken = trimmed.StartsWith("$", StringComparison.Ordinal);
        return bracketToken || localizeToken;
    }

    private static string? TryGetCategory(object tag)
    {
        PropertyInfo? categoryProp = tag.GetType().GetProperty("Category", AnyInstance);
        if (categoryProp?.GetValue(tag) is string fromProperty && !string.IsNullOrWhiteSpace(fromProperty))
        {
            return fromProperty.Trim();
        }

        FieldInfo? categoryField = tag.GetType().GetField("Category", AnyInstance);
        if (categoryField?.GetValue(tag) is string fromField && !string.IsNullOrWhiteSpace(fromField))
        {
            return fromField.Trim();
        }

        return null;
    }

    private static bool TrySetCategory(object tag, string section)
    {
        PropertyInfo? categoryProp = tag.GetType().GetProperty("Category", AnyInstance);
        if (categoryProp?.CanWrite == true && categoryProp.PropertyType == typeof(string))
        {
            categoryProp.SetValue(tag, section);
            return true;
        }

        FieldInfo? categoryField = tag.GetType().GetField("Category", AnyInstance);
        if (categoryField != null && categoryField.FieldType == typeof(string))
        {
            categoryField.SetValue(tag, section);
            return true;
        }

        return false;
    }
}
