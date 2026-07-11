using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Platform;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollContentBootstrap
{
    private static bool _initialized;
    private static GameObject? _prefabRoot;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        foreach (char tier in EnchantmentTierCatalog.AllTiers)
        {
            RegisterTierContent(tier);
        }
    }

    private static void RegisterTierContent(char tier)
    {
        GameObject? weaponPrefab = LoadTierPrefab("kg_EnchantScroll_Weapon", tier);
        GameObject? weaponBlessedPrefab = LoadTierPrefab("kg_EnchantScroll_Weapon_Blessed", tier);
        GameObject? armorPrefab = LoadTierPrefab("kg_EnchantScroll_Armor", tier);
        GameObject? armorBlessedPrefab = LoadTierPrefab("kg_EnchantScroll_Armor_Blessed", tier);
        GameObject? skillScrollPrefab = LoadTierPrefab("kg_EnchantSkillScroll", tier);

        if (weaponPrefab == null || weaponBlessedPrefab == null || armorPrefab == null || armorBlessedPrefab == null || skillScrollPrefab == null)
        {
            return;
        }

        ItemRegistrationService.RegisteredItem weaponScroll = ConfigureCraftableScroll(weaponPrefab, tier, false, false, $"$kg_enchantscroll_{tier}_weapon", "$kg_enchantscroll_weapon_description");
        ItemRegistrationService.RegisteredItem weaponBlessedScroll = ConfigureCraftableScroll(weaponBlessedPrefab, tier, true, false, $"$kg_enchantscroll_{tier}_weapon_blessed", "$kg_enchantscroll_weapon_blessed_description");
        ConfigureBlessedConvertRecipe(weaponBlessedScroll, weaponScroll.Prefab.name, tier);

        ItemRegistrationService.RegisteredItem armorScroll = ConfigureCraftableScroll(armorPrefab, tier, false, true, $"$kg_enchantscroll_{tier}_armor", "$kg_enchantscroll_armor_description");
        ItemRegistrationService.RegisteredItem armorBlessedScroll = ConfigureCraftableScroll(armorBlessedPrefab, tier, true, true, $"$kg_enchantscroll_{tier}_armor_blessed", "$kg_enchantscroll_armor_blessed_description");
        ConfigureBlessedConvertRecipe(armorBlessedScroll, armorScroll.Prefab.name, tier);

        ConfigureSkillScroll(skillScrollPrefab, tier);

        if (tier == 'S')
        {
            return;
        }

        ScrollRegistry.RegisterUpgradeablePrefab(weaponScroll.Prefab);
        ScrollRegistry.RegisterUpgradeablePrefab(weaponBlessedScroll.Prefab);
        ScrollRegistry.RegisterUpgradeablePrefab(armorScroll.Prefab);
        ScrollRegistry.RegisterUpgradeablePrefab(armorBlessedScroll.Prefab);
    }

    private static ItemRegistrationService.RegisteredItem ConfigureCraftableScroll(GameObject prefab, char tier, bool blessed, bool isArmor, string sharedName, string descriptionKey)
    {
        ItemRegistrationService.RegisteredItem item = ItemRegistrationService.CreateRecipeItem(prefab, sharedName, descriptionKey);
        ItemRegistrationService.ApplyRecipe(item, ScrollRecipeCatalog.GetRecipe(tier, blessed, isArmor), "kg_EnchantmentScrollStation");
        ScrollRegistry.RegisterLocalizedPrefab(sharedName, item.Prefab);
        return item;
    }

    private static void ConfigureBlessedConvertRecipe(ItemRegistrationService.RegisteredItem blessedItem, string normalPrefabName, char tier)
    {
        ItemRegistrationService.ConfigureConversion(
            blessedItem,
            "ConvertNormal",
            ScrollRecipeCatalog.GetBlessedConvertAmount(tier),
            normalPrefabName,
            ScrollRecipeCatalog.GetBlessedConvertRequirement(),
            "kg_EnchantmentScrollStation");
    }

    private static void ConfigureSkillScroll(GameObject skillScrollPrefab, char tier)
    {
        string sharedName = $"$kg_enchantskillscroll_{tier}";
        if (skillScrollPrefab.GetComponent<ItemDrop>() is { } skillItemDrop)
        {
            skillItemDrop.m_itemData.m_shared.m_name = sharedName;
        }

        ScrollRegistry.RegisterLocalizedPrefab(sharedName, skillScrollPrefab);
        SkillScrollService.RegisterSkillScrollPrefab(skillScrollPrefab);
    }

    private static GameObject? LoadTierPrefab(string assetPrefix, char tier)
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
