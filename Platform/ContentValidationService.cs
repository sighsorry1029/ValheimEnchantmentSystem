namespace kg.ValheimEnchantmentSystem.Platform;

internal static class ContentValidationService
{
    private static readonly Dictionary<string, PieceRegistrationService.RegisteredPiece> CraftingPieces = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ItemRegistrationService.RegisteredItem> RegisteredItems = new(StringComparer.Ordinal);
    private static readonly HashSet<string> WarningCache = new(StringComparer.Ordinal);

    public static void RegisterCraftingPiece(PieceRegistrationService.RegisteredPiece piece)
    {
        if (piece == null || piece.Prefab == null)
        {
            return;
        }

        CraftingPieces[piece.PrefabName] = piece;
    }

    public static void RegisterRecipeItem(ItemRegistrationService.RegisteredItem item)
    {
        if (item == null || item.Prefab == null)
        {
            return;
        }

        RegisteredItems[item.PrefabName] = item;
    }

    internal static void ValidateRegisteredContent(ZNetScene? scene)
    {
        if (scene == null)
        {
            return;
        }

        foreach (PieceRegistrationService.RegisteredPiece pieceRegistration in CraftingPieces.Values)
        {
            ValidateCraftingPiece(pieceRegistration, scene);
        }

        foreach (ItemRegistrationService.RegisteredItem itemRegistration in RegisteredItems.Values)
        {
            ValidateRegisteredItem(itemRegistration, scene);
        }
    }

    private static void ValidateCraftingPiece(PieceRegistrationService.RegisteredPiece registration, ZNetScene scene)
    {
        GameObject prefab = registration.Prefab;
        if (prefab == null)
        {
            WarnOnce($"Missing registered crafting piece prefab for '{registration.PrefabName}'.");
            return;
        }

        Piece? piece = registration.TryGetComponent<Piece>();
        if (piece == null)
        {
            WarnOnce($"Registered crafting piece '{registration.PrefabName}' is missing a Piece component.");
            return;
        }

        if (string.IsNullOrWhiteSpace(piece.m_name))
        {
            WarnOnce($"Registered crafting piece '{registration.PrefabName}' is missing a localized name token.");
        }

        if (string.IsNullOrWhiteSpace(piece.m_description))
        {
            WarnOnce($"Registered crafting piece '{registration.PrefabName}' is missing a localized description token.");
        }

        if (!string.IsNullOrWhiteSpace(registration.CraftingStationName) &&
            scene.GetPrefab(registration.CraftingStationName)?.GetComponent<CraftingStation>() == null)
        {
            WarnOnce($"Registered crafting piece '{registration.PrefabName}' references unknown crafting station '{registration.CraftingStationName}'.");
        }

        if (registration.TryGetComponent<CraftingStation>() is not { } station)
        {
            return;
        }

        if (registration.RequirePlaceEffect && IsEffectListEmpty(piece.m_placeEffect))
        {
            WarnOnce($"Crafting station piece '{registration.PrefabName}' has no place effect.");
        }

        if (station.m_useAnimation == 0)
        {
            WarnOnce($"Crafting station piece '{registration.PrefabName}' has no use animation configured.");
        }

        if (IsEffectListEmpty(station.m_craftItemEffects))
        {
            WarnOnce($"Crafting station piece '{registration.PrefabName}' has no craft item effects.");
        }

        if (IsEffectListEmpty(station.m_craftItemDoneEffects))
        {
            WarnOnce($"Crafting station piece '{registration.PrefabName}' has no craft completion effects.");
        }

        if (IsEffectListEmpty(station.m_repairItemDoneEffects))
        {
            WarnOnce($"Crafting station piece '{registration.PrefabName}' has no repair completion effects.");
        }
    }

    private static void ValidateRegisteredItem(ItemRegistrationService.RegisteredItem registration, ZNetScene scene)
    {
        if (registration.Prefab == null)
        {
            WarnOnce($"Missing registered item prefab for '{registration.PrefabName}'.");
            return;
        }

        foreach (string craftingStationName in registration.CraftingStationNames)
        {
            if (scene.GetPrefab(craftingStationName)?.GetComponent<CraftingStation>() == null)
            {
                WarnOnce($"Registered item '{registration.PrefabName}' references unknown crafting station '{craftingStationName}'.");
            }
        }
    }

    private static bool IsEffectListEmpty(EffectList? effectList)
    {
        return effectList == null || effectList.m_effectPrefabs == null || effectList.m_effectPrefabs.Length == 0;
    }

    private static void WarnOnce(string message)
    {
        if (!WarningCache.Add(message))
        {
            return;
        }

        Debug.LogWarning($"[kg.ValheimEnchantmentSystem] {message}");
    }
}
