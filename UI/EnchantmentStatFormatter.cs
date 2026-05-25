using System.Globalization;
using System.Text;
using kg.ValheimEnchantmentSystem.Configs;

namespace kg.ValheimEnchantmentSystem.UI;

public static class EnchantmentStatFormatter
{
    private static readonly List<FieldInfo> CachedValueFields = AccessTools
        .GetDeclaredFields(typeof(SyncedData.Stat_Data))
        .Where(field => field.FieldType.IsValueType)
        .ToList();
    private static readonly (Func<SyncedData.Stat_Data, int> Getter, string Label, bool Percent)[] IntTransitions =
    {
        (stats => stats.damage_percentage, "$enchantment_bonusespercentdamage", true),
        (stats => stats.attack_speed, "$enchantment_attackspeed", true),
        (stats => stats.movement_speed, "$enchantment_movementspeed", true),
        (stats => stats.durability_percentage, "$item_durability", true),
        (stats => stats.durability, "$item_durability", false),
        (stats => stats.damage_true, "$enchantment_truedamage", false),
        (stats => stats.damage_fire, "$inventory_fire", false),
        (stats => stats.damage_blunt, "$inventory_blunt", false),
        (stats => stats.damage_slash, "$inventory_slash", false),
        (stats => stats.damage_pierce, "$inventory_pierce", false),
        (stats => stats.damage_chop, "$enchantment_chopdamage", false),
        (stats => stats.damage_pickaxe, "$enchantment_pickaxedamage", false),
        (stats => stats.damage_frost, "$inventory_frost", false),
        (stats => stats.damage_lightning, "$inventory_lightning", false),
        (stats => stats.damage_poison, "$inventory_poison", false),
        (stats => stats.damage_spirit, "$inventory_spirit", false),
        (stats => stats.max_hp, "$se_health", false),
        (stats => stats.max_stamina, "$se_stamina", false),
        (stats => stats.API_backpacks_additionalrow_x, "$enchantment_backpacks_additionalrow_x", false),
        (stats => stats.API_backpacks_additionalrow_y, "$enchantment_backpacks_additionalrow_y", false),
    };
    private static readonly (Func<SyncedData.Stat_Data, float> Getter, string Label, bool Percent, string Suffix)[] FloatTransitions =
    {
        (stats => stats.armor_percentage, "$enchantment_bonusespercentarmor", true, string.Empty),
        (stats => stats.armor, "$item_armor", false, string.Empty),
        (stats => stats.hp_regen, "$se_healthregen", false, "/10s"),
        (stats => stats.stamina_regen, "$se_staminaregen", false, "/s"),
    };
    private static readonly (Func<SyncedData.Stat_Data, HitData.DamageModifier> Getter, string Label)[] ResistanceTransitions =
    {
        (stats => stats.resistance_blunt, "$inventory_blunt"),
        (stats => stats.resistance_slash, "$inventory_slash"),
        (stats => stats.resistance_pierce, "$inventory_pierce"),
        (stats => stats.resistance_chop, "$enchantment_chopdamage"),
        (stats => stats.resistance_pickaxe, "$enchantment_pickaxedamage"),
        (stats => stats.resistance_fire, "$inventory_fire"),
        (stats => stats.resistance_frost, "$inventory_frost"),
        (stats => stats.resistance_lightning, "$inventory_lightning"),
        (stats => stats.resistance_poison, "$inventory_poison"),
        (stats => stats.resistance_spirit, "$inventory_spirit"),
    };

    public static string BuildAdditionalStats(SyncedData.Stat_Data stats, string color)
    {
        if (stats == null || !ShouldShow(stats))
        {
            return "\n";
        }

        StringBuilder builder = new();
        if (stats.attack_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_attackspeed: <color=#DF745D>+{stats.attack_speed}%</color>");
        if (stats.movement_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_movementspeed: <color=#DF745D>+{stats.movement_speed}%</color>");
        if (stats.durability_percentage > 0) builder.Append($"\n<color={color}>•</color> $item_durability: <color=#DF745D>+{stats.durability_percentage}%</color>");
        if (stats.durability > 0) builder.Append($"\n<color={color}>•</color> $item_durability: <color=#DF745D>+{stats.durability}</color>");
        if (stats.damage_true > 0) builder.Append($"\n<color={color}>•</color> $enchantment_truedamage: +{stats.damage_true}");
        if (stats.damage_fire > 0) builder.Append($"\n<color={color}>•</color> $inventory_fire: <color=#FFA500>+{stats.damage_fire}</color>");
        if (stats.damage_blunt > 0) builder.Append($"\n<color={color}>•</color> $inventory_blunt: <color=#FFFF00>+{stats.damage_blunt}</color>");
        if (stats.damage_slash > 0) builder.Append($"\n<color={color}>•</color> $inventory_slash: <color=#7F00FF>+{stats.damage_slash}</color>");
        if (stats.damage_pierce > 0) builder.Append($"\n<color={color}>•</color> $inventory_pierce: <color=#D499B9>+{stats.damage_pierce}</color>");
        if (stats.damage_chop > 0) builder.Append($"\n<color={color}>•</color> $enchantment_chopdamage: <color=#FFAF00>+{stats.damage_chop}</color>");
        if (stats.damage_pickaxe > 0) builder.Append($"\n<color={color}>•</color> $enchantment_pickaxedamage: <color=#FF00FF>+{stats.damage_pickaxe}</color>");
        if (stats.damage_frost > 0) builder.Append($"\n<color={color}>•</color> $inventory_frost: <color=#00FFFF>+{stats.damage_frost}</color>");
        if (stats.damage_lightning > 0) builder.Append($"\n<color={color}>•</color> $inventory_lightning: <color=#0000FF>+{stats.damage_lightning}</color>");
        if (stats.damage_poison > 0) builder.Append($"\n<color={color}>•</color> $inventory_poison: <color=#00FF00>+{stats.damage_poison}</color>");
        if (stats.damage_spirit > 0) builder.Append($"\n<color={color}>•</color> $inventory_spirit: <color=#FFFFA0>+{stats.damage_spirit}</color>");
        if (stats.max_hp > 0) builder.Append($"\n<color={color}>•</color> $se_health: <color=#DD3333>+{stats.max_hp}</color>");
        if (stats.hp_regen > 0) builder.Append($"\n<color={color}>•</color> $se_healthregen: <color=#DD3333>+{stats.hp_regen}/10s</color>");
        if (stats.armor > 0) builder.Append($"\n<color={color}>•</color> $item_armor: <color=#009FAF>+{stats.armor}</color>");
        if (stats.max_stamina > 0) builder.Append($"\n<color={color}>•</color> $se_stamina: <color=#EEEE11>+{stats.max_stamina}</color>");
        if (stats.stamina_regen > 0) builder.Append($"\n<color={color}>•</color> $se_staminaregen: <color=#EEEE11>+{stats.stamina_regen}/s</color>");
        if (stats.API_backpacks_additionalrow_x > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_x: <color=#7393B3>{stats.API_backpacks_additionalrow_x}</color>");
        if (stats.API_backpacks_additionalrow_y > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_y: <color=#7393B3>{stats.API_backpacks_additionalrow_y}</color>");

        builder.Append(SE_Stats.GetDamageModifiersTooltipString(stats.GetResistancePairs()).Replace("\n", $"\n<color={color}>•</color> "));
        builder.Append("\n");
        return builder.ToString();
    }

    public static string BuildInfoDescription(SyncedData.Stat_Data stats)
    {
        if (stats == null)
        {
            return string.Empty;
        }

        string result = string.Empty;
        if (stats.damage_percentage > 0)
        {
            result += $"\n• $enchantment_bonusespercentdamage: <color=#AF009F>+{stats.damage_percentage}%</color>";
        }

        if (stats.armor_percentage > 0)
        {
            result += $"\n• $enchantment_bonusespercentarmor: <color=#009FAF>+{stats.armor_percentage}%</color>";
        }

        result += BuildAdditionalStats(stats, "#FFFFFF");
        return result;
    }

    public static string BuildTransitionDescription(SyncedData.Stat_Data? current, SyncedData.Stat_Data? next)
    {
        if (next == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        foreach ((Func<SyncedData.Stat_Data, int> getter, string label, bool percent) in IntTransitions)
        {
            int currentValue = current != null ? getter(current) : 0;
            int nextValue = getter(next);
            AppendTransitionLine(builder, label, currentValue, nextValue, percent, string.Empty);
        }

        foreach ((Func<SyncedData.Stat_Data, float> getter, string label, bool percent, string suffix) in FloatTransitions)
        {
            float currentValue = current != null ? getter(current) : 0f;
            float nextValue = getter(next);
            AppendTransitionLine(builder, label, currentValue, nextValue, percent, suffix);
        }

        foreach ((Func<SyncedData.Stat_Data, HitData.DamageModifier> getter, string label) in ResistanceTransitions)
        {
            HitData.DamageModifier currentValue = current != null ? getter(current) : HitData.DamageModifier.Normal;
            HitData.DamageModifier nextValue = getter(next);
            AppendResistanceTransitionLine(builder, label, currentValue, nextValue);
        }

        return builder.ToString().Trim();
    }

    private static bool ShouldShow(SyncedData.Stat_Data stats)
    {
        return CachedValueFields.Any(field => !field.GetValue(stats).Equals(Activator.CreateInstance(field.FieldType)));
    }

    private static void AppendTransitionLine(StringBuilder builder, string label, int currentValue, int nextValue, bool percent, string suffix)
    {
        if (currentValue == nextValue && nextValue == 0)
        {
            return;
        }

        string formattedCurrent = FormatValue(currentValue, percent, suffix);
        string formattedNext = FormatValue(nextValue, percent, suffix);
        AppendTransitionLine(builder, label, currentValue != 0, formattedCurrent, formattedNext);
    }

    private static void AppendTransitionLine(StringBuilder builder, string label, float currentValue, float nextValue, bool percent, string suffix)
    {
        if (Math.Abs(currentValue - nextValue) < 0.001f && Math.Abs(nextValue) < 0.001f)
        {
            return;
        }

        string formattedCurrent = FormatValue(currentValue, percent, suffix);
        string formattedNext = FormatValue(nextValue, percent, suffix);
        AppendTransitionLine(builder, label, Math.Abs(currentValue) >= 0.001f, formattedCurrent, formattedNext);
    }

    private static void AppendTransitionLine(StringBuilder builder, string label, bool hasCurrentValue, string formattedCurrent, string formattedNext)
    {
        if (string.Equals(formattedCurrent, formattedNext, StringComparison.Ordinal) && hasCurrentValue)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append("• ").Append(label).Append(": ");
        if (hasCurrentValue)
        {
            builder.Append(formattedCurrent).Append(" > ").Append(formattedNext);
        }
        else
        {
            builder.Append(formattedNext);
        }
    }

    private static void AppendResistanceTransitionLine(StringBuilder builder, string label, HitData.DamageModifier currentValue, HitData.DamageModifier nextValue)
    {
        if (currentValue == nextValue && nextValue == HitData.DamageModifier.Normal)
        {
            return;
        }

        if (currentValue == nextValue)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append("• ").Append(label).Append(": ");
        if (currentValue != HitData.DamageModifier.Normal)
        {
            builder.Append(FormatDamageModifier(currentValue)).Append(" > ").Append(FormatDamageModifier(nextValue));
        }
        else
        {
            builder.Append(FormatDamageModifier(nextValue));
        }
    }

    private static string FormatValue(int value, bool percent, string suffix)
    {
        return percent ? $"+{value}%{suffix}" : $"+{value}{suffix}";
    }

    private static string FormatValue(float value, bool percent, string suffix)
    {
        string number = value.ToString("0.#", CultureInfo.InvariantCulture);
        return percent ? $"+{number}%{suffix}" : $"+{number}{suffix}";
    }

    private static string FormatDamageModifier(HitData.DamageModifier modifier)
    {
        return modifier switch
        {
            HitData.DamageModifier.Resistant => "$enchantment_modifier_resistant".Localize(),
            HitData.DamageModifier.Weak => "$enchantment_modifier_weak".Localize(),
            HitData.DamageModifier.Immune => "$enchantment_modifier_immune".Localize(),
            HitData.DamageModifier.Ignore => "$enchantment_modifier_ignore".Localize(),
            HitData.DamageModifier.VeryResistant => "$enchantment_modifier_veryresistant".Localize(),
            HitData.DamageModifier.VeryWeak => "$enchantment_modifier_veryweak".Localize(),
            _ => "$enchantment_modifier_normal".Localize()
        };
    }
}
