/*
Archived feature: Enable External Localization Reinject
Moved out of LocalizationManager.cs on 2026-03-10 by request.
This file is intentionally commented out so the feature is fully inactive.

To restore later:
1) Re-add these fields to Localizer.
2) Re-add the config bind in EnsureConfig().
3) Re-add call sites:
   - CaptureExternalAddWord(__instance, __0, currentValue ?? string.Empty) in TraceAddWordPostfix()
   - ReinjectExternalWords(__instance, language) in LoadLocalization()
4) Uncomment the methods below.

namespace LocalizationManager;

public partial class Localizer
{
    private static readonly object ExternalLocalizationLock = new();
    private static readonly Dictionary<string, Dictionary<string, string>> ExternalWordsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private static ConfigEntry<bool>? _enableExternalLocalizationReinject;
    [ThreadStatic] private static bool _reinjectingExternalWords;

    // EnsureConfig() addition:
    // _enableExternalLocalizationReinject = plugin.Config.Bind(
    //     "Client",
    //     "Enable External Localization Reinject",
    //     false,
    //     "When enabled, external mod AddWord tokens are cached and mirrored into the active Localization to reduce cross-instance token misses from old manager templates.");
    // if (_enableExternalLocalizationReinject.Value)
    // {
    //     _enableExternalLocalizationReinject.Value = false;
    // }

    private static void CaptureExternalAddWord(Localization sourceLocalization, string key, string value)
    {
        EnsureConfig();
        if (_enableExternalLocalizationReinject?.Value != true || _reinjectingExternalWords)
        {
            return;
        }

        if (!TryGetRelevantCaller(out string callerAssembly, out _, out _, 3))
        {
            return;
        }

        string language = SafeLanguage(sourceLocalization);
        if (string.IsNullOrWhiteSpace(language) || language == "<unknown>")
        {
            return;
        }

        lock (ExternalLocalizationLock)
        {
            if (!ExternalWordsByLanguage.TryGetValue(language, out Dictionary<string, string>? words))
            {
                words = new Dictionary<string, string>(StringComparer.Ordinal);
                ExternalWordsByLanguage[language] = words;
            }

            words[key] = value;
        }

        MirrorExternalWordToActiveLocalization(sourceLocalization, language, key, value, callerAssembly);
    }

    private static void MirrorExternalWordToActiveLocalization(Localization sourceLocalization, string language, string key, string value, string callerAssembly)
    {
        Localization? activeLocalization;
        try
        {
            if (Localization.m_instance == null)
            {
                return;
            }

            activeLocalization = Localization.instance;
        }
        catch
        {
            return;
        }

        if (ReferenceEquals(activeLocalization, sourceLocalization))
        {
            return;
        }

        if (!string.Equals(SafeLanguage(activeLocalization), language, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (activeLocalization.m_translations.ContainsKey(key))
        {
            return;
        }

        try
        {
            _reinjectingExternalWords = true;
            activeLocalization.AddWord(key, value);
        }
        finally
        {
            _reinjectingExternalWords = false;
        }

        if (DiagnosticsEnabled() && ShouldTraceToken(key) && TryConsumeDiagnosticBudget("external_mirror", key))
        {
            LogDiagnostics($"Mirrored external AddWord lang='{language}' key='{NormalizeToken(key)}' sourceInstance={sourceLocalization.GetHashCode()} activeInstance={activeLocalization.GetHashCode()} caller={callerAssembly}");
        }
    }

    private static void ReinjectExternalWords(Localization localization, string language)
    {
        EnsureConfig();
        if (_enableExternalLocalizationReinject?.Value != true)
        {
            return;
        }

        Dictionary<string, string>? cachedWords;
        lock (ExternalLocalizationLock)
        {
            if (!ExternalWordsByLanguage.TryGetValue(language, out Dictionary<string, string>? words) || words.Count == 0)
            {
                return;
            }

            cachedWords = new Dictionary<string, string>(words, StringComparer.Ordinal);
        }

        int inserted = 0;
        try
        {
            _reinjectingExternalWords = true;
            foreach (KeyValuePair<string, string> kv in cachedWords)
            {
                if (localization.m_translations.ContainsKey(kv.Key))
                {
                    continue;
                }

                localization.AddWord(kv.Key, kv.Value);
                inserted++;
            }
        }
        finally
        {
            _reinjectingExternalWords = false;
        }

        if (inserted > 0 && DiagnosticsEnabled() && TryConsumeDiagnosticBudget("external_reinject", language))
        {
            LogDiagnostics($"Reinjected external words lang='{language}' instance={localization.GetHashCode()} inserted={inserted}");
        }
    }
}
*/
