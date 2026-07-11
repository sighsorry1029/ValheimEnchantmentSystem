using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

public static class ScrollDropService
{
    private static ConfigEntry<float> DropChance = null!;
    private static ConfigEntry<float> DropChanceBosses = null!;
    private static ConfigEntry<float> DropChanceBlessed = null!;
    private static ConfigEntry<float> DropChanceBlessedBosses = null!;
    private static ConfigEntry<float> DropChanceSkill = null!;
    private static ConfigEntry<float> DropChanceSkillBosses = null!;
    private static ConfigEntry<bool> MonsterDroppingScrolls = null!;
    private static ConfigEntry<bool> MonsterDroppingSkillScrolls = null!;
    private static ConfigEntry<string> ExcludePrefabsFromDrop = null!;
    private static readonly HashSet<string> ExcludedDropPrefabs = new();
    private static bool _configurationBound;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        BindConfiguration();
        _initialized = true;
    }

    internal static void BindConfiguration()
    {
        if (_configurationBound)
            return;

        MonsterDroppingScrolls = ValheimEnchantmentSystem.config("Scrolls", "Drop From Monsters", true,
            Display("Allow monsters to drop scrolls.", ConfigurationManagerDisplay.Scrolls, "Drop From Monsters", 1000));
        MonsterDroppingSkillScrolls = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop From Monsters (Skill exp)", true,
            Display("Allow monsters to drop enchant skill exp scrolls.", ConfigurationManagerDisplay.Skill, "Skill Scroll - Drop From Monsters", 700));
        DropChance = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance", 7f,
            Display("Chance to drop from enemies.", ConfigurationManagerDisplay.Scrolls, "Drop Chance", 990));
        DropChanceBosses = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance (Bosses)", 100f,
            Display("Chance to drop from bosses.", ConfigurationManagerDisplay.Scrolls, "Boss Drop Chance", 980));
        DropChanceBlessed = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance", 0.5f,
            Display("Chance to drop blessed scrolls from enemies.", ConfigurationManagerDisplay.Scrolls, "Blessed Drop Chance", 970));
        DropChanceBlessedBosses = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance (Bosses)", 40f,
            Display("Chance to drop blessed scrolls from bosses.", ConfigurationManagerDisplay.Scrolls, "Blessed Boss Drop Chance", 960));
        DropChanceSkill = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp)", 1f,
            Display("Chance to drop enchant skill exp scrolls from enemies.", ConfigurationManagerDisplay.Skill, "Skill Scroll - Drop Chance", 690));
        DropChanceSkillBosses = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp) (Bosses)", 100f,
            Display("Chance to drop enchant skill exp scrolls from bosses.", ConfigurationManagerDisplay.Skill, "Skill Scroll - Boss Drop Chance", 680));
        ExcludePrefabsFromDrop = ValheimEnchantmentSystem.config("Scrolls", "Exclude Prefabs From Drop", "TentaRoot",
            Display("Comma separated list of prefabs to exclude from dropping scrolls.", ConfigurationManagerDisplay.Scrolls, "Exclude Prefabs From Drop", 950));
        ExcludePrefabsFromDrop.SettingChanged += FillExclude;
        FillExclude();
        _configurationBound = true;
    }

    private static ConfigDescription Display(string description, string category, string displayName, int order)
    {
        return ConfigurationManagerDisplay.Description(description, category, order, displayName);
    }

    private static void FillExclude(object? sender = null, EventArgs? e = null)
    {
        ExcludedDropPrefabs.Clear();
        if (string.IsNullOrWhiteSpace(ExcludePrefabsFromDrop.Value))
            return;

        foreach (string entry in ExcludePrefabsFromDrop.Value.Replace(" ", string.Empty).Split(','))
        {
            if (!string.IsNullOrWhiteSpace(entry))
                ExcludedDropPrefabs.Add(entry);
        }
    }

    private static bool IsOwnerDropContext(Character character)
    {
        if (character == null || character.IsPlayer() || character.IsTamed())
            return false;
        if (character.m_nview == null || !character.m_nview.IsValid() || !character.m_nview.IsOwner())
            return false;

        return ZNetScene.instance != null;
    }

    private static bool HasPlayerLastHit(Character character)
    {
        return character?.m_lastHit?.GetAttacker() is Player;
    }

    private static void DropItem(GameObject prefab, Vector3 centerPos, float dropArea)
    {
        if (prefab == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
        Vector3 offset = Random.insideUnitSphere * dropArea;
        GameObject dropped = Object.Instantiate(prefab, centerPos + offset, rotation);
        dropped.SetActive(true);

        if (dropped.GetComponent<Rigidbody>() is not { } rigidbody)
            return;

        Vector3 force = Random.insideUnitSphere;
        if (force.y < 0f)
            force.y = -force.y;
        rigidbody.AddForce(force * 5f, ForceMode.VelocityChange);
    }

    private static void TryDropDefault(char tier, bool isBoss, Vector3 position)
    {
        float dropChance = (isBoss ? DropChanceBosses.Value : DropChance.Value) / 100f;
        if (Random.value > dropChance)
            return;

        bool isWeapon = Random.value < 0.5f;
        string prefabName = isWeapon ? $"kg_EnchantScroll_Weapon_{tier}" : $"kg_EnchantScroll_Armor_{tier}";
        DropItem(ZNetScene.instance.GetPrefab(prefabName), position + Vector3.up * 0.75f, 0.5f);
    }

    private static void TryDropBlessed(char tier, bool isBoss, Vector3 position)
    {
        float dropChance = (isBoss ? DropChanceBlessedBosses.Value : DropChanceBlessed.Value) / 100f;
        if (Random.value > dropChance)
            return;

        bool isWeapon = Random.value < 0.5f;
        string prefabName = isWeapon ? $"kg_EnchantScroll_Weapon_Blessed_{tier}" : $"kg_EnchantScroll_Armor_Blessed_{tier}";
        DropItem(ZNetScene.instance.GetPrefab(prefabName), position + Vector3.up * 0.75f, 0.5f);
    }

    private static void TryDropSkillScroll(char tier, bool isBoss, Vector3 position)
    {
        float dropChance = (isBoss ? DropChanceSkillBosses.Value : DropChanceSkill.Value) / 100f;
        if (Random.value > dropChance)
            return;

        DropItem(ZNetScene.instance.GetPrefab($"kg_EnchantSkillScroll_{tier}"), position + Vector3.up * 0.75f, 0.5f);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    private static class Tome_SpawnLoot_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingScrolls.Value || !IsOwnerDropContext(__instance))
                return;
            if (!HasPlayerLastHit(__instance))
                return;

            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExcludedDropPrefabs.Contains(prefabName))
                return;
            if (!BiomeTierResolver.TryResolveTier(__instance, out char tier))
                return;

            Vector3 position = __instance.transform.position;
            TryDropDefault(tier, __instance.IsBoss(), position);
            TryDropBlessed(tier, __instance.IsBoss(), position);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    private static class Tome_SpawnLootSkill_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingSkillScrolls.Value || !IsOwnerDropContext(__instance))
                return;
            if (!HasPlayerLastHit(__instance))
                return;

            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExcludedDropPrefabs.Contains(prefabName))
                return;
            if (!BiomeTierResolver.TryResolveTier(__instance, out char tier))
                return;

            TryDropSkillScroll(tier, __instance.IsBoss(), __instance.transform.position);
        }
    }
}
