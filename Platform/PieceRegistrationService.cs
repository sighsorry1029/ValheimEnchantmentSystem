using PieceManager;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class PieceRegistrationService
{
    internal sealed class RegisteredPiece
    {
        private readonly BuildPiece _buildPiece;

        public GameObject Prefab => _buildPiece.Prefab;
        public string PrefabName => Prefab.name;
        internal string CraftingStationName { get; }
        internal bool RequirePlaceEffect { get; }

        public RegisteredPiece(BuildPiece buildPiece, string craftingStationName, bool requirePlaceEffect)
        {
            _buildPiece = buildPiece;
            CraftingStationName = craftingStationName;
            RequirePlaceEffect = requirePlaceEffect;
        }

        public T? TryGetComponent<T>() where T : Component
        {
            return Prefab.GetComponent<T>();
        }

        public T? GetComponent<T>() where T : Component => TryGetComponent<T>();
    }

    internal readonly struct CraftingPieceDefinition
    {
        public readonly string PrefabName;
        public readonly string NameKey;
        public readonly string DescriptionKey;
        public readonly string ToolName;
        public readonly string CraftingStationName;
        public readonly Requirement[] Requirements;
        public readonly ContentFixupRegistry.SceneFixupDefinition[] SceneFixups;
        public readonly bool RequirePlaceEffect;

        public CraftingPieceDefinition(
            string prefabName,
            string nameKey,
            string descriptionKey,
            string toolName,
            string craftingStationName,
            Requirement[] requirements,
            ContentFixupRegistry.SceneFixupDefinition[]? sceneFixups = null,
            bool requirePlaceEffect = true)
        {
            PrefabName = prefabName;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            ToolName = toolName;
            CraftingStationName = craftingStationName;
            Requirements = requirements ?? Array.Empty<Requirement>();
            SceneFixups = sceneFixups ?? Array.Empty<ContentFixupRegistry.SceneFixupDefinition>();
            RequirePlaceEffect = requirePlaceEffect;
        }
    }

    internal readonly struct Requirement
    {
        public readonly string ItemName;
        public readonly int Amount;
        public readonly bool Recover;

        public Requirement(string itemName, int amount, bool recover)
        {
            ItemName = itemName;
            Amount = amount;
            Recover = recover;
        }
    }

    public static RegisteredPiece RegisterCraftingPiece(CraftingPieceDefinition definition)
    {
        BuildPiece buildPiece = new(ValheimEnchantmentSystem._asset, definition.PrefabName);
        Piece piece = buildPiece.Prefab.GetComponent<Piece>();
        piece.m_name = definition.NameKey;
        piece.m_description = definition.DescriptionKey;
        buildPiece.Category.Set(BuildPieceCategory.Crafting);
        buildPiece.Tool.Add(definition.ToolName);
        if (string.Equals(definition.CraftingStationName, "piece_workbench", StringComparison.OrdinalIgnoreCase))
        {
            buildPiece.Crafting.Set(CraftingTable.Workbench);
        }
        else
        {
            buildPiece.Crafting.Set(definition.CraftingStationName);
        }

        foreach (Requirement requirement in definition.Requirements)
        {
            buildPiece.RequiredItems.Add(requirement.ItemName, requirement.Amount, requirement.Recover);
        }

        RegisteredPiece registeredPiece = new(buildPiece, definition.CraftingStationName, definition.RequirePlaceEffect);
        ContentValidationService.RegisterCraftingPiece(registeredPiece);
        foreach (ContentFixupRegistry.SceneFixupDefinition sceneFixup in definition.SceneFixups)
        {
            ContentFixupRegistry.RegisterSceneFixup(registeredPiece, sceneFixup);
        }

        return registeredPiece;
    }
}
