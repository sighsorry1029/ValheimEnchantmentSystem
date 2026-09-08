namespace kg.ValheimEnchantmentSystem.Configs;

internal sealed class ConfigReloadDomain
{
    public string Name { get; }
    public IReadOnlyList<string> Files { get; }
    public IReadOnlyList<string> Directories { get; }
    public Func<bool> Reload { get; }
    public Func<bool> CanReload { get; }
    public Func<bool> CanPoll { get; }
    public float PollDebounceSeconds { get; }

    public ConfigReloadDomain(
        string name,
        IEnumerable<string> files,
        IEnumerable<string> directories,
        Func<bool> reload,
        Func<bool> canReload,
        Func<bool>? canPoll = null,
        float pollDebounceSeconds = 0f)
    {
        Name = name;
        Files = (files ?? Array.Empty<string>()).ToArray();
        Directories = (directories ?? Array.Empty<string>()).ToArray();
        Reload = reload;
        CanReload = canReload;
        CanPoll = canPoll ?? canReload;
        PollDebounceSeconds = Math.Max(0f, pollDebounceSeconds);
    }
}

internal static class ConfigHotReloadRegistrar
{
    private const float YamlPollDebounceSeconds = 1.5f;
    private static readonly Dictionary<string, ConfigReloadDomain> Domains = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(string mainConfigPath, Func<bool> reloadMainConfig)
    {
        Domains.Clear();

        Add(new ConfigReloadDomain(
            "main",
            new[] { mainConfigPath },
            Array.Empty<string>(),
            reloadMainConfig,
            () => true,
            () => !IsServerRuntime()));
        Add(new ConfigReloadDomain(
            "chance",
            new[] { EnchantmentConfigPaths.ChancesWeaponsYaml, EnchantmentConfigPaths.ChancesArmorYaml },
            new[] { EnchantmentConfigPaths.OverrideChancesDirectory },
            EnchantmentChanceRepository.TryReloadAll,
            IsServerRuntime,
            pollDebounceSeconds: YamlPollDebounceSeconds));
        Add(new ConfigReloadDomain(
            "stat",
            new[] { EnchantmentConfigPaths.StatsWeaponsYaml, EnchantmentConfigPaths.StatsArmorYaml },
            new[] { EnchantmentConfigPaths.OverrideStatsDirectory },
            EnchantmentStatRepository.TryReloadAll,
            IsServerRuntime,
            pollDebounceSeconds: YamlPollDebounceSeconds));
        Add(new ConfigReloadDomain(
            "color",
            new[] { EnchantmentConfigPaths.ColorsYaml },
            new[] { EnchantmentConfigPaths.OverrideColorsDirectory },
            EnchantmentColorRepository.TryReloadAll,
            IsServerRuntime,
            pollDebounceSeconds: YamlPollDebounceSeconds));
        Add(new ConfigReloadDomain(
            "reqs",
            new[] { EnchantmentConfigPaths.RequirementsYaml, EnchantmentConfigPaths.ResourceMapYaml },
            new[] { EnchantmentConfigPaths.AdditionalRequirementsDirectory },
            EnchantmentRequirementRepository.ReloadAuthoritativeRequirements,
            IsServerRuntime,
            pollDebounceSeconds: YamlPollDebounceSeconds));
    }

    public static IReadOnlyCollection<ConfigReloadDomain> GetDomains()
    {
        return Domains.Values.ToArray();
    }

    public static bool TryReload(string target, out string message)
    {
        string normalizedTarget = NormalizeTarget(target);
        if (string.IsNullOrEmpty(normalizedTarget) || string.Equals(normalizedTarget, "all", StringComparison.OrdinalIgnoreCase))
        {
            return TryReloadAll(out message);
        }

        if (!Domains.TryGetValue(normalizedTarget, out ConfigReloadDomain domain))
        {
            message = $"Unknown config domain '{target}'. Use: {Usage}.";
            return false;
        }

        return TryReloadDomain(domain, out message);
    }

    public static string Usage => "ves_reloadconfig [all|main|chance|stat|color|reqs]";

    private static void Add(ConfigReloadDomain domain)
    {
        Domains[domain.Name] = domain;
    }

    private static bool TryReloadAll(out string message)
    {
        List<string> reloaded = new();
        List<string> skipped = new();
        List<string> failed = new();

        foreach (ConfigReloadDomain domain in Domains.Values)
        {
            if (!CanReload(domain))
            {
                skipped.Add(domain.Name);
                continue;
            }

            if (domain.Reload())
            {
                reloaded.Add(domain.Name);
            }
            else
            {
                failed.Add(domain.Name);
            }
        }

        List<string> parts = new();
        if (reloaded.Count > 0)
        {
            parts.Add($"reloaded: {string.Join(", ", reloaded)}");
        }

        if (skipped.Count > 0)
        {
            parts.Add($"skipped: {string.Join(", ", skipped)}");
        }

        if (failed.Count > 0)
        {
            parts.Add($"failed: {string.Join(", ", failed)}");
        }

        message = parts.Count > 0 ? string.Join(" | ", parts) : "No config domains were reloaded.";
        return failed.Count == 0 && reloaded.Count > 0;
    }

    private static bool TryReloadDomain(ConfigReloadDomain domain, out string message)
    {
        if (!CanReload(domain))
        {
            message = $"Config domain '{domain.Name}' is only reloadable on the server/runtime host.";
            return false;
        }

        bool success = domain.Reload();
        message = success
            ? $"Reloaded config domain '{domain.Name}'."
            : $"Failed to reload config domain '{domain.Name}'.";
        return success;
    }

    private static bool CanReload(ConfigReloadDomain domain)
    {
        try
        {
            return domain.CanReload();
        }
        catch (Exception ex)
        {
            Utils.print($"Config reload predicate failed for {domain.Name}: {ex}", ConsoleColor.Red);
            return false;
        }
    }

    public static bool CanPoll(ConfigReloadDomain domain)
    {
        try
        {
            return domain.CanPoll();
        }
        catch (Exception ex)
        {
            Utils.print($"Config polling predicate failed for {domain.Name}: {ex}", ConsoleColor.Red);
            return false;
        }
    }

    private static string NormalizeTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return string.Empty;
        }

        return target.Trim().ToLowerInvariant() switch
        {
            "chances" => "chance",
            "stats" => "stat",
            "colors" => "color",
            "requirements" => "reqs",
            "requirement" => "reqs",
            _ => target.Trim().ToLowerInvariant()
        };
    }

    private static bool IsServerRuntime()
    {
        return Game.instance && ZNet.instance && ZNet.instance.IsServer();
    }
}
