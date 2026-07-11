namespace kg.ValheimEnchantmentSystem.Integrations;

public readonly struct CustomBiomeDefinition
{
    public readonly Heightmap.Biome Biome;
    public readonly string Identifier;

    public CustomBiomeDefinition(Heightmap.Biome biome, string identifier)
    {
        Biome = biome;
        Identifier = identifier;
    }
}

internal interface IOptionalIntegration
{
    bool IsAvailable();
    void Initialize();
}

internal interface IInventoryGridCompatibility
{
    int GetReservedTopRows();
}

internal interface IBiomeCatalogSource
{
    event Action? Changed;
    bool TryGetCustomBiomes(out List<CustomBiomeDefinition> customBiomes);
}
