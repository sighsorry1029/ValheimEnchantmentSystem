using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class ContentFixupRegistry
{
    internal enum SceneFixupKind
    {
        CraftingStationFeedbackFromDonor,
    }

    internal readonly struct SceneFixupDefinition
    {
        public readonly string FixupName;
        public readonly SceneFixupKind Kind;
        public readonly string DonorPrefabName;

        public SceneFixupDefinition(string fixupName, SceneFixupKind kind, string donorPrefabName)
        {
            FixupName = fixupName;
            Kind = kind;
            DonorPrefabName = donorPrefabName;
        }
    }

    private sealed class RegisteredFixup
    {
        public readonly string OwnerName;
        public readonly GameObject Prefab;
        public readonly SceneFixupDefinition Definition;

        public RegisteredFixup(string ownerName, GameObject prefab, SceneFixupDefinition definition)
        {
            OwnerName = ownerName;
            Prefab = prefab;
            Definition = definition;
        }
    }

    private static readonly List<RegisteredFixup> SceneFixups = new();
    private static readonly HashSet<string> FixupKeys = new(StringComparer.Ordinal);

    public static void RegisterSceneFixup(PieceRegistrationService.RegisteredPiece piece, SceneFixupDefinition definition)
    {
        if (piece == null)
        {
            return;
        }

        RegisterSceneFixup(piece.PrefabName, piece.Prefab, definition);
    }

    private static void RegisterSceneFixup(string ownerName, GameObject prefab, SceneFixupDefinition definition)
    {
        if (prefab == null || string.IsNullOrWhiteSpace(ownerName) || string.IsNullOrWhiteSpace(definition.FixupName))
        {
            return;
        }

        string key = $"{ownerName}:{definition.FixupName}";
        if (!FixupKeys.Add(key))
        {
            return;
        }

        SceneFixups.Add(new RegisteredFixup(ownerName, prefab, definition));
    }

    internal static void ApplySceneFixups(ZNetScene? scene)
    {
        if (scene == null)
        {
            return;
        }

        foreach (RegisteredFixup fixup in SceneFixups)
        {
            try
            {
                ApplySceneFixup(scene, fixup);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[kg.ValheimEnchantmentSystem] Content fixup '{fixup.Definition.FixupName}' failed for '{fixup.OwnerName}': {ex.Message}");
            }
        }

        ContentValidationService.ValidateRegisteredContent(scene);
    }

    private static void ApplySceneFixup(ZNetScene scene, RegisteredFixup fixup)
    {
        switch (fixup.Definition.Kind)
        {
            case SceneFixupKind.CraftingStationFeedbackFromDonor:
                ApplyCraftingStationFeedbackFromDonor(scene, fixup.Prefab, fixup.Definition.DonorPrefabName);
                break;
            default:
                Debug.LogWarning($"[kg.ValheimEnchantmentSystem] Unsupported content fixup kind '{fixup.Definition.Kind}' for '{fixup.OwnerName}'.");
                break;
        }
    }

    private static void ApplyCraftingStationFeedbackFromDonor(ZNetScene scene, GameObject stationPrefab, string donorPrefabName)
    {
        if (scene == null || stationPrefab == null || string.IsNullOrWhiteSpace(donorPrefabName))
        {
            return;
        }

        GameObject donorPrefab = scene.GetPrefab(donorPrefabName);
        if (!donorPrefab)
        {
            return;
        }

        Piece? donorPiece = donorPrefab.GetComponent<Piece>();
        Piece? stationPiece = stationPrefab.GetComponent<Piece>();
        CraftingStation? donorStation = donorPrefab.GetComponent<CraftingStation>();
        CraftingStation? station = stationPrefab.GetComponent<CraftingStation>();
        if (donorPiece == null || stationPiece == null || donorStation == null || station == null)
        {
            return;
        }

        if (station.m_useAnimation == 0 && donorStation.m_useAnimation != 0)
        {
            station.m_useAnimation = donorStation.m_useAnimation;
        }

        if (IsEffectListEmpty(station.m_craftItemEffects) && !IsEffectListEmpty(donorStation.m_craftItemEffects))
        {
            station.m_craftItemEffects = donorStation.m_craftItemEffects;
        }

        if (IsEffectListEmpty(station.m_craftItemDoneEffects) && !IsEffectListEmpty(donorStation.m_craftItemDoneEffects))
        {
            station.m_craftItemDoneEffects = donorStation.m_craftItemDoneEffects;
        }

        if (IsEffectListEmpty(station.m_repairItemDoneEffects) && !IsEffectListEmpty(donorStation.m_repairItemDoneEffects))
        {
            station.m_repairItemDoneEffects = donorStation.m_repairItemDoneEffects;
        }
    }

    private static bool IsEffectListEmpty(EffectList? effectList)
    {
        return effectList == null || effectList.m_effectPrefabs == null || effectList.m_effectPrefabs.Length == 0;
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_ContentFixup_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Low)]
        private static void Postfix(ZNetScene __instance)
        {
            ApplySceneFixups(__instance);
        }
    }
}
