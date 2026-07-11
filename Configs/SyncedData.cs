using System.Text;
using JetBrains.Annotations;
using HarmonyLib;
using System.Reflection;
using kg.ValheimEnchantmentSystem.Misc;
using ServerSync;
using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class SyncedData
{
    internal static void Initialize()
    {
        EnchantmentSettings.Bind();
        EnchantmentConfigPaths.Initialize();
        EnchantmentChanceRepository.Initialize();
        EnchantmentStatRepository.Initialize();
        EnchantmentColorRepository.Initialize();
        EnchantmentRequirementRepository.Initialize(Synced_EnchantmentReqs);
        ConfigRefreshCoordinator.Initialize();
    }

    private static void LoadAuthoritativeYamlData()
    {
        EnchantmentChanceRepository.LoadAuthoritativeData();
        EnchantmentStatRepository.LoadAuthoritativeData();
        EnchantmentColorRepository.LoadAuthoritativeData();
        EnchantmentRequirementRepository.LoadAuthoritativeData(Synced_EnchantmentReqs);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    private static class ZNet_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNet __instance)
        {
            if (!__instance || !__instance.IsServer())
            {
                return;
            }

            LoadAuthoritativeYamlData();
        }
    }

    public static string GetColor(Enchantment_Core.Enchanted en, out int variant, bool trimApha) =>
        GetColor(en.Item.m_dropPrefab?.name, en.level, out variant, trimApha);

    public static string GetColor(string dropPrefab, int level, out int variant, bool trimApha, string defaultValue = "#00000000")
    {
        return EnchantmentColorRepository.GetColor(dropPrefab, level, out variant, trimApha, defaultValue);
    }

    public static Chance_Data GetEnchantmentChance(Enchantment_Core.Enchanted en)
        => GetEnchantmentChance(en.Item.m_dropPrefab?.name, en.level, en.Item.IsWeapon());

    public static Chance_Data GetEnchantmentChance(string dropPrefab, int level)
        => GetEnchantmentChance(dropPrefab, level, true);

    public static Chance_Data GetEnchantmentChance(string dropPrefab, int level, bool isWeapon)
    {
        return EnchantmentChanceRepository.GetEnchantmentChance(dropPrefab, level, isWeapon);
    }

    public static bool IsLevelEnchantable(string dropPrefab, int level, bool isWeapon)
    {
        return EnchantmentChanceRepository.IsLevelEnchantable(dropPrefab, level, isWeapon) &&
               EnchantmentStatRepository.HasStatIncrease(dropPrefab, level + 1, isWeapon);
    }

    public static Stat_Data GetStatIncrease(Enchantment_Core.Enchanted en)
    {
        return EnchantmentStatRepository.GetStatIncrease(en);
    }

    public static Stat_Data GetStatIncrease(string dropPrefab, int level, bool isWeapon)
    {
        return EnchantmentStatRepository.GetStatIncrease(dropPrefab, level, isWeapon);
    }

    public static EnchantmentReqs GetReqs(string prefab)
    {
        return EnchantmentRequirementRepository.GetReqs(Synced_EnchantmentReqs.Value, prefab);
    }

    public enum ItemDesctructionTypeEnum{ LevelDecrease, Destroy, Combined, CombinedEasy }
    
    public static ConfigEntry<int> SafetyLevel => EnchantmentSettings.SafetyLevel;
    public static ConfigEntry<bool> DropEnchantmentOnUpgrade => EnchantmentSettings.DropEnchantmentOnUpgrade;
    public static ConfigEntry<ItemDesctructionTypeEnum> ItemFailureType => EnchantmentSettings.ItemFailureType;
    public static ConfigEntry<int> FailedEnchantLevelDecrease => EnchantmentSettings.FailedEnchantLevelDecrease;
    public static ConfigEntry<bool> BlessedScrollsPreventBreak => EnchantmentSettings.BlessedScrollsPreventBreak;
    public static ConfigEntry<int> BlessedScrollsAdditionalChance => EnchantmentSettings.BlessedScrollsAdditionalChance;
    public static ConfigEntry<bool> AllowJewelcraftingMirrorCopyEnchant => EnchantmentSettings.AllowJewelcraftingMirrorCopyEnchant;
    public static ConfigEntry<float> AdditionalEnchantmentChancePerLevel => EnchantmentSettings.AdditionalEnchantmentChancePerLevel;
    public static ConfigEntry<float> FailedEnchantSkillExpMultiplier => EnchantmentSettings.FailedEnchantSkillExpMultiplier;
    public static ConfigEntry<int> EnchantmentNotificationMinLevel => EnchantmentSettings.EnchantmentNotificationMinLevel;
    public static ConfigEntry<bool> EnchantmentEnableNotifications => EnchantmentSettings.EnchantmentEnableNotifications;

    public static readonly CustomSyncedValue<Dictionary<int, Chance_Data>> Synced_EnchantmentChances_Weapons =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentGlobalChances_Weapons",
            new Dictionary<int, Chance_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, Chance_Data>> Synced_EnchantmentChances_Armor =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentGlobalChances_Armor",
            new Dictionary<int, Chance_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, VFX_Data>> Synced_EnchantmentColors =
        new(ValheimEnchantmentSystem.ConfigSync, "OverridenEnchantmentColors",
            new Dictionary<int, VFX_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Weapons =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Weapons",
            new Dictionary<int, Stat_Data>());
    
    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Armor =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Armor",
            new Dictionary<int, Stat_Data>());

    public static readonly CustomSyncedValue<List<OverrideChances>> Overrides_EnchantmentChances =
        new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentChances",
            new());

    public static readonly CustomSyncedValue<List<OverrideColors>> Overrides_EnchantmentColors =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentColors",
                new());

    public static readonly CustomSyncedValue<List<OverrideStats>> Overrides_EnchantmentStats =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentStats",
                new());

    public static readonly CustomSyncedValue<List<EnchantmentReqs>> Synced_EnchantmentReqs =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentReqs",
            new List<EnchantmentReqs>());

    private static void WriteEnum<TEnum>(ref ZPackage pkg, TEnum value) where TEnum : struct, Enum
        => pkg.Write(Convert.ToInt32(value));

    private static TEnum ReadEnum<TEnum>(ref ZPackage pkg) where TEnum : struct, Enum
        => (TEnum)Enum.ToObject(typeof(TEnum), pkg.ReadInt());

    private static void WriteSerializable<T>(ref ZPackage pkg, T value) where T : class, ISerializableParameter
    {
        pkg.Write(value != null);
        value?.Serialize(ref pkg);
    }

    private static T ReadSerializable<T>(ref ZPackage pkg) where T : class, ISerializableParameter, new()
    {
        if (!pkg.ReadBool())
        {
            return null;
        }

        T value = new();
        value.Deserialize(ref pkg);
        return value;
    }

    private static void WriteStringList(ref ZPackage pkg, List<string> values)
    {
        values ??= new List<string>();
        pkg.Write(values.Count);
        foreach (string value in values)
        {
            pkg.Write(value ?? "");
        }
    }

    private static List<string> ReadStringList(ref ZPackage pkg)
    {
        int count = pkg.ReadInt();
        List<string> values = new(count);
        for (int i = 0; i < count; i++)
        {
            values.Add(pkg.ReadString());
        }

        return values;
    }

    private static void WriteSerializableDictionary<T>(ref ZPackage pkg, Dictionary<int, T> values)
        where T : class, ISerializableParameter
    {
        values ??= new Dictionary<int, T>();
        pkg.Write(values.Count);
        foreach (KeyValuePair<int, T> entry in values)
        {
            pkg.Write(entry.Key);
            pkg.Write(entry.Value != null);
            entry.Value?.Serialize(ref pkg);
        }
    }

    private static Dictionary<int, T> ReadSerializableDictionary<T>(ref ZPackage pkg)
        where T : class, ISerializableParameter, new()
    {
        int count = pkg.ReadInt();
        Dictionary<int, T> values = new(count);
        for (int i = 0; i < count; i++)
        {
            int key = pkg.ReadInt();
            if (!pkg.ReadBool())
            {
                continue;
            }

            T value = new();
            value.Deserialize(ref pkg);
            values[key] = value;
        }

        return values;
    }

    public partial class Stat_Data
    {
        private List<HitData.DamageModPair> cached_resistance_pairs;
        public List<HitData.DamageModPair> GetResistancePairs()
        {
            if (cached_resistance_pairs != null) return cached_resistance_pairs;
            cached_resistance_pairs =
            [
                new() { m_type = HitData.DamageType.Blunt, m_modifier = resistance_blunt },
                new() { m_type = HitData.DamageType.Slash, m_modifier = resistance_slash },
                new() { m_type = HitData.DamageType.Pierce, m_modifier = resistance_pierce },
                new() { m_type = HitData.DamageType.Chop, m_modifier = resistance_chop },
                new() { m_type = HitData.DamageType.Pickaxe, m_modifier = resistance_pickaxe },
                new() { m_type = HitData.DamageType.Fire, m_modifier = resistance_fire },
                new() { m_type = HitData.DamageType.Frost, m_modifier = resistance_frost },
                new() { m_type = HitData.DamageType.Lightning, m_modifier = resistance_lightning },
                new() { m_type = HitData.DamageType.Poison, m_modifier = resistance_poison },
                new() { m_type = HitData.DamageType.Spirit, m_modifier = resistance_spirit }
            ];
            cached_resistance_pairs.RemoveAll(x => x.m_modifier == HitData.DamageModifier.Normal);
            return cached_resistance_pairs;
        }
    }
    
    public partial class Stat_Data : ISerializableParameter
    {
        [SerializeField] public int durability;
        [SerializeField] public int durability_percentage;
        [SerializeField] public float armor_percentage;
        [SerializeField] public float armor;
        [SerializeField] public int damage_percentage;
        [SerializeField] public int damage_true;
        [SerializeField] public int damage_blunt;
        [SerializeField] public int damage_slash;
        [SerializeField] public int damage_pierce;
        [SerializeField] public int damage_chop;
        [SerializeField] public int damage_pickaxe;
        [SerializeField] public int damage_fire;
        [SerializeField] public int damage_frost;
        [SerializeField] public int damage_lightning;
        [SerializeField] public int damage_poison;
        [SerializeField] public int damage_spirit;
        [SerializeField] public HitData.DamageModifier resistance_blunt;
        [SerializeField] public HitData.DamageModifier resistance_slash;
        [SerializeField] public HitData.DamageModifier resistance_pierce;
        [SerializeField] public HitData.DamageModifier resistance_chop;
        [SerializeField] public HitData.DamageModifier resistance_pickaxe;
        [SerializeField] public HitData.DamageModifier resistance_fire;
        [SerializeField] public HitData.DamageModifier resistance_frost;
        [SerializeField] public HitData.DamageModifier resistance_lightning;
        [SerializeField] public HitData.DamageModifier resistance_poison;
        [SerializeField] public HitData.DamageModifier resistance_spirit;
        [SerializeField] public int attack_speed;
        [SerializeField] public int movement_speed;
        [SerializeField] public int max_hp;
        [SerializeField] public int max_stamina;
        [SerializeField] public float hp_regen;
        [SerializeField] public float stamina_regen;
        //api stats
        [SerializeField] public int API_backpacks_additionalrow_x;
        [SerializeField] public int API_backpacks_additionalrow_y;
        
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(durability);
            pkg.Write(durability_percentage);
            pkg.Write(armor_percentage);
            pkg.Write(armor);
            pkg.Write(damage_percentage);
            pkg.Write(damage_true);
            pkg.Write(damage_blunt);
            pkg.Write(damage_slash);
            pkg.Write(damage_pierce);
            pkg.Write(damage_chop);
            pkg.Write(damage_pickaxe);
            pkg.Write(damage_fire);
            pkg.Write(damage_frost);
            pkg.Write(damage_lightning);
            pkg.Write(damage_poison);
            pkg.Write(damage_spirit);
            WriteEnum(ref pkg, resistance_blunt);
            WriteEnum(ref pkg, resistance_slash);
            WriteEnum(ref pkg, resistance_pierce);
            WriteEnum(ref pkg, resistance_chop);
            WriteEnum(ref pkg, resistance_pickaxe);
            WriteEnum(ref pkg, resistance_fire);
            WriteEnum(ref pkg, resistance_frost);
            WriteEnum(ref pkg, resistance_lightning);
            WriteEnum(ref pkg, resistance_poison);
            WriteEnum(ref pkg, resistance_spirit);
            pkg.Write(attack_speed);
            pkg.Write(movement_speed);
            pkg.Write(max_hp);
            pkg.Write(max_stamina);
            pkg.Write(hp_regen);
            pkg.Write(stamina_regen);
            pkg.Write(API_backpacks_additionalrow_x);
            pkg.Write(API_backpacks_additionalrow_y);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            durability = pkg.ReadInt();
            durability_percentage = pkg.ReadInt();
            armor_percentage = pkg.ReadSingle();
            armor = pkg.ReadSingle();
            damage_percentage = pkg.ReadInt();
            damage_true = pkg.ReadInt();
            damage_blunt = pkg.ReadInt();
            damage_slash = pkg.ReadInt();
            damage_pierce = pkg.ReadInt();
            damage_chop = pkg.ReadInt();
            damage_pickaxe = pkg.ReadInt();
            damage_fire = pkg.ReadInt();
            damage_frost = pkg.ReadInt();
            damage_lightning = pkg.ReadInt();
            damage_poison = pkg.ReadInt();
            damage_spirit = pkg.ReadInt();
            resistance_blunt = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_slash = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_pierce = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_chop = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_pickaxe = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_fire = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_frost = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_lightning = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_poison = ReadEnum<HitData.DamageModifier>(ref pkg);
            resistance_spirit = ReadEnum<HitData.DamageModifier>(ref pkg);
            attack_speed = pkg.ReadInt();
            movement_speed = pkg.ReadInt();
            max_hp = pkg.ReadInt();
            max_stamina = pkg.ReadInt();
            hp_regen = pkg.ReadSingle();
            stamina_regen = pkg.ReadSingle();
            API_backpacks_additionalrow_x = pkg.ReadInt();
            API_backpacks_additionalrow_y = pkg.ReadInt();
            cached_resistance_pairs = null;
        }
    }
    
    public class Chance_Data : ISerializableParameter
    {
        [SerializeField] public float success;
        [SerializeField] public float destroy;
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(success);
            pkg.Write(destroy);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            success = pkg.ReadSingle();
            destroy = pkg.ReadSingle();
        }
    }
    
    public class SingleReq : ISerializableParameter
    {
        [SerializeField] public string prefab;
        public SingleReq(){}
        public SingleReq (string prefab) { this.prefab = prefab; }
        public bool IsValid() => !string.IsNullOrEmpty(prefab) && ZNetScene.instance != null && ZNetScene.instance.GetPrefab(prefab) != null;
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(prefab ?? "");
        }

        public void Deserialize(ref ZPackage pkg)
        {
            prefab = pkg.ReadString();
        }
    }
    
    public class EnchantmentReqs : ISerializableParameter
    {
        [SerializeField] public SingleReq enchant_prefab = new();
        [SerializeField] public SingleReq blessed_enchant_prefab = new();
        [SerializeField] public List<string> Items = new();
        public void Serialize(ref ZPackage pkg)
        {
            WriteSerializable(ref pkg, enchant_prefab);
            WriteSerializable(ref pkg, blessed_enchant_prefab);
            WriteStringList(ref pkg, Items);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            enchant_prefab = ReadSerializable<SingleReq>(ref pkg);
            blessed_enchant_prefab = ReadSerializable<SingleReq>(ref pkg);
            Items = ReadStringList(ref pkg);
        }
    }

    public class VFX_Data : ISerializableParameter
    {
        [SerializeField] public string color = "#00000000";
        [SerializeField] public int variant;
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(color ?? "");
            pkg.Write(variant);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            color = pkg.ReadString();
            variant = pkg.ReadInt();
        }
    }
    
    public class OverrideChances : ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, Chance_Data> Chances = new();
        public void Serialize(ref ZPackage pkg)
        {
            WriteStringList(ref pkg, Items);
            WriteSerializableDictionary(ref pkg, Chances);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            Items = ReadStringList(ref pkg);
            Chances = ReadSerializableDictionary<Chance_Data>(ref pkg);
        }
    }

    public class OverrideColors : ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, VFX_Data> Colors = new();
        public void Serialize(ref ZPackage pkg)
        {
            WriteStringList(ref pkg, Items);
            WriteSerializableDictionary(ref pkg, Colors);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            Items = ReadStringList(ref pkg);
            Colors = ReadSerializableDictionary<VFX_Data>(ref pkg);
        }
    }

    public class OverrideStats : ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, Stat_Data> Stats = new();
        public void Serialize(ref ZPackage pkg)
        {
            WriteStringList(ref pkg, Items);
            WriteSerializableDictionary(ref pkg, Stats);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            Items = ReadStringList(ref pkg);
            Stats = ReadSerializableDictionary<Stat_Data>(ref pkg);
        }
    }
    
}
