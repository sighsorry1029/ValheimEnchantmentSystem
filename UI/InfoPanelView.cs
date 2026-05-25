namespace kg.ValheimEnchantmentSystem.UI;

internal sealed class InfoPanelView : MonoBehaviour
{
    public InputField Search = null!;
    public Transform Content = null!;

    private readonly Dictionary<InfoPanelCategory, GameObject> _categoryRoots = new();
    private readonly Dictionary<InfoPanelCategory, Text> _categoryLabels = new();

    public static InfoPanelView Attach(GameObject root)
    {
        InfoPanelView view = UIBindingHelper.GetOrAddComponent<InfoPanelView>(root);
        view.Bind();
        return view;
    }

    public Button GetCategoryButton(InfoPanelCategory category) => _categoryRoots[category].GetComponent<Button>();

    public void SetCategorySelected(InfoPanelCategory category)
    {
        foreach (KeyValuePair<InfoPanelCategory, Text> entry in _categoryLabels)
        {
            entry.Value.color = entry.Key == category ? Color.yellow : Color.white;
        }
    }

    public void ClearContent()
    {
        foreach (Transform child in Content)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    public void ForceCanvasLayout()
    {
        List<ContentSizeFitter> allFitters = gameObject.GetComponentsInChildren<ContentSizeFitter>(true).ToList();
        Canvas.ForceUpdateCanvases();
        allFitters.ForEach(fitter => fitter.enabled = false);
        allFitters.ForEach(fitter => fitter.enabled = true);
    }

    public void LocalizeRoot()
    {
        if (Localization.instance != null)
        {
            Localization.instance.Localize(transform);
        }
    }

    private void Bind()
    {
        Transform root = transform;
        Search = UIBindingHelper.GetRequired<InputField>(root, "Canvas/Background/Search");
        Content = UIBindingHelper.FindRequired(root, "Canvas/Background/Scroll View/Viewport/Content");

        BindCategory(root, InfoPanelCategory.Reqs, "Canvas/Background/Categories/Reqs");
        BindCategory(root, InfoPanelCategory.Stats, "Canvas/Background/Categories/Stats");
        BindCategory(root, InfoPanelCategory.Chances, "Canvas/Background/Categories/Chances");
    }

    private void BindCategory(Transform root, InfoPanelCategory category, string path)
    {
        Transform categoryRoot = UIBindingHelper.FindRequired(root, path);
        _categoryRoots[category] = categoryRoot.gameObject;
        _categoryLabels[category] = UIBindingHelper.GetRequired<Text>(categoryRoot, "Text");
    }
}
