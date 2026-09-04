using BepInEx.Bootstrap;
using BepInEx.Configuration;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine.Audio;

namespace kg.ValheimEnchantmentSystem.UI;

public static class VES_UI
{
    private static AudioSource? AUsrc;
    private static AudioClip _oneSecondClip = null!;
    private static AudioClip _click = null!;
    private static AudioClip _successSound = null!;
    private static AudioClip _failSound = null!;
    private static GameObject _completionVfxPrefab = null!;
    private static Sprite _defaultQuestionMark = null!;
    private static MainEnchantmentView? _view;
    private static MainEnchantmentController? _controller;
    private static Button? _enchantmentButton;
    private static GameObject? _enchantmentBackground;

    public static ConfigEntry<float> _enchantmentAnimationDuration = null!;
    private static ConfigEntry<GamepadShortcutButton> _gamepadShortcut = null!;
    private static ConfigEntry<GamepadShortcutButton> _gamepadShortcutModifier = null!;
    private static volatile bool _gamepadShortcutChanged;
    private static ConfigEntry<KeyboardShortcut> _keyboardShortcut = null!;
    private static volatile bool _keyboardShortcutChanged;

    private enum GamepadShortcutButton
    {
        Disabled,
        JoyButtonA,
        JoyButtonB,
        JoyButtonX,
        JoyButtonY,
        JoyLBumper,
        JoyRBumper,
        JoyLTrigger,
        JoyRTrigger,
        JoyLStick,
        JoyRStick,
        JoyBack,
        JoyStart,
        JoyDPadUp,
        JoyDPadDown,
        JoyDPadLeft,
        JoyDPadRight
    }

    public static bool IsVisible() => _controller?.IsVisible ?? false;

    internal static void Initialize()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        _enchantmentAnimationDuration = ValheimEnchantmentSystem.ClientConfig(
            string.Empty,
            "EnchantmentAnimationDuration",
            EnchantmentAnimationTiming.DefaultDuration,
            ConfigurationManagerDisplay.Description(
                "Enchantment animation duration in seconds, from 0 to 1. Set to 0 for an instant result without the waiting animation or sound; success/failure effects and sounds are retained. Fractional values such as 0.5 are supported. Values outside 0-1 are clamped. Changes apply to the next enchantment attempt.",
                ConfigurationManagerDisplay.Client,
                990,
                "UI - Enchantment Animation Duration",
                new AcceptableValueRange<float>(0f, 1f),
                showRangeAsPercent: false));
        _gamepadShortcut = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "Gamepad Shortcut",
            GamepadShortcutButton.JoyButtonY,
            ConfigurationManagerDisplay.Description(
                "Press this button while holding Gamepad Shortcut Modifier to open the inventory and enchantment panel together, or toggle the panel if the inventory is already open. Client-only. Default: LB + Y. Set to Disabled to disable the gamepad shortcut. Accepted combinations consume the buttons' normal actions until release; a modifier pressed before the combination can still perform its normal action.",
                ConfigurationManagerDisplay.Client,
                1000,
                "UI - Gamepad Shortcut"));
        _gamepadShortcutModifier = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "Gamepad Shortcut Modifier",
            GamepadShortcutButton.JoyLBumper,
            ConfigurationManagerDisplay.Description(
                "Hold this button before pressing Gamepad Shortcut. Default: LB. Set to Disabled for a single-button shortcut, but avoid buttons used by normal gameplay. The modifier must differ from the shortcut button. Release both buttons after changing the binding. Client-only; ignored while typing or using settings/dialogs.",
                ConfigurationManagerDisplay.Client,
                999,
                "UI - Gamepad Shortcut Modifier"));
        _gamepadShortcut.SettingChanged += (_, _) => _gamepadShortcutChanged = true;
        _gamepadShortcutModifier.SettingChanged += (_, _) => _gamepadShortcutChanged = true;
        ConfigureGamepadShortcut();
        _keyboardShortcut = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "Keyboard Shortcut",
            new KeyboardShortcut(KeyCode.Y),
            ConfigurationManagerDisplay.Description(
                "Keyboard shortcut to open the inventory and enchantment panel together, or toggle the panel if the inventory is already open. Client-only; supports modifier keys. Default: Y. Set to None to disable. Ignored while typing or using settings/dialogs. Avoid keys already used by the inventory or other mods; Escape, Tab, Space and Return already control the enchantment panel.",
                ConfigurationManagerDisplay.Client,
                1001,
                "UI - Keyboard Shortcut"));
        _keyboardShortcut.SettingChanged += (_, _) => _keyboardShortcutChanged = true;
        ConfigEntry<float> panelOffsetX = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "Enchantment Panel Offset X",
            0f,
            ConfigurationManagerDisplay.Description(
                "Horizontal offset from the default enchantment panel position, in canvas UI units (scaled with the UI, not physical screen pixels). Positive moves right; negative moves left. Set both offsets to 0 to restore the default position. Changes apply immediately; off-screen positions are constrained to keep the title reachable.",
                ConfigurationManagerDisplay.Client,
                978,
                "UI - Enchantment Panel Offset X",
                browsable: false));
        ConfigEntry<float> panelOffsetY = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "Enchantment Panel Offset Y",
            0f,
            ConfigurationManagerDisplay.Description(
                "Vertical offset from the default enchantment panel position, in canvas UI units (scaled with the UI, not physical screen pixels). Positive moves up; negative moves down. Set both offsets to 0 to restore the default position. The position is client-only and persists across sessions.",
                ConfigurationManagerDisplay.Client,
                977,
                "UI - Enchantment Panel Offset Y",
                browsable: false));
        _oneSecondClip = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_1");
        _completionVfxPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_VFX1");
        _defaultQuestionMark = ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_EnchantmentQuestion");
        _click = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentClick");
        _successSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Success");
        _failSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Fail");

        GameObject uiRoot = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI"));
        uiRoot.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(uiRoot);

        _view = MainEnchantmentView.Attach(uiRoot);
        EnchantmentPanelDrag.Attach(uiRoot.transform, panelOffsetX, panelOffsetY, Info_UI.IsVisible);
        _controller = new MainEnchantmentController(
            _view,
            _defaultQuestionMark,
            _completionVfxPrefab,
            _oneSecondClip,
            _successSound,
            _failSound,
            () => AUsrc,
            () => _enchantmentAnimationDuration.Value,
            PlayClick,
            Info_UI.Show,
            Info_UI.IsVisible);
        OverlayUiHost.Register(new OverlayUiHost.PanelRegistration(
            "Enchantment",
            IsVisible,
            blocksInventoryHide: IsVisible,
            configureTooltipPrefab: tooltipPrefab => _controller?.ConfigureTooltipPrefab(tooltipPrefab),
            onInventoryGuiAwake: HandleInventoryGuiAwake,
            onInventoryGuiShow: HandleInventoryGuiShow));
    }

    public static void Update()
    {
        if (_gamepadShortcutChanged)
        {
            _gamepadShortcutChanged = false;
            ConfigureGamepadShortcut();
        }

        bool gamepadPressed = EnchantmentGamepadShortcutInput.TakePendingPress();
        // Consume the toggle before panel actions so a rebound Space/Return cannot also start enchanting.
        if (TryHandleKeyboardShortcut() || (gamepadPressed && TryToggleFromShortcut())) return;
        _controller?.Update();
    }

    private static bool TryHandleKeyboardShortcut()
    {
        if (_keyboardShortcutChanged)
        {
            _keyboardShortcutChanged = false;
            return false;
        }

        return _keyboardShortcut != null &&
               EnchantmentShortcutInput.IsShortcutDown(_keyboardShortcut.Value) && TryToggleFromShortcut();
    }

    internal static bool CanHandleShortcut()
    {
        Player? player = Player.m_localPlayer;
        return _controller != null && InventoryGui.instance != null && player != null &&
               Application.isFocused && !player.IsDead() && !player.InCutscene() && !player.IsTeleporting() &&
               !GameCamera.InFreeFly() && !EnchantmentShortcutInput.IsBlocked();
    }

    private static bool TryToggleFromShortcut()
    {
        if (!CanHandleShortcut()) return false;

        // Global shortcuts do not depend on an inactive inventory button or its gamepad UI group.
        UIGamePad.m_lastInteractFrame = Time.frameCount;
        // Holding a shoulder modifier may already have opened the radial menu. Cancel it without
        // activating its selected item before opening the inventory or toggling our panel.
        if (Hud.instance != null && Hud.instance.m_radialMenu != null && Hud.instance.m_radialMenu.Active)
            Hud.instance.m_radialMenu.InstantClose();
        TogglePanel();
        return true;
    }

    private static void ConfigureGamepadShortcut()
    {
        EnchantmentGamepadShortcutInput.Configure(ToZInputKey(_gamepadShortcut.Value),
            ToZInputKey(_gamepadShortcutModifier.Value));
    }

    private static string GetEnchantmentShortcutLabel()
    {
        if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
        {
            return GetGamepadShortcutLabel();
        }

        return _keyboardShortcut.Value.MainKey == KeyCode.None ? string.Empty : _keyboardShortcut.Value.ToString();
    }

    private static void Show()
    {
        _controller?.Show();
    }

    private static void Hide()
    {
        _controller?.Hide();
    }

    private static void TogglePanel()
    {
        if (IsVisible()) Hide();
        else Show();
        PlayClick();
    }

    private static void HandleInventoryGuiAwake(InventoryGui inventoryGui)
    {
        if (_enchantmentButton != null)
        {
            return;
        }

        if (inventoryGui.m_repairPanel.gameObject != inventoryGui.m_repairButton.gameObject)
        {
            _enchantmentBackground = UnityEngine.Object.Instantiate(inventoryGui.m_repairPanel.gameObject);
            _enchantmentBackground.transform.SetParent(inventoryGui.m_repairPanel.transform.parent, false);
            RectTransform rectTransform = _enchantmentBackground.GetComponent<RectTransform>();
            rectTransform.anchoredPosition += new Vector2(0, 74);
            _enchantmentBackground.transform.SetAsFirstSibling();
        }
        else
        {
            _enchantmentBackground = new GameObject("enchantment_menu_background");
        }

        _enchantmentButton = UnityEngine.Object.Instantiate(inventoryGui.m_repairButton.gameObject).GetComponent<Button>();
        _enchantmentButton.transform.SetParent(inventoryGui.m_repairButton.transform.parent, false);
        _enchantmentButton.name = "enchantment_menu";
        _enchantmentButton.onClick.RemoveAllListeners();
        _enchantmentButton.onClick.AddListener(TogglePanel);

        RectTransform rect = _enchantmentButton.GetComponent<RectTransform>();
        rect.anchoredPosition += new Vector2(0, 74);
        UIBindingHelper.FindOptional(_enchantmentButton.transform, "Glow")?.gameObject.SetActive(false);
        _enchantmentButton.gameObject.SetActive(true);
        UIBindingHelper.GetRequired<Image>(_enchantmentButton.transform, "Image").sprite =
            ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_Enchantment_Icon");
        EnchantmentButtonHint.Attach(_enchantmentButton, GetEnchantmentShortcutLabel, IsVisible);
        ApplyEnchantmentGamepadShortcut();
    }

    private static void HandleInventoryGuiShow()
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        _enchantmentBackground?.SetActive(true);
        _enchantmentButton?.gameObject.SetActive(true);
        ApplyEnchantmentGamepadShortcut();
    }

    private static void ApplyEnchantmentGamepadShortcut()
    {
        if (_enchantmentButton == null || _gamepadShortcut == null)
        {
            return;
        }

        UIGamePad gamepad = _enchantmentButton.GetComponent<UIGamePad>();
        if (gamepad == null)
        {
            return;
        }

        // The global chord handler owns input. The cloned repair button must not also submit it.
        gamepad.m_zinputKey = string.Empty;
        gamepad.m_keyCode = KeyCode.None;
        if (gamepad.m_hint != null) gamepad.m_hint.SetActive(false);
        gamepad.m_hint = null;
        gamepad.enabled = false;
    }

    private static string ToZInputKey(GamepadShortcutButton button)
    {
        return button == GamepadShortcutButton.Disabled ? string.Empty : button.ToString();
    }

    private static string GetGamepadShortcutLabel()
    {
        string key = ToZInputKey(_gamepadShortcut.Value);
        string modifier = ToZInputKey(_gamepadShortcutModifier.Value);
        if (string.IsNullOrEmpty(key) || key == modifier) return string.Empty;
        string label = ResolveGamepadShortcutLabel(key);
        return string.IsNullOrEmpty(modifier) ? label : ResolveGamepadShortcutLabel(modifier) + " + " + label;
    }

    private static string ResolveGamepadShortcutLabel(string zinputKey)
    {
        try
        {
            string label = ZInput.instance.GetBoundKeyString(zinputKey, true);
            return string.IsNullOrWhiteSpace(label) ? zinputKey : label;
        }
        catch
        {
            return zinputKey;
        }
    }

    public static void PlayClick()
    {
        if (AUsrc != null && _click != null)
        {
            AUsrc.PlayOneShot(_click);
        }
    }

    private static AudioSource EnsureAudioSource(AudioMixerGroup sfxGroup)
    {
        if (AUsrc == null)
        {
            AUsrc = Chainloader.ManagerObject.GetComponent<AudioSource>();
            if (AUsrc == null)
            {
                AUsrc = Chainloader.ManagerObject.AddComponent<AudioSource>();
            }
        }

        AUsrc.reverbZoneMix = 0f;
        AUsrc.spatialBlend = 0f;
        AUsrc.bypassListenerEffects = true;
        AUsrc.bypassEffects = true;
        AUsrc.volume = 1f;
        AUsrc.outputAudioMixerGroup = sfxGroup;
        return AUsrc;
    }

    [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
    [ClientOnlyPatch]
    private static class AudioMan_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(AudioMan __instance)
        {
            AudioMixerGroup sfxGroup = __instance.m_masterMixer.FindMatchingGroups("SFX")[0];
            EnsureAudioSource(sfxGroup);

            foreach (GameObject asset in ValheimEnchantmentSystem._asset.LoadAllAssets<GameObject>())
            {
                foreach (AudioSource audioSource in asset.GetComponentsInChildren<AudioSource>(true))
                {
                    audioSource.outputAudioMixerGroup = sfxGroup;
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
    [ClientOnlyPatch]
    private static class InventoryGui_SetupDragItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_dragGo && __instance.m_dragItem != null)
            {
                _controller?.HandleDraggedItem(__instance.m_dragItem);
            }
        }
    }

}
