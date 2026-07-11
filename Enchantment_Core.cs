using System.Reflection.Emit;
using System.Text.RegularExpressions;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;

namespace kg.ValheimEnchantmentSystem;

public static class Enchantment_Core
{
    private static readonly Regex ItemDurabilityRegex = new("(\\$item_durability.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryDamageRegex = new("(\\$inventory_damage.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryBluntRegex = new("(\\$inventory_blunt.*)", RegexOptions.Compiled);
    private static readonly Regex InventorySlashRegex = new("(\\$inventory_slash.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryPierceRegex = new("(\\$inventory_pierce.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryFireRegex = new("(\\$inventory_fire.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryFrostRegex = new("(\\$inventory_frost.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryLightningRegex = new("(\\$inventory_lightning.*)", RegexOptions.Compiled);
    private static readonly Regex InventoryPoisonRegex = new("(\\$inventory_poison.*)", RegexOptions.Compiled);
    private static readonly Regex InventorySpiritRegex = new("(\\$inventory_spirit.*)", RegexOptions.Compiled);
    private static readonly Regex ItemBlockArmorRegex = new("(\\$item_blockarmor.*)", RegexOptions.Compiled);
    private static readonly Regex ItemArmorRegex = new("(\\$item_armor.*)", RegexOptions.Compiled);

    internal static void Initialize()
    {
        if (ValheimEnchantmentSystem.NoGraphics) return;
        AnimationSpeedManager.Add(ModifyAttackSpeed);
    }
    
    public static IEnumerator FrameSkipEquip(ItemDrop.ItemData weapon)
    {
        if (!Player.m_localPlayer.IsItemEquiped(weapon) || !weapon.IsWeapon()) yield break;
        Player.m_localPlayer.UnequipItem(weapon);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (Player.m_localPlayer && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
            Player.m_localPlayer?.EquipItem(weapon);
    }

    public class Enchanted : ItemData
    {
        public int level;

        public SyncedData.Stat_Data Stats => SyncedData.GetStatIncrease(this);

        public override void Save()
        {
            Value = level.ToString();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(Value)) return;
            level = int.TryParse(Value, out int lvl) ? lvl : 0;
        }

        public override void Upgraded()
        {
            EnchantmentSideEffects.HandleUpgrade(this);
        }

        public float GetEnchantmentChance()
        {
            return SyncedData.GetEnchantmentChance(this).success;
        }

        public EnchantmentResult Enchant(Player player, bool useBlessedScroll, bool blessedScrollPreventsBreak)
        {
            return EnchantmentService.Execute(this, player, useBlessedScroll, blessedScrollPreventsBreak);
        }
        public static implicit operator bool(Enchanted en) => en != null;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    [ClientOnlyPatch]
    private static class InventoryGrid_CreateItemTooltip_Patch
    {
        [UsedImplicitly]
        private static void Prefix(InventoryGrid __instance, ItemDrop.ItemData item, out string __state)
        {
            __state = null;
            if (item?.Data().Get<Enchanted>() is not { level: > 0 } idm) return;
            __state = item.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true).IncreaseColorLight();
            item.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
        }

        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance, ItemDrop.ItemData item, string __state)
        {
            if (__state != null) item.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    [ClientOnlyPatch]
    private static class ItemDrop_GetHoverText_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop __instance, out string __state)
        {
            __state = null;
            if (__instance.m_itemData?.Data().Get<Enchanted>() is not { level: > 0 } idm) return;
            __state = __instance.m_itemData.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true)
                .IncreaseColorLight();
            __instance.m_itemData.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    [ClientOnlyPatch]
    public class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, int qualityLevel, ref string __result)
        {
            Enchanted en = item.Data().Get<Enchanted>();
            int currentLevel = en ? en.level : 0;
            string dropName = item.m_dropPrefab ? item.m_dropPrefab.name : Utils.GetPrefabNameByItemName(item.m_shared.m_name);
            var reqs = SyncedData.GetReqs(dropName);
            string statusLine = BuildTooltipStatusLine(reqs, dropName, currentLevel, item);

            if (currentLevel > 0)
            {
                SyncedData.Stat_Data stats = SyncedData.GetStatIncrease(en);
                string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
                
                if (stats != null)
                {
                    int damagePercent = stats.damage_percentage;
                    if (stats.durability > 0)
                        __result = ItemDurabilityRegex.Replace(__result, $"$1 (<color={color}>+{stats.durability}</color>)");
                    if (stats.durability_percentage > 0)
                        __result = ItemDurabilityRegex.Replace(__result, $"$1 (<color={color}>+{stats.durability_percentage}%</color>)");

                    __result += "\n";
                    
                    if (damagePercent > 0)
                    {
                        Player.m_localPlayer.GetSkills().GetRandomSkillRange(out float minFactor, out float maxFactor, item.m_shared.m_skillType);
                        HitData.DamageTypes damage = item.GetDamage(qualityLevel, item.m_worldLevel);
                        __result = InventoryDamageRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_damage * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_damage * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryBluntRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_blunt * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_blunt * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventorySlashRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_slash * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_slash * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryPierceRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_pierce * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_pierce * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryFireRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_fire * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_fire * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryFrostRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_frost * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_frost * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryLightningRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_lightning * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_lightning * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventoryPoisonRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_poison * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_poison * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = InventorySpiritRegex.Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_spirit * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_spirit * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result += $"\n<color={color}>•</color> $enchantment_bonusespercentdamage (<color={color}>+{damagePercent}%</color>)";
                    }
                    float armorPercent = stats.armor_percentage;
                    if (armorPercent > 0)
                    {
                        __result = ItemBlockArmorRegex.Replace(__result, $"$1 (<color={color}>+{(item.GetBaseBlockPower(qualityLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%)</color>)");
                        __result = ItemArmorRegex.Replace(__result, $"$1 (<color={color}>+{(item.GetArmor(qualityLevel, item.m_worldLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%)</color>)");
                        __result += $"\n<color={color}>•</color> $enchantment_bonusespercentarmor (<color={color}>+{armorPercent}%</color>)";
                    }
                    float armor = stats.armor;
                    if (armor > 0)
                    {
                        __result = ItemBlockArmorRegex.Replace(__result, $"$1 (<color={color}>+{stats.armor}</color>)");
                        __result = ItemArmorRegex.Replace(__result, $"$1 (<color={color}>+{stats.armor}</color>)");
                    }

                    __result += EnchantmentStatFormatter.BuildAdditionalStats(stats, color);
                    if (!string.IsNullOrWhiteSpace(statusLine))
                    {
                        __result += statusLine;
                    }
                }
            }

            if (currentLevel <= 0 && !string.IsNullOrWhiteSpace(statusLine))
            {
                __result += $"\n{statusLine}";
            }
        }
    }

    private static string BuildTooltipStatusLine(SyncedData.EnchantmentReqs reqs, string dropName, int currentLevel, ItemDrop.ItemData item)
    {
        if (reqs == null || item == null)
        {
            return string.Empty;
        }

        string color = SyncedData.GetColor(dropName, currentLevel, out _, true).IncreaseColorLight();
        if (currentLevel == 0)
        {
            color = "white";
        }

        if (!SyncedData.IsLevelEnchantable(dropName, currentLevel, item.IsWeapon()))
        {
            return $"<color={color}>•</color> $enchantment_maxedout".Localize();
        }

        string scrollName = ResolveTooltipScrollName(reqs.enchant_prefab);
        if (string.IsNullOrWhiteSpace(scrollName))
        {
            scrollName = ResolveTooltipScrollName(reqs.blessed_enchant_prefab);
        }

        return string.IsNullOrWhiteSpace(scrollName)
            ? string.Empty
            : $"<color={color}>•</color> $enchantment_canbeenchantedwith <color=yellow>{scrollName}</color>".Localize();
    }

    private static string ResolveTooltipScrollName(SyncedData.SingleReq requirement)
    {
        if (requirement == null || !requirement.IsValid())
        {
            return string.Empty;
        }

        return ZNetScene.instance?.GetPrefab(requirement.prefab)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name ?? string.Empty;
    }
 
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [ClientOnlyPatch]
    private static class InventoryGui_UpdateRecipe_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            Enchanted en = __instance.m_selectedRecipe.ItemData?.Data().Get<Enchanted>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            __instance.m_recipeName.text += $" (<color={color}>+{en!.level}</color>)";
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
    [ClientOnlyPatch]
    private static class InventoryGui_AddRecipeToList_Patch
    {
        private static void Modify(ref string text, ItemDrop.ItemData item)
        {
            Enchanted en = item?.Data().Get<Enchanted>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            text += $" (<color={color}>+{en!.level}</color>)";
        }

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            CodeMatcher matcher = new(code);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_2));
            if (matcher.IsInvalid) return matcher.InstructionEnumeration();
            MethodInfo method = AccessTools.Method(typeof(InventoryGui_AddRecipeToList_Patch), nameof(Modify));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, 2))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, method));
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    [ClientOnlyPatch]
    private static class ModifyBlockPower
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyArmor
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyDamage
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                ApplyDamageStats(ref __result, stats);
            }
        }
    }

    internal static bool HasDamageStats(SyncedData.Stat_Data stats)
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

    internal static void ApplyDamageStats(ref HitData.DamageTypes damage, SyncedData.Stat_Data stats)
    {
        damage.Modify(1 + stats.damage_percentage / 100f);
        damage.m_blunt += stats.damage_blunt;
        damage.m_slash += stats.damage_slash;
        damage.m_pierce += stats.damage_pierce;
        damage.m_fire += stats.damage_fire;
        damage.m_frost += stats.damage_frost;
        damage.m_lightning += stats.damage_lightning;
        damage.m_poison += stats.damage_poison;
        damage.m_spirit += stats.damage_spirit;
        damage.m_damage += stats.damage_true;
        damage.m_chop += stats.damage_chop;
        damage.m_pickaxe += stats.damage_pickaxe;
    }
    
    [HarmonyPatch(typeof(Player),nameof(Player.ApplyArmorDamageMods))]
    [ClientOnlyPatch]
    private static class Player_ApplyArmorDamageMods_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            EquippedEnchantmentSnapshotService.Snapshot snapshot = EquippedEnchantmentSnapshotService.GetSnapshot(__instance);
            if (snapshot.ResistancePairs.Count > 0)
            {
                mods.Apply(snapshot.ResistancePairs);
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
    [ClientOnlyPatch]
    private static class Character_SetMaxHealth_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, ref float health)
        {
            if (__instance is Player player)
            {
                health += EquippedEnchantmentSnapshotService.GetSnapshot(player).MaxHealthBonus;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetMaxStamina))]
    [ClientOnlyPatch]
    private static class Player_SetMaxStamina_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Player __instance, ref float stamina)
        {
            stamina += EquippedEnchantmentSnapshotService.GetSnapshot(__instance).MaxStaminaBonus;
        }
    }

    [HarmonyPatch(typeof(Player),nameof(Player.GetEquipmentMovementModifier))]
    [ClientOnlyPatch] 
    private static class Player_UpdateMovementModifier_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref float __result)
        {
            __result += EquippedEnchantmentSnapshotService.GetSnapshot(__instance).MovementModifier;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    [ClientOnlyPatch]
    public class ApplySkillToDurability
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                __result *= 1 + stats.durability_percentage / 100f;
                __result += stats.durability;
            }
        }
    }

    private static double ModifyAttackSpeed(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;
    
        ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
        if (weapon == null) return speed;
        
        if (weapon.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is { attack_speed: > 0} stats)
            return speed * (1 + stats.attack_speed / 100f);
        
        return speed;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
    [ClientOnlyPatch]
    public static class Player_FixedUpdate_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer && !__instance.IsDead())
            {
                float fixedDeltaTime = Time.fixedDeltaTime;
                __instance.UpdateEnchantmentRegen(fixedDeltaTime);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    [ClientOnlyPatch]
    public static class Player_UpdateStats_Patch
    {
        private static readonly FieldInfo CharacterNViewField = AccessTools.Field(typeof(Character), nameof(Character.m_nview));
        private static readonly MethodInfo ZNetViewGetZdoMethod = AccessTools.Method(typeof(ZNetView), nameof(ZNetView.GetZDO));
        private static readonly FieldInfo ZdoVarsStaminaField = AccessTools.Field(typeof(ZDOVars), nameof(ZDOVars.s_stamina));
        private static readonly FieldInfo PlayerStaminaField = AccessTools.Field(typeof(Player), nameof(Player.m_stamina));
        private static readonly MethodInfo ZdoSetFloatMethod = AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), new[] { typeof(int), typeof(float) });
        private static readonly MethodInfo ApplyEnchantmentStaminaRegenMethod = AccessTools.DeclaredMethod(typeof(PlayerExtensions), nameof(PlayerExtensions.UpdateEnchantmentStaminaRegen));

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, CharacterNViewField),
                new CodeMatch(OpCodes.Callvirt, ZNetViewGetZdoMethod),
                new CodeMatch(OpCodes.Ldsfld, ZdoVarsStaminaField),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, PlayerStaminaField),
                new CodeMatch(OpCodes.Callvirt, ZdoSetFloatMethod));

            if (matcher.IsInvalid)
            {
                Utils.print("Failed to inject enchantment stamina regen into Player.UpdateStats; using vanilla stamina regen only.", ConsoleColor.Yellow);
                return instructions;
            }

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, ApplyEnchantmentStaminaRegenMethod));

            return matcher.InstructionEnumeration();
        }
    }
}

public static class PlayerExtensions
{
    private static float enchantmentRegenTimer = 0f;

    public static void UpdateEnchantmentRegen(this Player player, float dt)
    {
        enchantmentRegenTimer += dt;
        if (enchantmentRegenTimer >= 10f)
        {
            enchantmentRegenTimer = 0f;
            float regen = EquippedEnchantmentSnapshotService.GetSnapshot(player).HealthRegen;
            if (regen > 0)
            {
                player.Heal(regen);
            }
        }
    }

    public static void UpdateEnchantmentStaminaRegen(this Player player, float dt)
    {
        if (player != Player.m_localPlayer)
        {
            return;
        }

        if (player.IsDead())
        {
            return;
        }

        bool flag = player.IsEncumbered();
        float maxStamina = player.GetMaxStamina();
        float num = 1f;
        if (player.IsBlocking())
        {
            num *= 0.8f;
        }
        if ((player.IsSwimming() && !player.IsOnGround()) || player.InAttack() || player.InDodge() || player.m_wallRunning || flag)
        {
            num = 0f;
        }

        float additionalRegen = EquippedEnchantmentSnapshotService.GetSnapshot(player).StaminaRegen;

        if (additionalRegen > 0f)
        {
            float staminaMultiplier = 1f;
            player.m_seman.ModifyStaminaRegen(ref staminaMultiplier);
            float regenAmount = additionalRegen * staminaMultiplier * num * dt;

            player.m_stamina = Mathf.Min(maxStamina, player.m_stamina + regenAmount * Game.m_staminaRegenRate);
        }
    }
}

