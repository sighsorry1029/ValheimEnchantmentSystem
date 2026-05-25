using JetBrains.Annotations;
using LocalizationManager;
using kg.ValheimEnchantmentSystem.Integrations;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class ThinItemManager
{
    // Hammer alone appears too early during ObjectDB bootstrap; gate on a late vanilla item as well.
    private static readonly string[] ObjectDbReadinessSentinels = { "Hammer", "TrophyFader" };
    private const string FixedScrollCraftingStationName = "kg_EnchantmentScrollStation";
    private const string ScrollRecipeConfigSection = "Scroll Recipes";

    private sealed class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private sealed class ItemConfig
    {
        public readonly Dictionary<string, RecipeConfig> Recipes = new(StringComparer.Ordinal);
    }

    private sealed class RecipeConfig
    {
        public ConfigEntry<string>? CraftingCosts;
    }

    internal sealed class RegisteredItemData
    {
        private readonly Dictionary<string, RecipeDefinition> _recipes = new(StringComparer.Ordinal);

        public readonly GameObject Prefab;
        public readonly string SharedName;
        public readonly string DescriptionKey;

        public RegisteredItemData(GameObject prefab, string sharedName, string descriptionKey)
        {
            Prefab = prefab;
            SharedName = sharedName;
            DescriptionKey = descriptionKey;
        }

        public IEnumerable<RecipeDefinition> Recipes => _recipes.Values;

        public void SetRecipe(RecipeDefinition recipe)
        {
            _recipes[recipe.Key] = recipe;
            EnsureConfigBindings(this);
        }
    }

    internal readonly struct RequirementDefinition
    {
        public readonly string ItemName;
        public readonly int Amount;
        public readonly int Quality;

        public RequirementDefinition(string itemName, int amount, int quality = 0)
        {
            ItemName = itemName;
            Amount = amount;
            Quality = quality;
        }
    }

    internal readonly struct RecipeDefinition
    {
        public readonly string Key;
        public readonly int CraftAmount;
        public readonly string CraftingStationName;
        public readonly int MinStationLevel;
        public readonly RequirementDefinition[] Requirements;

        public RecipeDefinition(string key, int craftAmount, string craftingStationName, int minStationLevel, RequirementDefinition[] requirements)
        {
            Key = key;
            CraftAmount = craftAmount;
            CraftingStationName = craftingStationName;
            MinStationLevel = minStationLevel;
            Requirements = requirements ?? Array.Empty<RequirementDefinition>();
        }
    }

    private static readonly Dictionary<string, RegisteredItemData> RegisteredItems = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ItemConfig> ItemConfigs = new(StringComparer.Ordinal);

    public static RegisteredItemData RegisterRecipeItem(GameObject prefab, string sharedName, string descriptionKey)
    {
        if (prefab == null)
        {
            throw new ArgumentNullException(nameof(prefab));
        }

        if (prefab.GetComponent<ItemDrop>() is not { } itemDrop)
        {
            throw new InvalidOperationException($"Item prefab '{prefab.name}' is missing ItemDrop.");
        }

        itemDrop.m_itemData.m_shared.m_name = sharedName;
        itemDrop.m_itemData.m_shared.m_description = descriptionKey;

        RegisteredItemData registeredItem = new(prefab, sharedName, descriptionKey);
        RegisteredItems[prefab.name] = registeredItem;
        return registeredItem;
    }

    private static void ApplySceneRegistration(ZNetScene scene)
    {
        foreach (RegisteredItemData item in RegisteredItems.Values)
        {
            if (!scene.m_prefabs.Contains(item.Prefab))
            {
                scene.m_prefabs.Add(item.Prefab);
            }

            int prefabHash = item.Prefab.name.GetStableHashCode();
            if (!scene.m_namedPrefabs.ContainsKey(prefabHash))
            {
                scene.m_namedPrefabs.Add(prefabHash, item.Prefab);
            }
        }
    }

    private static void ApplyObjectDbRegistration(ObjectDB objectDb)
    {
        if (objectDb == null || objectDb.m_items == null || objectDb.m_recipes == null)
        {
            return;
        }

        EnsureConfigBindings();

        if (!IsObjectDbReady(objectDb))
        {
            return;
        }

        bool changed = false;
        foreach (RegisteredItemData item in RegisteredItems.Values)
        {
            changed |= EnsureItemRegistered(objectDb, item);
            changed |= RebuildRecipes(objectDb, item);
        }

        if (changed)
        {
            objectDb.UpdateRegisters();
            ZenUiCompatibility.InvalidateCraftingRecipeCache();
        }
    }

    private static bool IsObjectDbReady(ObjectDB objectDb)
    {
        if (objectDb == null)
        {
            return false;
        }

        foreach (string prefabName in ObjectDbReadinessSentinels)
        {
            if (objectDb.GetItemPrefab(prefabName) == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool EnsureItemRegistered(ObjectDB objectDb, RegisteredItemData item)
    {
        bool changed = false;
        if (!objectDb.m_items.Contains(item.Prefab))
        {
            objectDb.m_items.Add(item.Prefab);
            changed = true;
        }

        objectDb.m_itemByHash ??= new Dictionary<int, GameObject>();
        int prefabHash = item.Prefab.name.GetStableHashCode();
        if (!objectDb.m_itemByHash.ContainsKey(prefabHash))
        {
            objectDb.m_itemByHash.Add(prefabHash, item.Prefab);
            changed = true;
        }

        return changed;
    }

    private static bool RebuildRecipes(ObjectDB objectDb, RegisteredItemData item)
    {
        if (item.Prefab.GetComponent<ItemDrop>() is not { } itemDrop)
        {
            return false;
        }

        string recipePrefix = BuildRecipeNamePrefix(item.Prefab.name);
        int removed = objectDb.m_recipes.RemoveAll(recipe =>
            recipe != null &&
            recipe.m_item == itemDrop &&
            recipe.name != null &&
            recipe.name.StartsWith(recipePrefix, StringComparison.Ordinal));
        List<Recipe> newRecipes = item.Recipes
            .Select(recipe => BuildRecipe(objectDb, item, GetEffectiveRecipe(item, recipe)))
            .Where(recipe => recipe != null)
            .ToList()!;

        if (newRecipes.Count == 0 && removed == 0)
        {
            return false;
        }

        objectDb.m_recipes.AddRange(newRecipes);
        return removed > 0 || newRecipes.Count > 0;
    }

    private static Recipe? BuildRecipe(ObjectDB objectDb, RegisteredItemData item, RecipeDefinition recipe)
    {
        if (item.Prefab.GetComponent<ItemDrop>() is not { } itemDrop)
        {
            return null;
        }

        Recipe newRecipe = ScriptableObject.CreateInstance<Recipe>();
        newRecipe.name = BuildRecipeName(item.Prefab.name, recipe);
        newRecipe.m_item = itemDrop;
        newRecipe.m_amount = Math.Max(1, recipe.CraftAmount);
        newRecipe.m_enabled = recipe.CraftAmount > 0;
        newRecipe.m_resources = ToPieceRequirements(objectDb, recipe.Requirements);
        newRecipe.m_craftingStation = ResolveCraftingStation(recipe.CraftingStationName);
        newRecipe.m_minStationLevel = Math.Max(0, recipe.MinStationLevel);
        newRecipe.m_requireOnlyOneIngredient = false;
        newRecipe.m_qualityResultAmountMultiplier = 1f;
        return newRecipe;
    }

    private static CraftingStation? ResolveCraftingStation(string craftingStationName)
    {
        if (string.IsNullOrWhiteSpace(craftingStationName) || ZNetScene.instance == null)
        {
            return null;
        }

        return ZNetScene.instance.GetPrefab(craftingStationName)?.GetComponent<CraftingStation>();
    }

    private static void EnsureConfigBindings()
    {
        foreach (RegisteredItemData item in RegisteredItems.Values)
        {
            EnsureConfigBindings(item);
        }
    }

    private static void EnsureConfigBindings(RegisteredItemData item)
    {
        if (item == null || item.Prefab == null)
        {
            return;
        }

        if (!ItemConfigs.TryGetValue(item.Prefab.name, out ItemConfig? itemConfig))
        {
            itemConfig = new ItemConfig();
            ItemConfigs[item.Prefab.name] = itemConfig;
        }

        string configGroup = GetConfigGroup(item);
        string displayName = GetDisplayName(item);

        RecipeDefinition[] orderedRecipes = item.Recipes
            .OrderBy(GetRecipeSortIndex)
            .ThenBy(recipe => GetRecipeConfigKey(recipe), StringComparer.Ordinal)
            .ToArray();

        int recipeOrder = orderedRecipes.Length * 10;
        foreach (RecipeDefinition recipe in orderedRecipes)
        {
            string recipeKey = GetRecipeConfigKey(recipe);
            if (itemConfig.Recipes.ContainsKey(recipeKey))
            {
                recipeOrder -= 10;
                continue;
            }

            string configSuffix = GetRecipeConfigSuffix(recipe);
            string recipeLabel = string.IsNullOrWhiteSpace(configSuffix) ? displayName : $"{displayName}{configSuffix}";
            RecipeConfig recipeConfig = new();

            recipeConfig.CraftingCosts = Config(
                configGroup,
                $"{displayName} - Crafting Costs{configSuffix}",
                SerializedRequirements.Serialize(recipe.Requirements, recipe.CraftAmount),
                OrderedDescription($"Item costs to craft {recipeLabel}. Use item:amount entries and append amount=N. Set amount=0 to disable the recipe.", recipeOrder));
            recipeConfig.CraftingCosts.SettingChanged += (_, _) => ReapplyRuntimeConfiguration(item);

            itemConfig.Recipes[recipeKey] = recipeConfig;
            recipeOrder -= 10;
        }
    }

    private static void ReapplyRuntimeConfiguration(RegisteredItemData item)
    {
        if (ObjectDB.instance == null || !IsObjectDbReady(ObjectDB.instance))
        {
            return;
        }

        bool changed = EnsureItemRegistered(ObjectDB.instance, item);
        changed |= RebuildRecipes(ObjectDB.instance, item);

        if (changed)
        {
            ObjectDB.instance.UpdateRegisters();
            ZenUiCompatibility.InvalidateCraftingRecipeCache();
        }
    }

    private static RecipeDefinition GetEffectiveRecipe(RegisteredItemData item, RecipeDefinition recipe)
    {
        if (!ItemConfigs.TryGetValue(item.Prefab.name, out ItemConfig? itemConfig) ||
            !itemConfig.Recipes.TryGetValue(GetRecipeConfigKey(recipe), out RecipeConfig? recipeConfig))
        {
            return recipe;
        }

        SerializedRequirements.SerializedRecipe recipeData = recipeConfig.CraftingCosts is { } craftConfig
            ? SerializedRequirements.Deserialize(craftConfig.Value, recipe.CraftAmount)
            : new SerializedRequirements.SerializedRecipe(recipe.Requirements, recipe.CraftAmount);

        return new RecipeDefinition(
            recipe.Key,
            Mathf.Clamp(recipeData.CraftAmount, 0, 24),
            FixedScrollCraftingStationName,
            recipe.MinStationLevel,
            recipeData.Requirements);
    }

    private static Piece.Requirement[] ToPieceRequirements(ObjectDB objectDb, IEnumerable<RequirementDefinition> requirements)
    {
        return requirements
            .Where(requirement => !string.IsNullOrWhiteSpace(requirement.ItemName))
            .Select(requirement =>
            {
                ItemDrop? itemDrop = objectDb.GetItemPrefab(requirement.ItemName)?.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    Debug.LogWarning($"[kg.ValheimEnchantmentSystem] The required item '{requirement.ItemName}' does not exist.");
                    return null;
                }

                return new Piece.Requirement
                {
                    m_resItem = itemDrop,
                    m_amount = requirement.Amount
                };
            })
            .Where(requirement => requirement != null)
            .ToArray()!;
    }

    private static string GetConfigGroup(RegisteredItemData item)
    {
        return ScrollRecipeConfigSection;
    }

    private static string GetDisplayName(RegisteredItemData item)
    {
        if (string.IsNullOrWhiteSpace(item.SharedName))
        {
            return item.Prefab.name;
        }

        string localized = SharedLocalizationCache.LocalizeForConfig(item.SharedName).Trim();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, item.SharedName, StringComparison.Ordinal)
            ? item.Prefab.name
            : localized;
    }

    private static string GetRecipeConfigKey(RecipeDefinition recipe)
    {
        return string.IsNullOrWhiteSpace(recipe.Key) ? "Default" : recipe.Key;
    }

    private static int GetRecipeSortIndex(RecipeDefinition recipe)
    {
        return string.IsNullOrWhiteSpace(recipe.Key) ? 0 : 1;
    }

    private static string GetRecipeConfigSuffix(RecipeDefinition recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Key))
        {
            return string.Empty;
        }

        string displayKey = string.Equals(recipe.Key, "ConvertNormal", StringComparison.Ordinal)
            ? "Alt"
            : recipe.Key;
        return $" ({displayKey})";
    }

    private static string BuildRecipeNamePrefix(string prefabName)
    {
        return $"{prefabName}_Recipe_";
    }

    private static string BuildRecipeName(string prefabName, RecipeDefinition recipe)
    {
        string key = string.IsNullOrWhiteSpace(recipe.Key) ? "Default" : recipe.Key;
        return $"{prefabName}_Recipe_{key}";
    }

    private static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description)
    {
        return global::kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem.config(group, name, value, description, true);
    }

    private static ConfigDescription OrderedDescription(string description, int order, AcceptableValueBase? acceptableValues = null)
    {
        return new ConfigDescription(description, acceptableValues, new ConfigurationManagerAttributes { Order = order });
    }

    private static class SerializedRequirements
    {
        internal readonly struct SerializedRecipe
        {
            public readonly RequirementDefinition[] Requirements;
            public readonly int CraftAmount;

            public SerializedRecipe(IEnumerable<RequirementDefinition> requirements, int craftAmount)
            {
                Requirements = requirements?.ToArray() ?? Array.Empty<RequirementDefinition>();
                CraftAmount = craftAmount;
            }
        }

        public static string Serialize(IEnumerable<RequirementDefinition> requirements, int craftAmount)
        {
            List<string> entries = requirements
                .Where(requirement => !string.IsNullOrWhiteSpace(requirement.ItemName))
                .Select(requirement => requirement.Quality > 0
                    ? $"{requirement.ItemName}:{requirement.Amount}:{requirement.Quality}"
                    : $"{requirement.ItemName}:{requirement.Amount}")
                .ToList();
            entries.Add($"amount={Mathf.Clamp(craftAmount, 0, 24)}");
            return string.Join(",", entries);
        }

        public static SerializedRecipe Deserialize(string serialized, int defaultCraftAmount)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return new SerializedRecipe(Array.Empty<RequirementDefinition>(), defaultCraftAmount);
            }

            List<RequirementDefinition> requirements = new();
            int craftAmount = defaultCraftAmount;

            foreach (string rawEntry in serialized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string entry = rawEntry.Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                if (entry.StartsWith("amount=", StringComparison.OrdinalIgnoreCase))
                {
                    string amountValue = entry.Substring("amount=".Length).Trim();
                    if (int.TryParse(amountValue, out int parsedCraftAmount))
                    {
                        craftAmount = parsedCraftAmount;
                    }

                    continue;
                }

                string[] split = entry.Split(':');
                string itemName = split[0].Trim();
                if (string.IsNullOrWhiteSpace(itemName))
                {
                    continue;
                }

                int amount = split.Length > 1 && int.TryParse(split[1], out int parsedAmount) ? parsedAmount : 1;
                int quality = split.Length > 2 && int.TryParse(split[2], out int parsedQuality) ? parsedQuality : 0;
                requirements.Add(new RequirementDefinition(itemName, amount, quality));
            }

            return new SerializedRecipe(requirements, craftAmount);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ApplySceneRegistration(__instance);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class ObjectDB_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ObjectDB __instance)
        {
            ApplyObjectDbRegistration(__instance);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    private static class ObjectDB_CopyOtherDb_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ObjectDB __instance)
        {
            ApplyObjectDbRegistration(__instance);
        }
    }
}
