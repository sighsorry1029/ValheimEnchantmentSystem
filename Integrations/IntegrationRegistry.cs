using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.Integrations;

[VES_Autoload(VES_Autoload.Priority.Normal, "OnInit", typeof(SyncedData))]
public static class IntegrationRegistry
{
    private static readonly IOptionalIntegration[] Integrations =
    {
        AugaIntegration.Instance,
        JewelcraftingIntegration.Instance,
        ExpandWorldDataIntegration.Instance
    };

    private static readonly List<IEnchantCompatibilityRule> CompatibilityRules = new();
    private static readonly List<IEnchantmentStateIntegration> StateIntegrations = new();
    private static readonly List<IInventoryGridCompatibility> InventoryGridCompatibilities = new();
    private static readonly List<IBiomeCatalogSource> BiomeCatalogSources = new();
    private static bool _initialized;

    public static event Action? BiomesChanged;

    [UsedImplicitly]
    private static void OnInit()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        foreach (IOptionalIntegration integration in Integrations)
        {
            if (!integration.IsAvailable())
            {
                continue;
            }

            if (integration is IEnchantCompatibilityRule compatibilityRule)
            {
                CompatibilityRules.Add(compatibilityRule);
            }

            if (integration is IEnchantmentStateIntegration stateIntegration)
            {
                StateIntegrations.Add(stateIntegration);
            }

            if (integration is IInventoryGridCompatibility inventoryGridCompatibility)
            {
                InventoryGridCompatibilities.Add(inventoryGridCompatibility);
            }

            if (integration is IBiomeCatalogSource biomeCatalogSource)
            {
                BiomeCatalogSources.Add(biomeCatalogSource);
                biomeCatalogSource.Changed += NotifyBiomesChanged;
            }

            integration.Initialize();
        }
    }

    public static bool CanEnchant(ItemDrop.ItemData item, out string message)
    {
        foreach (IEnchantCompatibilityRule compatibilityRule in CompatibilityRules)
        {
            if (!compatibilityRule.CanEnchant(item, out message))
            {
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    public static void ApplyEnchantState(Enchantment_Core.Enchanted enchantment)
    {
        foreach (IEnchantmentStateIntegration integration in StateIntegrations)
        {
            integration.Apply(enchantment);
        }
    }

    public static void ApplyEnchantUpgradedState(Enchantment_Core.Enchanted enchantment)
    {
        foreach (IEnchantmentStateIntegration integration in StateIntegrations)
        {
            integration.ApplyUpgraded(enchantment);
        }
    }

    public static int GetReservedTopRows()
    {
        int reservedTopRows = 0;
        foreach (IInventoryGridCompatibility compatibility in InventoryGridCompatibilities)
        {
            reservedTopRows = Mathf.Max(reservedTopRows, compatibility.GetReservedTopRows());
        }

        return reservedTopRows;
    }

    public static bool TryGetCustomBiomes(out List<CustomBiomeDefinition> customBiomes)
    {
        customBiomes = new List<CustomBiomeDefinition>();
        bool foundAny = false;

        foreach (IBiomeCatalogSource biomeCatalogSource in BiomeCatalogSources)
        {
            if (!biomeCatalogSource.TryGetCustomBiomes(out List<CustomBiomeDefinition> sourceBiomes))
            {
                continue;
            }

            foundAny = true;
            customBiomes.AddRange(sourceBiomes);
        }

        return foundAny;
    }

    private static void NotifyBiomesChanged()
    {
        BiomesChanged?.Invoke();
    }
}
