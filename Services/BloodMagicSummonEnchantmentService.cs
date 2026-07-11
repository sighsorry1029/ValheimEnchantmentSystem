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
        if (!Enchantment_Core.HasDamageStats(stats))
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
        return Enchantment_Core.HasDamageStats(stats);
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

            Enchantment_Core.ApplyDamageStats(ref hit.m_damage, stats);
        }
    }
}
