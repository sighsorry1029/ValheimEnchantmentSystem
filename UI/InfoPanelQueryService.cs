using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Items_Structures;

namespace kg.ValheimEnchantmentSystem.UI;

internal static class InfoPanelQueryService
{
    public static List<InfoPanelEntryModel> GetEntries(InfoPanelCategory category, string? searchText)
    {
        if (!Player.m_localPlayer)
        {
            return new List<InfoPanelEntryModel>();
        }

        return category switch
        {
            InfoPanelCategory.Reqs => GetRequirementEntries(searchText),
            InfoPanelCategory.Stats => GetStatEntries(searchText),
            InfoPanelCategory.Chances => GetChanceEntries(searchText),
            _ => new List<InfoPanelEntryModel>()
        };
    }

    private static List<InfoPanelEntryModel> GetRequirementEntries(string? searchText)
    {
        List<InfoPanelEntryModel> entries = new();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            entries.Add(CreateGuideEntry());
        }

        foreach (SyncedData.EnchantmentReqs req in SyncedData.Synced_EnchantmentReqs.Value)
        {
            string? found = null;
            if (!string.IsNullOrWhiteSpace(searchText) && !HasAny(req.Items, searchText, out found))
            {
                continue;
            }

            InfoPanelEntryModel? entry = CreateEntry(req.Items, GenerateReqsText(req), found, null);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<InfoPanelEntryModel> GetStatEntries(string? searchText)
    {
        List<InfoPanelEntryModel> entries = new();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AddIfNotNull(entries, CreateEntry(null, GenerateStatsText(SyncedData.Synced_EnchantmentStats_Weapons.Value), null, "$enchantment_defaultstats_weapon".Localize()));
            AddIfNotNull(entries, CreateEntry(null, GenerateStatsText(SyncedData.Synced_EnchantmentStats_Armor.Value), null, "$enchantment_defaultstats_armor".Localize()));
        }

        foreach (SyncedData.OverrideStats stat in SyncedData.Overrides_EnchantmentStats.Value)
        {
            string? found = null;
            if (!string.IsNullOrWhiteSpace(searchText) && !HasAny(stat.Items, searchText, out found))
            {
                continue;
            }

            string text = GenerateStatsText(stat.Stats);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            AddIfNotNull(entries, CreateEntry(stat.Items, text, found, null));
        }

        return entries;
    }

    private static List<InfoPanelEntryModel> GetChanceEntries(string? searchText)
    {
        List<InfoPanelEntryModel> entries = new();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AddIfNotNull(entries, CreateEntry(null, GenerateChancesText(SyncedData.Synced_EnchantmentChances_Weapons.Value), null, "$enchantment_defaultchances_weapon".Localize()));
            AddIfNotNull(entries, CreateEntry(null, GenerateChancesText(SyncedData.Synced_EnchantmentChances_Armor.Value), null, "$enchantment_defaultchances_armor".Localize()));
        }

        foreach (SyncedData.OverrideChances chance in SyncedData.Overrides_EnchantmentChances.Value)
        {
            string? found = null;
            if (!string.IsNullOrWhiteSpace(searchText) && !HasAny(chance.Items, searchText, out found))
            {
                continue;
            }

            string text = GenerateChancesText(chance.Chances);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            AddIfNotNull(entries, CreateEntry(chance.Items, text, found, null));
        }

        return entries;
    }

    private static void AddIfNotNull(List<InfoPanelEntryModel> entries, InfoPanelEntryModel? entry)
    {
        if (entry != null)
        {
            entries.Add(entry);
        }
    }

    private static InfoPanelEntryModel CreateGuideEntry()
    {
        return new InfoPanelEntryModel
        {
            AdditionalText = "$enchantment_info_guide".Localize(),
            BodyText = string.Join(
                "\n",
                "• $enchantment_info_guide_search".Localize(),
                "• $enchantment_info_guide_tabs".Localize(),
                $"• {ScrollCombineService.GetCombineInstructionText()}"),
            StartExpanded = true
        };
    }

    private static InfoPanelEntryModel? CreateEntry(IEnumerable<string>? prefabs, string text, string? found, string? additionalText)
    {
        InfoPanelEntryModel entry = new()
        {
            BodyText = text,
            AdditionalText = additionalText
        };

        if (prefabs != null)
        {
            entry.Icons.AddRange(BuildIcons(prefabs, found));
        }

        if (entry.Icons.Count == 0 && string.IsNullOrWhiteSpace(entry.AdditionalText))
        {
            return null;
        }

        return entry;
    }

    private static IEnumerable<InfoPanelIconModel> BuildIcons(IEnumerable<string> prefabs, string? found)
    {
        foreach (string prefab in prefabs)
        {
            GameObject itemPrefab = ZNetScene.instance?.GetPrefab(prefab);
            if (itemPrefab?.GetComponent<ItemDrop>() is not { } item)
            {
                continue;
            }

            string itemName = item.m_itemData.m_shared.m_name;
            if (!Player.m_localPlayer.m_knownRecipes.Contains(itemName) && !Player.m_localPlayer.m_knownMaterial.Contains(itemName))
            {
                continue;
            }

            yield return new InfoPanelIconModel
            {
                Icon = item.m_itemData.GetIcon(),
                TooltipTopic = itemName.Localize(),
                TooltipText = item.m_itemData.GetTooltip(),
                Highlight = string.Equals(found, prefab, StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    private static bool HasAny(IEnumerable<string> list, string search, out string? found)
    {
        string normalizedSearch = NormalizeSearch(search);
        foreach (string prefab in list)
        {
            if (NormalizeSearch(prefab).Contains(normalizedSearch))
            {
                found = prefab;
                return true;
            }

            GameObject tryFind = ZNetScene.instance?.GetPrefab(prefab);
            if (tryFind?.GetComponent<ItemDrop>() is not { } item)
            {
                continue;
            }

            if (NormalizeSearch(item.m_itemData.m_shared.m_name.Localize()).Contains(normalizedSearch))
            {
                found = prefab;
                return true;
            }
        }

        found = null;
        return false;
    }

    private static string NormalizeSearch(string value)
    {
        return (value ?? string.Empty).ToLowerInvariant().Replace(" ", string.Empty);
    }

    private static string GenerateReqsText(SyncedData.EnchantmentReqs reqs)
    {
        string result = "• $enchantment_canbeenchantedwith:";
        if (reqs.enchant_prefab.IsValid() && ZNetScene.instance?.GetPrefab(reqs.enchant_prefab.prefab)?.GetComponent<ItemDrop>() is { } mainItem)
        {
            result += $"\n<color=yellow>• {mainItem.m_itemData.m_shared.m_name}</color>";
        }

        if (reqs.blessed_enchant_prefab.IsValid() && ZNetScene.instance?.GetPrefab(reqs.blessed_enchant_prefab.prefab)?.GetComponent<ItemDrop>() is { } blessItem)
        {
            result += $"\n<color=yellow>• {blessItem.m_itemData.m_shared.m_name}</color>";
        }

        return result.Localize();
    }

    private static string GenerateStatsText(Dictionary<int, SyncedData.Stat_Data> stats)
    {
        string result = string.Empty;
        foreach (KeyValuePair<int, SyncedData.Stat_Data> stat in stats.OrderBy(x => x.Key))
        {
            result += $"<color=yellow>• lvl{stat.Key}:</color>";
            result += EnchantmentStatFormatter.BuildInfoDescription(stat.Value);
        }

        return result.Localize().Trim();
    }

    private static string GenerateChancesText(Dictionary<int, SyncedData.Chance_Data> chances)
    {
        string result = string.Empty;
        foreach (KeyValuePair<int, SyncedData.Chance_Data> chance in chances.OrderBy(x => x.Key))
        {
            if (chance.Value.success < 0 || chance.Value.destroy < 0)
            {
                continue;
            }

            string success = FormatChance(chance.Value.success);
            string destroy = chance.Value.destroy > 0
                ? $", $enchantment_destroychance: {FormatChance(chance.Value.destroy)}%".Localize()
                : string.Empty;
            result += $"<color=yellow>• lvl{chance.Key}:</color> {success}%{destroy}\n";
        }

        return result.Trim();
    }

    private static string FormatChance(float chance)
    {
        return Math.Round(chance, 2, MidpointRounding.AwayFromZero).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
