using System.Reflection;
using BepInEx.Bootstrap;
using TMPro;
using UnityEngine.EventSystems;

namespace kg.ValheimEnchantmentSystem.UI;

internal static class EnchantmentShortcutInput
{
    internal static bool IsShortcutDown(KeyboardShortcut shortcut)
    {
        // Use Valheim's input backend and require only the configured modifiers. BepInEx's
        // IsDown additionally rejects the shortcut when any unrelated non-mouse key is held.
        return EnchantmentShortcutRules.IsPressed(shortcut.MainKey, KeyCode.None, shortcut.Modifiers,
            key => ZInput.GetKeyDown(key, false), key => ZInput.GetKey(key, false));
    }

    internal static bool IsBlocked() => GetBlockReason() != null;

    internal static string? GetBlockReason()
    {
        if (Console.IsVisible()) return "Console";
        if (Menu.IsVisible()) return "Menu";
        if (Info_UI.IsVisible()) return "Info";
        if (Chat.instance != null && Chat.instance.HasFocus()) return "Chat";
        if (Minimap.IsOpen() || Minimap.InTextInput()) return "Minimap";
        if (TextViewer.instance != null && TextViewer.instance.IsVisible()) return "TextViewer";
        if (ZInput.VirtualKeyboardOpen) return "VirtualKeyboard";
        // StoreGui.IsVisible also includes our overlay through OverlayUiHost.
        if (StoreGui.instance != null && StoreGui.instance.m_rootPanel != null &&
            StoreGui.instance.m_rootPanel.activeInHierarchy) return "Store";

        // TextInput.IsVisible is patched by this mod to include the enchantment panel itself.
        // Check the actual input dialog instead, so the shortcut can still close our panel.
        if (TextInput.instance != null && TextInput.instance.m_panel != null &&
            TextInput.instance.m_panel.activeInHierarchy) return "SignTextInput";

        InventoryGui? inventory = InventoryGui.instance;
        if (inventory != null)
        {
            if (inventory.m_craftTimer >= 0f) return "Crafting";
            if (IsVisible(inventory.m_splitPanel)) return "InventorySplitDialog";
            if (IsVisible(inventory.m_variantDialog)) return "InventoryVariantDialog";
            if (IsVisible(inventory.m_skillsDialog)) return "InventorySkillsDialog";
            if (IsVisible(inventory.m_textsDialog)) return "InventoryTextsDialog";
            if (inventory.m_trophiesPanel != null && inventory.m_trophiesPanel.activeInHierarchy)
                return "InventoryTrophiesPanel";
        }

        GameObject? selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null)
        {
            if (selected.GetComponentInParent<TMP_InputField>() is { isFocused: true }) return "TMP_InputField";
            if (selected.GetComponentInParent<InputField>() is { isFocused: true }) return "InputField";
        }

        // Configuration Manager uses IMGUI, not EventSystem input fields. Its optional public
        // visibility property avoids firing the shortcut while editing settings or rebinding it.
        foreach (var plugin in Chainloader.PluginInfos.Values)
        {
            if (plugin.Instance == null ||
                !EnchantmentShortcutRules.IsConfigurationManager(plugin.Metadata.GUID, plugin.Metadata.Name)) continue;

            PropertyInfo? visible = plugin.Instance.GetType().GetProperty("DisplayingWindow",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (visible?.PropertyType != typeof(bool) || !visible.CanRead) continue;
            try
            {
                if (visible.GetValue(plugin.Instance) is true) return "ConfigManager:" + plugin.Metadata.GUID;
            }
            catch (Exception)
            {
                // An optional integration must not break the inventory if its plugin is unavailable.
            }
        }

        return null;
    }

    private static bool IsVisible(Component? component) => component != null && component.gameObject.activeInHierarchy;
}
