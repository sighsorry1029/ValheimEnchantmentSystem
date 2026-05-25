using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

internal static class OverlayUiHost
{
    internal sealed class PanelRegistration
    {
        public readonly string Key;
        public readonly Func<bool> IsVisible;
        public readonly Func<bool>? BlocksInventoryHide;
        public readonly Action<GameObject>? ConfigureTooltipPrefab;
        public readonly Action<InventoryGui>? OnInventoryGuiAwake;
        public readonly Action? OnInventoryGuiShow;

        public PanelRegistration(
            string key,
            Func<bool> isVisible,
            Func<bool>? blocksInventoryHide = null,
            Action<GameObject>? configureTooltipPrefab = null,
            Action<InventoryGui>? onInventoryGuiAwake = null,
            Action? onInventoryGuiShow = null)
        {
            Key = key;
            IsVisible = isVisible ?? throw new ArgumentNullException(nameof(isVisible));
            BlocksInventoryHide = blocksInventoryHide;
            ConfigureTooltipPrefab = configureTooltipPrefab;
            OnInventoryGuiAwake = onInventoryGuiAwake;
            OnInventoryGuiShow = onInventoryGuiShow;
        }
    }

    private static readonly Dictionary<string, PanelRegistration> Panels = new(StringComparer.Ordinal);

    public static void Register(PanelRegistration registration)
    {
        if (registration == null || string.IsNullOrWhiteSpace(registration.Key))
        {
            return;
        }

        Panels[registration.Key] = registration;
    }

    public static bool AnyVisible()
    {
        return Panels.Values.Any(panel => SafeInvoke(panel.Key, panel.IsVisible));
    }

    public static bool BlocksInventoryHide()
    {
        return Panels.Values.Any(panel =>
            panel.BlocksInventoryHide != null &&
            SafeInvoke(panel.Key, panel.BlocksInventoryHide));
    }

    public static void NotifyInventoryGuiAwake(InventoryGui inventoryGui)
    {
        if (inventoryGui == null)
        {
            return;
        }

        GameObject? tooltipPrefab = inventoryGui.m_playerGrid?.m_elementPrefab?.GetComponent<UITooltip>()?.m_tooltipPrefab;
        if (tooltipPrefab != null)
        {
            foreach (PanelRegistration panel in Panels.Values)
            {
                SafeInvoke(panel.Key, () => panel.ConfigureTooltipPrefab?.Invoke(tooltipPrefab));
            }
        }

        foreach (PanelRegistration panel in Panels.Values)
        {
            SafeInvoke(panel.Key, () => panel.OnInventoryGuiAwake?.Invoke(inventoryGui));
        }
    }

    public static void NotifyInventoryGuiShow()
    {
        foreach (PanelRegistration panel in Panels.Values)
        {
            SafeInvoke(panel.Key, () => panel.OnInventoryGuiShow?.Invoke());
        }
    }

    private static bool SafeInvoke(string key, Func<bool> callback)
    {
        try
        {
            return callback();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[kg.ValheimEnchantmentSystem] Overlay host callback failed for '{key}': {ex.Message}");
            return false;
        }
    }

    private static void SafeInvoke(string key, Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[kg.ValheimEnchantmentSystem] Overlay host callback failed for '{key}': {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    [ClientOnlyPatch]
    private static class TextInput_IsVisible_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result)
        {
            __result |= AnyVisible();
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    [ClientOnlyPatch]
    private static class StoreGui_IsVisible_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ref bool __result)
        {
            __result |= AnyVisible();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [ClientOnlyPatch]
    private static class InventoryGui_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            NotifyInventoryGuiAwake(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [ClientOnlyPatch]
    private static class InventoryGui_Show_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            NotifyInventoryGuiShow();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    [ClientOnlyPatch]
    private static class InventoryGui_Hide_Patch
    {
        [UsedImplicitly]
        private static bool Prefix()
        {
            return !BlocksInventoryHide();
        }
    }
}
