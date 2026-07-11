using ServerSync;
using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

internal static class EnchantmentRequirementRepository
{
    private static readonly Dictionary<string, SyncedData.EnchantmentReqs> OptimizedRequirements = new(StringComparer.Ordinal);
    private static List<SyncedData.EnchantmentReqs>? CachedRequirements;
    private static bool IsInitialized;

    public static void Initialize(CustomSyncedValue<List<SyncedData.EnchantmentReqs>> target)
    {
        EnsureSubscriptions(target);
        OptimizeRequirementsCache(target.Value);
    }

    public static void LoadAuthoritativeData(CustomSyncedValue<List<SyncedData.EnchantmentReqs>> target)
    {
        if (TryParseReqsText(Defaults.YAML_Reqs, "built-in defaults", out List<SyncedData.EnchantmentReqs> builtInReqs, out string fallbackError))
        {
            target.Value = builtInReqs;
        }
        else
        {
            Utils.print($"Failed to load built-in defaults for enchantment requirements: {fallbackError}", ConsoleColor.Red);
            target.Value = new List<SyncedData.EnchantmentReqs>();
        }

        if (!TryReload(target))
        {
            Utils.print("Using built-in defaults for enchantment requirements.", ConsoleColor.Yellow);
        }

        OptimizeRequirementsCache(target.Value);
    }

    public static bool TryReload(CustomSyncedValue<List<SyncedData.EnchantmentReqs>> target)
    {
        if (!TryRead(out List<SyncedData.EnchantmentReqs> result, out string error, out List<string> warnings))
        {
            Utils.print($"Skipped reload for enchantment requirements: {error}", ConsoleColor.Red);
            return false;
        }

        foreach (string warning in warnings)
        {
            Utils.print(warning, ConsoleColor.Yellow);
        }

        target.Value = result;
        return true;
    }

    public static SyncedData.EnchantmentReqs GetReqs(List<SyncedData.EnchantmentReqs> requirements, string prefab)
    {
        if (string.IsNullOrEmpty(prefab))
        {
            return null;
        }

        if (!ReferenceEquals(CachedRequirements, requirements))
        {
            OptimizeRequirementsCache(requirements);
        }

        return OptimizedRequirements.TryGetValue(prefab, out SyncedData.EnchantmentReqs reqs) ? reqs : null;
    }

    private static void EnsureSubscriptions(CustomSyncedValue<List<SyncedData.EnchantmentReqs>> target)
    {
        if (IsInitialized)
        {
            return;
        }

        target.ValueChanged += () => OptimizeRequirementsCache(target.Value);
        IsInitialized = true;
    }

    private static void OptimizeRequirementsCache(List<SyncedData.EnchantmentReqs>? requirements)
    {
        OptimizedRequirements.Clear();
        CachedRequirements = requirements;
        if (requirements == null)
        {
            return;
        }

        foreach (SyncedData.EnchantmentReqs requirement in requirements)
        {
            if (requirement?.Items == null)
            {
                continue;
            }

            foreach (string itemPrefab in requirement.Items)
            {
                if (string.IsNullOrWhiteSpace(itemPrefab))
                {
                    continue;
                }

                OptimizedRequirements[itemPrefab] = requirement;
            }
        }
    }

    private static bool TryRead(out List<SyncedData.EnchantmentReqs> result, out string error, out List<string> warnings)
    {
        result = new List<SyncedData.EnchantmentReqs>();
        warnings = new List<string>();
        Dictionary<string, SyncedData.EnchantmentReqs> requirementsByOwner = new(StringComparer.Ordinal);
        Dictionary<string, string> itemOwners = new(StringComparer.Ordinal);
        bool loadedAnySource = false;

        if (TryParseReqsFile(EnchantmentConfigPaths.RequirementsYaml, out List<SyncedData.EnchantmentReqs> baseReqs, out string mainError))
        {
            MergeRequirements(baseReqs, EnchantmentConfigPaths.RequirementsYaml, result, requirementsByOwner, itemOwners, warnings);
            loadedAnySource = true;
        }
        else
        {
            warnings.Add($"Skipped enchantment requirements file: {mainError}");
        }

        if (!TryGetYamlFiles(EnchantmentConfigPaths.AdditionalRequirementsDirectory, out string[] files, out string fileError))
        {
            warnings.Add($"Skipped enchantment requirements directory: {fileError}");
        }
        else
        {
            foreach (string file in files)
            {
                if (!TryParseReqsFile(file, out List<SyncedData.EnchantmentReqs> parsed, out string parseError))
                {
                    warnings.Add($"Skipped enchantment requirements file: {parseError}");
                    continue;
                }

                MergeRequirements(parsed, file, result, requirementsByOwner, itemOwners, warnings);
                loadedAnySource = true;
            }
        }

        ResourceMapBuildStatus automaticStatus = ResourceMapRequirementResolver.BuildAutomaticRequirements(
            out List<SyncedData.EnchantmentReqs> automaticRequirements,
            out string automaticError,
            out List<string> automaticWarnings);
        warnings.AddRange(automaticWarnings);
        if (automaticStatus == ResourceMapBuildStatus.Failed)
        {
            error = automaticError;
            return false;
        }

        if (automaticStatus == ResourceMapBuildStatus.Success && automaticRequirements.Count > 0)
        {
            MergeRequirements(
                automaticRequirements,
                "automatic resource map",
                result,
                requirementsByOwner,
                itemOwners,
                warnings,
                warnOnDuplicate: false);
            loadedAnySource = true;
        }

        if (!loadedAnySource)
        {
            error = warnings.Count > 0
                ? string.Join(" | ", warnings)
                : "No valid enchantment requirements were loaded.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseReqsFile(string path, out List<SyncedData.EnchantmentReqs> result, out string error)
    {
        result = new List<SyncedData.EnchantmentReqs>();

        if (!File.Exists(path))
        {
            error = $"{path}: file does not exist";
            return false;
        }

        try
        {
            string text = File.ReadAllText(path);
            return TryParseReqsText(text, path, out result, out error);
        }
        catch (Exception ex)
        {
            error = $"{path}: {ex}";
            return false;
        }
    }

    private static bool TryParseReqsText(string text, string source, out List<SyncedData.EnchantmentReqs> result, out string error)
    {
        result = new List<SyncedData.EnchantmentReqs>();

        if (!HasYamlCompatibleListIndentation(text))
        {
            error = $"{source}: invalid reqs indentation, use spaces for list indentation and avoid tabs";
            return false;
        }

        if (!TryDeserializeReqMap(text, out Dictionary<string, List<string>> map, out error))
        {
            error = $"{source}: {error}";
            return false;
        }

        return TryConvertSimpleReqs(map, source, out result, out error);
    }

    private static bool TryDeserializeReqMap(string text, out Dictionary<string, List<string>> map, out string error)
    {
        map = new Dictionary<string, List<string>>();
        error = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "content is empty";
                return false;
            }

            Dictionary<string, List<string>>? parsed = new DeserializerBuilder()
                .WithDuplicateKeyChecking()
                .Build()
                .Deserialize<Dictionary<string, List<string>>>(text);
            if (parsed == null)
            {
                error = "deserialized to null";
                return false;
            }

            map = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
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

    private static bool HasYamlCompatibleListIndentation(string text)
    {
        using StringReader reader = new(text);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#") || !trimmed.StartsWith("-"))
            {
                continue;
            }

            int dashIndex = line.IndexOf('-');
            if (dashIndex < 0)
            {
                continue;
            }

            string leading = line.Substring(0, dashIndex);
            if (leading.Contains('\t') || leading.Any(c => c != ' '))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryConvertSimpleReqs(Dictionary<string, List<string>> map, string source, out List<SyncedData.EnchantmentReqs> result, out string error)
    {
        result = new List<SyncedData.EnchantmentReqs>();
        foreach (KeyValuePair<string, List<string>> entry in map)
        {
            if (!TryResolveReqKey(entry.Key, out SyncedData.SingleReq enchant, out SyncedData.SingleReq blessed, out string reason))
            {
                error = $"{source}: invalid reqs key '{entry.Key}'. {reason}";
                return false;
            }

            result.Add(new SyncedData.EnchantmentReqs
            {
                enchant_prefab = enchant,
                blessed_enchant_prefab = blessed,
                Items = entry.Value?
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList() ?? new List<string>()
            });
        }

        error = string.Empty;
        return true;
    }

    private static void MergeRequirements(
        IEnumerable<SyncedData.EnchantmentReqs> parsedRequirements,
        string source,
        List<SyncedData.EnchantmentReqs> mergedRequirements,
        Dictionary<string, SyncedData.EnchantmentReqs> requirementsByOwner,
        Dictionary<string, string> itemOwners,
        List<string> warnings,
        bool warnOnDuplicate = true)
    {
        foreach (SyncedData.EnchantmentReqs parsedRequirement in parsedRequirements)
        {
            if (parsedRequirement?.Items == null)
            {
                continue;
            }

            string ownerKey = GetRequirementOwnerKey(parsedRequirement);
            string ownerLabel = GetRequirementOwnerLabel(parsedRequirement);

            foreach (string itemPrefab in parsedRequirement.Items)
            {
                if (string.IsNullOrWhiteSpace(itemPrefab))
                {
                    continue;
                }

                string normalizedItem = itemPrefab.Trim();
                if (itemOwners.TryGetValue(normalizedItem, out string existingOwner))
                {
                    if (warnOnDuplicate)
                    {
                        warnings.Add($"{source}: duplicate item prefab '{normalizedItem}' is mapped to both '{existingOwner}' and '{ownerLabel}'. Keeping '{existingOwner}'.");
                    }

                    continue;
                }

                if (!requirementsByOwner.TryGetValue(ownerKey, out SyncedData.EnchantmentReqs mergedRequirement))
                {
                    mergedRequirement = new SyncedData.EnchantmentReqs
                    {
                        enchant_prefab = parsedRequirement.enchant_prefab,
                        blessed_enchant_prefab = parsedRequirement.blessed_enchant_prefab,
                        Items = new List<string>()
                    };
                    requirementsByOwner[ownerKey] = mergedRequirement;
                    mergedRequirements.Add(mergedRequirement);
                }

                itemOwners[normalizedItem] = ownerLabel;
                mergedRequirement.Items.Add(normalizedItem);
            }
        }
    }

    private static string GetRequirementOwnerKey(SyncedData.EnchantmentReqs requirement)
    {
        string enchant = requirement?.enchant_prefab?.prefab?.Trim() ?? string.Empty;
        string blessed = requirement?.blessed_enchant_prefab?.prefab?.Trim() ?? string.Empty;
        return $"{enchant}|{blessed}";
    }

    private static string GetRequirementOwnerLabel(SyncedData.EnchantmentReqs requirement)
    {
        if (!string.IsNullOrWhiteSpace(requirement?.enchant_prefab?.prefab))
        {
            return requirement.enchant_prefab.prefab;
        }

        return "<unknown>";
    }

    private static bool TryResolveReqKey(string key, out SyncedData.SingleReq enchant, out SyncedData.SingleReq blessed, out string reason)
    {
        enchant = null;
        blessed = null;
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(key))
        {
            reason = $"Expected format '(Tier)Weapon' or '(Tier)Armor' with tiers [{GetAllowedTierList()}].";
            return false;
        }

        string normalized = key.Trim();
        if (normalized.Length < 4 || normalized[0] != '(' || normalized[2] != ')')
        {
            reason = $"Expected exact format '(Tier)Weapon' or '(Tier)Armor' with tiers [{GetAllowedTierList()}].";
            return false;
        }

        char tier = char.ToUpperInvariant(normalized[1]);
        if (!EnchantmentTierCatalog.IsValid(tier))
        {
            reason = $"Tier '{normalized[1]}' is invalid. Allowed tiers: [{GetAllowedTierList()}].";
            return false;
        }

        string suffix = normalized.Substring(3);
        string type;
        if (string.Equals(suffix, "Armor", StringComparison.OrdinalIgnoreCase))
        {
            type = "Armor";
        }
        else if (string.Equals(suffix, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            type = "Weapon";
        }
        else
        {
            reason = $"Type segment '{suffix}' is invalid. Expected 'Weapon' or 'Armor'.";
            return false;
        }

        enchant = new SyncedData.SingleReq($"kg_EnchantScroll_{type}_{tier}");
        blessed = new SyncedData.SingleReq($"kg_EnchantScroll_{type}_Blessed_{tier}");
        return true;
    }

    private static string GetAllowedTierList()
    {
        return string.Join(", ", EnchantmentTierCatalog.AllTiers);
    }
}
