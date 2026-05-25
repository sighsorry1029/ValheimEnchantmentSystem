using ServerSync;

namespace kg.ValheimEnchantmentSystem.Configs;

internal static class EnchantmentYamlConfigSupport
{
    public static void InitializeYamlConfig<T>(string path, string fallbackYaml, CustomSyncedValue<T> target, string label)
    {
        if (Utils.TryDeserializeYAML(fallbackYaml, out T builtIn, out string fallbackError))
        {
            target.Value = builtIn;
        }
        else
        {
            Utils.print($"Failed to load built-in defaults for {label}: {fallbackError}", ConsoleColor.Red);
        }

        if (!TryReloadYamlConfig(path, target, label))
        {
            Utils.print($"Using built-in defaults for {label}.", ConsoleColor.Yellow);
        }
    }

    public static bool TryReloadYamlConfig<T>(string path, CustomSyncedValue<T> target, string label)
    {
        if (!path.TryFromYAML(out T result, out string error))
        {
            Utils.print($"Skipped reload for {label} ({path}): {error}", ConsoleColor.Red);
            return false;
        }

        target.Value = result;
        return true;
    }

    public static bool TryReadYamlListDirectory<T>(string directory, out List<T> result, out string error)
    {
        result = new List<T>();

        if (!TryGetYamlFiles(directory, out string[] files, out error))
        {
            return false;
        }

        foreach (string file in files)
        {
            if (!file.TryFromYAML(out List<T> data, out string parseError))
            {
                error = $"{file}: {parseError}";
                return false;
            }

            result.AddRange(data);
        }

        error = string.Empty;
        return true;
    }

    private static bool TryGetYamlFiles(string directory, out string[] files, out string error)
    {
        files = Array.Empty<string>();

        if (!Directory.Exists(directory))
        {
            error = $"{directory}: directory does not exist";
            return false;
        }

        try
        {
            files = Directory.GetFiles(directory, "*.yml", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"{directory}: {ex}";
            return false;
        }
    }
}
