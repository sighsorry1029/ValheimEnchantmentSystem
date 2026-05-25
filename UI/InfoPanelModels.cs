namespace kg.ValheimEnchantmentSystem.UI;

internal enum InfoPanelCategory
{
    Reqs,
    Stats,
    Chances
}

internal sealed class InfoPanelIconModel
{
    public Sprite Icon = null!;
    public string TooltipTopic = string.Empty;
    public string TooltipText = string.Empty;
    public bool Highlight;
}

internal sealed class InfoPanelEntryModel
{
    public readonly List<InfoPanelIconModel> Icons = new();
    public string BodyText = string.Empty;
    public string? AdditionalText;
    public bool StartExpanded;
}
