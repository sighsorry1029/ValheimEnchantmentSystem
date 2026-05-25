namespace kg.ValheimEnchantmentSystem.UI;

internal sealed class InfoEntryView : MonoBehaviour
{
    private Transform _itemsRoot = null!;
    private GameObject _iconTemplate = null!;
    private Text _additionalLabel = null!;
    private Text _infoText = null!;
    private Button _toggleButton = null!;
    private GameObject _openIcon = null!;
    private GameObject _closeIcon = null!;

    public bool IsExpanded { get; private set; }

    public static InfoEntryView Attach(GameObject root)
    {
        InfoEntryView view = UIBindingHelper.GetOrAddComponent<InfoEntryView>(root);
        view.Bind();
        return view;
    }

    public void Apply(InfoPanelEntryModel model, GameObject? tooltipPrefab)
    {
        ClearIcons();
        _infoText.text = model.BodyText;
        bool hasAdditionalText = !string.IsNullOrWhiteSpace(model.AdditionalText);
        _additionalLabel.gameObject.SetActive(hasAdditionalText);
        if (hasAdditionalText)
        {
            _additionalLabel.text = model.AdditionalText;
        }

        _itemsRoot.gameObject.SetActive(model.Icons.Count > 0);
        foreach (InfoPanelIconModel iconModel in model.Icons)
        {
            GameObject icon = UnityEngine.Object.Instantiate(_iconTemplate, _itemsRoot);
            icon.SetActive(true);
            UIBindingHelper.GetRequired<Image>(icon.transform, "Icon").sprite = iconModel.Icon;
            UIBindingHelper.GetRequired<Image>(icon.transform, "border").color = iconModel.Highlight ? Color.green : Color.white;

            if (icon.GetComponent<UITooltip>() is { } tooltip)
            {
                tooltip.m_topic = iconModel.TooltipTopic;
                tooltip.m_text = iconModel.TooltipText;
                if (tooltipPrefab != null)
                {
                    tooltip.m_tooltipPrefab = tooltipPrefab;
                }
            }
        }

        SetExpanded(model.StartExpanded);
    }

    public void BindToggle(Action onToggle)
    {
        _toggleButton.onClick.RemoveAllListeners();
        _toggleButton.onClick.AddListener(() => onToggle());
    }

    public void SetExpanded(bool expanded)
    {
        IsExpanded = expanded;
        _infoText.gameObject.SetActive(expanded);
        _openIcon.SetActive(!expanded);
        _closeIcon.SetActive(expanded);
    }

    private void Bind()
    {
        Transform root = transform;
        _itemsRoot = UIBindingHelper.FindRequired(root, "Items");
        _iconTemplate = UIBindingHelper.FindRequired(_itemsRoot, "Icon").gameObject;
        _iconTemplate.SetActive(false);
        _additionalLabel = UIBindingHelper.GetRequired<Text>(root, "ANY");
        _infoText = UIBindingHelper.GetRequired<Text>(root, "Info");
        _toggleButton = UIBindingHelper.FindRequired(root, "Open").GetComponent<Button>();
        _openIcon = UIBindingHelper.FindRequired(root, "Open/open").gameObject;
        _closeIcon = UIBindingHelper.FindRequired(root, "Open/close").gameObject;
    }

    private void ClearIcons()
    {
        for (int i = _itemsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _itemsRoot.GetChild(i);
            if (child.gameObject == _iconTemplate)
            {
                continue;
            }

            UnityEngine.Object.Destroy(child.gameObject);
        }
    }
}
