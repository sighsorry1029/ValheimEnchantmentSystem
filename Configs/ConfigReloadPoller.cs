using System.Text;

namespace kg.ValheimEnchantmentSystem.Configs;

internal static class ConfigReloadPoller
{
    private sealed class PendingReload
    {
        public string Signature = string.Empty;
        public float FirstObservedAtRealtime;
    }

    private const float PollIntervalSeconds = 2f;
    private static readonly Dictionary<string, string> LastSignatures = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, PendingReload> PendingReloads = new(StringComparer.OrdinalIgnoreCase);
    private static bool IsInitialized;
    private static float NextPollAtRealtime;

    public static void Initialize()
    {
        LastSignatures.Clear();
        PendingReloads.Clear();
        CaptureSnapshots();
        NextPollAtRealtime = Time.realtimeSinceStartup;
        IsInitialized = true;
    }

    public static void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (Time.realtimeSinceStartup < NextPollAtRealtime)
        {
            return;
        }

        NextPollAtRealtime = Time.realtimeSinceStartup + PollIntervalSeconds;
        PollDomains();
    }

    public static void ResetSnapshots()
    {
        PendingReloads.Clear();
        CaptureSnapshots();
        NextPollAtRealtime = Time.realtimeSinceStartup + PollIntervalSeconds;
    }

    public static void Shutdown()
    {
        LastSignatures.Clear();
        PendingReloads.Clear();
        NextPollAtRealtime = 0f;
        IsInitialized = false;
    }

    private static void PollDomains()
    {
        foreach (ConfigReloadDomain domain in ConfigHotReloadRegistrar.GetDomains())
        {
            if (!CanPoll(domain))
            {
                PendingReloads.Remove(domain.Name);
                continue;
            }

            string signature;
            try
            {
                signature = BuildSignature(domain);
            }
            catch (Exception ex)
            {
                Utils.print($"Failed to snapshot config domain {domain.Name}: {ex}", ConsoleColor.Red);
                continue;
            }

            if (!LastSignatures.TryGetValue(domain.Name, out string previousSignature))
            {
                LastSignatures[domain.Name] = signature;
                PendingReloads.Remove(domain.Name);
                continue;
            }

            if (string.Equals(previousSignature, signature, StringComparison.Ordinal))
            {
                PendingReloads.Remove(domain.Name);
                continue;
            }

            if (!TryConsumePendingReload(domain, signature))
            {
                continue;
            }

            LastSignatures[domain.Name] = signature;
            if (domain.Reload())
            {
                Utils.print($"Reloaded config domain '{domain.Name}' via polling.");
            }
        }
    }

    private static void CaptureSnapshots()
    {
        foreach (ConfigReloadDomain domain in ConfigHotReloadRegistrar.GetDomains())
        {
            try
            {
                LastSignatures[domain.Name] = BuildSignature(domain);
            }
            catch (Exception ex)
            {
                Utils.print($"Failed to capture config snapshot for domain {domain.Name}: {ex}", ConsoleColor.Red);
            }
        }
    }

    private static bool CanPoll(ConfigReloadDomain domain)
    {
        if (!ConfigHotReloadRegistrar.CanPoll(domain))
        {
            return false;
        }

        try
        {
            return domain.CanReload();
        }
        catch (Exception ex)
        {
            Utils.print($"Config domain predicate failed for {domain.Name}: {ex}", ConsoleColor.Red);
            return false;
        }
    }

    private static bool TryConsumePendingReload(ConfigReloadDomain domain, string signature)
    {
        if (!PendingReloads.TryGetValue(domain.Name, out PendingReload pending) ||
            !string.Equals(pending.Signature, signature, StringComparison.Ordinal))
        {
            PendingReloads[domain.Name] = new PendingReload
            {
                Signature = signature,
                FirstObservedAtRealtime = Time.realtimeSinceStartup
            };
            return false;
        }

        if (Time.realtimeSinceStartup - pending.FirstObservedAtRealtime < domain.PollDebounceSeconds)
        {
            return false;
        }

        PendingReloads.Remove(domain.Name);
        return true;
    }

    private static string BuildSignature(ConfigReloadDomain domain)
    {
        StringBuilder builder = new();

        foreach (string file in domain.Files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            AppendFileSignature(builder, file);
        }

        foreach (string directory in domain.Directories.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            AppendDirectorySignature(builder, directory);
        }

        return builder.ToString();
    }

    private static void AppendDirectorySignature(StringBuilder builder, string directory)
    {
        string normalized = NormalizePath(directory);
        builder.Append("D|").Append(normalized).Append('|');
        if (!Directory.Exists(directory))
        {
            builder.Append("missing;");
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.yml", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        builder.Append(files.Length).Append(';');
        foreach (string file in files)
        {
            AppendFileSignature(builder, file);
        }
    }

    private static void AppendFileSignature(StringBuilder builder, string file)
    {
        string normalized = NormalizePath(file);
        builder.Append("F|").Append(normalized).Append('|');
        if (!File.Exists(file))
        {
            builder.Append("missing;");
            return;
        }

        FileInfo info = new(file);
        builder.Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append(';');
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path;
        }
    }
}
