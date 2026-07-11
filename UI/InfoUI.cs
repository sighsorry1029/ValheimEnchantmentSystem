using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

public static class Info_UI
{
    private static InfoPanelView? _view;
    private static InfoPanelController? _controller;

    public static bool IsVisible() => _controller?.IsVisible ?? false;

    internal static void Initialize()
    {
        if (ValheimEnchantmentSystem.NoGraphics)
        {
            return;
        }

        GameObject uiRoot = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Info"));
        GameObject elementPrefab = ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Info_Element");
        uiRoot.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(uiRoot);

        _view = InfoPanelView.Attach(uiRoot);
        _controller = new InfoPanelController(_view, elementPrefab, VES_UI.PlayClick);
        OverlayUiHost.Register(new OverlayUiHost.PanelRegistration(
            "InfoPanel",
            IsVisible,
            configureTooltipPrefab: tooltipPrefab => _controller?.ConfigureTooltipPrefab(tooltipPrefab)));
    }

    internal static void Show(InfoPanelCategory category = InfoPanelCategory.Reqs, string? searchText = null)
    {
        _controller?.Show(category, searchText);
    }

    public static void Update()
    {
        _controller?.Update();
    }

}
