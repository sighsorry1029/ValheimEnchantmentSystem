using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace SkillManager;

[PublicAPI]
public class Skill
{
	private static readonly Dictionary<Skills.SkillType, Skill> skills = new();
	internal static readonly Dictionary<string, Skill> skillByName = new();

	private readonly struct SavedSkillState
	{
		public readonly float Level;
		public readonly float Accumulator;

		public SavedSkillState(float level, float accumulator)
		{
			Level = level;
			Accumulator = accumulator;
		}
	}

	private readonly string skillName;
	private readonly string internalSkillName;
	private readonly Skills.SkillDef skillDef;

	public readonly LocalizeKey Name;
	public readonly LocalizeKey Description;

	public float SkillGainFactor
	{
		get => skillDef.m_increseStep;
		set
		{
			skillDef.m_increseStep = value;
			SkillGainFactorChanged?.Invoke(value);
		}
	}

	public event Action<float>? SkillGainFactorChanged;

	private float skillEffectFactor = 1;

	public float SkillEffectFactor
	{
		get => skillEffectFactor;
		set
		{
			skillEffectFactor = value;
			SkillEffectFactorChanged?.Invoke(value);
		}
	}

	public event Action<float>? SkillEffectFactorChanged;

	private int skillLoss = 5;

	public int SkillLoss
	{
		get => skillLoss;
		set
		{
			skillLoss = value;
			SkillLossChanged?.Invoke(value);
		}
	}

	public event Action<float>? SkillLossChanged;

	public bool Configurable = false;

	public Skill(string englishName, string icon) : this(englishName, loadSprite(icon, 64, 64)) { }

	public Skill(string englishName, Sprite icon)
	{
		Skills.SkillType skill = fromName(englishName);
		string sanitizedName = new Regex("[^a-zA-Z]").Replace(englishName, "_");

		skills[skill] = this;
		skillByName[englishName] = this;
		skillDef = new Skills.SkillDef
		{
			m_description = "$skilldesc_" + sanitizedName,
			m_icon = icon,
			m_increseStep = 1f,
			m_skill = skill,
		};
		internalSkillName = sanitizedName;
		skillName = englishName;

		Name = new LocalizeKey("skill_" + skill).English(englishName);
		Description = new LocalizeKey("skilldesc_" + sanitizedName);
	}

	public static Skills.SkillType fromName(string englishName) => (Skills.SkillType)Math.Abs(englishName.GetStableHashCode());

	public static class LocalizationCache
	{
	internal static void LocalizationPostfix(Localization __instance, string language) => LocalizationManager.SharedLocalizationCache.Track(__instance, language);

		public static Localization ForLanguage(string? language = null) => LocalizationManager.SharedLocalizationCache.ForLanguage(language);

		public static string LocalizeForConfig(string token, string preferredLanguage = "English") =>
			LocalizationManager.SharedLocalizationCache.LocalizeForConfig(token, preferredLanguage);
	}

	[PublicAPI]
	public class LocalizeKey
	{
		private static readonly List<LocalizeKey> keys = new();

		public readonly string Key;
		public readonly Dictionary<string, string> Localizations = new();

		public LocalizeKey(string key)
		{
			Key = key.Replace("$", "");
			keys.Add(this);
		}

		public void Alias(string alias)
		{
			Localizations.Clear();
			if (!alias.Contains("$"))
			{
				alias = $"${alias}";
			}
			Localizations["alias"] = alias;
			if (Localization.m_instance != null)
			{
				Localization.instance.AddWord(Key, Localization.instance.Localize(alias));
			}
		}

		public LocalizeKey English(string key) => addForLang("English", key);
		public LocalizeKey Swedish(string key) => addForLang("Swedish", key);
		public LocalizeKey French(string key) => addForLang("French", key);
		public LocalizeKey Italian(string key) => addForLang("Italian", key);
		public LocalizeKey German(string key) => addForLang("German", key);
		public LocalizeKey Spanish(string key) => addForLang("Spanish", key);
		public LocalizeKey Russian(string key) => addForLang("Russian", key);
		public LocalizeKey Romanian(string key) => addForLang("Romanian", key);
		public LocalizeKey Bulgarian(string key) => addForLang("Bulgarian", key);
		public LocalizeKey Macedonian(string key) => addForLang("Macedonian", key);
		public LocalizeKey Finnish(string key) => addForLang("Finnish", key);
		public LocalizeKey Danish(string key) => addForLang("Danish", key);
		public LocalizeKey Norwegian(string key) => addForLang("Norwegian", key);
		public LocalizeKey Icelandic(string key) => addForLang("Icelandic", key);
		public LocalizeKey Turkish(string key) => addForLang("Turkish", key);
		public LocalizeKey Lithuanian(string key) => addForLang("Lithuanian", key);
		public LocalizeKey Czech(string key) => addForLang("Czech", key);
		public LocalizeKey Hungarian(string key) => addForLang("Hungarian", key);
		public LocalizeKey Slovak(string key) => addForLang("Slovak", key);
		public LocalizeKey Polish(string key) => addForLang("Polish", key);
		public LocalizeKey Dutch(string key) => addForLang("Dutch", key);
		public LocalizeKey Portuguese_European(string key) => addForLang("Portuguese_European", key);
		public LocalizeKey Portuguese_Brazilian(string key) => addForLang("Portuguese_Brazilian", key);
		public LocalizeKey Chinese(string key) => addForLang("Chinese", key);
		public LocalizeKey Japanese(string key) => addForLang("Japanese", key);
		public LocalizeKey Korean(string key) => addForLang("Korean", key);
		public LocalizeKey Hindi(string key) => addForLang("Hindi", key);
		public LocalizeKey Thai(string key) => addForLang("Thai", key);
		public LocalizeKey Abenaki(string key) => addForLang("Abenaki", key);
		public LocalizeKey Croatian(string key) => addForLang("Croatian", key);
		public LocalizeKey Georgian(string key) => addForLang("Georgian", key);
		public LocalizeKey Greek(string key) => addForLang("Greek", key);
		public LocalizeKey Serbian(string key) => addForLang("Serbian", key);
		public LocalizeKey Ukrainian(string key) => addForLang("Ukrainian", key);

		private LocalizeKey addForLang(string lang, string value)
		{
			Localizations[lang] = value;
			if (Localization.m_instance != null)
			{
				if (Localization.instance.GetSelectedLanguage() == lang)
				{
					Localization.instance.AddWord(Key, value);
				}
				else if (lang == "English" && !Localization.instance.m_translations.ContainsKey(Key))
				{
					Localization.instance.AddWord(Key, value);
				}
			}

			return this;
		}

		[HarmonyPriority(Priority.LowerThanNormal)]
		internal static void AddLocalizedKeys(Localization __instance, string language)
		{
			foreach (LocalizeKey key in keys)
			{
				if (key.Localizations.TryGetValue(language, out string Translation) || key.Localizations.TryGetValue("English", out Translation))
				{
					__instance.AddWord(key.Key, Translation);
				}
				else if (key.Localizations.TryGetValue("alias", out string alias))
				{
					__instance.AddWord(key.Key, Localization.instance.Localize(alias));
				}
			}
		}
	}

	static Skill()
	{
		Harmony harmony = new("org.bepinex.helpers.skillmanager");
		harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_FejdStartup))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_ZNet_Awake))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_Awake))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.GetSkillDef)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_GetSkillDef))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.Load)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_Load_Prefix))), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_Load_Postfix))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.IsSkillValid)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_IsSkillValid))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.CheatRaiseSkill)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_CheatRaiseskill))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.CheatResetSkill)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_CheatResetSkill))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LocalizeKey), nameof(LocalizeKey.AddLocalizedKeys))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Terminal), nameof(Terminal.InitTerminal)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Terminal_InitTerminal_Prefix))), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Terminal_InitTerminal))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Skills), nameof(Skills.OnDeath)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_OnDeath_Prefix))), finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Skill), nameof(Patch_Skills_OnDeath_Finalizer))));
	}

	private class ConfigurationManagerAttributes
	{
		[UsedImplicitly] public string? Category;
	}

	private static bool configBindingsInitialized;
	private const string RequiredExperienceFormulaDescription = "Required EXP per level follows: ((current level + 1)^1.5) / 2 + 0.5.";

	private static bool IsEnchantmentSkill(Skill skill) => string.Equals(skill.internalSkillName, "kg_Enchantment", StringComparison.Ordinal);

	private static string GetConfigGroup(Skill skill) => IsEnchantmentSkill(skill) ? "Enchantment" : skill.internalSkillName;

	private static string GetConfigCategory(Skill skill, string localizedName) => IsEnchantmentSkill(skill) ? "Enchantment" : localizedName;

	private static string GetSkillKeyDescription(string skillKey) => $"Skill key: {skillKey}.";

	private static string GetLocalizedDisplayName(string token, string fallback)
	{
		if (Localization.instance == null)
		{
			return fallback;
		}

		string localized = Localization.instance.Localize(token).Trim();
		return string.IsNullOrWhiteSpace(localized) ? fallback : localized;
	}

	private static void Patch_FejdStartup() => EnsureConfigBindings();

	private static void Patch_ZNet_Awake() => EnsureConfigBindings();

	private static void Patch_Skills_Awake(Skills __instance) => EnsureSkillDefinitions(__instance);

	private static void EnsureSkillDefinitions(Skills? skillComponent)
	{
		if (skillComponent == null)
		{
			return;
		}

		foreach (Skill customSkill in skills.Values)
		{
			if (skillComponent.m_skills.Any(skillDef => skillDef?.m_skill == customSkill.skillDef.m_skill))
			{
				continue;
			}

			skillComponent.m_skills.Add(customSkill.skillDef);
		}
	}

	private static Dictionary<Skills.SkillType, SavedSkillState>? CaptureCustomSkillStates(ZPackage? pkg)
	{
		if (pkg == null || skills.Count == 0)
		{
			return null;
		}

		try
		{
			ZPackage copy = new(pkg.GetArray());
			copy.SetPos(pkg.GetPos());

			int version = copy.ReadInt();
			int count = copy.ReadInt();
			Dictionary<Skills.SkillType, SavedSkillState>? capturedStates = null;
			for (int i = 0; i < count; i++)
			{
				Skills.SkillType skillType = (Skills.SkillType)copy.ReadInt();
				float level = copy.ReadSingle();
				float accumulator = version >= 2 ? copy.ReadSingle() : 0f;

				if (!skills.ContainsKey(skillType))
				{
					continue;
				}

				capturedStates ??= new Dictionary<Skills.SkillType, SavedSkillState>();
				capturedStates[skillType] = new SavedSkillState(level, accumulator);
			}

			return capturedStates;
		}
		catch
		{
			return null;
		}
	}

	private static void RestoreCustomSkillStates(Skills? skillComponent, Dictionary<Skills.SkillType, SavedSkillState>? capturedStates)
	{
		if (skillComponent == null || capturedStates == null || capturedStates.Count == 0)
		{
			return;
		}

		EnsureSkillDefinitions(skillComponent);

		foreach (KeyValuePair<Skills.SkillType, SavedSkillState> entry in capturedStates)
		{
			Skills.Skill skill = skillComponent.GetSkill(entry.Key);
			skill.m_level = entry.Value.Level;
			skill.m_accumulator = entry.Value.Accumulator;
		}
	}

	private static void Patch_Skills_Load_Prefix(Skills __instance, ZPackage pkg, out Dictionary<Skills.SkillType, SavedSkillState>? __state)
	{
		EnsureSkillDefinitions(__instance);
		__state = CaptureCustomSkillStates(pkg);
	}

	private static void Patch_Skills_Load_Postfix(Skills __instance, Dictionary<Skills.SkillType, SavedSkillState>? __state)
	{
		RestoreCustomSkillStates(__instance, __state);
	}

	private static void EnsureConfigBindings()
	{
		if (configBindingsInitialized)
		{
			return;
		}

		foreach (Skill skill in skills.Values)
		{
			if (skill.Configurable)
			{
				string nameKey = skill.Name.Key;
				string englishName = new Regex("['[\"\\]]").Replace(LocalizationCache.LocalizeForConfig(nameKey), "").Trim();
				string localizedName = GetLocalizedDisplayName(nameKey, englishName);
				string configGroup = GetConfigGroup(skill);
				string configCategory = GetConfigCategory(skill, localizedName);

				ConfigEntry<float> skillGain = config(configGroup, "Skill gain factor", skill.SkillGainFactor, new ConfigDescription($"The rate at which you gain experience for the skill. {GetSkillKeyDescription(nameKey)} {RequiredExperienceFormulaDescription}", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Category = configCategory }));
				skill.SkillGainFactor = skillGain.Value;
				skillGain.SettingChanged += (_, _) => skill.SkillGainFactor = skillGain.Value;

				if (!IsEnchantmentSkill(skill))
				{
					ConfigEntry<float> skillEffect = config(configGroup, "Skill effect factor", skill.SkillEffectFactor, new ConfigDescription("The power of the skill, based on the default power.", new AcceptableValueRange<float>(0.01f, 5f), new ConfigurationManagerAttributes { Category = configCategory }));
					skill.SkillEffectFactor = skillEffect.Value;
					skillEffect.SettingChanged += (_, _) => skill.SkillEffectFactor = skillEffect.Value;
				}

				ConfigEntry<int> skillLoss = config(configGroup, "Skill loss", skill.skillLoss, new ConfigDescription($"How much experience to lose on death. {GetSkillKeyDescription(nameKey)} {RequiredExperienceFormulaDescription}", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Category = configCategory }));
				skill.skillLoss = skillLoss.Value;
				skillLoss.SettingChanged += (_, _) => skill.skillLoss = skillLoss.Value;
			}
		}

		configBindingsInitialized = true;
	}

	private static void Patch_Skills_GetSkillDef(ref Skills.SkillDef? __result, List<Skills.SkillDef> ___m_skills, Skills.SkillType type)
	{
		if (__result is null && GetSkillDef(type) is { } skillDef)
		{
			___m_skills.Add(skillDef);
			__result = skillDef;
		}
	}

	private static bool Patch_Skills_CheatRaiseskill(Skills __instance, string name, float value, Player ___m_player)
	{
		foreach (Skills.SkillType id in skills.Keys)
		{
			Skill skillDetails = skills[id];

			if (string.Equals(skillDetails.internalSkillName, name, StringComparison.CurrentCultureIgnoreCase))
			{
				Skills.Skill skill = __instance.GetSkill(id);
				skill.m_level += value;
				skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
				___m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + Localization.instance.Localize("$skill_" + id) + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
				Console.instance.Print("Skill " + skillDetails.internalSkillName + " = " + skill.m_level);
				return false;
			}
		}
		return true;
	}

	private static bool Patch_Skills_CheatResetSkill(Skills __instance, string name)
	{
		foreach (Skills.SkillType id in skills.Keys)
		{
			Skill skillDetails = skills[id];

			if (string.Equals(skillDetails.internalSkillName, name, StringComparison.CurrentCultureIgnoreCase))
			{
				__instance.ResetSkill(id);
				Console.instance.Print("Skill " + skillDetails.internalSkillName + " reset");
				return false;
			}
		}
		return true;
	}

	private static void Patch_Skills_OnDeath_Prefix(Skills __instance, ref Dictionary<Skills.SkillType, Skills.Skill>? __state)
	{
		__state ??= new Dictionary<Skills.SkillType, Skills.Skill>();
		foreach (KeyValuePair<Skills.SkillType, Skill> kv in skills)
		{
			if (__instance.m_skillData.TryGetValue(kv.Key, out Skills.Skill skill))
			{
				__state[kv.Key] = skill;
				if (kv.Value.skillLoss > 0)
				{
					skill.m_level -= skill.m_level * kv.Value.SkillLoss / 100f;
					skill.m_accumulator = 0.0f;
				}
				__instance.m_skillData.Remove(kv.Key);
			}
		}
	}

	private static void Patch_Skills_OnDeath_Finalizer(Skills __instance, ref Dictionary<Skills.SkillType, Skills.Skill>? __state)
	{
		if (__state is not null)
		{
			foreach (KeyValuePair<Skills.SkillType, Skills.Skill> kv in __state)
			{
				__instance.m_skillData[kv.Key] = kv.Value;
			}
			__state = null;
		}
	}

	private static bool InitializedTerminal = false;
	private static void Patch_Terminal_InitTerminal_Prefix() => InitializedTerminal = Terminal.m_terminalInitialized;

	private static void Patch_Terminal_InitTerminal()
	{
		if (InitializedTerminal)
		{
			return;
		}

		void AddSkill(Terminal.ConsoleCommand command)
		{
			Terminal.ConsoleOptionsFetcher fetcher = command.m_tabOptionsFetcher;
			command.m_tabOptionsFetcher = () =>
			{
				List<string> options = fetcher();
				options.AddRange(skills.Values.Select(skill => skill.internalSkillName));
				return options;
			};
		}

		AddSkill(Terminal.commands["raiseskill"]);
		AddSkill(Terminal.commands["resetskill"]);
	}

	private static Skills.SkillDef? GetSkillDef(Skills.SkillType skillType)
	{
		if (!skills.ContainsKey(skillType))
		{
			return null;
		}

		Skill skillDetails = skills[skillType];

		return skillDetails.skillDef;
	}

	private static void Patch_Skills_IsSkillValid(Skills.SkillType type, ref bool __result)
	{
		if (__result)
		{
			return;
		}

		if (skills.ContainsKey(type))
		{
			__result = true;
		}
	}

	private static byte[] ReadEmbeddedFileBytes(string name)
	{
		using MemoryStream stream = new();
		Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + "." + name)!.CopyTo(stream);
		return stream.ToArray();
	}

	private static Texture2D loadTexture(string name)
	{
		Texture2D texture = new(0, 0);
		texture.LoadImage(ReadEmbeddedFileBytes("icons." + name));
		return texture;
	}

	private static Sprite loadSprite(string name, int width, int height) => Sprite.Create(loadTexture(name), new Rect(0, 0, width, height), Vector2.zero);

	private static BaseUnityPlugin? _plugin;
	private static BaseUnityPlugin plugin => _plugin ??= (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(Assembly.GetExecutingAssembly().DefinedTypes.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

	private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
	{
		return global::kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem.config(group, name, value, description, true);
	}

	private static ConfigEntry<T> config<T>(string group, string name, T value, string description) => config(group, name, value, new ConfigDescription(description));
}

public static class SkillManagerVersion
{
	public const string Version = "1.7.0";
}

[PublicAPI]
public static class SkillExtensions
{
	public static float GetSkillFactor(this Character character, string name) => character.GetSkillFactor(Skill.fromName(name)) * Skill.skillByName[name].SkillEffectFactor;
	public static float GetSkillFactor(this Skills skills, string name) => skills.GetSkillFactor(Skill.fromName(name)) * Skill.skillByName[name].SkillEffectFactor;
	public static void RaiseSkill(this Character character, string name, float value = 1f) => character.RaiseSkill(Skill.fromName(name), value);
	public static void RaiseSkill(this Skills skill, string name, float value = 1f) => skill.RaiseSkill(Skill.fromName(name), value);

	public static void LowerSkill(this Character character, string name, float factor = 1f) => character.GetSkills().LowerSkill(name, factor);

	public static void LowerSkill(this Skills skills, string name, float factor)
	{
		if (factor > 0 && skills.m_skillData.TryGetValue(Skill.fromName(name), out Skills.Skill skill))
		{
			skill.m_level -= skill.m_level * factor;
			skill.m_accumulator = 0.0f;
		}
	}
}
