using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Utils = kg.ValheimEnchantmentSystem.Utils;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class OutputTransferRegressionTests
{
    internal static void Run(Action<bool, string> assert)
    {
        ItemDrop.ItemData target = CreateStack("Scroll", 1, 0, 8);
        target.m_customData["externalMeta"] = "retained";
        var inventory = new List<ItemDrop.ItemData>
        {
            target,
            CreateStack("Scroll", 2, 0, 30),
            CreateStack("Scroll", 1, 1, 40),
            CreateStack("OtherScroll", 1, 0, 50)
        };

        long Count() => Utils.CountOutputTransferItems(inventory, "Scroll", 1, 0);
        assert(Count() == 8, "output observation follows shared name, quality, and world level");

        bool known = Utils.TryAddItemAndGetRemainder(3, Count, () =>
        {
            // ItemDataManager can allow a merge while metadata belonging to another mod remains on the target.
            target.m_stack += 3;
            return true;
        }, out int remaining, out _);
        assert(known && remaining == 0, "a full merge into a stack with foreign metadata leaves no world duplicate");
        assert(target.m_customData["externalMeta"] == "retained", "observing a transfer leaves foreign metadata unchanged");

        known = Utils.TryAddItemAndGetRemainder(4, Count, () =>
        {
            target.m_stack += 2;
            // TryStack may return a newly merged serialized value rather than either original value.
            target.m_customData["modGuid#MergedValue"] = "new merged value";
            return false;
        }, out remaining, out _);
        assert(known && remaining == 2, "a partial merge with new custom values preserves only the real world remainder");

        known = Utils.TryAddItemAndGetRemainder(2, Count, () =>
        {
            target.m_customData["externalMeta"] = "metadata-only update";
            return false;
        }, out remaining, out _);
        assert(known && remaining == 2, "metadata changes alone cannot count as output transfer");
    }

    private static ItemDrop.ItemData CreateStack(string name, int quality, int worldLevel, int stack)
    {
        // ItemData's constructor reads Game.m_worldLevel. Populate just the managed fields used by the real observer.
        var item = (ItemDrop.ItemData)FormatterServices.GetUninitializedObject(typeof(ItemDrop.ItemData));
        item.m_shared = (ItemDrop.ItemData.SharedData)FormatterServices.GetUninitializedObject(typeof(ItemDrop.ItemData.SharedData));
        item.m_shared.m_name = name;
        item.m_quality = quality;
        item.m_worldLevel = worldLevel;
        item.m_stack = stack;
        item.m_customData = new Dictionary<string, string>();
        return item;
    }
}
