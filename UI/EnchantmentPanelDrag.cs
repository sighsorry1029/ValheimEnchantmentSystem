using UnityEngine.EventSystems;

namespace kg.ValheimEnchantmentSystem.UI;

// Only the title handles pointer gestures; the existing buttons and item drag path are untouched.
internal sealed class EnchantmentPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform _canvasRect = null!;
    private RectTransform _header = null!;
    private RectTransform _headerBounds = null!;
    private RectTransform _background = null!;
    private ConfigEntry<float> _offsetX = null!;
    private ConfigEntry<float> _offsetY = null!;
    private Func<bool> _blocksInput = null!;
    private readonly Vector3[] _corners = new Vector3[4];
    private Vector2 _headerDefault;
    private Vector2 _backgroundDefault;
    private Vector2 _requestedOffset;
    private Vector2 _appliedOffset;
    private Vector2 _previousPointer;
    private Vector2 _dragCanvasSize;
    private Vector3 _dragCanvasScale;
    private int _pointerId;
    private bool _initialized;
    private bool _dragging;
    private bool _saving;
    private volatile bool _configDirty = true;

    public static void Attach(Transform root, ConfigEntry<float> offsetX,
        ConfigEntry<float> offsetY, Func<bool> blocksInput)
    {
        RectTransform header = UIBindingHelper.GetRequired<RectTransform>(root, "Canvas/Header");
        EnchantmentPanelDrag drag = UIBindingHelper.GetOrAddComponent<EnchantmentPanelDrag>(header.gameObject);
        drag._canvasRect = UIBindingHelper.GetRequired<RectTransform>(root, "Canvas");
        drag._header = header;
        drag._headerBounds = UIBindingHelper.GetRequired<RectTransform>(root, "Canvas/Header/Background");
        drag._background = UIBindingHelper.GetRequired<RectTransform>(root, "Canvas/Background");
        drag._headerDefault = header.anchoredPosition;
        drag._backgroundDefault = drag._background.anchoredPosition;
        drag._offsetX = offsetX;
        drag._offsetY = offsetY;
        drag._blocksInput = blocksInput;

        Graphic hitTarget = drag._headerBounds.GetComponent<Graphic>() ??
                            throw new InvalidOperationException("Enchantment panel header background has no Graphic component.");
        hitTarget.raycastTarget = true;

        offsetX.SettingChanged += drag.OnSettingChanged;
        offsetY.SettingChanged += drag.OnSettingChanged;
        drag._initialized = true;
    }

    private void OnSettingChanged(object sender, EventArgs args)
    {
        // Config reloads may arrive outside a pointer event. Apply Unity changes on the next frame.
        if (!_saving) _configDirty = true;
    }

    private void OnEnable()
    {
        _configDirty = true;
    }

    private void LateUpdate()
    {
        if (!_initialized) return;

        if (_configDirty) ReadConfiguration();
        if (_dragging && (_blocksInput() || !Input.GetMouseButton(0) || HasCanvasChanged()))
        {
            FinishDrag();
        }

        // Re-evaluate against the live canvas so changing resolution or UI scale cannot strand the title.
        // Keep the requested config offset: a temporary smaller viewport must not overwrite the user's layout.
        ApplyPosition();
    }

    private void ReadConfiguration()
    {
        _configDirty = false;
        _dragging = false;
        _requestedOffset = new Vector2(PanelPositionMath.SanitizeOffset(_offsetX.Value),
            PanelPositionMath.SanitizeOffset(_offsetY.Value));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_initialized || eventData.button != PointerEventData.InputButton.Left ||
            _blocksInput() || (InventoryGui.instance && InventoryGui.instance.m_dragGo)) return;

        if (_configDirty) ReadConfiguration();
        Canvas.ForceUpdateCanvases();
        ApplyPosition();
        if (!TryGetPointer(eventData, out _previousPointer)) return;

        _pointerId = eventData.pointerId;
        _dragCanvasSize = _canvasRect.rect.size;
        _dragCanvasScale = _canvasRect.lossyScale;
        _dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || eventData.pointerId != _pointerId) return;
        if (_configDirty)
        {
            ReadConfiguration();
            ApplyPosition();
            return;
        }
        if (_blocksInput() || HasCanvasChanged())
        {
            FinishDrag();
            return;
        }
        if (!TryGetPointer(eventData, out Vector2 pointer)) return;

        _requestedOffset = _appliedOffset + pointer - _previousPointer;
        _previousPointer = pointer;
        ApplyPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == _pointerId && eventData.button == PointerEventData.InputButton.Left)
            FinishDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_initialized || eventData.button != PointerEventData.InputButton.Right || _blocksInput()) return;

        _dragging = false;
        _configDirty = false;
        _requestedOffset = Vector2.zero;
        ApplyPosition();
        SavePosition(Vector2.zero);
    }

    private bool TryGetPointer(PointerEventData eventData, out Vector2 pointer)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, eventData.position, eventData.pressEventCamera, out pointer);
    }

    private bool HasCanvasChanged() =>
        _dragCanvasSize != _canvasRect.rect.size || _dragCanvasScale != _canvasRect.lossyScale;

    private void ApplyPosition()
    {
        Rect viewport = _canvasRect.rect;
        if (viewport.width <= 0f || viewport.height <= 0f) return;

        Rect header = GetDefaultBounds(_headerBounds);
        Rect background = GetDefaultBounds(_background);
        Vector2 offset = new(
            PanelPositionMath.ClampOffset(_requestedOffset.x, Math.Min(header.xMin, background.xMin),
                Math.Max(header.xMax, background.xMax), header.xMin, header.xMax, viewport.xMin, viewport.xMax),
            PanelPositionMath.ClampOffset(_requestedOffset.y, Math.Min(header.yMin, background.yMin),
                Math.Max(header.yMax, background.yMax), header.yMin, header.yMax, viewport.yMin, viewport.yMax));

        // Preserve the prefab hierarchy and child-local animation coordinates, moving both siblings together.
        _header.anchoredPosition = _headerDefault + offset;
        _background.anchoredPosition = _backgroundDefault + offset;
        _appliedOffset = offset;
    }

    private Rect GetDefaultBounds(RectTransform rect)
    {
        rect.GetWorldCorners(_corners);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in _corners)
        {
            Vector2 point = (Vector2)_canvasRect.InverseTransformPoint(corner) - _appliedOffset;
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void FinishDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        if (_configDirty) return; // An explicit config edit wins over an interrupted drag.

        _requestedOffset = _appliedOffset;
        SavePosition(_appliedOffset);
    }

    private void SavePosition(Vector2 offset)
    {
        if (_offsetX.Value.Equals(offset.x) && _offsetY.Value.Equals(offset.y)) return;

        ConfigFile config = _offsetX.ConfigFile;
        bool saveOnSet = config.SaveOnConfigSet;
        _saving = true;
        try
        {
            // Commit X and Y together, with a single disk write rather than one write per axis/frame.
            config.SaveOnConfigSet = false;
            _offsetX.Value = offset.x;
            _offsetY.Value = offset.y;
            config.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[kg.ValheimEnchantmentSystem] Could not save enchantment panel position: {ex.Message}");
        }
        finally
        {
            config.SaveOnConfigSet = saveOnSet;
            _saving = false;
        }
    }

    private void OnDisable()
    {
        if (_initialized) FinishDrag();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (_initialized && !hasFocus) FinishDrag();
    }

    private void OnDestroy()
    {
        if (!_initialized) return;
        _offsetX.SettingChanged -= OnSettingChanged;
        _offsetY.SettingChanged -= OnSettingChanged;
    }
}
