using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class Defaults
{
    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Weapons =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { damage_percentage = 2  } },
            { 2,  new() { damage_percentage = 4  } },
            { 3,  new() { damage_percentage = 6  } },
            { 4,  new() { damage_percentage = 8  } },
            { 5,  new() { damage_percentage = 10 } },
            { 6,  new() { damage_percentage = 13 } },
            { 7,  new() { damage_percentage = 16 } },
            { 8,  new() { damage_percentage = 19 } },
            { 9,  new() { damage_percentage = 22 } },
            { 10, new() { damage_percentage = 25 } },
            { 11, new() { damage_percentage = 29 } },
            { 12, new() { damage_percentage = 33 } },
            { 13, new() { damage_percentage = 37 } },
            { 14, new() { damage_percentage = 41 } },
            { 15, new() { damage_percentage = 45 } },
            { 16, new() { damage_percentage = 50 } },
            { 17, new() { damage_percentage = 56 } },
            { 18, new() { damage_percentage = 63 } },
            { 19, new() { damage_percentage = 71 } },
            { 20, new() { damage_percentage = 80 } },
        };

    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Armor =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { durability_percentage = 3, max_hp = 1, hp_regen = 0.2f } },
            { 2,  new() { durability_percentage = 6, max_hp = 2, hp_regen = 0.4f } },
            { 3,  new() { durability_percentage = 9, max_hp = 3, hp_regen = 0.6f } },
            { 4,  new() { durability_percentage = 12, max_hp = 4, hp_regen = 0.8f } },
            { 5,  new() { durability_percentage = 15, max_hp = 5, hp_regen = 1f } },
            { 6,  new() { durability_percentage = 18, max_hp = 6, hp_regen = 1.2f } },
            { 7,  new() { durability_percentage = 21, max_hp = 7, hp_regen = 1.4f } },
            { 8,  new() { durability_percentage = 24, max_hp = 8, hp_regen = 1.6f } },
            { 9,  new() { durability_percentage = 27, max_hp = 9, hp_regen = 1.8f } },
            { 10, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f } },
            { 11, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 0.5f, armor = 0.5f } },
            { 12, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 1.5f, armor = 1.1f } },
            { 13, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 3f, armor = 1.8f } },
            { 14, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 5f, armor = 2.6f } },
            { 15, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 7.5f, armor = 3.5f } },
            { 16, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 11f, armor = 4.6f } },
            { 17, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 15f, armor = 5.8f } },
            { 18, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 19.5f, armor = 7.1f } },
            { 19, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 24.5f, armor = 8.5f } },
            { 20, new() { durability_percentage = 30, max_hp = 10, hp_regen = 2f, armor_percentage = 30f, armor = 10f } },
        };

    private static readonly Dictionary<int, SyncedData.Chance_Data> DefaultChances_Weapons = new()
    {
        { 0, new SyncedData.Chance_Data() {success = 90} }, { 1, new SyncedData.Chance_Data() {success = 86} }, { 2, new SyncedData.Chance_Data() {success = 82} }, { 3, new SyncedData.Chance_Data() {success = 78} }, 
        { 4, new SyncedData.Chance_Data() {success = 74} }, { 5, new SyncedData.Chance_Data() {success = 70} }, { 6, new SyncedData.Chance_Data() {success = 65} },
        { 7, new SyncedData.Chance_Data() {success = 60} }, { 8, new SyncedData.Chance_Data() {success = 55} }, { 9, new SyncedData.Chance_Data() {success = 50} }, 
        { 10, new SyncedData.Chance_Data() {success = 45} }, { 11, new SyncedData.Chance_Data() {success = 40} }, { 12, new SyncedData.Chance_Data() {success = 35} }, 
        { 13, new SyncedData.Chance_Data() {success = 30} }, { 14, new SyncedData.Chance_Data() {success = 25} }, { 15, new SyncedData.Chance_Data() {success = 20} }, 
        { 16, new SyncedData.Chance_Data() {success = 15} }, { 17, new SyncedData.Chance_Data() {success = 10} }, { 18, new SyncedData.Chance_Data() {success = 5} }, 
        { 19, new SyncedData.Chance_Data() {success = 0} }
    };

    private static readonly Dictionary<int, SyncedData.Chance_Data> DefaultChances_Armor = new()
    {
        { 0, new SyncedData.Chance_Data() {success = 93} }, { 1, new SyncedData.Chance_Data() {success = 89} }, { 2, new SyncedData.Chance_Data() {success = 85} }, { 3, new SyncedData.Chance_Data() {success = 81} }, 
        { 4, new SyncedData.Chance_Data() {success = 77} }, { 5, new SyncedData.Chance_Data() {success = 73} }, { 6, new SyncedData.Chance_Data() {success = 68} },
        { 7, new SyncedData.Chance_Data() {success = 63} }, { 8, new SyncedData.Chance_Data() {success = 58} }, { 9, new SyncedData.Chance_Data() {success = 53} }, 
        { 10, new SyncedData.Chance_Data() {success = 48} }, { 11, new SyncedData.Chance_Data() {success = 43} }, { 12, new SyncedData.Chance_Data() {success = 38} }, 
        { 13, new SyncedData.Chance_Data() {success = 33} }, { 14, new SyncedData.Chance_Data() {success = 28} }, { 15, new SyncedData.Chance_Data() {success = 23} }, 
        { 16, new SyncedData.Chance_Data() {success = 18} }, { 17, new SyncedData.Chance_Data() {success = 13} }, { 18, new SyncedData.Chance_Data() {success = 8} }, 
        { 19, new SyncedData.Chance_Data() {success = 3} }
    };

    private static readonly Dictionary<int, SyncedData.VFX_Data> DefaultColors =
        new Dictionary<int, SyncedData.VFX_Data>
        {
            { 1,  new() { color = "#1E151C01", variant = 0 } },
            { 2,  new() { color = "#1E181F02", variant = 0 } },
            { 3,  new() { color = "#1E1A2A03", variant = 0 } },
            { 4,  new() { color = "#1E1E3AA6", variant = 0 } },
            { 5,  new() { color = "#1E1E4AB0", variant = 0 } },
            { 6,  new() { color = "#23415A9B", variant = 0 } },
            { 7,  new() { color = "#28577EA2", variant = 0 } },
            { 8,  new() { color = "#1E508EA9", variant = 0 } },
            { 9,  new() { color = "#14469EB0", variant = 0 } },
            { 10, new() { color = "#0A3CAFB7", variant = 0 } },
            { 11, new() { color = "#0038BFC0", variant = 0 } },
            { 12, new() { color = "#0038BFC0", variant = 0 } },
            { 13, new() { color = "#001CDBC4", variant = 0 } },
            { 14, new() { color = "#001CDBDB", variant = 0 } },
            { 15, new() { color = "#001CDFE2", variant = 0 } },
            { 16, new() { color = "#A0140EE9", variant = 0 } },
            { 17, new() { color = "#B40A0EF0", variant = 0 } },
            { 18, new() { color = "#C8000EF7", variant = 0 } },
            { 19, new() { color = "#D2000EFE", variant = 0 } },
            { 20, new() { color = "#FF000EFF", variant = 0 } }
        };

    private static readonly Dictionary<string, List<string>> DefaultReqs = new()
    {
        ["(S)Weapon"] = new()
        {
            "SwordCheat", 
            "SledgeCheat"
        },
        ["(S)Armor"] = new()
        {
            "HelmetFishingHat"
        },
        ["(A)Weapon"] = new()
        {
            "SwordNiedhogg",
            "SwordNiedhoggBlood",
            "SwordNiedhoggLightning",
            "SwordNiedhoggNature",
            "SwordDyrnwyn",
            "THSwordSlayer",
            "THSwordSlayerBlood",
            "THSwordSlayerNature",
            "THSwordSlayerLightning",
            "SpearSplitner",
            "SpearSplitner_Blood",
            "SpearSplitner_Nature",
            "SpearSplitner_Lightning",
            "AxeBerzerkr",
            "AxeBerzerkrBlood",
            "AxeBerzerkrLightning",
            "AxeBerzerkrNature",
            "MaceEldner",
            "MaceEldnerBlood",
            "MaceEldnerLightning",
            "MaceEldnerNature",
            "ShieldFlametal",
            "ShieldFlametalTower",
            "BowAshlands",
            "BowAshlandsBlood",
            "BowAshlandsRoot",
            "BowAshlandsStorm",
            "CrossbowRipper",
            "CrossbowRipperBlood",
            "CrossbowRipperLightning",
            "CrossbowRipperNature",
            "StaffClusterbomb",
            "StaffGreenRoots",
            "StaffLightning"
        },
        ["(A)Armor"] = new()
        {
            "HelmetAshlandsMediumHood",
            "ArmorAshlandsMediumChest",
            "ArmorAshlandsMediumLegs",
            "HelmetMage_Ashlands",
            "ArmorMageChest_Ashlands",
            "ArmorMageLegs_Ashlands",
            "HelmetFlametal",
            "ArmorFlametalChest",
            "ArmorFlametalLegs",
            "CapeAsksvin",
            "CapeAsh"
        },
        ["(B)Weapon"] = new()
        {
            "AtgeirHimminAfl",
            "THSwordKrom",
            "SwordMistwalker",
            "AxeJotunBane",
            "BattleaxeSkullSplittur",
            "SledgeDemolisher",
            "SpearCarapace",
            "KnifeSkollAndHati",
            "ShieldCarapaceBuckler",
            "ShieldCarapace",
            "BowSpineSnap",
            "CrossbowArbalest",
            "StaffFireball",
            "StaffIceShards",
            "PickaxeBlackMetal"
        },
        ["(B)Armor"] = new()
        {
            "HelmetCarapace",
            "ArmorCarapaceChest",
            "ArmorCarapaceLegs",
            "HelmetMage",
            "ArmorMageChest",
            "ArmorMageLegs",
            "CapeFeather"
        },
        ["(C)Weapon"] = new()
        {
            "AtgeirBlackmetal",
            "SwordBlackmetal",
            "AxeBlackMetal",
            "BattleaxeBlackmetal",
            "MaceNeedle",
            "FistBjornUndeadClaw",
            "KnifeBlackMetal",
            "ShieldBlackmetal",
            "ShieldBlackmetalTower",
        },
        ["(C)Armor"] = new()
        {
            "HelmetPadded",
            "ArmorPaddedCuirass",
            "ArmorPaddedGreaves",
            "HelmetBerserkerUndead",
            "ArmorBerserkerUndeadChest",
            "ArmorBerserkerUndeadLegs",
            "CapeLinen",
            "CapeLox"
        },
        ["(D)Weapon"] = new()
        {
            "KnifeSilver",
            "SwordSilver",
            "MaceSilver",
            "SpearWolfFang",
            "BattleaxeCrystal",
            "FistFenrirClaw",
            "ShieldSilver",
            "BowDraugrFang"
        },
        ["(D)Armor"] = new()
        {
            "HelmetDrake",
            "ArmorWolfChest",
            "ArmorWolfLegs",
            "CapeWolf",
            "HelmetFenring",
            "ArmorFenringChest",
            "ArmorFenringLegs"
        },
        ["(E)Weapon"] = new()
        {
            "AtgeirIron",
            "AxeIron",
            "MaceIron",
            "SledgeIron",
            "SwordIron",
            "SpearElderbark",
            "KnifeChitin",
            "ShieldBanded",
            "ShieldIronBuckler",
            "ShieldIronTower",
            "ShieldSerpentscale",
            "BowHuntsman",
            "PickaxeIron"
        },
        ["(E)Armor"] = new()
        {
            "HelmetIron",
            "ArmorIronChest",
            "ArmorIronLegs",
            "HelmetRoot",
            "ArmorRootChest",
            "ArmorRootLegs"
        },
        ["(F)Weapon"] = new()
        {
            "Club",
            "AxeStone",
            "AxeFlint",
            "SpearFlint",
            "KnifeFlint",
            "AxeEarly",
            "SledgeStagbreaker",
            "KnifeButcher",
            "KnifeCopper",
            "AtgeirBronze",
            "AxeBronze",
            "MaceBronze",
            "SpearBronze",
            "SwordBronze",
            "FistBjornClaw",
            "ShieldWood",
            "ShieldWoodTower",
            "ShieldBronzeBuckler",
            "ShieldBoneTower",
            "Bow",
            "BowFineWood",
            "PickaxeAntler",
            "PickaxeBronze"
        },
        ["(F)Armor"] = new()
        {
            "ArmorRagsChest",
            "ArmorRagsLegs",
            "HelmetLeather",
            "ArmorLeatherChest",
            "ArmorLeatherLegs", 
            "CapeDeerHide", 
            "HelmetTrollLeather",
            "ArmorTrollLeatherChest",
            "ArmorTrollLeatherLegs",
            "CapeTrollHide",
            "HelmetBronze",
            "ArmorBronzeChest",
            "ArmorBronzeLegs",
            "HelmetBerserkerHood",
            "ArmorBerserkerChest",
            "ArmorBerserkerLegs"
        }
    };

    private static List<SyncedData.OverrideChances> DefaultOverrides_Chances = new()
    {
        {new() {
            Items = new() { "SwordCheat", "SledgeCheat" },
            Chances = new() { { 1, new() {success = 50} }, { 2, new() {success = 45} }, { 3, new() {success = 40} }, { 4, new() {success = 30} }, { 5, new() {success = 25} },
                { 6, new() {success = 20} }, { 7, new() {success = 10} }, { 8, new() {success = 5} }, { 9, new() {success = 3} }, { 10, new() {success = 0} } }
        }}
    };

    private static readonly List<SyncedData.OverrideColors> DefaultOverrides_Colors = new()
    {
        {new() {
            Items = new() { "SwordCheat", "SledgeCheat" },
            Colors = new()
            {
                { 1, new() { color = "#00190019", variant = 0 } },
                { 2, new() { color = "#00320032", variant = 0 } },
                { 3, new() { color = "#004B004B", variant = 0 } },
                { 4, new() { color = "#00640064", variant = 0 } },
                { 5, new() { color = "#007D007D", variant = 0 } },
                { 6, new() { color = "#00960096", variant = 0 } },
                { 7, new() { color = "#00AF00AF", variant = 0 } },
                { 8, new() { color = "#00C800C8", variant = 0 } },
                { 9, new() { color = "#00E100E1", variant = 0 } },
                { 10, new() { color = "#00FA00FA", variant = 0 } }
            }
        }}
    };

    private static readonly List<SyncedData.OverrideStats> DefaultOverrides_Stats = new()
    {
        {new() { 
                Items = new() { "SwordCheat", "SledgeCheat" } , 
                Stats = new() 
                {
                    { 1, new() { damage_percentage = 5, damage_fire = 10 } },
                    { 2, new() { damage_percentage = 10, damage_fire = 20 } },
                    { 3, new() { damage_percentage = 15, damage_fire = 30 } },
                    { 4, new() { damage_percentage = 20, damage_fire = 40 } },
                    { 5, new() { damage_percentage = 25, damage_fire = 50 } },
                    { 6, new() { damage_percentage = 30, damage_fire = 60 } },
                    { 7, new() { damage_percentage = 35, damage_fire = 70 } },
                    { 8, new() { damage_percentage = 40, damage_fire = 80 } },
                    { 9, new() { damage_percentage = 45, damage_fire = 90 } },
                    { 10, new() { damage_percentage = 50, damage_fire = 100 } },
                }
        }}
    };

    public static string YAML_Stats_Weapons => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultStats_Weapons);

    public static string YAML_Stats_Armor => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultStats_Armor);

    public static string YAML_Reqs => new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultReqs);

    public static string YAML_Colors => new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling
        .OmitDefaults).Build().Serialize(DefaultColors);

    public static string YAML_Chances_Weapons => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultChances_Weapons);

    public static string YAML_Chances_Armor => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultChances_Armor);

    public static string YAML_Overrides_Chances => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Chances);

    public static string YAML_Overrides_Colors => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Colors);

    public static string YAML_Overrides_Stats => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Stats);
}
