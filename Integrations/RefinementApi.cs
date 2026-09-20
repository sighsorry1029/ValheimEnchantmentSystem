using ItemDataManager;
using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem.Integrations;

/// <summary>Read-only information for optional refinement UI integrations. VES retains ownership of stored enchantments.</summary>
public static class RefinementApi
{
    /// <summary>The configured blessed material, independent of current enchantment level or item quality.</summary>
    public static bool TryGetBlessedScroll(ItemDrop.ItemData item, out string prefab)
    {
        prefab = string.Empty;
        if (item?.m_shared == null) return false;
        string itemPrefab = EnchantmentDomainHelper.ResolveItemPrefabName(item);
        if (string.IsNullOrEmpty(itemPrefab)) return false;
        prefab = SyncedData.GetReqs(itemPrefab)?.blessed_enchant_prefab?.prefab ?? string.Empty;
        return !string.IsNullOrWhiteSpace(prefab);
    }

    public static bool TryGetEnchantmentInfo(ItemDrop.ItemData item, out int level, out bool atMaximum)
    {
        level = 0;
        atMaximum = false;
        if (item?.m_shared == null) return false;
        var enchantment = item.Data().Get<Enchantment_Core.Enchanted>();
        string prefab = EnchantmentDomainHelper.ResolveItemPrefabName(item);
        bool configured = !string.IsNullOrEmpty(prefab) && SyncedData.GetReqs(prefab) != null;
        if (enchantment == null && !configured) return false;
        level = enchantment?.level ?? 0;
        atMaximum = configured && !SyncedData.IsLevelEnchantable(prefab, level, item.IsWeapon());
        return true;
    }
}
