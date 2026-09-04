namespace kg.ValheimEnchantmentSystem.Configs;

public static class EnchantmentTierCatalog
{
    private static readonly char[] OrderedTierValues = { 'F', 'E', 'D', 'C', 'B', 'A', 'S' };
    private static readonly HashSet<char> ValidTiers = new(OrderedTierValues);
    private static readonly Dictionary<char, int> SkillScrollDefaultExpByTier = new()
    {
        { 'F', 2 },
        { 'E', 3 },
        { 'D', 5 },
        { 'C', 8 },
        { 'B', 13 },
        { 'A', 21 },
        { 'S', 34 }
    };
    private static readonly Dictionary<Heightmap.Biome, char> BuiltInBiomeTierByBiome = new()
    {
        { Heightmap.Biome.Meadows, 'F' },
        { Heightmap.Biome.BlackForest, 'F' },
        { Heightmap.Biome.Swamp, 'E' },
        { Heightmap.Biome.Ocean, 'E' },
        { Heightmap.Biome.Mountain, 'D' },
        { Heightmap.Biome.Plains, 'C' },
        { Heightmap.Biome.Mistlands, 'B' },
        { Heightmap.Biome.AshLands, 'A' },
        { Heightmap.Biome.DeepNorth, 'S' }
    };

    public static IEnumerable<char> AllTiers => OrderedTierValues;

    public static IEnumerable<KeyValuePair<Heightmap.Biome, char>> BuiltInBiomeTiers => BuiltInBiomeTierByBiome;

    public static bool IsValid(char tier)
    {
        return ValidTiers.Contains(char.ToUpperInvariant(tier));
    }

    public static bool TryParse(string? rawTier, out char tier)
    {
        tier = default;
        if (string.IsNullOrWhiteSpace(rawTier))
            return false;

        tier = char.ToUpperInvariant(rawTier.Trim()[0]);
        return IsValid(tier);
    }

    public static bool IsBuiltInBiome(Heightmap.Biome biome)
    {
        return BuiltInBiomeTierByBiome.ContainsKey(biome);
    }

    public static bool TryGetDefaultBiomeTier(Heightmap.Biome biome, out char tier)
    {
        return BuiltInBiomeTierByBiome.TryGetValue(biome, out tier);
    }

    public static string GetDefaultBiomeTierValue(Heightmap.Biome biome)
    {
        return TryGetDefaultBiomeTier(biome, out char tier) ? tier.ToString() : string.Empty;
    }

    public static int GetDefaultSkillScrollExp(char tier)
    {
        return SkillScrollDefaultExpByTier.TryGetValue(char.ToUpperInvariant(tier), out int exp) ? exp : 0;
    }
}
