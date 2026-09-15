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
        if (item == null) return null;
        if (item.m_dropPrefab)
        {
            return item.m_dropPrefab.name;
        }

        if (item.m_shared == null) return null;
        ObjectDB database = ObjectDB.instance;
        if (!database) return null;

        // Recipe templates can have no drop prefab. The game's shared-data index
        // preserves their identity and avoids scanning ObjectDB on every UI frame.
        GameObject prefab = database.GetItemPrefab(item.m_shared);
        return prefab ? prefab.name : Utils.GetPrefabNameByItemName(item.m_shared.m_name);
    }
}
