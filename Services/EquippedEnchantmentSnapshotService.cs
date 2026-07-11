using System.Runtime.CompilerServices;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

internal static class EquippedEnchantmentSnapshotService
{
    internal sealed class Snapshot
    {
        public int MaxHealthBonus;
        public int MaxStaminaBonus;
        public float MovementModifier;
        public float HealthRegen;
        public float StaminaRegen;
        public readonly List<HitData.DamageModPair> ResistancePairs = new();

        public void Reset()
        {
            MaxHealthBonus = 0;
            MaxStaminaBonus = 0;
            MovementModifier = 0f;
            HealthRegen = 0f;
            StaminaRegen = 0f;
            ResistancePairs.Clear();
        }
    }

    private sealed class CacheEntry
    {
        public readonly WeakReference<Player> PlayerRef;
        public readonly Snapshot Snapshot = new();
        public Inventory? Inventory;
        public bool Dirty = true;
        public int Revision = -1;

        public CacheEntry(Player player)
        {
            PlayerRef = new WeakReference<Player>(player);
            Inventory = player.m_inventory;
        }
    }

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private static readonly Snapshot EmptySnapshot = new();
    private static readonly Dictionary<int, CacheEntry> EntriesByPlayerId = new();
    private static readonly Dictionary<Inventory, HashSet<int>> PlayerIdsByInventory = new(ReferenceComparer<Inventory>.Instance);
    private static int _globalRevision;
    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        SyncedData.Synced_EnchantmentStats_Weapons.ValueChanged += InvalidateAll;
        SyncedData.Synced_EnchantmentStats_Armor.ValueChanged += InvalidateAll;
        SyncedData.Overrides_EnchantmentStats.ValueChanged += InvalidateAll;
        EngineEvents.InventoryChanged += MarkDirty;
        EngineEvents.EquipmentChanged += MarkDirty;
        EngineEvents.MainMenuAwake += OnMainMenuAwake;
    }

    internal static Snapshot GetSnapshot(Player? player)
    {
        if (player == null || !player || player.m_inventory == null)
        {
            return EmptySnapshot;
        }

        CacheEntry entry = GetOrCreateEntry(player);
        if (entry.Dirty || entry.Revision != _globalRevision)
        {
            RebuildSnapshot(player, entry);
        }

        return entry.Snapshot;
    }

    internal static void MarkDirty(Player? player)
    {
        if (player == null)
        {
            return;
        }

        CacheEntry entry = GetOrCreateEntry(player);
        entry.Dirty = true;
    }

    internal static void MarkDirty(Inventory? inventory)
    {
        if (inventory == null || !PlayerIdsByInventory.TryGetValue(inventory, out HashSet<int>? playerIds))
        {
            return;
        }

        foreach (int playerId in playerIds.ToArray())
        {
            if (!EntriesByPlayerId.TryGetValue(playerId, out CacheEntry? entry))
            {
                playerIds.Remove(playerId);
                continue;
            }

            if (!TryGetAlivePlayer(entry, out _))
            {
                RemoveEntry(playerId, entry);
                playerIds.Remove(playerId);
                continue;
            }

            entry.Dirty = true;
        }

        if (playerIds.Count == 0)
        {
            PlayerIdsByInventory.Remove(inventory);
        }
    }

    internal static void InvalidateAll()
    {
        unchecked
        {
            ++_globalRevision;
        }

        PruneDeadEntries();
    }

    internal static void Unregister(Player? player)
    {
        if (player == null)
        {
            return;
        }

        int playerId = player.GetInstanceID();
        if (!EntriesByPlayerId.TryGetValue(playerId, out CacheEntry? entry))
        {
            return;
        }

        RemoveEntry(playerId, entry);
    }

    private static CacheEntry GetOrCreateEntry(Player player)
    {
        int playerId = player.GetInstanceID();
        if (!EntriesByPlayerId.TryGetValue(playerId, out CacheEntry? entry))
        {
            entry = new CacheEntry(player);
            EntriesByPlayerId[playerId] = entry;
            BindInventory(playerId, player.m_inventory);
            return entry;
        }

        Inventory? currentInventory = player.m_inventory;
        if (!ReferenceEquals(entry.Inventory, currentInventory))
        {
            UnbindInventory(playerId, entry.Inventory);
            entry.Inventory = currentInventory;
            BindInventory(playerId, currentInventory);
            entry.Dirty = true;
        }

        return entry;
    }

    private static void RebuildSnapshot(Player player, CacheEntry entry)
    {
        Snapshot snapshot = entry.Snapshot;
        snapshot.Reset();
        foreach (ItemDrop.ItemData item in player.m_inventory.GetEquippedItems())
        {
            if (item?.Data().Get<Enchantment_Core.Enchanted>() is not { level: > 0 } enchantment)
            {
                continue;
            }

            if (enchantment.Stats is not { } stats)
            {
                continue;
            }

            snapshot.MaxHealthBonus += stats.max_hp;
            snapshot.MaxStaminaBonus += stats.max_stamina;
            snapshot.MovementModifier += stats.movement_speed / 100f;
            snapshot.HealthRegen += stats.hp_regen;
            snapshot.StaminaRegen += stats.stamina_regen;

            foreach (HitData.DamageModPair resistancePair in stats.GetResistancePairs())
            {
                snapshot.ResistancePairs.Add(new HitData.DamageModPair
                {
                    m_type = resistancePair.m_type,
                    m_modifier = resistancePair.m_modifier
                });
            }
        }

        entry.Dirty = false;
        entry.Revision = _globalRevision;
    }

    private static void BindInventory(int playerId, Inventory? inventory)
    {
        if (inventory == null)
        {
            return;
        }

        if (!PlayerIdsByInventory.TryGetValue(inventory, out HashSet<int>? playerIds))
        {
            playerIds = new HashSet<int>();
            PlayerIdsByInventory[inventory] = playerIds;
        }

        playerIds.Add(playerId);
    }

    private static void UnbindInventory(int playerId, Inventory? inventory)
    {
        if (inventory == null || !PlayerIdsByInventory.TryGetValue(inventory, out HashSet<int>? playerIds))
        {
            return;
        }

        playerIds.Remove(playerId);
        if (playerIds.Count == 0)
        {
            PlayerIdsByInventory.Remove(inventory);
        }
    }

    private static bool TryGetAlivePlayer(CacheEntry entry, out Player? player)
    {
        if (!entry.PlayerRef.TryGetTarget(out player) || !player)
        {
            player = null;
            return false;
        }

        return true;
    }

    private static void PruneDeadEntries()
    {
        foreach (KeyValuePair<int, CacheEntry> entry in EntriesByPlayerId.ToArray())
        {
            if (!TryGetAlivePlayer(entry.Value, out _))
            {
                RemoveEntry(entry.Key, entry.Value);
            }
        }
    }

    private static void RemoveEntry(int playerId, CacheEntry entry)
    {
        UnbindInventory(playerId, entry.Inventory);
        EntriesByPlayerId.Remove(playerId);
    }

    private static void ClearAll()
    {
        EntriesByPlayerId.Clear();
        PlayerIdsByInventory.Clear();
    }

    private static void OnMainMenuAwake(FejdStartup _)
    {
        ClearAll();
    }

    [HarmonyPatch(typeof(Player), "OnDestroy")]
    [ClientOnlyPatch]
    private static class Player_OnDestroy_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Player __instance)
        {
            Unregister(__instance);
        }
    }
}
