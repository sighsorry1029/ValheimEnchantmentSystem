using kg.ValheimEnchantmentSystem.Platform;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

internal static class ScrollStationBootstrap
{
    private const string StationPrefabName = "kg_EnchantmentScrollStation";
    private const string FeedbackDonorPrefabName = "piece_workbench";
    private static readonly PieceRegistrationService.CraftingPieceDefinition StationDefinition = new(
        StationPrefabName,
        "$kg_enchantment_scrollstation",
        "$kg_enchantment_scrollstation_description",
        "Hammer",
        "piece_workbench",
        new[]
        {
            new PieceRegistrationService.Requirement("SurtlingCore", 1, true),
            new PieceRegistrationService.Requirement("BoneFragments", 5, true),
            new PieceRegistrationService.Requirement("Flint", 10, true),
            new PieceRegistrationService.Requirement("Stone", 20, true),
        },
        new[]
        {
            new ContentFixupRegistry.SceneFixupDefinition(
                "WorkbenchFeedback",
                ContentFixupRegistry.SceneFixupKind.CraftingStationFeedbackFromDonor,
                FeedbackDonorPrefabName),
        },
        requirePlaceEffect: false);
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        PieceRegistrationService.RegisterCraftingPiece(StationDefinition);
    }
}
