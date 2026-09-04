namespace kg.ValheimEnchantmentSystem.UI;

// Input policy separated from Unity objects so disabled keys and modifier handling can be tested.
internal static class EnchantmentShortcutRules
{
    internal static bool IsPressed<TKey>(TKey mainKey, TKey disabledKey, IEnumerable<TKey> modifiers,
        Func<TKey, bool> pressedThisFrame, Func<TKey, bool> held)
    {
        return !EqualityComparer<TKey>.Default.Equals(mainKey, disabledKey) &&
               pressedThisFrame(mainKey) && modifiers.All(held);
    }

    internal static bool IsConfigurationManager(string guid, string name)
    {
        return IsManagerName(guid) || IsManagerName(name);
    }

    private static bool IsManagerName(string name)
    {
        string compact = name.Replace(" ", string.Empty);
        return compact.IndexOf("configurationmanager", StringComparison.OrdinalIgnoreCase) >= 0 ||
               compact.IndexOf("configmanager", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
