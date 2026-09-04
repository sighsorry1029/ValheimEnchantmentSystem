using BepInEx.Bootstrap;

namespace kg.ValheimEnchantmentSystem.Integrations;

internal sealed class JewelcraftingIntegration : IOptionalIntegration
{
    public static readonly JewelcraftingIntegration Instance = new();

    private const string JewelcraftingGuid = "org.bepinex.plugins.jewelcrafting";
    private const string EnchantmentDataKey = "kg.ValheimEnchantmentSystem#kg.ValheimEnchantmentSystem.Enchantment_Core+Enchanted";

    public string Name => "Jewelcrafting";

    public bool IsAvailable()
    {
        return Chainloader.PluginInfos.ContainsKey(JewelcraftingGuid);
    }

    public void Initialize()
    {
        Jewelcrafting.API.OnItemMirrored(OnItemMirror);
    }

    private bool OnItemMirror(ItemDrop.ItemData item)
    {
        item.m_customData.Remove(EnchantmentDataKey);

        return true;
    }
}
