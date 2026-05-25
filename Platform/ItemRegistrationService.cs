using kg.ValheimEnchantmentSystem.Items_Structures;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class ItemRegistrationService
{
    internal sealed class RegisteredItem
    {
        private readonly ThinItemManager.RegisteredItemData _item;

        public GameObject Prefab => _item.Prefab;
        public string PrefabName => Prefab.name;
        internal IEnumerable<string> CraftingStationNames =>
            _item.Recipes
                .Select(recipe => recipe.CraftingStationName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase);

        public RegisteredItem(ThinItemManager.RegisteredItemData item)
        {
            _item = item;
        }

        public T? TryGetComponent<T>() where T : Component
        {
            return Prefab.GetComponent<T>();
        }

        internal void ApplyRecipe(ScrollRecipeCatalog.RecipeData recipe, string craftingStation, int level)
        {
            _item.SetRecipe(new ThinItemManager.RecipeDefinition(
                string.Empty,
                recipe.Amount,
                craftingStation,
                level,
                ParseRequirements(recipe.Requirements)));
        }

        internal void ConfigureConversion(string recipeKey, int craftAmount, string requiredItemName, int requiredItemAmount, string craftingStation, int level)
        {
            _item.SetRecipe(new ThinItemManager.RecipeDefinition(
                recipeKey,
                craftAmount,
                craftingStation,
                level,
                new[]
                {
                    new ThinItemManager.RequirementDefinition(requiredItemName, requiredItemAmount)
                }));
        }
    }

    public static RegisteredItem CreateRecipeItem(GameObject prefab, string sharedName, string descriptionKey)
    {
        RegisteredItem registeredItem = new(ThinItemManager.RegisterRecipeItem(prefab, sharedName, descriptionKey));
        ContentValidationService.RegisterRecipeItem(registeredItem);
        return registeredItem;
    }

    public static void ApplyRecipe(RegisteredItem item, ScrollRecipeCatalog.RecipeData recipe, string craftingStation, int level = 1)
    {
        item.ApplyRecipe(recipe, craftingStation, level);
    }

    public static void ConfigureConversion(RegisteredItem item, string recipeKey, int craftAmount, string requiredItemName, int requiredItemAmount, string craftingStation, int level = 1)
    {
        item.ConfigureConversion(recipeKey, craftAmount, requiredItemName, requiredItemAmount, craftingStation, level);
    }

    private static ThinItemManager.RequirementDefinition[] ParseRequirements(IEnumerable<string> requirements)
    {
        return requirements.Select(requirement =>
            {
                string[] split = requirement.Split(',');
                string itemName = split[0];
                int amount = split.Length > 1 && int.TryParse(split[1], out int parsedAmount) ? parsedAmount : 1;
                int quality = split.Length > 2 && int.TryParse(split[2], out int parsedQuality) ? parsedQuality : 0;
                return new ThinItemManager.RequirementDefinition(itemName, amount, quality);
            })
            .ToArray();
    }
}
