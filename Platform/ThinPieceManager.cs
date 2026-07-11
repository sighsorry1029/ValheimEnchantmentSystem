using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using LocalizationManager;
using UnityEngine;

namespace PieceManager;

[PublicAPI]
public enum CraftingTable
{
    None,
    [InternalName("piece_workbench")] Workbench,
    [InternalName("piece_cauldron")] Cauldron,
    [InternalName("forge")] Forge,
    [InternalName("piece_artisanstation")] ArtisanTable,
    [InternalName("piece_stonecutter")] StoneCutter,
    [InternalName("piece_magetable")] MageTable,
    [InternalName("blackforge")] BlackForge,
    [InternalName("piece_preptable")] FoodPreparationTable,
    [InternalName("piece_MeadCauldron")] MeadKetill,
    Custom,
}

public class InternalName : Attribute
{
    public readonly string internalName;

    public InternalName(string internalName)
    {
        this.internalName = internalName;
    }
}

[PublicAPI]
public enum BuildPieceCategory
{
    Misc = 0,
    Crafting = 1,
    BuildingWorkbench = 2,
    BuildingStonecutter = 3,
    Furniture = 4,
    All = 100,
    Custom = 99,
}

public struct Requirement
{
    public string itemName;
    public int amount;
    public bool recover;
}

[PublicAPI]
public class RequiredResourcesList
{
    public readonly List<Requirement> Requirements = new();

    public void Add(string item, int amount, bool recover)
    {
        Requirements.Add(new Requirement { itemName = item, amount = amount, recover = recover });
    }
}

public struct CraftingStationConfig
{
    public CraftingTable Table;
    public string? custom;
}

[PublicAPI]
public class CraftingStationList
{
    public readonly List<CraftingStationConfig> Stations = new();

    public void Set(CraftingTable table)
    {
        Stations.Add(new CraftingStationConfig { Table = table });
    }

    public void Set(string customTable)
    {
        Stations.Add(new CraftingStationConfig { Table = CraftingTable.Custom, custom = customTable });
    }
}

[PublicAPI]
public class BuildingPieceCategory
{
    public BuildPieceCategory Category;
    public string custom = "";

    public void Set(BuildPieceCategory category)
    {
        Category = category;
    }

    public void Set(string customCategory)
    {
        Category = BuildPieceCategory.Custom;
        custom = customCategory;
    }
}

[PublicAPI]
public class PieceTool
{
    public readonly HashSet<string> Tools = new();

    public void Add(string tool)
    {
        Tools.Add(tool);
    }
}

[PublicAPI]
public class BuildPiece
{
    private sealed class PieceConfig
    {
        public ConfigEntry<string>? craft;
    }

    private static readonly Harmony Harmony = new("kg.ValheimEnchantmentSystem.ThinPieceManager");
    internal static readonly List<BuildPiece> registeredPieces = new();
    private static readonly Dictionary<BuildPiece, PieceConfig> pieceConfigs = new();
    private static bool configBindingsInitialized;

    [Description("Disables generation of configs for registered pieces.")]
    public static bool ConfigurationEnabled = true;

    public readonly GameObject Prefab;
    public readonly RequiredResourcesList RequiredItems = new();
    public readonly BuildingPieceCategory Category = new();
    public readonly PieceTool Tool = new();
    public CraftingStationList Crafting = new();
    public ConfigEntryBase? RecipeIsActive;

    static BuildPiece()
    {
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(Patch_FejdStartup))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(Patch_ObjectDBInit))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(Patch_ObjectDBInit))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(Patch_ZNetSceneAwake))));
    }

    public BuildPiece(AssetBundle bundle, string prefabName)
    {
        Prefab = bundle.LoadAsset<GameObject>(prefabName) ?? throw new InvalidOperationException($"Could not load piece prefab '{prefabName}' from asset bundle.");
        Prefab.name = prefabName;
        ApplyStaticDefaults(this);
        registeredPieces.Add(this);
    }

    private static void Patch_FejdStartup() => EnsureConfigBindings();

    internal static void Patch_ObjectDBInit(ObjectDB __instance)
    {
        EnsureConfigBindings();
        if (__instance.GetItemPrefab("Hammer") == null)
        {
            return;
        }

        foreach (BuildPiece piece in registeredPieces)
        {
            ApplyRuntimeConfiguration(piece, __instance, ZNetScene.instance);
        }
    }

    internal static void Patch_ZNetSceneAwake(ZNetScene __instance)
    {
        foreach (BuildPiece piece in registeredPieces)
        {
            if (!__instance.m_prefabs.Contains(piece.Prefab))
            {
                __instance.m_prefabs.Add(piece.Prefab);
            }

            int prefabHash = piece.Prefab.name.GetStableHashCode();
            if (!__instance.m_namedPrefabs.ContainsKey(prefabHash))
            {
                __instance.m_namedPrefabs.Add(prefabHash, piece.Prefab);
            }
        }
    }

    internal static void EnsureConfigBindings()
    {
        if (configBindingsInitialized)
        {
            return;
        }

        foreach (BuildPiece piece in registeredPieces)
        {
            ApplyStaticDefaults(piece);
            if (!ConfigurationEnabled)
            {
                continue;
            }

            string pieceNameToken = piece.Prefab.GetComponent<Piece>()?.m_name ?? piece.Prefab.name;
            string localizedName = GetLocalizedDisplayName(pieceNameToken, piece.Prefab.name);
            string englishName = SharedLocalizationCache.LocalizeForConfig(pieceNameToken);
            string configGroup = string.IsNullOrWhiteSpace(englishName) ? localizedName : englishName;
            PieceConfig cfg = pieceConfigs[piece] = new PieceConfig();

            cfg.craft = Config(configGroup, "Crafting Costs", SerializedRequirements.Serialize(piece.RequiredItems.Requirements),
                ConfigurationManagerDisplay.Description(
                    $"Item costs to craft {localizedName}.",
                    ConfigurationManagerDisplay.General,
                    900,
                    $"{configGroup} - Crafting Costs"));
            cfg.craft.SettingChanged += (_, _) => ReapplyRuntimeConfiguration(piece);
        }

        configBindingsInitialized = true;
    }

    private static void ReapplyRuntimeConfiguration(BuildPiece piece)
    {
        if (ObjectDB.instance == null)
        {
            return;
        }

        ApplyRuntimeConfiguration(piece, ObjectDB.instance, ZNetScene.instance);
    }

    private static void ApplyStaticDefaults(BuildPiece piece)
    {
        Piece? pieceComponent = piece.Prefab.GetComponent<Piece>();
        if (pieceComponent == null)
        {
            return;
        }

        pieceComponent.m_category = piece.Category.Category == BuildPieceCategory.Custom
            ? Piece.PieceCategory.Crafting
            : (Piece.PieceCategory)piece.Category.Category;
    }

    private static void ApplyRuntimeConfiguration(BuildPiece piece, ObjectDB objectDb, ZNetScene? scene)
    {
        Piece? pieceComponent = piece.Prefab.GetComponent<Piece>();
        if (pieceComponent == null)
        {
            return;
        }

        if (pieceConfigs.TryGetValue(piece, out PieceConfig? cfg) && cfg.craft is { } craftConfig)
        {
            pieceComponent.m_resources = SerializedRequirements.ToPieceReqs(objectDb, craftConfig.Value);
        }
        else
        {
            pieceComponent.m_resources = SerializedRequirements.ToPieceReqs(objectDb, piece.RequiredItems.Requirements);
        }

        ApplyCraftingStation(piece, pieceComponent, scene);
        ApplyToolRegistration(piece, objectDb);

        if (piece.RecipeIsActive is { } enabledConfig)
        {
            pieceComponent.m_enabled = Convert.ToInt32(enabledConfig.BoxedValue) != 0;
        }
    }

    private static void ApplyCraftingStation(BuildPiece piece, Piece pieceComponent, ZNetScene? scene)
    {
        if (piece.Crafting.Stations.Count == 0 || scene == null)
        {
            pieceComponent.m_craftingStation = null;
            return;
        }

        CraftingStationConfig station = piece.Crafting.Stations.First();

        switch (station.Table)
        {
            case CraftingTable.None:
                pieceComponent.m_craftingStation = null;
                break;
            case CraftingTable.Custom:
                pieceComponent.m_craftingStation = scene.GetPrefab(station.custom)?.GetComponent<CraftingStation>();
                break;
            default:
                pieceComponent.m_craftingStation = scene.GetPrefab(GetInternalName(station.Table))?.GetComponent<CraftingStation>();
                break;
        }
    }

    private static void ApplyToolRegistration(BuildPiece piece, ObjectDB objectDb)
    {
        foreach (GameObject itemPrefab in objectDb.m_items)
        {
            PieceTable? pieceTable = itemPrefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_buildPieces;
            pieceTable?.m_pieces.Remove(piece.Prefab);
        }

        IEnumerable<string> tools = piece.Tool.Tools.DefaultIfEmpty("Hammer");

        foreach (string tool in tools)
        {
            GameObject toolPrefab = objectDb.GetItemPrefab(tool);
            if (!toolPrefab)
            {
                continue;
            }

            PieceTable? pieceTable = toolPrefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_buildPieces;
            if (pieceTable != null && !pieceTable.m_pieces.Contains(piece.Prefab))
            {
                pieceTable.m_pieces.Add(piece.Prefab);
            }
        }
    }

    private static string GetLocalizedDisplayName(string? token, string fallback)
    {
        if (string.IsNullOrWhiteSpace(token) || Localization.instance == null)
        {
            return fallback;
        }

        string localized = Localization.instance.Localize(token).Trim();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, token, StringComparison.Ordinal) ? fallback : localized;
    }

    private static string GetInternalName(CraftingTable value)
    {
        return ((InternalName)typeof(CraftingTable).GetMember(value.ToString())[0].GetCustomAttributes(typeof(InternalName), false).First()).internalName;
    }

    private static BaseUnityPlugin? _plugin;
    private static BaseUnityPlugin Plugin => _plugin ??= (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(Assembly.GetExecutingAssembly().DefinedTypes.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

    private static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description)
    {
        return global::kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem.config(group, name, value, description, true);
    }

    private static class SerializedRequirements
    {
        public static string Serialize(IEnumerable<Requirement> requirements)
        {
            return string.Join(",", requirements.Select(requirement => $"{requirement.itemName}:{requirement.amount}:{requirement.recover}"));
        }

        public static Piece.Requirement[] ToPieceReqs(ObjectDB objectDb, IEnumerable<Requirement> requirements)
        {
            return requirements.Where(requirement => !string.IsNullOrWhiteSpace(requirement.itemName))
                .Select(requirement =>
                {
                    ItemDrop? itemDrop = objectDb.GetItemPrefab(requirement.itemName)?.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        Debug.LogWarning($"{Plugin.Info.Metadata.GUID}: required build piece item '{requirement.itemName}' does not exist.");
                        return null;
                    }

                    return new Piece.Requirement
                    {
                        m_resItem = itemDrop,
                        m_amount = requirement.amount,
                        m_recover = requirement.recover
                    };
                })
                .Where(requirement => requirement != null)
                .ToArray()!;
        }

        public static Piece.Requirement[] ToPieceReqs(ObjectDB objectDb, string serialized)
        {
            return ToPieceReqs(objectDb, serialized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
            {
                string[] split = entry.Split(':');
                return new Requirement
                {
                    itemName = split[0],
                    amount = split.Length > 1 && int.TryParse(split[1], out int amount) ? amount : 1,
                    recover = split.Length <= 2 || !bool.TryParse(split[2], out bool recover) || recover
                };
            }));
        }
    }
}
