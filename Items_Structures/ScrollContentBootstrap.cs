using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Platform;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollContentBootstrap
{
    private static bool _initialized;

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
        GameObject? weaponPrefab = ScrollPrefabFactory.LoadTierPrefab("kg_EnchantScroll_Weapon", tier);
        GameObject? weaponBlessedPrefab = ScrollPrefabFactory.LoadTierPrefab("kg_EnchantScroll_Weapon_Blessed", tier);
        GameObject? armorPrefab = ScrollPrefabFactory.LoadTierPrefab("kg_EnchantScroll_Armor", tier);
        GameObject? armorBlessedPrefab = ScrollPrefabFactory.LoadTierPrefab("kg_EnchantScroll_Armor_Blessed", tier);
        GameObject? skillScrollPrefab = ScrollPrefabFactory.LoadTierPrefab("kg_EnchantSkillScroll", tier);

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
}
