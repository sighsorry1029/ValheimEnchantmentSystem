using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem;

internal static class EnchantmentDomainHelper
{
    public static bool CanEnchant(ItemDrop.ItemData item, int currentLevel, string itemPrefabName)
    {
        if (item == null || string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return false;
        }

        if (!SyncedData.IsLevelEnchantable(itemPrefabName, currentLevel, item.IsWeapon()))
        {
            return false;
        }

        return SyncedData.GetReqs(itemPrefabName) != null;
    }

    public static string ResolveItemPrefabName(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab)
        {
            return item.m_dropPrefab.name;
        }

        return Utils.GetPrefabNameByItemName(item?.m_shared?.m_name);
    }
}
