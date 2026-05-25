using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollPrefabFactory
{
    private static GameObject? _prefabRoot;

    public static GameObject? LoadTierPrefab(string assetPrefix, char tier)
    {
        if (tier == 'E')
        {
            return ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"{assetPrefix}_S"), $"{assetPrefix}_E");
        }

        return ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"{assetPrefix}_{tier}");
    }

    private static GameObject? ClonePrefab(GameObject prefab, string newName)
    {
        if (prefab == null)
        {
            return null;
        }

        bool wasActive = prefab.activeSelf;
        prefab.SetActive(false);
        GameObject clone = Object.Instantiate(prefab);
        prefab.SetActive(wasActive);

        clone.name = newName;
        if (clone.GetComponent<ItemDrop>() is { } itemDrop)
        {
            itemDrop.m_itemData.m_dropPrefab = clone;
        }

        Object.DontDestroyOnLoad(clone);
        EnsurePrefabRoot();
        clone.transform.SetParent(_prefabRoot!.transform, false);
        clone.SetActive(wasActive);
        return clone;
    }

    private static void EnsurePrefabRoot()
    {
        if (_prefabRoot != null)
        {
            return;
        }

        _prefabRoot = new GameObject("VES_PrefabRoot");
        _prefabRoot.SetActive(false);
        Object.DontDestroyOnLoad(_prefabRoot);
    }
}
