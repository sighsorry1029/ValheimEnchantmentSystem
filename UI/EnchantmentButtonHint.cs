using TMPro;
using UnityEngine.EventSystems;

namespace kg.ValheimEnchantmentSystem.UI;

// Unlike UITooltip, this hint stays beside the button instead of following the mouse.
internal sealed class EnchantmentButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float ButtonGap = 12f;
    private const float ScreenMargin = 8f;
    private const float MaximumWidth = 420f;
    private Button _button = null!;
    private RectTransform _buttonRect = null!;
    private RectTransform _canvasRect = null!;
    private TMP_Text _label = null!;
    private GameObject? _gamepadFocusObject;
    private Func<string> _shortcutLabel = null!;
    private Func<bool> _isPanelVisible = null!;
    private bool _hovered;

    public static void Attach(Button button, Func<string> shortcutLabel, Func<bool> isPanelVisible)
    {
        Canvas canvas = button.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        EnchantmentButtonHint hint = UIBindingHelper.GetOrAddComponent<EnchantmentButtonHint>(button.gameObject);
        hint._button = button;
        hint._buttonRect = button.GetComponent<RectTransform>();
        hint._canvasRect = canvas.GetComponent<RectTransform>();
        hint._shortcutLabel = shortcutLabel;
        hint._isPanelVisible = isPanelVisible;

        UITooltip tooltip = button.GetComponent<UITooltip>();
        TMP_Text? style = null;
        if (tooltip != null)
        {
            hint._gamepadFocusObject = tooltip.m_gamepadFocusObject;
            if (tooltip.m_tooltipPrefab != null)
            {
                TMP_Text[] texts = tooltip.m_tooltipPrefab.GetComponentsInChildren<TMP_Text>(true);
                style = texts.FirstOrDefault(text => text.name == "Text") ?? texts.FirstOrDefault();
            }

            // Only this cloned button loses its vanilla tooltip; repair and item tooltips are untouched.
            tooltip.enabled = false;
        }

        if (hint._label == null) hint.CreateLabel(style);
    }

    private void CreateLabel(TMP_Text? style)
    {
        GameObject root = new("EnchantmentButtonHint", typeof(RectTransform));
        root.SetActive(false);
        root.transform.SetParent(_canvasRect, false);
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        _label = root.AddComponent<TextMeshProUGUI>();
        if (style != null)
        {
            _label.font = style.font;
            _label.fontSharedMaterial = style.fontSharedMaterial;
            _label.fontSize = style.fontSize;
            _label.fontStyle = style.fontStyle;
            _label.color = style.color;
        }
        else
        {
            _label.font = TMP_Settings.defaultFontAsset;
            _label.fontSize = 20f;
            _label.color = Color.white;
        }

        _label.raycastTarget = false;
        _label.richText = true;
        _label.enableAutoSizing = false;
        _label.textWrappingMode = TextWrappingModes.Normal;
        _label.overflowMode = TextOverflowModes.Truncate;
        _label.alignment = TextAlignmentOptions.Right;
        _label.margin = Vector4.zero;
        _label.rectTransform.anchorMin = Vector2.zero;
        _label.rectTransform.anchorMax = Vector2.zero;
        _label.rectTransform.pivot = new Vector2(1f, 0.5f);
    }

    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void LateUpdate()
    {
        if (_label == null) return;

        bool gamepadActive = ZInput.IsGamepadActive() && !ZInput.IsMouseActive();
        bool focused = gamepadActive && ((_gamepadFocusObject != null && _gamepadFocusObject.activeInHierarchy) ||
            (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _button.gameObject));
        bool show = _button.IsInteractable() && (gamepadActive ? focused : _hovered);
        if (!show)
        {
            _label.gameObject.SetActive(false);
            return;
        }

        string text = "$enchantment_menu".Localize();
        string shortcut = _shortcutLabel();
        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            string key = _isPanelVisible() ? "$enchantment_shortcut_close" : "$enchantment_shortcut_open";
            string highlightedShortcut = "<color=#FFA500>" + shortcut + "</color>";
            text += "\n<size=80%><color=#CFCFCF>" + key.Localize(highlightedShortcut) + "</color></size>";
        }

        if (_label.text != text) _label.text = text;
        PositionLabel();
        if (!_label.gameObject.activeSelf)
        {
            _label.transform.SetAsLastSibling();
            _label.gameObject.SetActive(true);
        }
    }

    private void PositionLabel()
    {
        Rect viewport = _canvasRect.rect;
        Vector2 buttonLeft = _canvasRect.InverseTransformPoint(
            _buttonRect.TransformPoint(new Vector3(_buttonRect.rect.xMin, _buttonRect.rect.center.y, 0f)));
        float right = Mathf.Min(buttonLeft.x - ButtonGap, viewport.xMax - ScreenMargin);
        float width = Mathf.Min(MaximumWidth, Mathf.Max(1f, right - viewport.xMin - ScreenMargin));
        Vector2 preferred = _label.GetPreferredValues(_label.text, width, Mathf.Infinity);
        float height = Mathf.Min(preferred.y, Mathf.Max(1f, viewport.height - ScreenMargin * 2f));
        float centerY = Mathf.Clamp(buttonLeft.y, viewport.yMin + ScreenMargin + height * 0.5f,
            viewport.yMax - ScreenMargin - height * 0.5f);
        _label.rectTransform.sizeDelta = new Vector2(width, height);
        _label.rectTransform.anchoredPosition = new Vector2(right - viewport.xMin, centerY - viewport.yMin);
    }

    private void OnDisable()
    {
        _hovered = false;
        if (_label != null) _label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_label != null) UnityEngine.Object.Destroy(_label.gameObject);
    }
}
