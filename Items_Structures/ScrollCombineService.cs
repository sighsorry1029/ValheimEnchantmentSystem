using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Integrations;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollCombineService
{
    private const int AllowCombineOrder = 1000;
    private const int RequiredShapeOrder = 999;

    private enum RequiredShape
    {
        [Description("three in a row")] Row3,
        [Description("five in a cross")] Cross5
    }

    private sealed class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private sealed class OverlayRefs
    {
        public readonly int ElementInstanceId;
        public readonly GameObject? Root;
        public readonly GameObject? Row3;
        public readonly GameObject? Cross5;

        public OverlayRefs(int elementInstanceId, GameObject? root, GameObject? row3, GameObject? cross5)
        {
            ElementInstanceId = elementInstanceId;
            Root = root;
            Row3 = row3;
            Cross5 = cross5;
        }
    }

    private sealed class GridState
    {
        public readonly int GridInstanceId;
        public InventoryGrid? Grid;
        public Inventory? Inventory;
        public OverlayRefs[] Overlays = Array.Empty<OverlayRefs>();
        public bool Dirty = true;
        public int RefreshAfterFrame;

        public GridState(int gridInstanceId, InventoryGrid grid)
        {
            GridInstanceId = gridInstanceId;
            Grid = grid;
        }
    }

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private static ConfigEntry<bool> _allowScrollCombine = null!;
    private static ConfigEntry<RequiredShape> _requiredShape = null!;
    private static GameObject _combineOutline = null!;
    private static readonly HashSet<GameObject> InitializedElementPrefabs = new();
    private static readonly Dictionary<int, GridState> GridStates = new();
    private static readonly Dictionary<Inventory, HashSet<int>> GridIdsByInventory = new(ReferenceComparer<Inventory>.Instance);
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _allowScrollCombine = ValheimEnchantmentSystem.config(
            "Scrolls",
            "Allow Combine",
            true,
            OrderedDescription("Allow combining scrolls by arranging those in a certain shape and right-clicking the center.", AllowCombineOrder));
        _requiredShape = ValheimEnchantmentSystem.config(
            "Scrolls",
            "Required Shape",
            RequiredShape.Cross5,
            OrderedDescription("what shape of the same items is required to combine", RequiredShapeOrder));
        _combineOutline = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("Enchantment_CombinePart");
        _allowScrollCombine.SettingChanged += (_, _) => RequestRefreshAll();
        _requiredShape.SettingChanged += (_, _) => RequestRefreshAll();
    }

    public static void AttachCombineOutline(InventoryGrid inventoryGrid)
    {
        if (inventoryGrid?.m_elementPrefab == null || _combineOutline == null)
        {
            return;
        }

        if (InitializedElementPrefabs.Add(inventoryGrid.m_elementPrefab))
        {
            GameObject newIcon = Object.Instantiate(_combineOutline);
            newIcon.transform.SetParent(inventoryGrid.m_elementPrefab.transform, false);
            newIcon.name = "VES_Combine";
            newIcon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            newIcon.SetActive(false);
        }

        _ = GetOrCreateGridState(inventoryGrid);
    }

    public static void BindInventoryGrid(InventoryGrid inventoryGrid, Inventory? inventory)
    {
        if (inventoryGrid == null)
        {
            return;
        }

        GridState state = GetOrCreateGridState(inventoryGrid);
        if (ReferenceEquals(state.Inventory, inventory))
        {
            return;
        }

        UnbindInventory(state);
        state.Inventory = inventory;
        if (inventory != null)
        {
            if (!GridIdsByInventory.TryGetValue(inventory, out HashSet<int>? gridIds))
            {
                gridIds = new HashSet<int>();
                GridIdsByInventory[inventory] = gridIds;
            }

            gridIds.Add(state.GridInstanceId);
        }

        RequestRefresh(inventoryGrid, nextFrame: false);
    }

    public static void RequestRefresh(InventoryGrid? inventoryGrid, bool nextFrame = true)
    {
        if (inventoryGrid == null)
        {
            return;
        }

        GridState state = GetOrCreateGridState(inventoryGrid);
        state.Dirty = true;
        int targetFrame = Time.frameCount + (nextFrame ? 1 : 0);
        state.RefreshAfterFrame = nextFrame
            ? Math.Max(state.RefreshAfterFrame, targetFrame)
            : targetFrame;
    }

    public static void RequestRefresh(Inventory? inventory, bool nextFrame = true)
    {
        if (inventory == null || !GridIdsByInventory.TryGetValue(inventory, out HashSet<int>? gridIds))
        {
            return;
        }

        foreach (int gridId in gridIds.ToArray())
        {
            if (!GridStates.TryGetValue(gridId, out GridState? state) || state.Grid == null)
            {
                gridIds.Remove(gridId);
                continue;
            }

            RequestRefresh(state.Grid, nextFrame);
        }

        if (gridIds.Count == 0)
        {
            GridIdsByInventory.Remove(inventory);
        }
    }

    public static void RequestRefreshAll(bool nextFrame = true)
    {
        foreach (KeyValuePair<int, GridState> entry in GridStates.ToArray())
        {
            if (entry.Value.Grid == null)
            {
                RemoveGridState(entry.Key);
                continue;
            }

            RequestRefresh(entry.Value.Grid, nextFrame);
        }
    }

    public static void TryFlushDirtyRefresh(InventoryGrid inventoryGrid)
    {
        if (inventoryGrid == null)
        {
            return;
        }

        GridState state = GetOrCreateGridState(inventoryGrid);
        if (!ReferenceEquals(state.Inventory, inventoryGrid.m_inventory))
        {
            BindInventoryGrid(inventoryGrid, inventoryGrid.m_inventory);
            state = GetOrCreateGridState(inventoryGrid);
        }

        if (!state.Dirty || Time.frameCount < state.RefreshAfterFrame)
        {
            return;
        }

        if (!RefreshCombineIndicators(state))
        {
            return;
        }

        state.Dirty = false;
        state.RefreshAfterFrame = 0;
    }

    private static bool RefreshCombineIndicators(GridState state)
    {
        InventoryGrid? inventoryGrid = state.Grid;
        Inventory? inventory = inventoryGrid?.m_inventory ?? state.Inventory;
        if (inventoryGrid == null || inventory == null || inventoryGrid.m_elements == null)
        {
            return false;
        }

        EnsureOverlayCache(state);
        foreach (OverlayRefs overlay in state.Overlays)
        {
            overlay.Root?.SetActive(false);
        }

        if (!_allowScrollCombine.Value)
        {
            return true;
        }

        bool row3Visible = _requiredShape.Value == RequiredShape.Row3;
        bool cross5Visible = _requiredShape.Value == RequiredShape.Cross5;

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (!ScrollRegistry.IsUpgradeableScroll(item))
            {
                continue;
            }

            if (!CanCombine(item, inventory, out _))
            {
                continue;
            }

            int elementIndex = item.m_gridPos.y * inventory.m_width + item.m_gridPos.x;
            if (elementIndex < 0 || elementIndex >= state.Overlays.Length)
            {
                continue;
            }

            OverlayRefs overlay = state.Overlays[elementIndex];
            if (overlay.Root == null)
            {
                continue;
            }

            overlay.Root.SetActive(true);
            overlay.Row3?.SetActive(row3Visible);
            overlay.Cross5?.SetActive(cross5Visible);
        }

        return true;
    }

    private static void EnsureOverlayCache(GridState state)
    {
        InventoryGrid? inventoryGrid = state.Grid;
        if (inventoryGrid?.m_elements == null)
        {
            state.Overlays = Array.Empty<OverlayRefs>();
            return;
        }

        bool rebuild = state.Overlays.Length != inventoryGrid.m_elements.Count;
        if (!rebuild)
        {
            for (int i = 0; i < inventoryGrid.m_elements.Count; ++i)
            {
                InventoryGrid.Element element = inventoryGrid.m_elements[i];
                GameObject? elementGo = element?.m_go;
                OverlayRefs overlay = state.Overlays[i];
                if (elementGo == null || overlay.Root == null || overlay.ElementInstanceId != elementGo.GetInstanceID())
                {
                    rebuild = true;
                    break;
                }
            }
        }

        if (!rebuild)
        {
            return;
        }

        OverlayRefs[] overlays = new OverlayRefs[inventoryGrid.m_elements.Count];
        for (int i = 0; i < inventoryGrid.m_elements.Count; ++i)
        {
            InventoryGrid.Element element = inventoryGrid.m_elements[i];
            GameObject? elementGo = element?.m_go;
            Transform? combine = elementGo != null ? elementGo.transform.Find("VES_Combine") : null;
            overlays[i] = new OverlayRefs(
                elementGo ? elementGo.GetInstanceID() : 0,
                combine?.gameObject,
                combine != null && combine.childCount > 1 ? combine.GetChild(1).gameObject : null,
                combine != null && combine.childCount > 2 ? combine.GetChild(2).gameObject : null);
        }

        state.Overlays = overlays;
    }

    private static GridState GetOrCreateGridState(InventoryGrid inventoryGrid)
    {
        int gridInstanceId = inventoryGrid.GetInstanceID();
        if (!GridStates.TryGetValue(gridInstanceId, out GridState? state))
        {
            state = new GridState(gridInstanceId, inventoryGrid);
            GridStates[gridInstanceId] = state;
        }

        state.Grid = inventoryGrid;
        return state;
    }

    private static void UnbindInventory(GridState state)
    {
        if (state.Inventory == null)
        {
            return;
        }

        if (GridIdsByInventory.TryGetValue(state.Inventory, out HashSet<int>? gridIds))
        {
            gridIds.Remove(state.GridInstanceId);
            if (gridIds.Count == 0)
            {
                GridIdsByInventory.Remove(state.Inventory);
            }
        }

        state.Inventory = null;
    }

    private static void RemoveGridState(int gridInstanceId)
    {
        if (!GridStates.TryGetValue(gridInstanceId, out GridState? state))
        {
            return;
        }

        UnbindInventory(state);
        GridStates.Remove(gridInstanceId);
    }

    public static void TryCombineAt(InventoryGrid inventoryGrid, UIInputHandler element)
    {
        if (!_allowScrollCombine.Value || inventoryGrid == null || element == null)
        {
            return;
        }

        Vector2i buttonPos = inventoryGrid.GetButtonPos(element.gameObject);
        ItemDrop.ItemData itemAt = inventoryGrid.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
        if (!ScrollRegistry.IsUpgradeableScroll(itemAt))
        {
            return;
        }

        if (!TryConsumePattern(itemAt, inventoryGrid.m_inventory, out int amountToInstantiate))
        {
            return;
        }

        string currentPrefabName = itemAt.m_dropPrefab.name;
        if (!ScrollRegistry.TryGetUpgradePrefabName(currentPrefabName, out string upgradedPrefabName))
        {
            return;
        }

        Utils.InstantiateItem(ZNetScene.instance.GetPrefab(upgradedPrefabName), amountToInstantiate, 1, inventoryGrid.m_inventory);
        RequestRefresh(inventoryGrid.m_inventory);
        UI.VES_UI.PlayClick();
    }

    public static void AppendCombineTooltip(ItemDrop.ItemData item, bool crafting, ref string tooltip)
    {
        if (!_allowScrollCombine.Value || !ScrollRegistry.IsUpgradeableScroll(item))
        {
            return;
        }

        tooltip += "\n\n" + GetCombineInstructionText();
    }

    public static string GetCombineInstructionText()
    {
        return "$enchantment_putinlinetocombine".Localize(GetRequiredShapeMarkup());
    }

    private static ConfigDescription OrderedDescription(string description, int order)
    {
        return new ConfigDescription(description, null, new ConfigurationManagerAttributes { Order = order });
    }

    private static string GetRequiredShapeMarkup()
    {
        RequiredShape shape = _initialized ? _requiredShape.Value : RequiredShape.Cross5;
        return shape == RequiredShape.Row3 ? "<color=yellow><b>-</b></color>" : "<color=yellow><b>+</b></color>";
    }

    private static bool CanCombine(ItemDrop.ItemData item, Inventory grid, out int toInstantiate)
    {
        return _requiredShape.Value switch
        {
            RequiredShape.Row3 => HaveSurrounds3(item, grid, out toInstantiate),
            RequiredShape.Cross5 => HaveSurrounds5(item, grid, out toInstantiate),
            _ => ReturnFalse(out toInstantiate)
        };
    }

    private static bool TryConsumePattern(ItemDrop.ItemData item, Inventory grid, out int toInstantiate)
    {
        return _requiredShape.Value switch
        {
            RequiredShape.Row3 => HaveSurrounds3(item, grid, out toInstantiate, true),
            RequiredShape.Cross5 => HaveSurrounds5(item, grid, out toInstantiate, true),
            _ => ReturnFalse(out toInstantiate)
        };
    }

    private static bool ReturnFalse(out int value)
    {
        value = 0;
        return false;
    }

    private static bool HaveSurrounds3(ItemDrop.ItemData item, Inventory grid, out int toInstantiate, bool removeIfTrue = false)
    {
        toInstantiate = 0;
        Vector2i pos = item.m_gridPos;
        int left = pos.x - 1;
        int right = pos.x + 1;
        int leftLeft = pos.x - 2;
        int rightRight = pos.x + 2;

        if (left < 0 || right >= grid.m_width)
        {
            return false;
        }

        ItemDrop.ItemData leftItem = grid.GetItemAt(left, pos.y);
        ItemDrop.ItemData rightItem = grid.GetItemAt(right, pos.y);
        if (!MatchesSamePrefab(item, leftItem, rightItem))
        {
            return false;
        }

        if (leftLeft >= 0 && grid.GetItemAt(leftLeft, pos.y) is { } leftLeftItem && leftLeftItem.m_dropPrefab.name == item.m_dropPrefab.name)
        {
            return false;
        }

        if (rightRight < grid.m_width && grid.GetItemAt(rightRight, pos.y) is { } rightRightItem && rightRightItem.m_dropPrefab.name == item.m_dropPrefab.name)
        {
            return false;
        }

        toInstantiate = Mathf.Min(item.m_stack, leftItem.m_stack, rightItem.m_stack);
        if (removeIfTrue)
        {
            grid.RemoveItem(item, toInstantiate);
            grid.RemoveItem(leftItem, toInstantiate);
            grid.RemoveItem(rightItem, toInstantiate);
        }

        return true;
    }

    private static bool HaveSurrounds5(ItemDrop.ItemData item, Inventory grid, out int toInstantiate, bool removeIfTrue = false)
    {
        toInstantiate = 0;
        Vector2i pos = item.m_gridPos;
        int reservedTopRows = IntegrationRegistry.GetReservedTopRows();
        if (reservedTopRows > 0 && pos.y < reservedTopRows)
        {
            return false;
        }

        int left = pos.x - 1;
        int right = pos.x + 1;
        int up = pos.y - 1;
        int down = pos.y + 1;
        if (left < 0 || right >= grid.m_width || up < 0 || down >= grid.m_height)
        {
            return false;
        }

        ItemDrop.ItemData leftItem = grid.GetItemAt(left, pos.y);
        ItemDrop.ItemData rightItem = grid.GetItemAt(right, pos.y);
        ItemDrop.ItemData upItem = grid.GetItemAt(pos.x, up);
        ItemDrop.ItemData downItem = grid.GetItemAt(pos.x, down);
        if (!MatchesSamePrefab(item, leftItem, rightItem, upItem, downItem))
        {
            return false;
        }

        if (HasAdjacentDuplicate(grid, item.m_dropPrefab.name, pos.x - 2, pos.y) ||
            HasAdjacentDuplicate(grid, item.m_dropPrefab.name, pos.x + 2, pos.y) ||
            HasAdjacentDuplicate(grid, item.m_dropPrefab.name, pos.x, pos.y - 2) ||
            HasAdjacentDuplicate(grid, item.m_dropPrefab.name, pos.x, pos.y + 2))
        {
            return false;
        }

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

    private static bool MatchesSamePrefab(ItemDrop.ItemData centerItem, params ItemDrop.ItemData[] surroundingItems)
    {
        if (centerItem?.m_dropPrefab == null)
        {
            return false;
        }

        foreach (ItemDrop.ItemData surroundingItem in surroundingItems)
        {
            if (surroundingItem?.m_dropPrefab == null || surroundingItem.m_dropPrefab.name != centerItem.m_dropPrefab.name)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAdjacentDuplicate(Inventory inventory, string prefabName, int x, int y)
    {
        if (x < 0 || y < 0 || x >= inventory.m_width || y >= inventory.m_height)
        {
            return false;
        }

        return inventory.GetItemAt(x, y) is { } item && item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName;
    }
}
