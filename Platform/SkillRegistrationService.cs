using SkillManager;

namespace kg.ValheimEnchantmentSystem.Platform;

internal static class SkillRegistrationService
{
    public static Skills.SkillType RegisterConfigurableSkill(string englishName, string iconResourceName)
    {
        new Skill(englishName, iconResourceName)
        {
            Configurable = true
        };

        return (Skills.SkillType)Mathf.Abs(englishName.GetStableHashCode());
    }
}
