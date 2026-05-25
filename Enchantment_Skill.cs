using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.Platform;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Enchantment_Skill
{
    public static Skills.SkillType SkillType_Enchantment;
    [UsedImplicitly]
    private static void OnInit()
    {
        SkillType_Enchantment = SkillRegistrationService.RegisterConfigurableSkill("kg_Enchantment", "enchantment.png");
    }
} 
