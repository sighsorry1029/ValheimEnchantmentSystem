using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using UnityEngine;

namespace SkillManager;

[PublicAPI]
public class Skill
{
    private static readonly Harmony Harmony = new("kg.ValheimEnchantmentSystem.ThinSkillManager");
    private static readonly Dictionary<Skills.SkillType, Skill> SkillsByType = new();
    private static bool _configBindingsInitialized;
    private static bool _terminalInitialized;

    private readonly string _englishName;
    private readonly string _descriptionKey;
    private readonly Skills.SkillDef _skillDef;

    internal string InternalSkillName { get; }

    public float SkillGainFactor
    {
        get => _skillDef.m_increseStep;
        set => _skillDef.m_increseStep = value;
    }

    public float SkillEffectFactor { get; set; } = 1f;
    public int SkillLoss { get; set; } = 5;
    public bool Configurable { get; set; }

    static Skill()
    {
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_FejdStartup))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_ZNet_Awake))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.GetSkillDef)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_GetSkillDef))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.CheatRaiseSkill)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_CheatRaiseSkill))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.CheatResetSkill)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_CheatResetSkill))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.IsSkillValid)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_IsSkillValid))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.OnDeath)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_OnDeath_Prefix))), finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_OnDeath_Finalizer))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Localization_LoadCSV))));
        Harmony.Patch(AccessTools.DeclaredMethod(typeof(Terminal), nameof(Terminal.InitTerminal)),
            prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Terminal_InitTerminal_Prefix))),
            postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Terminal_InitTerminal))));
    }

    public Skill(string englishName, string iconResourceName) : this(englishName, LoadSprite(iconResourceName, 64, 64))
    {
    }

    public Skill(string englishName, Sprite icon)
    {
        Skills.SkillType skillType = FromName(englishName);
        string sanitizedName = new Regex("[^a-zA-Z]").Replace(englishName, "_");

        InternalSkillName = sanitizedName;
        _englishName = englishName;
        _descriptionKey = "skilldesc_" + sanitizedName;
        _skillDef = new Skills.SkillDef
        {
            m_description = "$" + _descriptionKey,
            m_icon = icon,
            m_increseStep = 1f,
            m_skill = skillType
        };

        SkillsByType[skillType] = this;
    }

    public static Skills.SkillType FromName(string englishName) => (Skills.SkillType)Math.Abs(englishName.GetStableHashCode());

    private static void Patch_FejdStartup() => EnsureConfigBindings();

    private static void Patch_ZNet_Awake() => EnsureConfigBindings();

    private static void EnsureConfigBindings()
    {
        if (_configBindingsInitialized)
        {
            return;
        }

        foreach (Skill skill in SkillsByType.Values.Where(skill => skill.Configurable))
        {
            string internalSkillKey = $"skill_{(int)skill._skillDef.m_skill}";
            string displayName = GetEnglishDisplayName(internalSkillKey, skill._englishName);
            string configGroup = $"{displayName} Skill";
            ConfigEntry<float> skillGain = Config(configGroup, "Skill gain factor", skill.SkillGainFactor,
                new ConfigDescription($"The rate at which you gain experience for the skill. Internal skill key: {internalSkillKey}.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Category = configGroup }));
            skill.SkillGainFactor = skillGain.Value;
            skillGain.SettingChanged += (_, _) => skill.SkillGainFactor = skillGain.Value;

            ConfigEntry<float> skillEffect = Config(configGroup, "Skill effect factor", skill.SkillEffectFactor,
                new ConfigDescription($"The power of the skill, based on the default power. Internal skill key: {internalSkillKey}.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Category = configGroup }));
            skill.SkillEffectFactor = skillEffect.Value;
            skillEffect.SettingChanged += (_, _) => skill.SkillEffectFactor = skillEffect.Value;

            ConfigEntry<int> skillLoss = Config(configGroup, "Skill loss", skill.SkillLoss,
                new ConfigDescription($"How much experience to lose on death. Internal skill key: {internalSkillKey}.", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Category = configGroup }));
            skill.SkillLoss = skillLoss.Value;
            skillLoss.SettingChanged += (_, _) => skill.SkillLoss = skillLoss.Value;
        }

        _configBindingsInitialized = true;
    }

    private static void Patch_Skills_GetSkillDef(ref Skills.SkillDef? __result, List<Skills.SkillDef> ___m_skills, Skills.SkillType type)
    {
        if (__result is not null || !SkillsByType.TryGetValue(type, out Skill? skill))
        {
            return;
        }

        if (!___m_skills.Contains(skill._skillDef))
        {
            ___m_skills.Add(skill._skillDef);
        }

        __result = skill._skillDef;
    }

    private static bool Patch_Skills_CheatRaiseSkill(Skills __instance, string name, float value, Player ___m_player)
    {
        if (!TryGetSkillByInternalName(name, out Skills.SkillType skillType, out Skill? skillDetails))
        {
            return true;
        }

        Skills.Skill skill = __instance.GetSkill(skillType);
        skill.m_level = Mathf.Clamp(skill.m_level + value, 0f, 100f);
        ___m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + Localization.instance.Localize($"$skill_{(int)skillType}") + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
        Console.instance.Print("Skill " + skillDetails.InternalSkillName + " = " + skill.m_level);
        return false;
    }

    private static bool Patch_Skills_CheatResetSkill(Skills __instance, string name)
    {
        if (!TryGetSkillByInternalName(name, out Skills.SkillType skillType, out Skill? skillDetails))
        {
            return true;
        }

        __instance.ResetSkill(skillType);
        Console.instance.Print("Skill " + skillDetails.InternalSkillName + " reset");
        return false;
    }

    private static void Patch_Skills_IsSkillValid(Skills.SkillType type, ref bool __result)
    {
        if (!__result && SkillsByType.ContainsKey(type))
        {
            __result = true;
        }
    }

    private static void Patch_Skills_OnDeath_Prefix(Skills __instance, ref Dictionary<Skills.SkillType, Skills.Skill>? __state)
    {
        __state ??= new Dictionary<Skills.SkillType, Skills.Skill>();
        foreach (KeyValuePair<Skills.SkillType, Skill> entry in SkillsByType)
        {
            Skills.SkillType skillType = entry.Key;
            Skill config = entry.Value;
            if (!__instance.m_skillData.TryGetValue(skillType, out Skills.Skill? skill))
            {
                continue;
            }

            __state[skillType] = skill;
            if (config.SkillLoss > 0)
            {
                skill.m_level -= skill.m_level * config.SkillLoss / 100f;
                skill.m_accumulator = 0f;
            }

            __instance.m_skillData.Remove(skillType);
        }
    }

    private static void Patch_Skills_OnDeath_Finalizer(Skills __instance, ref Dictionary<Skills.SkillType, Skills.Skill>? __state)
    {
        if (__state is null)
        {
            return;
        }

        foreach (KeyValuePair<Skills.SkillType, Skills.Skill> entry in __state)
        {
            __instance.m_skillData[entry.Key] = entry.Value;
        }

        __state = null;
    }

    private static void Patch_Localization_LoadCSV(Localization __instance, string language)
    {
        foreach (KeyValuePair<Skills.SkillType, Skill> entry in SkillsByType)
        {
            AddWordIfMissing(__instance, $"skill_{(int)entry.Key}", entry.Value._englishName);
        }
    }

    private static void Patch_Terminal_InitTerminal_Prefix() => _terminalInitialized = Terminal.m_terminalInitialized;

    private static void Patch_Terminal_InitTerminal()
    {
        if (_terminalInitialized)
        {
            return;
        }

        AddSkillOption("raiseskill");
        AddSkillOption("resetskill");
    }

    private static void AddSkillOption(string commandName)
    {
        if (!Terminal.commands.TryGetValue(commandName, out Terminal.ConsoleCommand command))
        {
            return;
        }

        Terminal.ConsoleOptionsFetcher? fetcher = command.m_tabOptionsFetcher;
        command.m_tabOptionsFetcher = () =>
        {
            List<string> options = fetcher?.Invoke() ?? new List<string>();
            options.AddRange(SkillsByType.Values.Select(skill => skill.InternalSkillName));
            return options.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        };
    }

    private static bool TryGetSkillByInternalName(string name, out Skills.SkillType skillType, out Skill? skill)
    {
        foreach (KeyValuePair<Skills.SkillType, Skill> entry in SkillsByType)
        {
            if (string.Equals(entry.Value.InternalSkillName, name, StringComparison.OrdinalIgnoreCase))
            {
                skillType = entry.Key;
                skill = entry.Value;
                return true;
            }
        }

        skillType = default;
        skill = null;
        return false;
    }

    private static void AddWordIfMissing(Localization localization, string key, string value)
    {
        if (localization.m_translations.ContainsKey(key))
        {
            return;
        }

        localization.AddWord(key, value);
    }

    private static string GetEnglishDisplayName(string token, string fallback)
    {
        string englishName = SharedLocalizationCache.LocalizeForConfig(token);
        return string.IsNullOrWhiteSpace(englishName) ? fallback : englishName;
    }

    private static string GetLocalizedDisplayName(string token, string fallback)
    {
        if (Localization.instance == null)
        {
            return fallback;
        }

        string localized = Localization.instance.Localize(token).Trim();
        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, token, StringComparison.Ordinal) ? fallback : localized;
    }

    private static byte[] ReadEmbeddedFileBytes(string name)
    {
        using MemoryStream stream = new();
        Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + "." + name)!.CopyTo(stream);
        return stream.ToArray();
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = new(0, 0);
        texture.LoadImage(ReadEmbeddedFileBytes("icons." + name));
        return texture;
    }

    private static Sprite LoadSprite(string name, int width, int height) => Sprite.Create(LoadTexture(name), new Rect(0, 0, width, height), Vector2.zero);

    private static BaseUnityPlugin? _plugin;
    private static BaseUnityPlugin Plugin => _plugin ??= (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(Assembly.GetExecutingAssembly().DefinedTypes.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

    private static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description)
    {
        return global::kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem.config(group, name, value, description, true);
    }

    private sealed class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public string? Category;
    }
}
