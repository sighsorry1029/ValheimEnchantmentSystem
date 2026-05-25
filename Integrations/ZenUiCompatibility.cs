using BepInEx.Bootstrap;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Integrations;

[VES_RequiresPlugin(ZenUiGuid)]
internal static class ZenUiCompatibility
{
    private const string ZenUiGuid = "ZenDragon.ZenUI";
    private const string CraftingPanelTypeName = "ZenUI.Section.CraftingPanel";
    private const string LastObjectDbRecipesCountFieldName = "_lastObjectDBRecipesCount";
    private const string RecipeToGroupsFieldName = "RecipeToGroups";
    private const string RecipeItemCloneCacheFieldName = "RecipeItemCloneCache";

    private static bool _hooksResolved;
    private static bool _loggedMissingHooks;
    private static FieldInfo? _lastObjectDbRecipesCountField;
    private static FieldInfo? _recipeToGroupsField;
    private static FieldInfo? _recipeItemCloneCacheField;

    public static void InvalidateCraftingRecipeCache()
    {
        if (!TryResolveHooks())
        {
            return;
        }

        try
        {
            _lastObjectDbRecipesCountField?.SetValue(null, -1);
            ClearDictionary(_recipeToGroupsField);
            ClearDictionary(_recipeItemCloneCacheField);
        }
        catch (Exception ex)
        {
            Utils.print($"ZenUI crafting cache invalidation failed: {ex.Message}", ConsoleColor.Yellow);
        }
    }

    private static bool TryResolveHooks()
    {
        if (!Chainloader.PluginInfos.ContainsKey(ZenUiGuid))
        {
            return false;
        }

        if (_hooksResolved)
        {
            return _lastObjectDbRecipesCountField != null && _recipeToGroupsField != null;
        }

        _hooksResolved = true;
        Type? craftingPanelType = AccessTools.TypeByName(CraftingPanelTypeName);
        if (craftingPanelType != null)
        {
            _lastObjectDbRecipesCountField = AccessTools.Field(craftingPanelType, LastObjectDbRecipesCountFieldName);
            _recipeToGroupsField = AccessTools.Field(craftingPanelType, RecipeToGroupsFieldName);
            _recipeItemCloneCacheField = AccessTools.Field(craftingPanelType, RecipeItemCloneCacheFieldName);
        }

        if (_lastObjectDbRecipesCountField != null && _recipeToGroupsField != null)
        {
            return true;
        }

        if (!_loggedMissingHooks)
        {
            _loggedMissingHooks = true;
            Utils.print("ZenUI is installed, but crafting panel cache hooks were not found. Skipping recipe cache compatibility.", ConsoleColor.Yellow);
        }

        return false;
    }

    private static void ClearDictionary(FieldInfo? field)
    {
        if (field?.GetValue(null) is IDictionary dictionary)
        {
            dictionary.Clear();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
    [ClientOnlyPatch]
    private static class InventoryGui_UpdateRecipeList_Patch
    {
        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(List<Recipe> recipes)
        {
            if (recipes == null || recipes.Count == 0 || !TryResolveHooks())
            {
                return;
            }

            if (_recipeToGroupsField?.GetValue(null) is not IDictionary dictionary)
            {
                InvalidateCraftingRecipeCache();
                return;
            }

            foreach (Recipe recipe in recipes)
            {
                if (recipe != null && !dictionary.Contains(recipe))
                {
                    InvalidateCraftingRecipeCache();
                    return;
                }
            }
        }
    }
}
