using BepInEx.Bootstrap;

namespace kg.ValheimEnchantmentSystem.Integrations;

internal sealed class AugaIntegration : IOptionalIntegration, IInventoryGridCompatibility
{
    public static readonly AugaIntegration Instance = new();

    private const string AugaGuid = "Auga";

    public string Name => "Auga";

    public bool IsAvailable()
    {
        return Chainloader.PluginInfos.ContainsKey(AugaGuid);
    }

    public void Initialize()
    {
    }

    public int GetReservedTopRows()
    {
        return 2;
    }
}
