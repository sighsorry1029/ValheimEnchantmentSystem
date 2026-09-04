using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.Configs;

internal static class ConfigurationManagerDisplay
{
    internal const string General = "1 - General";
    internal const string Client = "2 - Client";
    internal const string Enchantment = "3 - Enchantment";
    internal const string Skill = "4 - Skill";
    internal const string Scrolls = "5 - Scrolls";
    internal const string BiomeTiers = "6 - Biome Tiers";
    internal const string ScrollRecipes = "7 - Scroll Recipes";

    // BepInEx.ConfigurationManager discovers this optional tag by its exact type
    // name, without taking a compile-time dependency on the plugin.
    private sealed class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public string? Category;
        [UsedImplicitly] public int? CategoryOrder;
        [UsedImplicitly] public string? DispName;
        [UsedImplicitly] public int? Order;
        [UsedImplicitly] public bool? ShowRangeAsPercent;
        [UsedImplicitly] public bool? Browsable;
    }

    internal static ConfigDescription Description(
        string description,
        string category,
        int? order,
        string? displayName = null,
        AcceptableValueBase? acceptableValues = null,
        bool? showRangeAsPercent = null,
        bool? browsable = null)
    {
        return WithDisplay(new ConfigDescription(description, acceptableValues), category, order, displayName, showRangeAsPercent, browsable);
    }

    internal static ConfigDescription WithDisplay(
        ConfigDescription description,
        string category,
        int? order,
        string? displayName = null,
        bool? showRangeAsPercent = null,
        bool? browsable = null)
    {
        object[] tags = (description.Tags ?? Array.Empty<object>())
            .Concat(new object[]
            {
                new ConfigurationManagerAttributes
                {
                    Category = category,
                    CategoryOrder = GetCategoryOrder(category),
                    DispName = displayName,
                    Order = order,
                    ShowRangeAsPercent = showRangeAsPercent,
                    Browsable = browsable
                }
            })
            .ToArray();

        return new ConfigDescription(description.Description, description.AcceptableValues, tags);
    }

    // ConfigManager sorts category priority descending, independently of each setting's Order.
    // Numbered display names alone do not override the original config registration order.
    private static int? GetCategoryOrder(string category) => category switch
    {
        General => 7,
        Client => 6,
        Enchantment => 5,
        Skill => 4,
        Scrolls => 3,
        BiomeTiers => 2,
        ScrollRecipes => 1,
        _ => null
    };
}
