using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using YamlDotNet.Serialization;

namespace LocalizationManager;

[PublicAPI]
public static class SharedLocalizationCache
{
    private static readonly Dictionary<string, WeakReference<Localization>> Localizations = new();
    private static readonly object CacheLock = new();

    public static void Track(Localization localization, string language)
    {
        lock (CacheLock)
        {
            if (Localizations.FirstOrDefault(pair => pair.Value.TryGetTarget(out Localization value) && value == localization).Key is { } oldLanguage)
            {
                Localizations.Remove(oldLanguage);
            }

            Localizations[language] = new WeakReference<Localization>(localization);
        }
    }

    public static Localization ForLanguage(string? language = null)
    {
        bool explicitLanguageRequested = language != null;
        string selectedLanguage = language ?? PlayerPrefs.GetString("language", "English");

        lock (CacheLock)
        {
            if (Localizations.TryGetValue(selectedLanguage, out WeakReference<Localization>? reference) &&
                reference.TryGetTarget(out Localization cachedLocalization))
            {
                return cachedLocalization;
            }
        }

        if (Localization.m_instance != null)
        {
            Localization active = Localization.instance;
            string activeLanguage = active.GetSelectedLanguage();
            Track(active, activeLanguage);
            if (!explicitLanguageRequested || string.Equals(activeLanguage, selectedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return active;
            }

            throw new InvalidOperationException($"Requested localization '{selectedLanguage}' is not cached. Active language is '{activeLanguage}'.");
        }

        throw new InvalidOperationException($"No localization instance is available for language '{selectedLanguage}'.");
    }

    public static string LocalizeForConfig(string token, string preferredLanguage = "English")
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        string normalizedToken = token.StartsWith("$", StringComparison.Ordinal) ? token : "$" + token;
        string localizationKey = normalizedToken.TrimStart('$');
        if (Localizer.TryGetConfigText(preferredLanguage, localizationKey, out string directText))
        {
            return directText;
        }

        try
        {
            string localized = ForLanguage(preferredLanguage).Localize(normalizedToken).Trim();
            if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, normalizedToken, StringComparison.Ordinal))
            {
                return localized;
            }
        }
        catch
        {
            // Fall through to deterministic token-derived fallback.
        }

        string fallback = normalizedToken.TrimStart('$').Replace('_', ' ').Trim();
        return fallback.Length == 0 ? normalizedToken : fallback;
    }
}

[PublicAPI]
public class Localizer
{
    private static readonly Dictionary<string, Dictionary<string, Func<string>>> PlaceholderProcessors = new();
    private static readonly Dictionary<string, Dictionary<string, string>> LoadedTexts = new();
    private static readonly object LoadedTextsLock = new();
    private static readonly ConditionalWeakTable<Localization, string> LocalizationLanguage = new();
    private static readonly List<WeakReference<Localization>> LocalizationObjects = [];
    private static readonly List<string> FileExtensions = [".json", ".yml"];

    private static BaseUnityPlugin? _plugin;
    private static readonly object ExternalLocalizationFilesLock = new();
    private static Dictionary<string, string>? _externalLocalizationFiles;
    public static event Action? OnLocalizationComplete;

    private static BaseUnityPlugin plugin
    {
        get
        {
            if (_plugin is null)
            {
                IEnumerable<TypeInfo> types;
                try
                {
                    types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
                }

                _plugin = (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
            }

            return _plugin;
        }
    }

    private static string? TryParseLanguageFromExternalFile(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string expectedPrefix = plugin.Info.Metadata.GUID + ".";
        if (!fileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string language = fileName.Substring(expectedPrefix.Length);
        return string.IsNullOrWhiteSpace(language) ? null : language;
    }

    private static bool HasSupportedExtension(string filePath) =>
        FileExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> BuildExternalLocalizationFiles()
    {
        Dictionary<string, string> localizationFiles = new();
        string pluginDirectory = Path.GetDirectoryName(Paths.PluginPath)!;
        foreach (string file in Directory.GetFiles(pluginDirectory, $"{plugin.Info.Metadata.GUID}.*", SearchOption.AllDirectories).Where(HasSupportedExtension))
        {
            if (TryParseLanguageFromExternalFile(file) is not { } key)
            {
                continue;
            }

            if (localizationFiles.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate key {key} found for {plugin.Info.Metadata.GUID}. The duplicate file found at {file} will be skipped.");
                continue;
            }

            localizationFiles[key] = file;
        }

        return localizationFiles;
    }

    private static Dictionary<string, string> GetExternalLocalizationFiles()
    {
        lock (ExternalLocalizationFilesLock)
        {
            _externalLocalizationFiles ??= BuildExternalLocalizationFiles();
            return _externalLocalizationFiles;
        }
    }

    private static void UpdatePlaceholderText(Localization localization, string key)
    {
        if (!LocalizationLanguage.TryGetValue(localization, out string language))
        {
            return;
        }

        if (!LoadedTexts.TryGetValue(language, out Dictionary<string, string> texts) || !texts.TryGetValue(key, out string text))
        {
            return;
        }

        if (PlaceholderProcessors.TryGetValue(key, out Dictionary<string, Func<string>> textProcessors))
        {
            text = textProcessors.Aggregate(text, (current, kv) => current.Replace("{" + kv.Key + "}", kv.Value()));
        }

        localization.AddWord(key, text);
    }

    public static void AddPlaceholder<T>(string key, string placeholder, ConfigEntry<T> config, Func<T, string>? convertConfigValue = null) where T : notnull
    {
        convertConfigValue ??= val => val.ToString();
        if (!PlaceholderProcessors.ContainsKey(key))
        {
            PlaceholderProcessors[key] = new Dictionary<string, Func<string>>();
        }

        void UpdatePlaceholder()
        {
            if (Localization.instance == null)
            {
                return;
            }

            PlaceholderProcessors[key][placeholder] = () => convertConfigValue(config.Value);
            UpdatePlaceholderText(Localization.instance, key);
        }

        config.SettingChanged += (_, _) => UpdatePlaceholder();
        if (Localization.instance != null && LoadedTexts.ContainsKey(Localization.instance.GetSelectedLanguage()))
        {
            UpdatePlaceholder();
        }
    }

    public static void AddText(string key, string text)
    {
        List<WeakReference<Localization>> remove = [];
        foreach (WeakReference<Localization> reference in LocalizationObjects)
        {
            if (reference.TryGetTarget(out Localization localization))
            {
                string language = LocalizationLanguage.GetOrCreateValue(localization);
                if (!LoadedTexts.TryGetValue(language, out Dictionary<string, string>? texts))
                {
                    texts = new Dictionary<string, string>();
                    LoadedTexts[language] = texts;
                }

                if (!localization.m_translations.ContainsKey(key))
                {
                    texts[key] = text;
                    localization.AddWord(key, text);
                }
            }
            else
            {
                remove.Add(reference);
            }
        }

        foreach (WeakReference<Localization> reference in remove)
        {
            LocalizationObjects.Remove(reference);
        }
    }

    public static bool TryGetConfigText(string language, string key, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        Dictionary<string, string>? texts = EnsureConfigTextsLoaded(language);
        if (texts == null || !texts.TryGetValue(key, out string localizedText) || string.IsNullOrWhiteSpace(localizedText))
        {
            return false;
        }

        text = localizedText.Trim();
        return text.Length > 0;
    }

    public static void Load()
    {
        _ = plugin;
    }

    public static void LoadLocalizationLater(Localization __instance) => LoadLocalization(Localization.instance, __instance.GetSelectedLanguage());

    public static void SafeCallLocalizeComplete() => OnLocalizationComplete?.Invoke();

    private static void LoadLocalization(Localization __instance, string language)
    {
            if (!LocalizationLanguage.Remove(__instance))
            {
                LocalizationObjects.Add(new WeakReference<Localization>(__instance));
            }

            LocalizationLanguage.Add(__instance, language);
            SharedLocalizationCache.Track(__instance, language);

            Dictionary<string, string> localizationFiles = GetExternalLocalizationFiles();

            if (LoadTranslationFromAssembly("English") is not { } englishAssemblyData)
            {
                throw new Exception($"Found no English localizations in mod {plugin.Info.Metadata.Name}. Expected an embedded resource translations/English.json or translations/English.yml.");
            }

            Dictionary<string, string>? localizationTexts = new DeserializerBuilder()
                .IgnoreFields()
                .Build()
                .Deserialize<Dictionary<string, string>?>(Encoding.UTF8.GetString(englishAssemblyData));
            if (localizationTexts is null)
            {
                throw new Exception($"Localization for mod {plugin.Info.Metadata.Name} failed: Localization file was empty.");
            }

            string? localizationData = null;
            if (language != "English")
            {
                if (localizationFiles.TryGetValue(language, out string? localizationFile))
                {
                    localizationData = File.ReadAllText(localizationFile);
                }
                else if (LoadTranslationFromAssembly(language) is { } languageAssemblyData)
                {
                    localizationData = Encoding.UTF8.GetString(languageAssemblyData);
                }
            }

            if (localizationData is null && localizationFiles.TryGetValue("English", out string? fallbackEnglishFile))
            {
                localizationData = File.ReadAllText(fallbackEnglishFile);
            }

            if (localizationData is not null)
            {
                foreach (KeyValuePair<string, string> kv in new DeserializerBuilder().IgnoreFields().Build()
                             .Deserialize<Dictionary<string, string>?>(localizationData) ?? new Dictionary<string, string>())
                {
                    localizationTexts[kv.Key] = kv.Value;
                }
            }

            LoadedTexts[language] = localizationTexts;
            foreach (KeyValuePair<string, string> s in localizationTexts)
            {
                UpdatePlaceholderText(__instance, s.Key);
            }
    }

    static Localizer()
    {
        Harmony harmony = new("org.bepinex.helpers.LocalizationManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.SetupLanguage)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Localizer), nameof(LoadLocalization))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.SetupGui)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Localizer), nameof(LoadLocalizationLater))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Start)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Localizer), nameof(SafeCallLocalizeComplete))));
    }

    private static byte[]? LoadTranslationFromAssembly(string language)
    {
        Assembly assembly = typeof(Localizer).Assembly;
        foreach (string extension in FileExtensions)
        {
            if (ReadEmbeddedFileBytes("translations." + language + extension, assembly) is { } data)
            {
                return data;
            }
        }

        return null;
    }

    private static Dictionary<string, string>? EnsureConfigTextsLoaded(string language)
    {
        lock (LoadedTextsLock)
        {
            if (LoadedTexts.TryGetValue(language, out Dictionary<string, string>? existingTexts))
            {
                return existingTexts;
            }

            Dictionary<string, string> localizationFiles = GetExternalLocalizationFiles();
            string? localizationData = null;
            if (localizationFiles.TryGetValue(language, out string? externalLocalizationFile))
            {
                localizationData = File.ReadAllText(externalLocalizationFile);
            }
            else if (LoadTranslationFromAssembly(language) is { } assemblyData)
            {
                localizationData = Encoding.UTF8.GetString(assemblyData);
            }

            if (localizationData == null)
            {
                return null;
            }

            Dictionary<string, string>? parsedTexts = new DeserializerBuilder()
                .IgnoreFields()
                .Build()
                .Deserialize<Dictionary<string, string>?>(localizationData);
            if (parsedTexts == null)
            {
                return null;
            }

            LoadedTexts[language] = parsedTexts;
            return parsedTexts;
        }
    }

    public static byte[]? ReadEmbeddedFileBytes(string resourceFileName, Assembly? containingAssembly = null)
    {
        using MemoryStream stream = new();
        containingAssembly ??= Assembly.GetCallingAssembly();
        if (containingAssembly.GetManifestResourceNames().FirstOrDefault(str => str.EndsWith(resourceFileName, StringComparison.Ordinal)) is { } name)
        {
            containingAssembly.GetManifestResourceStream(name)?.CopyTo(stream);
        }

        return stream.Length == 0 ? null : stream.ToArray();
    }
}

public static class LocalizationManagerVersion
{
    public const string Version = "1.4.2";
}
