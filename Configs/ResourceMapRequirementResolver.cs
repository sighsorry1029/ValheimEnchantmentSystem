using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Items_Structures;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace kg.ValheimEnchantmentSystem.Configs;

internal enum ResourceMapBuildStatus
{
    NotReady,
    Success,
    Failed
}

internal sealed class ResourceMapDocument
{
    [YamlMember(Alias = "resourceMap", ApplyNamingConventions = false)]
    public List<ResourceMapTier>? ResourceMap { get; set; }
}

internal sealed class ResourceMapTier
{
    public string Biome { get; set; } = string.Empty;
    public List<string> Materials { get; set; } = new();
}

internal readonly struct ResourceTierAssignment
{
    public ResourceTierAssignment(int rank, char scrollTier, string biome)
    {
        Rank = rank;
        ScrollTier = scrollTier;
        Biome = biome;
    }

    public int Rank { get; }
    public char ScrollTier { get; }
    public string Biome { get; }
}

internal static class ResourceMapRequirementResolver
{
    internal const string DefaultYaml = """
# VES uses this file to assign enchantment scroll tiers automatically from equipment crafting recipes.
# Biome entries are ordered from lowest to highest progression; the highest listed biome among a recipe's mapped materials wins.
# Unmapped recipe materials are ignored when at least one material is mapped, and duplicate materials keep their first mapping.
# Registered custom biome identifiers are supported when their Biome Tier config is set to a valid F-S tier.
# Explicit item assignments in EnchantmentReqs.yml override assignments generated from this resource map.
resourceMap:
- biome: Meadows
  materials:
  - Wood
  - Stone
  - Resin
  - Dandelion
  - Flint
  - LeatherScraps
  - BoneFragments
  - Honey
  - Raspberry
  - Blueberries
  - DeerHide
  - DeerMeat
  - Feathers
  - GreydwarfEye
  - RawMeat
- biome: BlackForest
  materials:
  - HardAntler
  - Bronze
  - BronzeNails
  - Copper
  - Tin
  - Ectoplasm
  - SurtlingCore
  - TrollHide
  - BjornHide
  - FineWood
  - AncientSeed
  - Carrot
  - BjornMeat
  - BjornPaw
  - RoundLog
  - Thistle
- biome: Swamp
  materials:
  - Iron
  - Ooze
  - Entrails
  - Guck
  - Bloodbag
  - Chain
  - ElderBark
  - IronNails
  - Root
  - Turnip
  - WitheredBone
  - CuredSquirrelHamstring
- biome: Ocean
  materials:
  - Chitin
  - Resin
  - SerpentScale
  - SerpentMeat
- biome: Mountain
  materials:
  - Silver
  - Crystal
  - DragonEgg
  - JuteRed
  - Obsidian
  - WolfClaw
  - WolfFang
  - WolfHairBundle
  - WolfMeat
  - WolfPelt
- biome: Plains
  materials:
  - UndeadBjornRibcage
  - BlackMetal
  - DragonTear
  - Barley
  - BarleyFlour
  - BoneFragments
  - ChickenEgg
  - ChickenMeat
  - Flax
  - GoblinTotem
  - LinenThread
  - LoxMeat
  - LoxPelt
  - Needle
  - Tar
- biome: Mistlands
  materials:
  - Eitr
  - Bilebag
  - BlackCore
  - BlackMarble
  - BugMeat
  - Carapace
  - DvergrKeyFragment
  - DvergrNeedle
  - GiantBloodSack
  - HareMeat
  - JuteBlue
  - Mandible
  - Sap
  - ScaleHide
  - Softtissue
  - Wisp
  - YagluthDrop
  - YggdrasilWood
- biome: AshLands
  materials:
  - FlametalNew
  - AskBladder
  - AskHide
  - AsksvinEgg
  - AsksvinMeat
  - Blackwood
  - BoneMawSerpentMeat
  - BonemawSerpentTooth
  - CelestialFeather
  - CeramicPlate
  - CharcoalResin
  - CharredBone
  - CharredCogwheel
  - Charredskull
  - Grausten
  - MoltenCore
  - MorgenHeart
  - MorgenSinew
  - ProustitePowder
  - SulfurStone
  - VoltureEgg
  - VoltureMeat
- biome: DeepNorth
  materials: []
""";

    private const int MaxSkippedRecipeExamples = 10;
    private static bool Initialized;

    private readonly struct AutomaticItemAssignment
    {
        public AutomaticItemAssignment(string prefab, bool armor, ResourceTierAssignment resourceTier)
        {
            Prefab = prefab;
            Armor = armor;
            ResourceTier = resourceTier;
        }

        public string Prefab { get; }
        public bool Armor { get; }
        public ResourceTierAssignment ResourceTier { get; }
    }

    public static void Initialize()
    {
        if (Initialized)
        {
            return;
        }

        if (!TryParseResourceMap(DefaultYaml, out _, out string error))
        {
            throw new InvalidOperationException($"Built-in resource map is invalid: {error}");
        }

        BiomeTierResolver.TierMappingsChanged += () => EnchantmentRequirementRepository.ScheduleAuthoritativeRebuild(force: true);
        Initialized = true;
    }

    internal static ResourceMapBuildStatus BuildAutomaticRequirements(
        out List<SyncedData.EnchantmentReqs> requirements,
        out string error,
        out List<string> warnings)
    {
        requirements = new List<SyncedData.EnchantmentReqs>();
        warnings = new List<string>();
        error = string.Empty;

        if (!TryReadResourceMap(out ResourceMapDocument document, out error))
        {
            return ResourceMapBuildStatus.Failed;
        }

        Dictionary<string, ResourceTierAssignment> resourceTiers;
        try
        {
            resourceTiers = BuildResourceTierMap(document, ResolveConfiguredBiomeTier, warnings);
        }
        catch (Exception ex)
        {
            error = $"{EnchantmentConfigPaths.ResourceMapYaml}: failed to build resource tiers: {ex}";
            return ResourceMapBuildStatus.Failed;
        }
        if (warnings.Count > 0)
        {
            error = $"{EnchantmentConfigPaths.ResourceMapYaml}: {string.Join(" | ", warnings)}";
            return ResourceMapBuildStatus.Failed;
        }

        if ((document.ResourceMap?.Count ?? 0) > 0 && resourceTiers.Count == 0)
        {
            error = $"{EnchantmentConfigPaths.ResourceMapYaml}: no valid biome material mappings were found";
            return ResourceMapBuildStatus.Failed;
        }

        if (resourceTiers.Count == 0)
        {
            return ResourceMapBuildStatus.Success;
        }

        if (!IsObjectDbReady())
        {
            return ResourceMapBuildStatus.NotReady;
        }

        try
        {
            requirements = ScanRecipes(ObjectDB.instance, resourceTiers, warnings);
        }
        catch (Exception ex)
        {
            error = $"Automatic enchantment recipe scan failed: {ex}";
            return ResourceMapBuildStatus.Failed;
        }

        return ResourceMapBuildStatus.Success;
    }

    internal static bool TryParseResourceMap(string text, out ResourceMapDocument document, out string error)
    {
        document = new ResourceMapDocument();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "content is empty";
            return false;
        }

        try
        {
            ResourceMapDocument? parsed = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithDuplicateKeyChecking()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize<ResourceMapDocument>(text);
            if (parsed?.ResourceMap == null)
            {
                error = "required root key 'resourceMap' is missing";
                return false;
            }

            document = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    internal static Dictionary<string, ResourceTierAssignment> BuildResourceTierMap(
        ResourceMapDocument document,
        Func<string, char?> resolveBiomeTier,
        ICollection<string> warnings)
    {
        Dictionary<string, ResourceTierAssignment> result = new(StringComparer.OrdinalIgnoreCase);
        List<ResourceMapTier> tiers = document?.ResourceMap ?? new List<ResourceMapTier>();
        for (int rank = 0; rank < tiers.Count; rank++)
        {
            ResourceMapTier tier = tiers[rank];
            if (tier == null)
            {
                continue;
            }

            char? scrollTier = resolveBiomeTier(tier.Biome ?? string.Empty);
            if (!scrollTier.HasValue || !IsValidScrollTier(scrollTier.Value))
            {
                warnings?.Add($"Skipped resource map biome '{tier.Biome}': no valid enchantment scroll tier is configured.");
                continue;
            }

            foreach (string material in tier.Materials ?? new List<string>())
            {
                string token = NormalizeResourceToken(material);
                if (!string.IsNullOrWhiteSpace(token) && !result.ContainsKey(token))
                {
                    result[token] = new ResourceTierAssignment(rank, scrollTier.Value, tier.Biome?.Trim() ?? string.Empty);
                }
            }
        }

        return result;
    }

    internal static bool TrySelectHighestTier(
        IEnumerable<ResourceTierAssignment> assignments,
        out ResourceTierAssignment highest)
    {
        highest = default;
        bool found = false;
        foreach (ResourceTierAssignment assignment in assignments ?? Array.Empty<ResourceTierAssignment>())
        {
            if (!found || assignment.Rank > highest.Rank)
            {
                highest = assignment;
                found = true;
            }
        }

        return found;
    }

    internal static bool TrySelectMappedRecipeTier(
        IEnumerable<ResourceTierAssignment?> materialAssignments,
        out ResourceTierAssignment highest)
    {
        IEnumerable<ResourceTierAssignment> mappedAssignments = (materialAssignments ?? Array.Empty<ResourceTierAssignment?>())
            .Where(assignment => assignment.HasValue)
            .Select(assignment => assignment!.Value);
        return TrySelectHighestTier(mappedAssignments, out highest);
    }

    internal static string NormalizeResourceToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        string text = CleanPrefabName(token.Trim());
        if (text.StartsWith("$item_", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring("$item_".Length);
        }
        else if (text.StartsWith("$", StringComparison.Ordinal))
        {
            text = text.Substring(1);
        }

        return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private static bool IsValidScrollTier(char tier)
    {
        return "FEDCBAS".IndexOf(char.ToUpperInvariant(tier)) >= 0;
    }

    private static bool TryReadResourceMap(out ResourceMapDocument document, out string error)
    {
        document = new ResourceMapDocument();
        error = string.Empty;
        string path = EnchantmentConfigPaths.ResourceMapYaml;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            error = $"{path}: file does not exist";
            return false;
        }

        try
        {
            string text = File.ReadAllText(path);
            if (TryParseResourceMap(text, out document, out string parseError))
            {
                return true;
            }

            error = $"{path}: {parseError}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"{path}: {ex}";
            return false;
        }
    }

    private static char? ResolveConfiguredBiomeTier(string rawBiome)
    {
        return BiomeTierResolver.TryResolveTierByIdentifier(rawBiome, out char tier) ? tier : null;
    }

    private static List<SyncedData.EnchantmentReqs> ScanRecipes(
        ObjectDB objectDb,
        IReadOnlyDictionary<string, ResourceTierAssignment> resourceTiers,
        ICollection<string> warnings)
    {
        Dictionary<string, AutomaticItemAssignment> assignments = new(StringComparer.Ordinal);
        int skippedRecipeCount = 0;
        List<string> skippedExamples = new();

        foreach (Recipe recipe in objectDb.m_recipes ?? new List<Recipe>())
        {
            if (recipe == null || !recipe.m_enabled || recipe.m_item?.m_itemData?.m_shared == null)
            {
                continue;
            }

            ItemDrop.ItemData item = recipe.m_item.m_itemData;
            if (!TryClassifyEquipment(item, out bool armor))
            {
                continue;
            }

            string itemPrefab = ResolveOutputPrefabName(recipe.m_item);
            if (string.IsNullOrWhiteSpace(itemPrefab))
            {
                continue;
            }

            if (!TryResolveRecipeTier(recipe, resourceTiers, out ResourceTierAssignment resourceTier, out List<string> unknownMaterials))
            {
                if (unknownMaterials.Count > 0)
                {
                    skippedRecipeCount++;
                    if (skippedExamples.Count < MaxSkippedRecipeExamples)
                    {
                        skippedExamples.Add($"{itemPrefab} ({string.Join(", ", unknownMaterials)})");
                    }
                }

                continue;
            }

            AutomaticItemAssignment candidate = new(itemPrefab, armor, resourceTier);
            if (!assignments.TryGetValue(itemPrefab, out AutomaticItemAssignment current) ||
                candidate.ResourceTier.Rank > current.ResourceTier.Rank)
            {
                assignments[itemPrefab] = candidate;
            }
        }

        if (skippedRecipeCount > 0)
        {
            string suffix = skippedRecipeCount > skippedExamples.Count ? ", ..." : string.Empty;
            warnings?.Add(
                $"Skipped {skippedRecipeCount} automatic enchantment recipe mapping(s) because none of their base materials exist in resourcemap.yml. " +
                $"Examples: {string.Join("; ", skippedExamples)}{suffix}");
        }

        Dictionary<string, List<string>> groupedItems = new(StringComparer.Ordinal);
        foreach (AutomaticItemAssignment assignment in assignments.Values.OrderBy(entry => entry.Prefab, StringComparer.Ordinal))
        {
            string type = assignment.Armor ? "Armor" : "Weapon";
            string key = $"{assignment.ResourceTier.ScrollTier}|{type}";
            if (!groupedItems.TryGetValue(key, out List<string> items))
            {
                items = new List<string>();
                groupedItems[key] = items;
            }

            items.Add(assignment.Prefab);
        }

        List<SyncedData.EnchantmentReqs> result = new();
        foreach (KeyValuePair<string, List<string>> entry in groupedItems.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            string[] parts = entry.Key.Split('|');
            char tier = parts[0][0];
            string type = parts[1];
            result.Add(new SyncedData.EnchantmentReqs
            {
                enchant_prefab = new SyncedData.SingleReq($"kg_EnchantScroll_{type}_{tier}"),
                blessed_enchant_prefab = new SyncedData.SingleReq($"kg_EnchantScroll_{type}_Blessed_{tier}"),
                Items = entry.Value
            });
        }

        return result;
    }

    private static bool TryResolveRecipeTier(
        Recipe recipe,
        IReadOnlyDictionary<string, ResourceTierAssignment> resourceTiers,
        out ResourceTierAssignment highest,
        out List<string> unknownMaterials)
    {
        highest = default;
        unknownMaterials = new List<string>();
        List<ResourceTierAssignment?> resolved = new();

        foreach (Piece.Requirement requirement in recipe.m_resources ?? Array.Empty<Piece.Requirement>())
        {
            if (requirement == null || requirement.m_amount <= 0)
            {
                continue;
            }

            if (requirement.m_resItem == null ||
                !TryGetResourceTier(requirement.m_resItem, resourceTiers, out ResourceTierAssignment resourceTier))
            {
                unknownMaterials.Add(ResolveResourceName(requirement?.m_resItem));
                resolved.Add(null);
                continue;
            }

            resolved.Add(resourceTier);
        }

        unknownMaterials = unknownMaterials
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return TrySelectMappedRecipeTier(resolved, out highest);
    }

    private static bool TryGetResourceTier(
        ItemDrop itemDrop,
        IReadOnlyDictionary<string, ResourceTierAssignment> resourceTiers,
        out ResourceTierAssignment resourceTier)
    {
        foreach (string token in GetResourceTokens(itemDrop))
        {
            if (resourceTiers.TryGetValue(token, out resourceTier))
            {
                return true;
            }
        }

        resourceTier = default;
        return false;
    }

    private static IEnumerable<string> GetResourceTokens(ItemDrop itemDrop)
    {
        string prefabName = itemDrop?.name ?? string.Empty;
        string dropPrefabName = itemDrop?.m_itemData?.m_dropPrefab?.name ?? string.Empty;
        string sharedName = itemDrop?.m_itemData?.m_shared?.m_name ?? string.Empty;
        foreach (string value in new[] { prefabName, dropPrefabName, sharedName })
        {
            string token = NormalizeResourceToken(value);
            if (!string.IsNullOrWhiteSpace(token))
            {
                yield return token;
            }
        }
    }

    private static bool TryClassifyEquipment(ItemDrop.ItemData item, out bool armor)
    {
        armor = false;
        if (item?.m_shared == null)
        {
            return false;
        }

        if (IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Helmet) ||
            IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Chest) ||
            IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Legs) ||
            IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Shoulder))
        {
            armor = true;
            return true;
        }

        return IsShield(item) ||
               item.m_shared.m_skillType == Skills.SkillType.Pickaxes ||
               IsSkillAttack(item, Skills.SkillType.Swords) ||
               IsSkillAttack(item, Skills.SkillType.Axes) ||
               IsSkillAttack(item, Skills.SkillType.Clubs) ||
               IsSkillAttack(item, Skills.SkillType.Knives) ||
               IsSkillAttack(item, Skills.SkillType.Spears) ||
               IsPolearm(item) ||
               IsSkillAttack(item, Skills.SkillType.Unarmed) ||
               IsBow(item) ||
               IsCrossbow(item) ||
               IsSkillAttack(item, Skills.SkillType.ElementalMagic) ||
               IsBloodMagicSummonWeapon(item);
    }

    private static bool IsShield(ItemDrop.ItemData item)
    {
        return IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Shield) ||
               item.m_shared.m_skillType == Skills.SkillType.Blocking;
    }

    private static bool IsBow(ItemDrop.ItemData item)
    {
        return (item.m_shared.m_skillType != Skills.SkillType.Crossbows &&
                item.m_shared.m_skillType != Skills.SkillType.ElementalMagic &&
                item.m_shared.m_skillType != Skills.SkillType.BloodMagic &&
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow) ||
               IsSkillAttack(item, Skills.SkillType.Bows);
    }

    private static bool IsCrossbow(ItemDrop.ItemData item)
    {
        return item.m_shared.m_skillType == Skills.SkillType.Crossbows &&
               (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || HasAttackAnimation(item));
    }

    private static bool IsPolearm(ItemDrop.ItemData item)
    {
        return IsSkillAttack(item, Skills.SkillType.Polearms) ||
               IsItemTypeOrAttach(item, ItemDrop.ItemData.ItemType.Attach_Atgeir);
    }

    private static bool IsBloodMagicSummonWeapon(ItemDrop.ItemData item)
    {
        return item.m_shared.m_skillType == Skills.SkillType.BloodMagic &&
               (UsesSpawnAbility(item.m_shared.m_attack) || UsesSpawnAbility(item.m_shared.m_secondaryAttack));
    }

    private static bool UsesSpawnAbility(Attack? attack)
    {
        GameObject projectile = attack?.m_attackProjectile;
        return projectile != null &&
               (projectile.GetComponent<SpawnAbility>() != null || projectile.GetComponentInChildren<SpawnAbility>(true) != null);
    }

    private static bool IsSkillAttack(ItemDrop.ItemData item, Skills.SkillType skillType)
    {
        return item.m_shared.m_skillType == skillType &&
               HasAttackAnimation(item) &&
               item.m_shared.m_damages.GetTotalDamage() > 0f;
    }

    private static bool HasAttackAnimation(ItemDrop.ItemData item)
    {
        return !string.IsNullOrWhiteSpace(item.m_shared.m_attack?.m_attackAnimation) ||
               !string.IsNullOrWhiteSpace(item.m_shared.m_secondaryAttack?.m_attackAnimation);
    }

    private static bool IsItemTypeOrAttach(ItemDrop.ItemData item, ItemDrop.ItemData.ItemType itemType)
    {
        return item.m_shared.m_itemType == itemType || item.m_shared.m_attachOverride == itemType;
    }

    private static string ResolveOutputPrefabName(ItemDrop itemDrop)
    {
        string dropPrefabName = itemDrop?.m_itemData?.m_dropPrefab?.name ?? string.Empty;
        return CleanPrefabName(string.IsNullOrWhiteSpace(dropPrefabName) ? itemDrop?.name ?? string.Empty : dropPrefabName);
    }

    private static string ResolveResourceName(ItemDrop? itemDrop)
    {
        return itemDrop == null ? "<missing resource>" : CleanPrefabName(itemDrop.name);
    }

    private static string CleanPrefabName(string value)
    {
        string result = value?.Trim() ?? string.Empty;
        const string cloneSuffix = "(Clone)";
        return result.EndsWith(cloneSuffix, StringComparison.OrdinalIgnoreCase)
            ? result.Substring(0, result.Length - cloneSuffix.Length).Trim()
            : result;
    }

    private static bool IsObjectDbReady()
    {
        return ObjectDB.instance != null && ObjectDB.instance.m_items != null && ObjectDB.instance.m_recipes != null;
    }

    // Keep these hooks under this root so its initialization failure still blocks them in PatchRegistry.
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class ObjectDB_Awake_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            EnchantmentRequirementRepository.ScheduleAuthoritativeRebuild();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    private static class ObjectDB_CopyOtherDB_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            EnchantmentRequirementRepository.ScheduleAuthoritativeRebuild();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.UpdateRegisters))]
    private static class ObjectDB_UpdateRegisters_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            EnchantmentRequirementRepository.ScheduleAuthoritativeRebuild();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    private static class ZNet_Awake_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            EnchantmentRequirementRepository.ScheduleAuthoritativeRebuild();
        }
    }
}
