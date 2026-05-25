using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem;

internal static class BloodMagicSummonEnchantmentService
{
    private static readonly int SourcePrefabHash = "VES_BloodMagicSummonSourcePrefab".GetStableHashCode();
    private static readonly int SourceLevelHash = "VES_BloodMagicSummonSourceLevel".GetStableHashCode();

    private static bool TryGetSourceEnchantment(ItemDrop.ItemData weapon, out string sourcePrefab, out int sourceLevel)
    {
        sourcePrefab = string.Empty;
        sourceLevel = 0;

        if (weapon == null || weapon.m_shared.m_skillType != Skills.SkillType.BloodMagic)
        {
            return false;
        }

        Enchantment_Core.Enchanted enchantment = weapon.Data().Get<Enchantment_Core.Enchanted>();
        if (enchantment is not { level: > 0 })
        {
            return false;
        }

        sourcePrefab = EnchantmentDomainHelper.ResolveItemPrefabName(weapon);
        if (string.IsNullOrWhiteSpace(sourcePrefab))
        {
            return false;
        }

        SyncedData.Stat_Data stats = SyncedData.GetStatIncrease(sourcePrefab, enchantment.level, true);
        if (!HasDamageStats(stats))
        {
            return false;
        }

        sourceLevel = enchantment.level;
        return true;
    }

    private static bool TryGetSummonDamageStats(Character attacker, out SyncedData.Stat_Data stats)
    {
        stats = null;
        if (!attacker || !attacker.m_nview || !attacker.m_nview.IsValid())
        {
            return false;
        }

        ZDO zdo = attacker.m_nview.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        string sourcePrefab = zdo.GetString(SourcePrefabHash);
        int sourceLevel = zdo.GetInt(SourceLevelHash);
        if (string.IsNullOrWhiteSpace(sourcePrefab) || sourceLevel <= 0)
        {
            return false;
        }

        stats = SyncedData.GetStatIncrease(sourcePrefab, sourceLevel, true);
        return HasDamageStats(stats);
    }

    private static bool HasDamageStats(SyncedData.Stat_Data stats)
    {
        return stats != null &&
               (stats.damage_percentage != 0 ||
                stats.damage_true != 0 ||
                stats.damage_blunt != 0 ||
                stats.damage_slash != 0 ||
                stats.damage_pierce != 0 ||
                stats.damage_chop != 0 ||
                stats.damage_pickaxe != 0 ||
                stats.damage_fire != 0 ||
                stats.damage_frost != 0 ||
                stats.damage_lightning != 0 ||
                stats.damage_poison != 0 ||
                stats.damage_spirit != 0);
    }

    private static void ApplyDamageStats(HitData hit, SyncedData.Stat_Data stats)
    {
        hit.m_damage.Modify(1 + stats.damage_percentage / 100f);
        hit.m_damage.m_blunt += stats.damage_blunt;
        hit.m_damage.m_slash += stats.damage_slash;
        hit.m_damage.m_pierce += stats.damage_pierce;
        hit.m_damage.m_fire += stats.damage_fire;
        hit.m_damage.m_frost += stats.damage_frost;
        hit.m_damage.m_lightning += stats.damage_lightning;
        hit.m_damage.m_poison += stats.damage_poison;
        hit.m_damage.m_spirit += stats.damage_spirit;
        hit.m_damage.m_damage += stats.damage_true;
        hit.m_damage.m_chop += stats.damage_chop;
        hit.m_damage.m_pickaxe += stats.damage_pickaxe;
    }

    [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.SetupAoe))]
    private static class SpawnAbility_SetupAoe_Patch
    {
        [UsedImplicitly]
        private static void Prefix(SpawnAbility __instance, Character owner)
        {
            if (!owner || !owner.m_nview || !owner.m_nview.IsValid())
            {
                return;
            }

            if (!TryGetSourceEnchantment(__instance.m_weapon, out string sourcePrefab, out int sourceLevel))
            {
                return;
            }

            ZDO zdo = owner.m_nview.GetZDO();
            if (zdo == null)
            {
                return;
            }

            zdo.Set(SourcePrefabHash, sourcePrefab);
            zdo.Set(SourceLevelHash, sourceLevel);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Prefix(HitData hit)
        {
            if (hit == null)
            {
                return;
            }

            Character attacker = hit.GetAttacker();
            if (!TryGetSummonDamageStats(attacker, out SyncedData.Stat_Data stats))
            {
                return;
            }

            ApplyDamageStats(hit, stats);
        }
    }
}
