using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload(VES_Autoload.Priority.Normal, "OnInit", typeof(SyncedData))]
internal static class InventoryOverlayVfx
{
    private const int EnableInventoryVisualOrder = 104;
    private static readonly HashSet<GameObject> InitializedElementPrefabs = new();
    private static readonly HashSet<int> BoundScrollGrids = new();
    private static GameObject? _hotbarPartPrefab;
    private static ConfigEntry<bool>? _enableInventoryVisual;

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private static ConfigDescription OrderedDescription(string description, int order) => new(
        description,
        null,
        new ConfigurationManagerAttributes { Order = order });

    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        _hotbarPartPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("Enchantment_HotbarPart");
        _enableInventoryVisual = ValheimEnchantmentSystem.ClientConfig(
            "",
            "EnableInventoryVisual",
            true,
            OrderedDescription("Enable inventory and hotbar enchant visuals.", EnableInventoryVisualOrder));
        _enableInventoryVisual.SettingChanged += (_, _) => UpdateGrid();
        EngineEvents.InventoryChanged += OnInventoryChanged;
        EngineEvents.EquipmentChanged += OnEquipmentChanged;
        EngineEvents.InventoryGuiShown += UpdateGrid;
        EngineEvents.MainMenuAwake += OnMainMenuAwake;
    }

    private static bool IsInventoryVisualEnabled()
    {
        return _enableInventoryVisual?.Value ?? true;
    }

    private static RectTransform? GetInventoryViewport(InventoryGrid grid)
    {
        if (!grid)
        {
            return null;
        }

        ScrollRect scrollRect = grid.GetComponentInParent<ScrollRect>();
        if (!scrollRect)
        {
            return null;
        }

        return scrollRect.viewport ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
    }

    private static bool IsGridElementVisible(InventoryGrid.Element element, RectTransform? viewport)
    {
        if (viewport == null || element.m_go == null)
        {
            return true;
        }

        RectTransform elementRect = element.m_go.GetComponent<RectTransform>();
        if (!elementRect || !element.m_go.activeInHierarchy)
        {
            return false;
        }

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, elementRect);
        Rect viewportRect = viewport.rect;
        return bounds.max.x > viewportRect.xMin &&
               bounds.min.x < viewportRect.xMax &&
               bounds.max.y > viewportRect.yMin &&
               bounds.min.y < viewportRect.yMax;
    }

    private static void EnsureOverlayTemplate(GameObject? elementPrefab)
    {
        if (elementPrefab == null || _hotbarPartPrefab == null || InitializedElementPrefabs.Contains(elementPrefab))
        {
            return;
        }

        InitializedElementPrefabs.Add(elementPrefab);
        Transform transform = elementPrefab.transform;
        GameObject overlay = Object.Instantiate(_hotbarPartPrefab);
        overlay.transform.SetParent(transform, false);
        overlay.name = "VES_Level";
        overlay.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        overlay.SetActive(false);
    }

    private static void BindScrollVisibilityUpdates(InventoryGrid grid)
    {
        if (!grid)
        {
            return;
        }

        int instanceId = grid.GetInstanceID();
        if (!BoundScrollGrids.Add(instanceId))
        {
            return;
        }

        ScrollRect scrollRect = grid.GetComponentInParent<ScrollRect>();
        if (!scrollRect)
        {
            return;
        }

        scrollRect.onValueChanged.AddListener(_ => ApplyInventoryGridVisuals(grid, false));
    }

    private static void ApplyInventoryGridVisuals(InventoryGrid grid, bool updateContent)
    {
        if (!grid || grid.m_inventory == null || grid.m_elements == null)
        {
            return;
        }

        RectTransform? viewport = GetInventoryViewport(grid);
        int width = grid.m_inventory.GetWidth();

        foreach (InventoryGrid.Element element in grid.m_elements)
        {
            if (element?.m_go == null)
            {
                continue;
            }

            Transform overlay = element.m_go.transform.Find("VES_Level");
            if (overlay == null)
            {
                continue;
            }

            if (!element.m_used || !IsGridElementVisible(element, viewport))
            {
                overlay.gameObject.SetActive(false);
            }
        }

        foreach (ItemDrop.ItemData itemData in grid.m_inventory.GetAllItems())
        {
            InventoryGrid.Element element = grid.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
            if (element?.m_go == null)
            {
                continue;
            }

            Transform overlay = element.m_go.transform.Find("VES_Level");
            if (overlay == null)
            {
                continue;
            }

            Enchantment_Core.Enchanted en = itemData.Data().Get<Enchantment_Core.Enchanted>();
            bool visible = IsGridElementVisible(element, viewport);
            if (!(en && en.level > 0) || !visible)
            {
                overlay.gameObject.SetActive(false);
                continue;
            }

            overlay.gameObject.SetActive(true);
            if (updateContent)
            {
                Color color = SyncedData.GetColor(en, out _, true).ToColorAlpha().IncreaseColorLight();
                overlay.transform.GetChild(0).GetComponent<TMP_Text>().text = "+" + en.level;
                overlay.transform.GetChild(0).GetComponent<TMP_Text>().color = color;
                overlay.transform.GetChild(1).GetComponent<Image>().color = color;
            }

            overlay.transform.GetChild(1).gameObject.SetActive(IsInventoryVisualEnabled());
        }
    }

    public static void UpdateGrid()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        HotkeyBar_UpdateIcons_Patch._needUpdateFrame = Time.frameCount + 1;
        InventoryGrid_UpdateGui_Patch._needUpdateFrame = Time.frameCount + 1;
    }

    private static void OnInventoryChanged(Inventory inventory)
    {
        if (inventory != Player.m_localPlayer?.m_inventory)
        {
            return;
        }

        UpdateGrid();
    }

    private static void OnEquipmentChanged(Player player)
    {
        if (player != Player.m_localPlayer)
        {
            return;
        }

        UpdateGrid();
    }

    private static bool _menuFontConfigured;

    private static void OnMainMenuAwake(FejdStartup startup)
    {
        if (_menuFontConfigured || _hotbarPartPrefab == null)
        {
            return;
        }

        _menuFontConfigured = true;
        if (startup.transform.Find("StartGame/Panel/JoinPanel/serverCount")?.GetComponent<TextMeshProUGUI>() is not { } vanilla)
        {
            return;
        }

        TextMeshProUGUI tmp = _hotbarPartPrefab.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        tmp.font = vanilla.font;
        AccessTools.Field(typeof(TextMeshProUGUI), "m_canvasRenderer").SetValue(tmp, tmp.GetComponent<CanvasRenderer>());
        tmp.outlineWidth = 0.15f;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.Awake))]
    [ClientOnlyPatch]
    private static class InventoryGrid_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance)
        {
            EnsureOverlayTemplate(__instance.m_elementPrefab);
            BindScrollVisibilityUpdates(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    [ClientOnlyPatch]
    private static class Hud_Awake_Patch
    {
        internal static HotkeyBar? BarRef;

        [UsedImplicitly]
        private static void Postfix(Hud __instance)
        {
            BarRef = __instance.m_rootObject.transform.Find("HotKeyBar")?.GetComponent<HotkeyBar>();
            if (BarRef == null)
            {
                return;
            }

            EnsureOverlayTemplate(BarRef.m_elementPrefab);
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    [ClientOnlyPatch]
    private static class HotkeyBar_UpdateIcons_Patch
    {
        internal static int _needUpdateFrame = -1;

        [UsedImplicitly]
        private static void Postfix(HotkeyBar __instance)
        {
            if (__instance != Hud_Awake_Patch.BarRef)
            {
                return;
            }

            if (!Player.m_localPlayer || Player.m_localPlayer.IsDead())
            {
                return;
            }

            if (_needUpdateFrame != Time.frameCount)
            {
                return;
            }

            foreach (HotkeyBar.ElementData element in __instance.m_elements.Where(element => !element.m_used))
            {
                element.m_go.transform.Find("VES_Level").gameObject.SetActive(false);
            }

            foreach (ItemDrop.ItemData itemData in __instance.m_items)
            {
                HotkeyBar.ElementData element = __instance.m_elements[itemData.m_gridPos.x];
                Transform overlay = element.m_go.transform.Find("VES_Level");
                Enchantment_Core.Enchanted en = itemData.Data().Get<Enchantment_Core.Enchanted>();
                if (en && en.level > 0)
                {
                    overlay.gameObject.SetActive(true);
                    Color color = SyncedData.GetColor(en, out _, true).ToColorAlpha().IncreaseColorLight();
                    overlay.transform.GetChild(0).GetComponent<TMP_Text>().text = "+" + en.level;
                    overlay.transform.GetChild(0).GetComponent<TMP_Text>().color = color;
                    overlay.transform.GetChild(1).GetComponent<Image>().color = color;
                    overlay.transform.GetChild(1).gameObject.SetActive(IsInventoryVisualEnabled());
                }
                else
                {
                    overlay.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [ClientOnlyPatch]
    private static class InventoryGrid_UpdateGui_Patch
    {
        internal static int _needUpdateFrame = -1;

        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance)
        {
            if (_needUpdateFrame != Time.frameCount)
            {
                return;
            }

            ApplyInventoryGridVisuals(__instance, true);
        }
    }

}
