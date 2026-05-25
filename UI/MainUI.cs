using BepInEx.Bootstrap;
using BepInEx.Configuration;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;
using UnityEngine.Audio;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload(VES_Autoload.Priority.Normal, "OnInit", typeof(SyncedData))]
public static class VES_UI
{
    private static AudioSource? AUsrc;
    private static AudioClip _oneSecondClip = null!;
    private static AudioClip _threeSecondClip = null!;
    private static AudioClip _sixSecondClip = null!;
    private static AudioClip _click = null!;
    private static AudioClip _successSound = null!;
    private static AudioClip _failSound = null!;
    private static GameObject _completionVfxPrefab = null!;
    private static Sprite _defaultQuestionMark = null!;
    private static MainEnchantmentView? _view;
    private static MainEnchantmentController? _controller;
    private static Button? _enchantmentButton;
    private static GameObject? _enchantmentBackground;
    private static GameObject? _enchantmentButtonGamepadHint;

    public static ConfigEntry<int> _enchantmentAnimationDuration = null!;
    private static ConfigEntry<GamepadShortcutButton> _gamepadShortcut = null!;

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

    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        _enchantmentAnimationDuration = ValheimEnchantmentSystem.ClientConfig(
            string.Empty,
            "EnchantmentAnimationDuration",
            1,
            new ConfigDescription("Duration of the enchantment animation in seconds. Values outside 1-5 are clamped at runtime."));
        _gamepadShortcut = ValheimEnchantmentSystem.ClientConfig(
            "UI",
            "GamepadShortcut",
            GamepadShortcutButton.JoyButtonX,
            new ConfigDescription("Controller shortcut assigned to the enchantment inventory button. Change this if it conflicts with repair; set to Disabled to remove the shortcut."));
        _gamepadShortcut.SettingChanged += (_, _) => ApplyEnchantmentGamepadShortcut();
        _oneSecondClip = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_1");
        _threeSecondClip = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_3");
        _sixSecondClip = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Main_6");
        _completionVfxPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_VFX1");
        _defaultQuestionMark = ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_EnchantmentQuestion");
        _click = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentClick");
        _successSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Success");
        _failSound = ValheimEnchantmentSystem._asset.LoadAsset<AudioClip>("kg_EnchantmentSound_Fail");

        GameObject uiRoot = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI"));
        uiRoot.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(uiRoot);

        _view = MainEnchantmentView.Attach(uiRoot);
        _controller = new MainEnchantmentController(
            _view,
            _defaultQuestionMark,
            _completionVfxPrefab,
            _oneSecondClip,
            _threeSecondClip,
            _sixSecondClip,
            _successSound,
            _failSound,
            () => AUsrc,
            () => Mathf.Clamp(_enchantmentAnimationDuration.Value, 1, 5),
            PlayClick,
            Info_UI.Show,
            Info_UI.IsVisible,
            () => true);
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
        _controller?.Update();
    }

    private static void Show()
    {
        _controller?.Show();
    }

    private static void Hide()
    {
        _controller?.Hide();
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
        _enchantmentButton.onClick.AddListener(() =>
        {
            if (IsVisible())
            {
                Hide();
            }
            else
            {
                Show();
            }

            PlayClick();
        });

        RectTransform rect = _enchantmentButton.GetComponent<RectTransform>();
        rect.anchoredPosition += new Vector2(0, 74);
        UIBindingHelper.FindOptional(_enchantmentButton.transform, "Glow")?.gameObject.SetActive(false);
        _enchantmentButton.gameObject.SetActive(true);
        UIBindingHelper.GetRequired<Image>(_enchantmentButton.transform, "Image").sprite =
            ValheimEnchantmentSystem._asset.LoadAsset<Sprite>("kg_Enchantment_Icon");
        ApplyEnchantmentGamepadShortcut();
    }

    private static void HandleInventoryGuiShow()
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        if (Localization.instance != null && _enchantmentButton != null)
        {
            _enchantmentButton.GetComponent<UITooltip>().m_text = "$enchantment_menu".Localize();
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

        if (_enchantmentButtonGamepadHint == null && gamepad.m_hint != null)
        {
            _enchantmentButtonGamepadHint = gamepad.m_hint;
        }

        string zinputKey = ToZInputKey(_gamepadShortcut.Value);
        gamepad.m_zinputKey = zinputKey;
        gamepad.m_keyCode = KeyCode.None;
        gamepad.m_hint = string.IsNullOrEmpty(zinputKey) ? null : _enchantmentButtonGamepadHint;
        UpdateGamepadHint(gamepad.m_hint, zinputKey);
    }

    private static string ToZInputKey(GamepadShortcutButton button)
    {
        return button == GamepadShortcutButton.Disabled ? string.Empty : button.ToString();
    }

    private static void UpdateGamepadHint(GameObject? hint, string zinputKey)
    {
        if (hint == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(zinputKey))
        {
            hint.SetActive(false);
            return;
        }

        string label = ResolveGamepadShortcutLabel(zinputKey);
        foreach (Text text in hint.GetComponentsInChildren<Text>(true))
        {
            text.text = label;
        }

        foreach (TMP_Text text in hint.GetComponentsInChildren<TMP_Text>(true))
        {
            text.text = label;
        }
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
