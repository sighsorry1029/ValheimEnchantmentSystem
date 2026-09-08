using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using ItemDataManager;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class InteropRegressionTests
{
    internal static void Run(Action<bool, string> assert)
    {
        var foreign = new FakeForeignItemInfo();
        foreign.Keys.Add("existing/key");
        foreign.Keys.Add(string.Empty);
        foreign.Keys.Add("preserved");

        // This adapter path only forwards to the foreign object. Avoid constructing a game item
        // so the regression exercises the real reflection dispatch without Unity dependencies.
        var adapter = (ForeignItemInfo)FormatterServices.GetUninitializedObject(typeof(ForeignItemInfo));
        typeof(ForeignItemInfo).GetField("foreignItemInfo", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(adapter, foreign);

        assert(adapter.Remove("existing/key"), "foreign Remove returns the foreign removal result");
        assert(!foreign.Keys.Contains("existing/key"), "foreign Remove deletes the requested key");
        assert(foreign.Keys.Contains("preserved"), "foreign Remove preserves unrelated keys");
        assert(!adapter.Remove("missing"), "foreign Remove returns false for a missing key");
        assert(!foreign.Keys.Contains("missing"), "foreign Remove never creates a missing key");
        assert(adapter.Remove(), "foreign Remove forwards its default empty key");
        assert(!foreign.Keys.Contains(string.Empty), "foreign Remove removes the default empty key");
        assert(foreign.RemoveCalls == 3, "foreign Remove dispatches each call to the non-generic overload");
        assert(foreign.AddCalls == 0, "foreign Remove never calls Add");
        assert(foreign.GenericRemoveCalls == 0, "foreign Remove does not select a generic overload");
    }

    private sealed class FakeForeignItemInfo
    {
        public readonly HashSet<string> Keys = new();
        public int AddCalls;
        public int RemoveCalls;
        public int GenericRemoveCalls;

        public object Add(string key)
        {
            ++AddCalls;
            Keys.Add(key);
            return new object();
        }

        public bool Remove(string key)
        {
            ++RemoveCalls;
            return Keys.Remove(key);
        }

        public bool Remove<T>(string key)
        {
            ++GenericRemoveCalls;
            return false;
        }
    }
}
