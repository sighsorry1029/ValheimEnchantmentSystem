using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Integrations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

[VES_Autoload(VES_Autoload.Priority.Normal, "OnInit", typeof(IntegrationRegistry))]
public static class BiomeTierResolver
{
    private const string ExpandWorldBiomeDirectoryName = "expand_world";
    private const string ExpandWorldBiomeFilePattern = "expand_biomes*.yaml";
    private static readonly Dictionary<Heightmap.Biome, ConfigEntry<string>> BuiltInBiomeTiers = new();
    private static readonly Dictionary<Heightmap.Biome, ConfigEntry<string>> ResolvedCustomBiomeTiers = new();
    private static readonly Dictionary<string, ConfigEntry<string>> CustomBiomeTierByIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Heightmap.Biome> LoggedResolvedCustomBiomeNumbersByIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    [UsedImplicitly]
    private static void OnInit()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        RegisterBuiltInBiomeConfigs();
        IntegrationRegistry.BiomesChanged += RefreshCustomBiomeConfigs;
        RefreshCustomBiomeConfigs();
    }

    public static bool TryResolveTier(Character character, out char tier)
    {
        tier = default;
        if (character == null)
            return false;

        return TryResolveTier(ResolveBiome(character.transform.position), out tier);
    }

    public static bool TryResolveTier(Heightmap.Biome biome, out char tier)
    {
        tier = default;

        if (BuiltInBiomeTiers.TryGetValue(biome, out ConfigEntry<string> exactTier))
            return EnchantmentTierCatalog.TryParse(exactTier.Value, out tier);
        if (ResolvedCustomBiomeTiers.TryGetValue(biome, out ConfigEntry<string> customExactTier))
            return EnchantmentTierCatalog.TryParse(customExactTier.Value, out tier);

        int bestMatch = int.MinValue;
        foreach (KeyValuePair<Heightmap.Biome, ConfigEntry<string>> entry in EnumerateBiomeTierMappings())
        {
            if ((biome & entry.Key) == 0)
                continue;

            int currentValue = (int)entry.Key;
            if (currentValue <= bestMatch)
                continue;
            if (!EnchantmentTierCatalog.TryParse(entry.Value.Value, out char candidateTier))
                continue;

            bestMatch = currentValue;
            tier = candidateTier;
        }

        return bestMatch != int.MinValue;
    }

    private static Heightmap.Biome ResolveBiome(Vector3 position)
    {
        if (WorldGenerator.instance != null)
        {
            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(position);
            if (biome != Heightmap.Biome.None)
                return biome;
        }

        return EnvMan.instance != null ? EnvMan.instance.m_currentBiome : Heightmap.Biome.None;
    }

    private static void RegisterBuiltInBiomeConfigs()
    {
        foreach (KeyValuePair<Heightmap.Biome, char> entry in EnchantmentTierCatalog.BuiltInBiomeTiers)
        {
            RegisterBuiltInBiomeConfig(entry.Key, entry.Value);
        }
    }

    private static void RegisterBuiltInBiomeConfig(Heightmap.Biome biome, char defaultTier)
    {
        if (BuiltInBiomeTiers.ContainsKey(biome))
            return;

        string name = GetBuiltInBiomeConfigName(biome);
        string description = $"Tier of scrolls {biome} (F E D C B A S)";
        BuiltInBiomeTiers[biome] = ValheimEnchantmentSystem.config("Scrolls", name, defaultTier.ToString(), description);
    }

    private static string GetBuiltInBiomeConfigName(Heightmap.Biome biome)
    {
        return biome switch
        {
            Heightmap.Biome.Meadows => "1 - Meadows Tier",
            Heightmap.Biome.BlackForest => "2 - BlackForest Tier",
            Heightmap.Biome.Swamp => "3 - Swamp Tier",
            Heightmap.Biome.Ocean => "4 - Ocean Tier",
            Heightmap.Biome.Mountain => "5 - Mountain Tier",
            Heightmap.Biome.Plains => "6 - Plains Tier",
            Heightmap.Biome.Mistlands => "7 - Mistlands Tier",
            Heightmap.Biome.AshLands => "8 - Ashlands Tier",
            Heightmap.Biome.DeepNorth => "9 - DeepNorth Tier",
            _ => $"{biome} Tier"
        };
    }

    private static void RefreshCustomBiomeConfigs()
    {
        bool addedAny = RegisterDeclaredCustomBiomeConfigsFromYaml();

        if (!IntegrationRegistry.TryGetCustomBiomes(out List<CustomBiomeDefinition> customBiomes))
        {
            if (addedAny)
            {
                ValheimEnchantmentSystem._thistype.Config.Save();
            }

            return;
        }

        ResolvedCustomBiomeTiers.Clear();

        foreach (CustomBiomeDefinition biome in customBiomes.OrderBy(entry => (int)entry.Biome))
        {
            if (biome.Biome == Heightmap.Biome.None || !TryNormalizeCustomBiomeIdentifier(biome.Identifier, out string identifier))
                continue;

            bool addedFromRuntime = TryGetOrCreateCustomBiomeConfig(identifier, biome.Identifier, out ConfigEntry<string> configEntry);
            addedAny |= addedFromRuntime;

            ResolvedCustomBiomeTiers[biome.Biome] = configEntry;
            LogResolvedCustomBiomeNumberIfChanged(identifier, biome.Identifier, biome.Biome);
        }

        if (addedAny)
        {
            ValheimEnchantmentSystem._thistype.Config.Save();
        }
    }

    private static bool RegisterDeclaredCustomBiomeConfigsFromYaml()
    {
        if (!ExpandWorldDataIntegration.Instance.IsAvailable())
        {
            return false;
        }

        if (!TryGetDeclaredCustomBiomeIdentifiers(out List<string> identifiers))
        {
            return false;
        }

        bool addedAny = false;
        foreach (string identifier in identifiers)
        {
            if (!TryGetOrCreateCustomBiomeConfig(identifier, identifier, out _))
            {
                continue;
            }

            Utils.print($"Registered custom biome scroll tier config slot: {identifier}");
            addedAny = true;
        }

        return addedAny;
    }

    private static bool TryGetDeclaredCustomBiomeIdentifiers(out List<string> identifiers)
    {
        identifiers = new List<string>();

        string directory = Path.Combine(Paths.ConfigPath, ExpandWorldBiomeDirectoryName);
        if (!Directory.Exists(directory))
        {
            return false;
        }

        string[] files;
        try
        {
            files = Directory
                .GetFiles(directory, ExpandWorldBiomeFilePattern)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            Utils.print($"Failed to enumerate Expand World Data biome yaml files: {ex.Message}", ConsoleColor.Yellow);
            return false;
        }

        if (files.Length == 0)
        {
            return false;
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            if (!TryReadDeclaredBiomeYaml(file, out List<DeclaredCustomBiomeYamlEntry> yamlEntries, out string error))
            {
                Utils.print($"Failed to parse Expand World Data biome yaml '{Path.GetFileName(file)}': {error}", ConsoleColor.Yellow);
                continue;
            }

            foreach (DeclaredCustomBiomeYamlEntry entry in yamlEntries)
            {
                if (!TryNormalizeCustomBiomeIdentifier(entry.biome, out string identifier))
                {
                    continue;
                }

                if (IsBuiltInBiomeIdentifier(identifier) || !seen.Add(identifier))
                {
                    continue;
                }

                identifiers.Add(identifier);
            }
        }

        return identifiers.Count > 0;
    }

    private static bool TryReadDeclaredBiomeYaml(string path, out List<DeclaredCustomBiomeYamlEntry> yamlEntries, out string error)
    {
        yamlEntries = new List<DeclaredCustomBiomeYamlEntry>();
        error = string.Empty;

        try
        {
            if (!File.Exists(path))
            {
                error = "file does not exist";
                return false;
            }

            string text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "content is empty";
                return false;
            }

            List<DeclaredCustomBiomeYamlEntry>? parsed = new YamlDotNet.Serialization.DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize<List<DeclaredCustomBiomeYamlEntry>>(text);

            if (parsed == null)
            {
                error = "deserialized to null";
                return false;
            }

            yamlEntries = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    private static bool TryGetOrCreateCustomBiomeConfig(string normalizedIdentifier, string rawIdentifier, out ConfigEntry<string> configEntry)
    {
        configEntry = null!;
        if (!TryNormalizeCustomBiomeIdentifier(normalizedIdentifier, out string identifier))
        {
            return false;
        }

        if (CustomBiomeTierByIdentifier.TryGetValue(identifier, out configEntry))
        {
            return false;
        }

        string label = $"Custom - {identifier} Tier";
        string description = $"Tier of scrolls {rawIdentifier} (ExpandWorldData custom biome identifier, use F E D C B A S)";
        configEntry = ValheimEnchantmentSystem.config("Scrolls", label, string.Empty, description);
        CustomBiomeTierByIdentifier[identifier] = configEntry;
        return true;
    }

    private static void LogResolvedCustomBiomeNumberIfChanged(string normalizedIdentifier, string rawIdentifier, Heightmap.Biome biome)
    {
        if (LoggedResolvedCustomBiomeNumbersByIdentifier.TryGetValue(normalizedIdentifier, out Heightmap.Biome loggedBiome) && loggedBiome == biome)
        {
            return;
        }

        LoggedResolvedCustomBiomeNumbersByIdentifier[normalizedIdentifier] = biome;
        Utils.print($"Resolved custom biome scroll tier config: {rawIdentifier} [{(int)biome}]");
    }

    private static bool TryNormalizeCustomBiomeIdentifier(string identifier, out string normalizedIdentifier)
    {
        normalizedIdentifier = NormalizeCustomBiomeIdentifier(identifier);
        return normalizedIdentifier.Length > 0 && !string.Equals(normalizedIdentifier, "none", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCustomBiomeIdentifier(string identifier)
    {
        return identifier.Replace('\r', ' ').Replace('\n', ' ').Trim().ToLowerInvariant();
    }

    private static bool IsBuiltInBiomeIdentifier(string identifier)
    {
        return Enum.TryParse(identifier, true, out Heightmap.Biome biome) && EnchantmentTierCatalog.IsBuiltInBiome(biome);
    }

    private static IEnumerable<KeyValuePair<Heightmap.Biome, ConfigEntry<string>>> EnumerateBiomeTierMappings()
    {
        foreach (KeyValuePair<Heightmap.Biome, ConfigEntry<string>> entry in BuiltInBiomeTiers)
            yield return entry;

        foreach (KeyValuePair<Heightmap.Biome, ConfigEntry<string>> entry in ResolvedCustomBiomeTiers)
            yield return entry;
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static class ZoneSystem_Start_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            RefreshCustomBiomeConfigs();
        }
    }

    private sealed class DeclaredCustomBiomeYamlEntry
    {
        public string biome { get; set; } = string.Empty;
    }
}
