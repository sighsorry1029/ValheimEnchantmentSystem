namespace kg.ValheimEnchantmentSystem.UI;

internal sealed class InfoPanelController
{
    private readonly InfoPanelView _view;
    private readonly GameObject _elementPrefab;
    private readonly Action _playClick;
    private bool _suppressRender;
    private GameObject? _tooltipPrefab;
    private InfoPanelCategory _currentCategory = InfoPanelCategory.Reqs;

    public InfoPanelController(InfoPanelView view, GameObject elementPrefab, Action playClick)
    {
        _view = view;
        _elementPrefab = elementPrefab;
        _playClick = playClick;

        _view.Search.onValueChanged.RemoveAllListeners();
        _view.Search.onValueChanged.AddListener(_ => OnSearchChanged());

        BindCategoryButton(InfoPanelCategory.Reqs);
        BindCategoryButton(InfoPanelCategory.Stats);
        BindCategoryButton(InfoPanelCategory.Chances);
        ResetState();
    }

    public bool IsVisible => _view != null && _view.gameObject.activeSelf;

    public void ConfigureTooltipPrefab(GameObject? tooltipPrefab)
    {
        _tooltipPrefab = tooltipPrefab;
    }

    public void Show(InfoPanelCategory initialCategory = InfoPanelCategory.Reqs, string? initialSearchText = null)
    {
        ResetState(initialCategory, initialSearchText);
        _view.LocalizeRoot();
        _view.gameObject.SetActive(true);
        Render();
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    private void BindCategoryButton(InfoPanelCategory category)
    {
        Button button = _view.GetCategoryButton(category);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            _playClick();
            SelectCategory(category);
            Render();
        });
    }

    private void OnSearchChanged()
    {
        if (_suppressRender)
        {
            return;
        }

        Render();
    }

    private void ResetState(InfoPanelCategory initialCategory = InfoPanelCategory.Reqs, string? initialSearchText = null)
    {
        _view.ClearContent();
        SelectCategory(initialCategory);
        _suppressRender = true;
        _view.Search.text = initialSearchText ?? string.Empty;
        _suppressRender = false;
    }

    private void SelectCategory(InfoPanelCategory category)
    {
        _currentCategory = category;
        _view.SetCategorySelected(category);
    }

    private void Render()
    {
        _view.ClearContent();
        if (!Player.m_localPlayer)
        {
            return;
        }

        List<InfoPanelEntryModel> entries = InfoPanelQueryService.GetEntries(_currentCategory, _view.Search.text);
        foreach (InfoPanelEntryModel entry in entries)
        {
            InfoEntryView entryView = InfoEntryView.Attach(UnityEngine.Object.Instantiate(_elementPrefab, _view.Content));
            entryView.Apply(entry, _tooltipPrefab);
            entryView.BindToggle(() =>
            {
                _playClick();
                entryView.SetExpanded(!entryView.IsExpanded);
                _view.ForceCanvasLayout();
            });
        }

        _view.ForceCanvasLayout();
    }
}
