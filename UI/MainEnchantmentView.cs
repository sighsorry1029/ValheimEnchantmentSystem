namespace kg.ValheimEnchantmentSystem.UI;

internal sealed class MainEnchantmentView : MonoBehaviour
{
    public Text HeaderText = null!;
    public Transform ItemRoot = null!;
    public RectTransform ItemRect = null!;
    public Text ItemText = null!;
    public Image ItemIcon = null!;
    public Image ItemVisual = null!;
    public Image ItemTrail = null!;
    public UITooltip ItemTooltip = null!;
    public Transform ScrollRoot = null!;
    public RectTransform ScrollRect = null!;
    public Text ScrollText = null!;
    public Image ScrollIcon = null!;
    public Image ScrollVisual = null!;
    public Image ScrollTrail = null!;
    public Transform UseBlessRoot = null!;
    public Text UseBlessText = null!;
    public Button UseBlessButton = null!;
    public Image UseBlessIcon = null!;
    public Transform StartRoot = null!;
    public Button StartButton = null!;
    public Text StartText = null!;
    public Transform ProgressRoot = null!;
    public RectTransform ProgressVfxRect = null!;
    public ParticleSystem ProgressVfx = null!;
    public Image ProgressFill = null!;
    public Image ProgressAccent = null!;
    public Transform ChanceRoot = null!;
    public Text ChanceText = null!;
    public UITooltip ChanceTooltip = null!;
    public Button InfoButton = null!;

    public static MainEnchantmentView Attach(GameObject root)
    {
        MainEnchantmentView view = UIBindingHelper.GetOrAddComponent<MainEnchantmentView>(root);
        view.Bind();
        return view;
    }

    public void LocalizeStaticText()
    {
        HeaderText.text = "$enchantment_header".Localize();
        UseBlessText.text = "$enchantment_usebless".Localize();
    }

    private void Bind()
    {
        Transform root = transform;
        HeaderText = UIBindingHelper.GetRequired<Text>(root, "Canvas/Header/Text");
        ItemRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/Item");
        ItemRect = ItemRoot.GetComponent<RectTransform>();
        ItemText = UIBindingHelper.GetRequired<Text>(ItemRoot, "Text");
        ItemIcon = UIBindingHelper.GetRequired<Image>(ItemRoot, "Icon");
        ItemVisual = UIBindingHelper.GetRequired<Image>(ItemRoot, "Visual");
        ItemTrail = UIBindingHelper.GetRequired<Image>(ItemRoot, "Trail");
        ItemTooltip = UIBindingHelper.GetOrAddComponent<UITooltip>(ItemRoot.gameObject);
        ItemTooltip.enabled = false;

        ScrollRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/Scroll");
        ScrollRect = ScrollRoot.GetComponent<RectTransform>();
        ScrollText = UIBindingHelper.GetRequired<Text>(ScrollRoot, "Text");
        ScrollIcon = UIBindingHelper.GetRequired<Image>(ScrollRoot, "Icon");
        ScrollVisual = UIBindingHelper.GetRequired<Image>(ScrollRoot, "Visual");
        ScrollTrail = UIBindingHelper.GetRequired<Image>(ScrollRoot, "Trail");

        UseBlessRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/UseBless");
        UseBlessText = UIBindingHelper.GetRequired<Text>(UseBlessRoot, "Text");
        UseBlessButton = UseBlessRoot.GetComponent<Button>();
        UseBlessIcon = UIBindingHelper.GetRequired<Image>(UseBlessRoot, "Icon");
        UseBlessText.color = Color.yellow;

        StartRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/Start");
        StartButton = StartRoot.GetComponent<Button>();
        StartText = UIBindingHelper.GetRequired<Text>(StartRoot, "Text");

        ProgressRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/Progress");
        ProgressVfxRect = UIBindingHelper.FindRequired(ProgressRoot, "VFX").GetComponent<RectTransform>();
        ProgressVfx = ProgressVfxRect.GetComponent<ParticleSystem>();
        ProgressFill = UIBindingHelper.GetRequired<Image>(ProgressRoot, "Fill");
        ProgressAccent = ProgressFill.transform.GetChild(0).GetComponent<Image>();

        ChanceRoot = UIBindingHelper.FindRequired(root, "Canvas/Background/Chance");
        ChanceText = UIBindingHelper.GetRequired<Text>(ChanceRoot, "Text");
        ChanceText.supportRichText = true;
        Image chanceHoverTarget = UIBindingHelper.GetOrAddComponent<Image>(ChanceRoot.gameObject);
        chanceHoverTarget.color = Color.clear;
        chanceHoverTarget.raycastTarget = true;
        ChanceTooltip = UIBindingHelper.GetOrAddComponent<UITooltip>(ChanceRoot.gameObject);
        ChanceTooltip.enabled = false;
        InfoButton = UIBindingHelper.FindRequired(root, "Canvas/Background/Info").GetComponent<Button>();
    }
}
