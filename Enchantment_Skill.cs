using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.Platform;

namespace kg.ValheimEnchantmentSystem;

public static class Enchantment_Skill
{
    public static Skills.SkillType SkillType_Enchantment;
    internal static void Initialize()
    {
        SkillType_Enchantment = SkillRegistrationService.RegisterConfigurableSkill("kg_Enchantment", "enchantment.png");
    }
} 
