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
    string Name { get; }
    bool IsAvailable();
    void Initialize();
}

internal interface IEnchantCompatibilityRule
{
    bool CanEnchant(ItemDrop.ItemData item, out string message);
}

internal interface IEnchantmentStateIntegration
{
    void Apply(Enchantment_Core.Enchanted enchantment);
    void ApplyUpgraded(Enchantment_Core.Enchanted enchantment);
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
