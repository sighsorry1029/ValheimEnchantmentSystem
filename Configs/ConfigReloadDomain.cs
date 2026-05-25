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
