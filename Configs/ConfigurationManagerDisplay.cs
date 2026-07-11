using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.Configs;

internal static class ConfigurationManagerDisplay
{
    internal const string General = "1 - General";
    internal const string Enchantment = "2 - Enchantment";
    internal const string Skill = "3 - Skill";
    internal const string Scrolls = "4 - Scrolls";
    internal const string BiomeTiers = "5 - Biome Tiers";
    internal const string Notifications = "6 - Notifications";
    internal const string Client = "7 - Client";
    internal const string ScrollRecipes = "8 - Scroll Recipes";

    // BepInEx.ConfigurationManager discovers this optional tag by its exact type
    // name, without taking a compile-time dependency on the plugin.
    private sealed class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public string? Category;
        [UsedImplicitly] public string? DispName;
        [UsedImplicitly] public int? Order;
    }

    internal static ConfigDescription Description(
        string description,
        string category,
        int? order,
        string? displayName = null,
        AcceptableValueBase? acceptableValues = null)
    {
        return WithDisplay(new ConfigDescription(description, acceptableValues), category, order, displayName);
    }

    internal static ConfigDescription WithDisplay(
        ConfigDescription description,
        string category,
        int? order,
        string? displayName = null)
    {
        object[] tags = (description.Tags ?? Array.Empty<object>())
            .Concat(new object[]
            {
                new ConfigurationManagerAttributes
                {
                    Category = category,
                    DispName = displayName,
                    Order = order
                }
            })
            .ToArray();

        return new ConfigDescription(description.Description, description.AcceptableValues, tags);
    }
}
