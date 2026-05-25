using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem;

public static class EnchantmentSkillBonusService
{
    public static float GetAdditionalEnchantmentChance(Player player = null)
    {
        Player sourcePlayer = player ? player : Player.m_localPlayer;
        if (!sourcePlayer)
        {
            return 0f;
        }

        float enchantmentLevel = sourcePlayer.GetSkillLevel(Enchantment_Skill.SkillType_Enchantment);
        return enchantmentLevel * SyncedData.AdditionalEnchantmentChancePerLevel.Value;
    }
}
