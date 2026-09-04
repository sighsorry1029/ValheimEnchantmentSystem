using kg.ValheimEnchantmentSystem.Integrations;

namespace kg.ValheimEnchantmentSystem;

// Only enchantment requirements use this service. EXP-scroll use and scroll combining stay inventory-only.
internal static class EnchantmentMaterialService
{
    private static bool _loggedFailure;

    internal static int CountAvailable(Player? player, string prefab)
    {
        if (player == null || string.IsNullOrEmpty(prefab)) return 0;
        Inventory inventory = player.GetInventory();
        int count = Count(inventory, prefab);
        try
        {
            // One current policy snapshot for the preview, rather than rescanning all nearby chests per chest.
            IReadOnlyList<Container> containers = AzuCraftyBoxesIntegration.Instance.GetEligibleContainers(player, prefab, out bool leaveOne);
            foreach (Container container in containers)
            {
                if (!IsUsable(container) || ReferenceEquals(container.GetInventory(), inventory)) continue;
                container.Load();
                if (!IsUsable(container)) continue;
                int available = EnchantmentMaterialRules.AvailableInContainer(Count(container.GetInventory(), prefab), leaveOne);
                count = EnchantmentMaterialRules.AddCounts(count, available);
            }
        }
        catch (Exception error)
        {
            LogFailure(error);
            // Do not advertise uncertain external materials; the player's own inventory remains usable.
            return Count(inventory, prefab);
        }
        return count;
    }

    internal static bool TryConsumeOne(Player? player, string prefab)
    {
        if (player == null || string.IsNullOrEmpty(prefab)) return false;
        try
        {
            return EnchantmentMaterialRules.TryConsumeOne(
                () => RemoveOne(player.GetInventory(), prefab),
                () => ContainerConsumers(player, prefab));
        }
        catch (Exception error)
        {
            // An exception after mutation is uncertain. Never retry another source or grant an enchantment.
            LogFailure(error);
            return false;
        }
    }

    private static IEnumerable<Func<bool>> ContainerConsumers(Player player, string prefab)
    {
        foreach (Container container in AzuCraftyBoxesIntegration.Instance.GetEligibleContainers(player, prefab, out _))
            yield return () => ConsumeFromContainer(container, player, prefab);
    }

    private static bool ConsumeFromContainer(Container container, Player player, string prefab)
    {
        if (!IsUsable(container) || ReferenceEquals(container.GetInventory(), player.GetInventory()) ||
            !AzuCraftyBoxesIntegration.Instance.IsEligibleContainer(container, player, prefab, out _)) return false;

        // Use the latest locally replicated contents, not a count cached when the animation started.
        container.Load();
        if (!IsUsable(container) ||
            !AzuCraftyBoxesIntegration.Instance.IsEligibleContainer(container, player, prefab, out bool leaveOne)) return false;
        Inventory inventory = container.GetInventory();
        if (EnchantmentMaterialRules.AvailableInContainer(Count(inventory, prefab), leaveOne) < 1) return false;
        if (!RemoveOne(inventory, prefab)) return false;

        // Match ACB's normal Container save/replication path. ClaimOwnership is not an atomic lock.
        container.Save();
        return true;
    }

    private static bool IsUsable(Container? container)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid() ||
            container.GetInventory() == null || container.GetComponent<TombStone>() != null) return false;
        bool inUse = container.IsInUse() || container.m_nview.GetZDO().GetBool(ZDOVars.s_inUse);
        return !inUse || (container.m_nview.IsOwner() && InventoryGui.instance != null &&
                         InventoryGui.instance.m_currentContainer == container);
    }

    private static int Count(Inventory? inventory, string prefab)
    {
        if (inventory == null) return 0;
        int count = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            if (EnchantmentMaterialRules.Matches(item?.m_dropPrefab?.name, prefab, item?.m_stack ?? 0))
                count = EnchantmentMaterialRules.AddCounts(count, item.m_stack);
        return count;
    }

    private static bool RemoveOne(Inventory inventory, string prefab)
    {
        ItemDrop.ItemData? item = inventory.GetAllItems().FirstOrDefault(candidate =>
            EnchantmentMaterialRules.Matches(candidate?.m_dropPrefab?.name, prefab, candidate?.m_stack ?? 0));
        if (item == null) return false;
        int before = item.m_stack;
        if (!inventory.RemoveItem(item, 1)) return false;
        int after = inventory.ContainsItem(item) ? item.m_stack : 0;
        if (before - after != 1)
            throw new InvalidOperationException("Enchantment material removal did not remove exactly one item.");
        return true;
    }

    private static void LogFailure(Exception error)
    {
        if (_loggedFailure) return;
        _loggedFailure = true;
        Utils.print($"Enchantment material access failed; no enchantment will be granted for an uncertain removal: {error.Message}",
            ConsoleColor.Yellow);
    }
}
