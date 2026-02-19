using ItemManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

[VES_Autoload]
public static class ScrollItems
{
    private static GameObject CombineOutline;
    private static GameObject PrefabRoot;
    
    private static ConfigEntry<float> DropChance;
    private static ConfigEntry<float> DropChance_Bosses;
    private static ConfigEntry<float> DropChance_Blessed;
    private static ConfigEntry<float> DropChance_Blessed_Bosses;
    private static ConfigEntry<float> DropChance_Skill;
    private static ConfigEntry<float> DropChance_Skill_Bosses;
    
    private static ConfigEntry<bool> MonsterDroppingScrolls;
    private static ConfigEntry<bool> MonsterDroppingSkilllScrolls;

    private static ConfigEntry<int> BlessedConvertRequirement;

    private static ConfigEntry<string> ExcludePrefabsFromDrop;
    
    private static readonly Dictionary<Heightmap.Biome, ConfigEntry<string>> BiomeMapper = new();
    private static readonly Dictionary<char, ConfigEntry<int>> BookXPMapper = new();
    private static readonly List<GameObject> SkillScrolls = new(5);
    
    private enum RequiredLine { Three, Five}
    
    private static ConfigEntry<bool> AllowScrollCombine;
    private static ConfigEntry<RequiredLine> RequiredLine_Config;

    private static readonly Dictionary<char, int> SkillExpScroll_DefaultValues = new()
    {
        {'F', 15}, { 'E', 25 }, { 'D', 50 }, { 'C', 75 }, { 'B', 100 }, { 'A', 140 }, { 'S', 200 }
    };
    
    private static readonly HashSet<string> UpgradeScrollHashset = new();

    private static readonly HashSet<string> ExludedDroPrefabs = new();
    private static readonly Dictionary<string, GameObject> NameToPrefab = new();

    private struct RecipeData
    {
        public int amount;
        public string[] reqs;
        public RecipeData(int amount, params string[] reqs)
        {
            this.amount = amount;
            this.reqs = reqs;
        }
    }

    private static readonly Dictionary<char, RecipeData> DefaultRecipes_Weapon = new()
    {
        { 'F', new RecipeData(6, "TrophyNeck,1", "Dandelion,6", "Wood,24", "Stone,12")},
        { 'E', new RecipeData(3, "TrophyDraugr,1", "Entrails,3", "ElderBark,12", "Guck,6")},
        { 'D', new RecipeData(3, "TrophyWolf,1", "WolfPelt,3", "Crystal,6", "Obsidian,9")},
        { 'C', new RecipeData(6, "TrophyDeathsquito,1", "Needle,6", "FineWood,24", "Tar,12")},
        { 'B', new RecipeData(6, "TrophySeeker,1", "Carapace,12", "YggdrasilWood,18", "BlackMarble,24")},
        { 'A', new RecipeData(3, "TrophyVolture,1", "ProustitePowder,6", "Blackwood,12", "Grausten,24")},
        { 'S', new RecipeData(1, "TrophyFader,1")},
    };
    private static readonly Dictionary<char, RecipeData> DefaultRecipes_Weapon_Blessed = new()
    {
        { 'F', new RecipeData(2, "TrophyBjorn,1", "BjornPaw,4", "BoneFragments,12", "Bronze,8")},
        { 'E', new RecipeData(2, "TrophyDraugrElite,1", "Bloodbag,4", "Chain,2", "Iron,8")},
        { 'D', new RecipeData(4, "TrophyFenring,1", "WolfFang,8", "JuteRed,4", "Silver,16")},
        { 'C', new RecipeData(4, "TrophyBjornUndead,1", "UndeadBjornRibcage,8", "LinenThread,24", "BlackMetal,16")},
        { 'B', new RecipeData(1, "TrophyGjall,1", "Bilebag,2", "BlackCore,1", "Eitr,4")},
        { 'A', new RecipeData(4, "TrophyFallenValkyrie,1", "CelestialFeather,12", "BonemawSerpentTooth,6", "FlametalNew,16")},
        { 'S', new RecipeData(1, "TrophyFader,1")},
    };
    private static readonly Dictionary<char, RecipeData> DefaultRecipes_Armor = new()
    {
        { 'F', new RecipeData(2, "TrophyBoar,1", "LeatherScraps,2", "Resin,6", "Flint,4")},
        { 'E', new RecipeData(3, "TrophyBlob,1", "Ooze,6", "ElderBark,12", "Guck,6")},
        { 'D', new RecipeData(3, "TrophyHatchling,1", "FreezeGland,3", "Crystal,6", "Obsidian,9")},
        { 'C', new RecipeData(3, "TrophyGoblin,1", "Coins,30", "FineWood,12", "Tar,6")},
        { 'B', new RecipeData(6, "TrophyTick,1", "GiantBloodSack,6", "YggdrasilWood,18", "BlackMarble,24")},
        { 'A', new RecipeData(3, "TrophyAsksvin,1", "Pickable_SulfurRock,3", "Blackwood,12", "Grausten,24")},
        { 'S', new RecipeData(1, "TrophyFader,1")},
    };
    private static readonly Dictionary<char, RecipeData> DefaultRecipes_Armor_Blessed = new()
    {
        { 'F', new RecipeData(1, "TrophyFrostTroll,1", "TrollHide,5", "Ectoplasm,1", "Bronze,4")},
        { 'E', new RecipeData(1, "TrophyAbomination,1", "Root,5", "Chitin,2", "Iron,4")},
        { 'D', new RecipeData(4, "TrophySGolem,1", "WolfClaw,2", "WolfHairBundle,8", "Silver,16")},
        { 'C', new RecipeData(2, "TrophyLox,1", "LoxPelt,6", "BarleyFlour,12", "BlackMetal,8")},
        { 'B', new RecipeData(4, "TrophySeekerBrute,1", "Mandible,4", "JuteBlue,6", "Eitr,16")},
        { 'A', new RecipeData(4, "TrophyMorgen,1", "MorgenHeart,4", "CharredBone,24", "FlametalNew,16")},
        { 'S', new RecipeData(1, "TrophyFader,1")},
    };

    private static readonly Dictionary<char, int> DefaultCraftAmount_Convert = new()
    {
        { 'F', 1 }, { 'E', 1 }, { 'D', 1 }, { 'C', 1 }, { 'B', 1 }, { 'A', 1 }, { 'S', 1 }
    };
    
    private static void FillRecipe(Item item, char tier, bool bless, bool isArmor)
    {
        Dictionary<char, RecipeData> targetDic;
        if (isArmor)
            targetDic = bless ? DefaultRecipes_Armor_Blessed : DefaultRecipes_Armor;
        else
            targetDic = bless ? DefaultRecipes_Weapon_Blessed : DefaultRecipes_Weapon;
        
        RecipeData recipe = targetDic[tier];
        item.CraftAmount = recipe.amount;
        foreach (string s in recipe.reqs)
        {
            string[] split = s.Split(',');
            string name = split[0];
            int amount = int.Parse(split[1]);
            item.RequiredItems.Add(name, amount);
        }
        item.Crafting.Add("kg_EnchantmentScrollStation", 1);
    }
    
    [UsedImplicitly]
    private static void OnInit()
    {
        AllowScrollCombine = ValheimEnchantmentSystem.config("Scrolls", "Allow Combine", true, "Allow combining scrolls.");
        CombineOutline = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("Enchantment_CombinePart");
        MonsterDroppingScrolls = ValheimEnchantmentSystem.config("Scrolls", "Drop From Monsters", true, "Allow monsters to drop scrolls.");
        MonsterDroppingSkilllScrolls = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop From Monsters (Skill exp)", true, "Allow monsters to drop enchant skill exp scrolls.");
        DropChance = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance", 7f, "Chance to drop from enemies.");
        DropChance_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Drop Chance (Bosses)", 100f, "Chance to drop from bosses.");
        DropChance_Blessed = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance", 0.5f, "Chance to drop from enemies.");
        DropChance_Blessed_Bosses = ValheimEnchantmentSystem.config("Scrolls", "Blessed Drop Chance (Bosses)", 40f, "Chance to drop from bosses.");
        DropChance_Skill = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp)", 0.20f, "Chance to drop from enemies.");
        DropChance_Skill_Bosses = ValheimEnchantmentSystem.config("Skill Scrolls", "Drop Chance (Skill exp) (Bosses)", 25f, "Chance to drop from bosses.");
        BlessedConvertRequirement = ValheimEnchantmentSystem.config("Scrolls", "Blessed Convert Requirement", 12, "Amount of normal scrolls required to craft a blessed scroll of the same tier.");
        ExcludePrefabsFromDrop = ValheimEnchantmentSystem.config("Scrolls", "Exclude Prefabs From Drop", "TentaRoot", "Comma separated list of prefabs to exclude from dropping scrolls.");
        RequiredLine_Config = ValheimEnchantmentSystem.config("Scrolls", "Required Line", RequiredLine.Five, "How many lines of the same item are required to combine.");
        ExcludePrefabsFromDrop.SettingChanged += FillExclude;
        FillExclude();
        
        BiomeMapper.Add(Heightmap.Biome.Meadows, ValheimEnchantmentSystem.config("Scrolls", "1 - Meadows Tier", "F", "Tier of scrolls Meadows (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.BlackForest, ValheimEnchantmentSystem.config("Scrolls", "2 - BlackForest Tier", "F", "Tier of scrolls BlackForest (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Swamp, ValheimEnchantmentSystem.config("Scrolls", "3 - Swamp Tier", "E", "Tier of scrolls Swamp (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Ocean, ValheimEnchantmentSystem.config("Scrolls", "4 - Ocean Tier", "E", "Tier of scrolls Ocean (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mountain, ValheimEnchantmentSystem.config("Scrolls", "5 - Mountain Tier", "D", "Tier of scrolls Mountain (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Plains, ValheimEnchantmentSystem.config("Scrolls", "6 - Plains Tier", "C", "Tier of scrolls Plains (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.Mistlands, ValheimEnchantmentSystem.config("Scrolls", "7 - Mistlands Tier", "B", "Tier of scrolls Mistlands (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.AshLands, ValheimEnchantmentSystem.config("Scrolls", "8 - Ashlands Tier", "A", "Tier of scrolls Ashlands (F E D C B A S)"));
        BiomeMapper.Add(Heightmap.Biome.DeepNorth, ValheimEnchantmentSystem.config("Scrolls", "9 - DeepNorth Tier", "S", "Tier of scrolls DeepNorth (F E D C B A S)"));
        
        
        char[] FEDCBAS = {'F', 'E', 'D', 'C', 'B', 'A', 'S'};
        foreach (char c in FEDCBAS)
        {
            Item weaponScroll;
            Item weaponScroll_Bless;
            Item armorScroll;
            Item armorScroll_Bless;
            GameObject skillScrollPrefab;

            if (c == 'E')
            {
                GameObject weaponPrefab = ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantScroll_Weapon_S"), "kg_EnchantScroll_Weapon_E");
                GameObject weaponBlessedPrefab = ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantScroll_Weapon_Blessed_S"), "kg_EnchantScroll_Weapon_Blessed_E");
                GameObject armorPrefab = ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantScroll_Armor_S"), "kg_EnchantScroll_Armor_E");
                GameObject armorBlessedPrefab = ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantScroll_Armor_Blessed_S"), "kg_EnchantScroll_Armor_Blessed_E");
                skillScrollPrefab = ClonePrefab(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantSkillScroll_S"), "kg_EnchantSkillScroll_E");

                if (weaponPrefab == null || weaponBlessedPrefab == null || armorPrefab == null || armorBlessedPrefab == null || skillScrollPrefab == null) continue;

                weaponScroll = new Item(weaponPrefab);
                weaponScroll_Bless = new Item(weaponBlessedPrefab);
                armorScroll = new Item(armorPrefab);
                armorScroll_Bless = new Item(armorBlessedPrefab);
            }
            else
            {
                GameObject weaponPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantScroll_Weapon_{c}");
                GameObject weaponBlessedPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantScroll_Weapon_Blessed_{c}");
                GameObject armorPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantScroll_Armor_{c}");
                GameObject armorBlessedPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantScroll_Armor_Blessed_{c}");
                skillScrollPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>($"kg_EnchantSkillScroll_{c}");

                if (weaponPrefab == null || weaponBlessedPrefab == null || armorPrefab == null || armorBlessedPrefab == null || skillScrollPrefab == null) continue;

                weaponScroll = new Item(weaponPrefab);
                weaponScroll_Bless = new Item(weaponBlessedPrefab);
                armorScroll = new Item(armorPrefab);
                armorScroll_Bless = new Item(armorBlessedPrefab);
            }

            weaponScroll.Configurable = Configurability.Recipe;
            weaponScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_{c}_weapon";
            weaponScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = "$kg_enchantscroll_weapon_description";
            FillRecipe(weaponScroll, c, false, false);
            NameToPrefab[$"$kg_enchantscroll_{c}_weapon"] = weaponScroll.Prefab;

            weaponScroll_Bless.Configurable = Configurability.Recipe;
            weaponScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_{c}_weapon_blessed";
            weaponScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = "$kg_enchantscroll_weapon_blessed_description";
            FillRecipe(weaponScroll_Bless, c, true, false);
            weaponScroll_Bless["ConvertNormal"].CraftAmount = DefaultCraftAmount_Convert[c];
            weaponScroll_Bless["ConvertNormal"].RequiredItems.Add(weaponScroll.Prefab.name, BlessedConvertRequirement);
            weaponScroll_Bless["ConvertNormal"].Crafting.Add("kg_EnchantmentScrollStation", 1);
            NameToPrefab[$"$kg_enchantscroll_{c}_weapon_blessed"] = weaponScroll_Bless.Prefab;

            armorScroll.Configurable = Configurability.Recipe;
            armorScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_{c}_armor";
            armorScroll.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = "$kg_enchantscroll_armor_description";
            FillRecipe(armorScroll, c, false, true);
            NameToPrefab[$"$kg_enchantscroll_{c}_armor"] = armorScroll.Prefab;

            armorScroll_Bless.Configurable = Configurability.Recipe;
            armorScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name = $"$kg_enchantscroll_{c}_armor_blessed";
            armorScroll_Bless.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_description = "$kg_enchantscroll_armor_blessed_description";
            FillRecipe(armorScroll_Bless, c, true, true);
            armorScroll_Bless["ConvertNormal"].CraftAmount = DefaultCraftAmount_Convert[c];
            armorScroll_Bless["ConvertNormal"].RequiredItems.Add(armorScroll.Prefab.name, BlessedConvertRequirement);
            armorScroll_Bless["ConvertNormal"].Crafting.Add("kg_EnchantmentScrollStation", 1);
            NameToPrefab[$"$kg_enchantscroll_{c}_armor_blessed"] = armorScroll_Bless.Prefab;
          
            BookXPMapper.Add(c, ValheimEnchantmentSystem.config("Skill Scrolls", $"Skill EXP Scroll {c}", SkillExpScroll_DefaultValues[c], $"Skill EXP Scroll {c}"));
            if (skillScrollPrefab.GetComponent<ItemDrop>() is { } skillItemDrop)
                skillItemDrop.m_itemData.m_shared.m_name = $"$kg_enchantskillscroll_{c}";
            NameToPrefab[$"$kg_enchantskillscroll_{c}"] = skillScrollPrefab;
            SkillScrolls.Add(skillScrollPrefab);

            if (c != 'S')
            {
                UpgradeScrollHashset.Add(weaponScroll.Prefab.name);
                UpgradeScrollHashset.Add(weaponScroll_Bless.Prefab.name);
                UpgradeScrollHashset.Add(armorScroll.Prefab.name);
                UpgradeScrollHashset.Add(armorScroll_Bless.Prefab.name);
            } 
        }
        
        SkillScrolls.ForEach(x => x.AddComponent<ExpScroll>());
    }

    private static GameObject ClonePrefab(GameObject prefab, string newName)
    {
        if (prefab == null) return null;
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
        if (PrefabRoot == null)
        {
            PrefabRoot = new GameObject("VES_PrefabRoot");
            PrefabRoot.SetActive(false);
            Object.DontDestroyOnLoad(PrefabRoot);
        }
        clone.transform.SetParent(PrefabRoot.transform, false);
        clone.SetActive(wasActive);
        return clone;
    }

    private static void FillExclude(object sender = null, EventArgs e = null)
    {
        ExludedDroPrefabs.Clear();
        if(string.IsNullOrWhiteSpace(ExcludePrefabsFromDrop.Value)) return;
        string[] split = ExcludePrefabsFromDrop.Value.Replace(" ","").Split(',');
        foreach (string s in split)
        {
            ExludedDroPrefabs.Add(s);
        }
    }

    [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            foreach (GameObject go in SkillScrolls)
            {
                if (go == null) continue;
                if (!__instance.m_prefabs.Contains(go))
                    __instance.m_prefabs.Add(go);
                __instance.m_namedPrefabs[go.name.GetStableHashCode()] = go;
            }
        }
    }
    
    static void DropItem(GameObject prefab, Vector3 centerPos, float dropArea)
    {
        if (prefab == null) return;
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
        Vector3 b = Random.insideUnitSphere * dropArea;
        GameObject gameObject = Object.Instantiate(prefab, centerPos + b, rotation);
        gameObject.SetActive(true);
        Rigidbody component = gameObject.GetComponent<Rigidbody>();
        if (component)
        {
            Vector3 insideUnitSphere = Random.insideUnitSphere;
            if (insideUnitSphere.y < 0f)
            {
                insideUnitSphere.y = -insideUnitSphere.y;
            }

            component.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
        }
    }
    
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [ServerOnlyPatch]
    static class Tome_SpawnLoot_Patch
    {
        private static void TryDropDefault(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            float dropChance = isBoss ? DropChance_Bosses.Value : DropChance.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                bool isWeapon = Random.value < 0.5f;
                string book = isWeapon ? $"kg_EnchantScroll_Weapon_{tier}" : $"kg_EnchantScroll_Armor_{tier}";
                DropItem(ZNetScene.instance.GetPrefab(book), pos + Vector3.up * 0.75f, 0.5f);
            }
        }
        
        private static void TryDropBlessed(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            float dropChance = isBoss ? DropChance_Blessed_Bosses.Value : DropChance_Blessed.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                bool isWeapon = Random.value < 0.5f;
                string book = isWeapon ? $"kg_EnchantScroll_Weapon_Blessed_{tier}" : $"kg_EnchantScroll_Armor_Blessed_{tier}";
                DropItem(ZNetScene.instance.GetPrefab(book), pos + Vector3.up * 0.75f, 0.5f);
            }
        }

        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingScrolls.Value || __instance.IsPlayer() || !__instance.m_nview.IsOwner() || __instance.IsTamed()) return;
            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExludedDroPrefabs.Contains(prefabName)) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            if(!BiomeMapper.TryGetValue(biome, out ConfigEntry<string> tier)) return;
            Vector3 position = __instance.transform.position;
            char tierValue = tier.Value[0];
            TryDropDefault(tierValue, __instance.IsBoss(), position);
            TryDropBlessed(tierValue, __instance.IsBoss(), position);
        }
    }
    
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [ServerOnlyPatch]
    static class Tome_SpawnLootSkill_Patch
    {
        private static void TryDropSkillScroll(char tier, bool isBoss, Vector3 pos)
        {
            float rand = Random.value;
            float dropChance = isBoss ? DropChance_Skill_Bosses.Value : DropChance_Skill.Value;
            dropChance /= 100f;
            if(rand <= dropChance)
            {
                DropItem(ZNetScene.instance.GetPrefab($"kg_EnchantSkillScroll_{tier}"), pos + Vector3.up * 0.75f, 0.5f);
            }
        }

        [UsedImplicitly]
        private static void Prefix(Character __instance)
        {
            if (!MonsterDroppingSkilllScrolls.Value || __instance.IsPlayer() || !__instance.m_nview.IsOwner() || __instance.IsTamed()) return;
            string prefabName = global::Utils.GetPrefabName(__instance.gameObject);
            if (ExludedDroPrefabs.Contains(prefabName)) return;
            Heightmap.Biome biome = EnvMan.instance.m_currentBiome;
            if(!BiomeMapper.TryGetValue(biome, out ConfigEntry<string> tier)) return;
            Vector3 position = __instance.transform.position;
            char tierValue = tier.Value[0];
            TryDropSkillScroll(tierValue, __instance.IsBoss(), position);
        }
    }


    public class ExpScroll : MonoBehaviour, Interactable, Hoverable
    {
        private ZNetView _znv;

        private int CreationTime {
            get => _znv.GetZDO().GetInt("CreationTime");
            set => _znv.GetZDO().Set("CreationTime", value);
        }

        private const int MaxDuration = 120;
        
        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
            if(!_znv.IsValid() || !_znv.IsOwner()) return;
            
            if (CreationTime == 0)
            {
                CreationTime = (int)EnvMan.instance.m_totalSeconds;
                return;
            }

            if (EnvMan.instance.m_totalSeconds - CreationTime > MaxDuration)
            {
                _znv.ClaimOwnership();
                ZNetScene.instance.Destroy(gameObject);
            }
            
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (_znv == null || !_znv.IsValid()) return false;
            string prefabName = global::Utils.GetPrefabName(gameObject);
            char tier = prefabName[prefabName.Length - 1];
            if (!BookXPMapper.TryGetValue(tier, out ConfigEntry<int> exp)) return false;
            Utils.IncreaseSkillEXP(Enchantment_Skill.SkillType_Enchantment, exp.Value);
            if (!ValheimEnchantmentSystem.NoGraphics && ZNetScene.instance != null)
            {
                Player player = user as Player ?? Player.m_localPlayer;
                if (player != null)
                {
                    GameObject fx = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
                    if (fx != null)
                        Instantiate(fx, player.transform.position, Quaternion.identity);
                }
            }
            _znv.ClaimOwnership();
            if (ZNetScene.instance != null)
                ZNetScene.instance.Destroy(gameObject);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return "<b>$enchantment_skill_scroll</b>\n\n[<color=yellow><b>$KEY_Use</b></color>] $enchantment_skill_scroll_use".Localize();
        }

        public string GetHoverName()
        {
            return "";
        }
    }
    
    
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
    [ClientOnlyPatch]
    public static class InventoryGrid_Awake_Patch
    {
        private static HashSet<GameObject> firsttime = new();
        
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            if (!__instance.m_elementPrefab) return;
            if (firsttime.Contains(__instance.m_elementPrefab)) return;
            firsttime.Add(__instance.m_elementPrefab);
            Transform transform = __instance.m_elementPrefab.transform;
            GameObject newIcon = Object.Instantiate(CombineOutline);
            newIcon!.transform.SetParent(transform);
            newIcon.name = "VES_Combine";
            newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            newIcon.gameObject.SetActive(false);
        }
    }


    private static bool HaveSurrounds_3(ItemDrop.ItemData item, Inventory grid, out int toInstantiate, bool removeIfTrue = false)
    {
        toInstantiate = 0;
        Vector2i pos = item.m_gridPos;
        int gridX = grid.m_width;
        
        int left = pos.x - 1;
        int right = pos.x + 1;
        int leftLeft = pos.x - 2;
        int rightRight = pos.x + 2; 
        
        if(left < 0 || right >= gridX) return false;
        
        ItemDrop.ItemData leftItem = grid.GetItemAt(left, pos.y);
        ItemDrop.ItemData rightItem = grid.GetItemAt(right, pos.y);
        if (leftItem == null || rightItem == null) return false;
        if (leftItem.m_dropPrefab.name != item.m_dropPrefab.name || rightItem.m_dropPrefab.name != item.m_dropPrefab.name) return false;
        
        if (leftLeft >= 0 && grid.GetItemAt(leftLeft, pos.y) is {} leftLeftItem && leftLeftItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;
        if (rightRight < gridX && grid.GetItemAt(rightRight, pos.y) is {} rightRightItem && rightRightItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;
        
        toInstantiate = Mathf.Min(item.m_stack, leftItem.m_stack, rightItem.m_stack);
        if (removeIfTrue)
        {
            grid.RemoveItem(item, toInstantiate);
            grid.RemoveItem(leftItem, toInstantiate);
            grid.RemoveItem(rightItem, toInstantiate);
        }
         
        return true;
    }

    private static bool HaveSurrounds_5(ItemDrop.ItemData item, Inventory grid, out int toInstantiate, bool removeIfTrue = false)
    {
        toInstantiate = 0;
        Vector2i pos = item.m_gridPos;
        if (Other_Mods_APIs.AUGA && pos.y <= 1) return false;
        int gridX = grid.m_width;

        int left = pos.x - 1;
        int right = pos.x + 1;
        int leftLeft = pos.x - 2;
        int rightRight = pos.x + 2;

        if (left < 0 || right >= gridX) return false;

        ItemDrop.ItemData leftItem = grid.GetItemAt(left, pos.y);
        ItemDrop.ItemData rightItem = grid.GetItemAt(right, pos.y);
        if (leftItem == null || rightItem == null) return false;
        if (leftItem.m_dropPrefab.name != item.m_dropPrefab.name ||
            rightItem.m_dropPrefab.name != item.m_dropPrefab.name) return false;

        if (leftLeft >= 0 && grid.GetItemAt(leftLeft, pos.y) is { } leftLeftItem &&
            leftLeftItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;
        if (rightRight < gridX && grid.GetItemAt(rightRight, pos.y) is { } rightRightItem &&
            rightRightItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;

        int up = pos.y - 1;
        int down = pos.y + 1;
        int upUp = pos.y - 2;
        int downDown = pos.y + 2;

        if (up < 0 || down >= grid.m_height) return false;

        ItemDrop.ItemData upItem = grid.GetItemAt(pos.x, up);
        ItemDrop.ItemData downItem = grid.GetItemAt(pos.x, down);
        if (upItem == null || downItem == null) return false;
        if (upItem.m_dropPrefab.name != item.m_dropPrefab.name ||
            downItem.m_dropPrefab.name != item.m_dropPrefab.name) return false;

        if (upUp >= 0 && grid.GetItemAt(pos.x, upUp) is { } upUpItem &&
            upUpItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;
        if (downDown < grid.m_height && grid.GetItemAt(pos.x, downDown) is { } downDownItem &&
            downDownItem.m_dropPrefab.name == item.m_dropPrefab.name) return false;
        
        toInstantiate = Mathf.Min(item.m_stack, leftItem.m_stack, rightItem.m_stack, upItem.m_stack, downItem.m_stack);
        if (removeIfTrue)
        {
            grid.RemoveItem(item, toInstantiate);
            grid.RemoveItem(leftItem, toInstantiate);
            grid.RemoveItem(rightItem, toInstantiate);
            grid.RemoveItem(upItem, toInstantiate);
            grid.RemoveItem(downItem, toInstantiate);
        }
        return true;
    }



    private static void FixDropPrefab(ItemDrop.ItemData item)
    {
        if (item != null && !item.m_dropPrefab && item.m_shared != null && item.m_shared.m_name != null)
        {
            if (NameToPrefab.TryGetValue(item.m_shared.m_name, out GameObject prefab))
            {
                item.m_dropPrefab = prefab;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [ClientOnlyPatch]
    private static class InventoryGrid_UpdateGui_Patch
    {
        [UsedImplicitly]
        public static void Postfix(InventoryGrid __instance)
        {
            foreach (InventoryGrid.Element element in __instance.m_elements)
            {
                element.m_go.transform.Find("VES_Combine").gameObject.SetActive(false); 
            }
            if (!AllowScrollCombine.Value) return;
            foreach (ItemDrop.ItemData item in __instance.m_inventory.GetAllItems())
            {
                FixDropPrefab(item);
                if (item.m_dropPrefab == null || !UpgradeScrollHashset.Contains(item.m_dropPrefab.name)) continue;

                switch (RequiredLine_Config.Value)
                {
                    case RequiredLine.Three:
                        if (!HaveSurrounds_3(item, __instance.m_inventory, out _)) continue;
                        break;
                    case RequiredLine.Five:
                        if (!HaveSurrounds_5(item, __instance.m_inventory, out _)) continue;
                        break;
                    default: continue;
                }
                InventoryGrid.Element element = __instance.m_elements[item.m_gridPos.y * __instance.m_inventory.m_width + item.m_gridPos.x];
                Transform combine = element.m_go.transform.Find("VES_Combine");
                combine.gameObject.SetActive(true);
                combine.transform.GetChild(1).gameObject.SetActive(RequiredLine_Config.Value == RequiredLine.Three);
                combine.transform.GetChild(2).gameObject.SetActive(RequiredLine_Config.Value == RequiredLine.Five);
            }
        }
    }
    
    
    private static readonly Dictionary<char, char> UpgradeMapper = new()
    {
        {'F', 'E'}, {'E', 'D'}, {'D', 'C'}, {'C', 'B'}, {'B', 'A'}, {'A', 'S'}
    };
    
    
    [HarmonyPatch(typeof(InventoryGrid),nameof(InventoryGrid.OnRightClick))]
    [ClientOnlyPatch]
    private static class InventoryGrid_OnRightClick_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance, UIInputHandler element)
        {
            if (!AllowScrollCombine.Value) return;
            GameObject gameObject = element.gameObject;
            Vector2i buttonPos = __instance.GetButtonPos(gameObject);
            ItemDrop.ItemData itemAt = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
            if (itemAt == null) return;
            FixDropPrefab(itemAt);
            if (itemAt.m_dropPrefab == null) return;
            string dropPrefab = itemAt.m_dropPrefab.name;
            if (!UpgradeScrollHashset.Contains(dropPrefab)) return;
            int toInstantiate;
            switch (RequiredLine_Config.Value)
            {
                case RequiredLine.Three:
                    if (!HaveSurrounds_3(itemAt, __instance.m_inventory, out toInstantiate, true)) return;
                    break;
                case RequiredLine.Five:
                    if (!HaveSurrounds_5(itemAt, __instance.m_inventory, out toInstantiate,  true)) return;
                    break;
                default: return;
            }
            char tier = dropPrefab[dropPrefab.Length - 1];
            char newTier = UpgradeMapper[tier];
            string newDropPrefab = dropPrefab.Substring(0, dropPrefab.Length - 1) + newTier;
            Utils.InstantiateItem(ZNetScene.instance.GetPrefab(newDropPrefab), toInstantiate, 1, __instance.m_inventory);
            VES_UI.PlayClick();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    [ClientOnlyPatch]
    public class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, ref string __result)
        {
            if (crafting || !AllowScrollCombine.Value) return;
            FixDropPrefab(item);
            if (!item.m_dropPrefab) return;
            string dropPrefab = item.m_dropPrefab.name;
            if (!UpgradeScrollHashset.Contains(dropPrefab)) return;
            string shape = RequiredLine_Config.Value == RequiredLine.Three ? "<color=yellow><b>-</b></color>" : "<color=yellow><b>+</b></color>";
            __result += "\n\n$enchantment_putinlinetocombine".Localize(shape);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem), typeof(ItemDrop.ItemData), typeof(int), typeof(Vector3), typeof(Quaternion))]
    private static class ItemDrop_DropItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData item)
        {
            FixDropPrefab(item);
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __result)
        {
            if (__result != null && !__result.gameObject.activeSelf)
            {
                __result.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Save))]
    private static class Inventory_Save_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Inventory __instance)
        {
            foreach (ItemDrop.ItemData item in __instance.GetAllItems())
            {
                FixDropPrefab(item);
            }
        }
    }

}
