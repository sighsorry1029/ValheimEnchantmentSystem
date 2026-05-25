using ItemDataManager;
using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem.UI;

internal sealed class MainEnchantmentController
{
    private static readonly Color VfxDefaultBless = new(161f / 255f, 157f / 255f, 0f, 1f);
    private static readonly int Speed = Shader.PropertyToID("_Speed");
    private static readonly Color DefaultTrailColor = new(1f, 1f, 1f, 0.8f);

    private readonly MainEnchantmentView _view;
    private readonly Sprite _defaultQuestionMark;
    private readonly GameObject _completionVfxPrefab;
    private readonly AudioClip _oneSecondClip;
    private readonly AudioClip _threeSecondClip;
    private readonly AudioClip _sixSecondClip;
    private readonly AudioClip _successSound;
    private readonly AudioClip _failSound;
    private readonly Func<AudioSource?> _audioSourceProvider;
    private readonly Func<int> _animationDurationProvider;
    private readonly Action _playClick;
    private readonly Action<InfoPanelCategory, string?> _showInfo;
    private readonly Func<bool> _isInfoVisible;
    private readonly Func<bool> _showChanceHud;
    private readonly float _itemStartX;
    private readonly float _scrollStartX;
    private readonly float _startY;

    private bool _useBless;
    private ItemDrop.ItemData? _currentItem;
    private bool _enchantProcessing;
    private float _enchantTimer;
    private float _timerMax;
    private bool _shouldReselect;

    public MainEnchantmentController(
        MainEnchantmentView view,
        Sprite defaultQuestionMark,
        GameObject completionVfxPrefab,
        AudioClip oneSecondClip,
        AudioClip threeSecondClip,
        AudioClip sixSecondClip,
        AudioClip successSound,
        AudioClip failSound,
        Func<AudioSource?> audioSourceProvider,
        Func<int> animationDurationProvider,
        Action playClick,
        Action<InfoPanelCategory, string?> showInfo,
        Func<bool> isInfoVisible,
        Func<bool> showChanceHud)
    {
        _view = view;
        _defaultQuestionMark = defaultQuestionMark;
        _completionVfxPrefab = completionVfxPrefab;
        _oneSecondClip = oneSecondClip;
        _threeSecondClip = threeSecondClip;
        _sixSecondClip = sixSecondClip;
        _successSound = successSound;
        _failSound = failSound;
        _audioSourceProvider = audioSourceProvider;
        _animationDurationProvider = animationDurationProvider;
        _playClick = playClick;
        _showInfo = showInfo;
        _isInfoVisible = isInfoVisible;
        _showChanceHud = showChanceHud;
        _itemStartX = _view.ItemRect.anchoredPosition.x;
        _scrollStartX = _view.ScrollRect.anchoredPosition.x;
        _startY = _view.ScrollRect.anchoredPosition.y;

        _view.UseBlessButton.onClick.RemoveAllListeners();
        _view.UseBlessButton.onClick.AddListener(() =>
        {
            _playClick();
            ToggleBless();
        });

        _view.StartButton.onClick.RemoveAllListeners();
        _view.StartButton.onClick.AddListener(() =>
        {
            _playClick();
            HandleStartButton();
        });

        _view.InfoButton.onClick.RemoveAllListeners();
        _view.InfoButton.onClick.AddListener(() =>
        {
            _playClick();
            _showInfo(InfoPanelCategory.Reqs, ResolveInfoSearchText());
        });

        ResetState();
    }

    public bool IsVisible => _view != null && _view.gameObject.activeSelf;

    public void ConfigureTooltipPrefab(GameObject? tooltipPrefab)
    {
        _view.ItemTooltip.m_tooltipPrefab = tooltipPrefab;
        _view.ChanceTooltip.m_tooltipPrefab = tooltipPrefab;

        EnchantmentPreview preview = EnchantmentPreviewService.GetPreview(_currentItem, _useBless);
        UpdateItemTooltip(preview);
        UpdateChanceTooltip(preview);
    }

    public void Show()
    {
        ResetState();
        if (!InventoryGui.IsVisible())
        {
            InventoryGui.instance.Show(null);
        }

        if (Localization.instance != null)
        {
            _view.LocalizeStaticText();
        }

        _view.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _view.gameObject.SetActive(false);
        ResetState();
    }

    public void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (!Player.m_localPlayer)
        {
            Hide();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !_isInfoVisible())
        {
            ValheimEnchantmentSystem._thistype.DelayedInvoke(Hide, 1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !_enchantProcessing && !_shouldReselect && !_isInfoVisible())
        {
            _playClick();
            ToggleBless();
            return;
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) && !_isInfoVisible())
        {
            HandleStartButton();
            return;
        }

        if (!_enchantProcessing)
        {
            return;
        }

        if (_currentItem == null || !Player.m_localPlayer.m_inventory.ContainsItem(_currentItem))
        {
            Hide();
            return;
        }

        _enchantTimer -= Time.deltaTime;
        if (_enchantTimer <= 0f)
        {
            FinishEnchantment();
            return;
        }

        UpdateAnimation();
    }

    public void HandleDraggedItem(ItemDrop.ItemData item)
    {
        SelectItem(item);
    }

    private void HandleStartButton()
    {
        if (_currentItem == null || !Player.m_localPlayer || !Player.m_localPlayer.m_inventory.ContainsItem(_currentItem))
        {
            ResetState();
            return;
        }

        if (_shouldReselect)
        {
            ReselectCurrentItemPreservingBless();
            _playClick();
            return;
        }

        if (_enchantProcessing)
        {
            _enchantProcessing = false;
            _enchantTimer = 0f;
            ReselectCurrentItemPreservingBless();
            StopAudio();
            return;
        }

        EnchantmentPreview preview = EnchantmentPreviewService.GetPreview(_currentItem, _useBless);
        if (!preview.CanSelect)
        {
            ResetState();
            return;
        }

        ApplyPreview(preview);
        if (!preview.CanAttempt)
        {
            return;
        }

        _enchantProcessing = true;
        _timerMax = _animationDurationProvider();
        _enchantTimer = _timerMax;
        _view.StartText.text = "$enchantment_cancel".Localize();
        _view.ProgressRoot.gameObject.SetActive(true);
        _view.ProgressFill.fillAmount = 0f;
        _view.ProgressVfxRect.anchoredPosition = Vector2.zero;
        ParticleSystem.MainModule progressVfxMain = _view.ProgressVfx.main;
        progressVfxMain.startColor = _useBless ? VfxDefaultBless : Color.white;
        _view.ProgressAccent.color = _useBless ? Color.yellow : Color.white;
        _view.ItemVisual.color = Color.clear;
        _view.ScrollVisual.color = Color.clear;
        SetChanceHudVisible(false);
        _view.UseBlessRoot.gameObject.SetActive(false);
        _view.ItemText.text = string.Empty;
        _view.ScrollText.text = string.Empty;
        SetTrailSpeed(_view.ItemTrail, 1f);
        SetTrailSpeed(_view.ScrollTrail, 1f);
        PlayAnimationClip();
    }

    private void ToggleBless()
    {
        if (_currentItem == null)
        {
            return;
        }

        _useBless = !_useBless;
        EnchantmentPreview preview = EnchantmentPreviewService.GetPreview(_currentItem, _useBless);
        if (!preview.CanSelect)
        {
            ResetState();
            return;
        }

        ApplyPreview(preview);
    }

    private void SelectItem(ItemDrop.ItemData? item)
    {
        if (!IsVisible || _enchantProcessing || item == null)
        {
            return;
        }

        ResetState();
        InventoryGui.instance.SetupDragItem(null, null, 1);
        if (!Player.m_localPlayer.m_inventory.ContainsItem(item))
        {
            return;
        }

        EnchantmentPreview preview = EnchantmentPreviewService.GetPreview(item, false);
        if (!preview.CanSelect)
        {
            if (!string.IsNullOrWhiteSpace(preview.BlockedMessage))
            {
                _view.ItemText.text = preview.BlockedMessage;
                _view.ItemText.color = Color.red;
            }

            return;
        }

        _currentItem = item;
        _view.ItemRect.anchoredPosition = new Vector2(_itemStartX, _startY);
        _view.ItemRoot.gameObject.SetActive(true);
        _view.ItemRoot.localScale = Vector3.one;
        _view.ItemTrail.gameObject.SetActive(true);
        SetChanceHudVisible(true);
        _view.UseBlessRoot.gameObject.SetActive(true);
        _view.ScrollRect.anchoredPosition = new Vector2(_scrollStartX, _startY);
        _view.ScrollRoot.gameObject.SetActive(true);
        ApplyPreview(preview);
    }

    private void ApplyPreview(EnchantmentPreview? preview)
    {
        if (preview?.Item == null)
        {
            return;
        }

        _useBless = preview.UseBlessedScroll;
        _view.UseBlessIcon.gameObject.SetActive(_useBless);
        _view.ItemText.text = preview.ItemLabel;
        _view.ItemIcon.sprite = preview.Item.GetIcon();
        _view.ItemTrail.color = preview.TrailColor;
        if (preview.IsMaxedOut)
        {
            _view.ItemRect.anchoredPosition = new Vector2(0f, _startY);
            _view.ItemRoot.localScale = Vector3.one;
            _view.ChanceText.text = string.Empty;
            SetChanceHudVisible(false);
            _view.UseBlessRoot.gameObject.SetActive(false);
            _view.ScrollRoot.gameObject.SetActive(false);
            _view.StartRoot.gameObject.SetActive(true);
            _view.StartButton.interactable = false;
            _view.StartText.text = "$enchantment_maxedout_short".Localize();
            UpdateItemTooltip(preview);
            UpdateChanceTooltip(preview);
            return;
        }

        _view.ChanceText.text = BuildChanceDisplayText(preview);
        SetChanceHudVisible(true);
        _view.StartRoot.gameObject.SetActive(true);
        _view.StartButton.interactable = preview.CanAttempt;
        _view.StartText.text = "$enchantment_enchant".Localize();
        UpdateItemTooltip(preview);
        UpdateChanceTooltip(preview);

        if (preview.HasRequirementDefinition)
        {
            _view.ScrollText.text = preview.RequirementStatusText;
            _view.ScrollText.color = preview.HasRequiredItems ? Color.white : Color.red;
            _view.ScrollIcon.sprite = preview.RequirementIcon;
            _view.ScrollTrail.gameObject.SetActive(true);
            _view.ScrollTrail.color = _useBless ? new Color(1f, 1f, 0f, 0.8f) : DefaultTrailColor;
            return;
        }

        _view.ScrollText.text = preview.RequirementStatusText;
        _view.ScrollText.color = Color.red;
        _view.ScrollIcon.sprite = _defaultQuestionMark;
        _view.ScrollTrail.gameObject.SetActive(false);
        _view.ScrollTrail.color = DefaultTrailColor;
    }

    private void ResetState()
    {
        StopAudio();
        _currentItem = null;
        _enchantProcessing = false;
        _enchantTimer = 0f;
        _timerMax = 0f;
        _shouldReselect = false;
        _useBless = false;

        _view.ItemRect.anchoredPosition = Vector2.zero;
        _view.ItemRoot.gameObject.SetActive(true);
        _view.ItemRoot.localScale = new Vector3(1.4f, 1.4f, 1f);
        _view.ItemText.text = "$enchantment_selectanitem".Localize();
        _view.ItemText.color = Color.white;
        _view.ItemIcon.sprite = _defaultQuestionMark;
        _view.ItemVisual.color = Color.clear;
        _view.ItemTrail.gameObject.SetActive(false);
        _view.ItemTrail.color = DefaultTrailColor;
        _view.ItemTooltip.enabled = false;
        _view.ItemTooltip.m_text = string.Empty;
        _view.ItemTooltip.m_topic = string.Empty;
        _view.ChanceTooltip.enabled = false;
        _view.ChanceTooltip.m_text = string.Empty;
        _view.ChanceTooltip.m_topic = string.Empty;
        SetTrailSpeed(_view.ItemTrail, 0.5f);

        _view.ScrollRect.anchoredPosition = new Vector2(_scrollStartX, _startY);
        _view.ScrollRoot.gameObject.SetActive(false);
        _view.ScrollRoot.localScale = Vector3.one;
        _view.ScrollText.text = "$enchantment_noenchantitems".Localize();
        _view.ScrollText.color = Color.red;
        _view.ScrollIcon.sprite = _defaultQuestionMark;
        _view.ScrollVisual.color = Color.clear;
        _view.ScrollTrail.gameObject.SetActive(false);
        _view.ScrollTrail.color = DefaultTrailColor;
        SetTrailSpeed(_view.ScrollTrail, 0.5f);

        _view.UseBlessRoot.gameObject.SetActive(false);
        _view.UseBlessIcon.gameObject.SetActive(false);
        _view.StartRoot.gameObject.SetActive(false);
        _view.StartButton.interactable = false;
        _view.StartText.text = "$enchantment_enchant".Localize();

        _view.ProgressRoot.gameObject.SetActive(false);
        _view.ProgressVfxRect.anchoredPosition = Vector2.zero;
        _view.ProgressFill.fillAmount = 0f;
        _view.ProgressAccent.color = Color.clear;
        ParticleSystem.MainModule progressVfxMain = _view.ProgressVfx.main;
        progressVfxMain.startColor = Color.clear;

        SetChanceHudVisible(false);
        _view.ChanceText.text = string.Empty;
    }

    private string BuildChanceDisplayText(EnchantmentPreview preview)
    {
        return $"{preview.AttemptSummary.FinalChancePercent:0.00}%";
    }

    private void SetChanceHudVisible(bool visible)
    {
        _view.ChanceRoot.gameObject.SetActive(visible && _showChanceHud());
    }

    private void UpdateItemTooltip(EnchantmentPreview? preview)
    {
        if (_view.ItemTooltip == null)
        {
            return;
        }

        if (preview?.Item == null || string.IsNullOrWhiteSpace(preview.NextStatsTooltipText))
        {
            _view.ItemTooltip.enabled = false;
            _view.ItemTooltip.m_topic = string.Empty;
            _view.ItemTooltip.m_text = string.Empty;
            return;
        }

        _view.ItemTooltip.m_topic = preview.ItemDisplayName;
        _view.ItemTooltip.m_text = preview.NextStatsTooltipText;
        _view.ItemTooltip.enabled = true;
    }

    private void UpdateChanceTooltip(EnchantmentPreview? preview)
    {
        if (_view.ChanceTooltip == null)
        {
            return;
        }

        if (!_showChanceHud() || preview?.Item == null)
        {
            _view.ChanceTooltip.enabled = false;
            _view.ChanceTooltip.m_topic = string.Empty;
            _view.ChanceTooltip.m_text = string.Empty;
            return;
        }

        List<string> lines = new();
        if (!string.IsNullOrWhiteSpace(preview.AttemptSummary.ChanceBreakdownText))
        {
            lines.Add(preview.AttemptSummary.ChanceBreakdownText);
        }

        if (!string.IsNullOrWhiteSpace(preview.AttemptSummary.FailureBreakdownText))
        {
            lines.Add(preview.AttemptSummary.FailureBreakdownText);
        }

        if (lines.Count == 0)
        {
            _view.ChanceTooltip.enabled = false;
            _view.ChanceTooltip.m_topic = string.Empty;
            _view.ChanceTooltip.m_text = string.Empty;
            return;
        }

        _view.ChanceTooltip.m_topic = "$enchantment_chance".Localize();
        _view.ChanceTooltip.m_text = string.Join("\n", lines);
        _view.ChanceTooltip.enabled = true;
    }

    private void ReselectCurrentItemPreservingBless()
    {
        if (_currentItem == null)
        {
            ResetState();
            return;
        }

        bool oldUseBless = _useBless;
        SelectItem(_currentItem);
        if (_useBless != oldUseBless)
        {
            ToggleBless();
        }
    }

    private void UpdateAnimation()
    {
        float progress = 1f - (_enchantTimer / _timerMax);
        _view.ProgressFill.fillAmount = progress;
        _view.ProgressVfxRect.anchoredPosition = new Vector2(300f * progress, 0f);

        Color visualColor = _useBless ? new Color(1f, 1f, 0f, progress) : new Color(1f, 1f, 1f, progress);
        _view.ItemVisual.color = visualColor;
        _view.ScrollVisual.color = visualColor;

        _view.ItemRect.anchoredPosition = new Vector2(Mathf.Lerp(_itemStartX, 0f, progress), Mathf.Lerp(_startY, 0f, progress));
        _view.ScrollRect.anchoredPosition = new Vector2(Mathf.Lerp(_scrollStartX, 0f, progress), Mathf.Lerp(_startY, 0f, progress));
        _view.ItemRoot.localScale = new Vector3(1f + progress * 0.4f, 1f + progress * 0.4f, 1f);
        _view.ScrollRoot.localScale = new Vector3(1f + progress * 0.4f, 1f + progress * 0.4f, 1f);
    }

    private void FinishEnchantment()
    {
        _view.ItemRect.anchoredPosition = Vector2.zero;
        _view.ItemRoot.localScale = new Vector3(1.4f, 1.4f, 1f);
        _enchantProcessing = false;
        _enchantTimer = 0f;
        _view.ScrollRoot.gameObject.SetActive(false);
        _view.ProgressRoot.gameObject.SetActive(false);

        Enchantment_Core.Enchanted enchantment = _currentItem!.Data().GetOrCreate<Enchantment_Core.Enchanted>();
        EnchantmentResult result = enchantment.Enchant(Player.m_localPlayer, _useBless, SyncedData.BlessedScrollsPreventBreak.Value);
        EnchantmentSideEffects.ApplyAttemptResult(result);

        _view.ItemText.text = result.Message;
        _view.ItemText.color = result.Success ? Color.green : Color.red;
        _view.ItemVisual.color = result.Success ? Color.green : Color.red;
        _view.ItemTrail.color = SyncedData.GetColor(enchantment, out _, true).IncreaseColorLight().ToColorAlpha();
        GameObject uiFx = UnityEngine.Object.Instantiate(_completionVfxPrefab, _view.ItemRoot);
        ParticleSystem.MainModule uiFxMain = uiFx.GetComponent<ParticleSystem>().main;
        uiFxMain.startColor = result.Success ? Color.green : Color.red;
        PlayOneShot(result.Success ? _successSound : _failSound);
        SetTrailSpeed(_view.ItemTrail, 0.5f);
        SetTrailSpeed(_view.ScrollTrail, 0.5f);
        _shouldReselect = true;
        _view.StartRoot.gameObject.SetActive(true);
        _view.StartButton.interactable = true;
        _view.StartText.text = "$enchantment_ok".Localize();
    }

    private void PlayAnimationClip()
    {
        AudioSource? source = _audioSourceProvider();
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = _timerMax <= 1 ? _oneSecondClip : _timerMax <= 3 ? _threeSecondClip : _sixSecondClip;
        source.Play();
    }

    private void PlayOneShot(AudioClip clip)
    {
        AudioSource? source = _audioSourceProvider();
        if (source == null || clip == null)
        {
            return;
        }

        source.PlayOneShot(clip);
    }

    private void StopAudio()
    {
        _audioSourceProvider()?.Stop();
    }

    private static void SetTrailSpeed(Image trailImage, float speed)
    {
        if (trailImage?.material == null)
        {
            return;
        }

        trailImage.material.SetFloat(Speed, speed);
    }

    private string? ResolveInfoSearchText()
    {
        return _currentItem?.m_shared?.m_name?.Localize();
    }
}
