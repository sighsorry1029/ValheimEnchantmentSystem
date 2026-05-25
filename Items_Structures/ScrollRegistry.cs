namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollRegistry
{
    private static readonly HashSet<string> UpgradeableScrollPrefabs = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, GameObject> NameToPrefab = new(StringComparer.Ordinal);
    private static readonly Dictionary<char, char> UpgradeMapper = new()
    {
        { 'F', 'E' }, { 'E', 'D' }, { 'D', 'C' }, { 'C', 'B' }, { 'B', 'A' }, { 'A', 'S' }
    };

    public static void RegisterLocalizedPrefab(string sharedName, GameObject prefab)
    {
        if (string.IsNullOrWhiteSpace(sharedName) || prefab == null)
        {
            return;
        }

        NameToPrefab[sharedName] = prefab;
    }

    public static void RegisterUpgradeablePrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            UpgradeableScrollPrefabs.Add(prefab.name);
        }
    }

    public static void FixDropPrefab(ItemDrop.ItemData item)
    {
        if (item != null && !item.m_dropPrefab && item.m_shared?.m_name != null && NameToPrefab.TryGetValue(item.m_shared.m_name, out GameObject prefab))
        {
            item.m_dropPrefab = prefab;
        }
    }

    public static bool IsUpgradeableScroll(ItemDrop.ItemData item)
    {
        FixDropPrefab(item);
        return item?.m_dropPrefab != null && UpgradeableScrollPrefabs.Contains(item.m_dropPrefab.name);
    }

    public static bool TryGetUpgradePrefabName(string currentPrefabName, out string upgradedPrefabName)
    {
        upgradedPrefabName = string.Empty;
        if (string.IsNullOrWhiteSpace(currentPrefabName))
        {
            return false;
        }

        char currentTier = currentPrefabName[currentPrefabName.Length - 1];
        if (!UpgradeMapper.TryGetValue(currentTier, out char upgradedTier))
        {
            return false;
        }

        upgradedPrefabName = currentPrefabName.Substring(0, currentPrefabName.Length - 1) + upgradedTier;
        return true;
    }

    public static void FixInventoryPrefabs(Inventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            FixDropPrefab(item);
        }
    }
}
