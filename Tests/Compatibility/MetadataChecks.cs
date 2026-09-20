using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class MetadataChecks
{
    internal static int Run()
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(AppContext.BaseDirectory);
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        var parameters = new ReaderParameters { AssemblyResolver = resolver };
        using var mod = ModuleDefinition.ReadModule(Path.Combine(AppContext.BaseDirectory, "kg.ValheimEnchantmentSystem.dll"), parameters);
        int failures = 0, references = 0, bindings = 0;
        void Fail(string message) { ++failures; System.Console.Error.WriteLine(message); }
        CheckPluginLoadOrder(mod, Fail);
        bool GameType(TypeReference t) => t.Scope.Name.StartsWith("assembly_") || t.Scope.Name.StartsWith("Unity") || t.Scope.Name.StartsWith("SoftReferenceable");
        void CheckReferences(ModuleDefinition module)
        {
            foreach (var member in module.GetMemberReferences().Where(m => GameType(m.DeclaringType)))
            {
                try
                {
                    ++references;
                    if (member is MethodReference m && m.Resolve() == null) Fail("Unresolved method: " + m);
                    if (member is FieldReference f)
                    {
                        var field = f.Resolve();
                        if (field == null) Fail("Unresolved field: " + f);
                        else if (field.IsLiteral) Fail("Literal field still referenced as storage: " + f);
                    }
                }
                catch (Exception e) { Fail(member + ": " + e.Message); }
            }
        }
        CheckReferences(mod);
        foreach (var resource in mod.Resources.OfType<EmbeddedResource>().Where(r => r.Name.EndsWith("VES_Scripts.dll")))
        {
            using var embedded = ModuleDefinition.ReadModule(new MemoryStream(resource.GetResourceData()), parameters);
            CheckReferences(embedded);
        }
        // Resolve all cached FieldRef bindings against original metadata, without initializing Unity/Harmony.
        foreach (var type in mod.GetTypes())
        foreach (var method in type.Methods.Where(m => m.HasBody))
        {
            var il = method.Body.Instructions;
            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Operand is not GenericInstanceMethod call || call.Name != "FieldRefAccess" ||
                    call.GenericArguments.Count != 2 || !GameType(call.GenericArguments[0])) continue;
                if (i == 0 || il[i - 1].OpCode != OpCodes.Ldstr) { Fail("Unreviewed FieldRef pattern: " + method); continue; }
                string name = (string)il[i - 1].Operand;
                var owner = call.GenericArguments[0].Resolve();
                FieldDefinition field = null;
                for (; owner != null && field == null; owner = owner.BaseType?.Resolve())
                    field = owner.Fields.FirstOrDefault(f => f.Name == name);
                ++bindings;
                if (field == null || field.FieldType.FullName != call.GenericArguments[1].FullName)
                    Fail("FieldRef mismatch: " + call.GenericArguments[0] + "." + name);
            }
        }
        using var game = ModuleDefinition.ReadModule(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"), parameters);
        MethodDefinition Method(string type, string name) => game.GetType(type).Methods.Single(m => m.Name == name);
        bool Calls(MethodDefinition method, string type, string name) => method.Body.Instructions.Any(i =>
            i.Operand is MethodReference m && m.DeclaringType.FullName == type && m.Name == name);
        int ilChecks = 0;
        void Require(bool condition, string label) { ++ilChecks; if (!condition) Fail("IL contract: " + label); }
        var recipe = Method("InventoryGui", "AddRecipeToList");
        Require(recipe.Body.Variables[2].VariableType.FullName == "System.String" &&
            recipe.Parameters[2].ParameterType.FullName == "ItemDrop/ItemData" &&
            recipe.Body.Instructions.First(i => i.OpCode == OpCodes.Stloc_2).Previous.Operand is MethodReference localize &&
            localize.DeclaringType.Name == "Localization" && localize.Name == "Localize", "recipe label injection local/argument");
        var selected = game.GetType("InventoryGui").Fields.Single(f => f.Name == "m_selectedRecipe").FieldType.Resolve();
        Require(selected.Properties.Any(p => p.Name == "ItemData" && p.PropertyType.FullName == "ItemDrop/ItemData"), "selected recipe ItemData property");
        foreach (string name in new[] { "FindFreeStackSpace", "FindFreeStackItem" })
        {
            var code = Method("Inventory", name).Body.Instructions;
            var initialBranch = code.First(i => i.OpCode == OpCodes.Br || i.OpCode == OpCodes.Br_S);
            var loopTest = (Instruction)initialBranch.Operand;
            var lastBranch = code.Last(i => i.Operand == loopTest);
            var current = code.First(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Name == "get_Current");
            Require(current.Next.OpCode.Code.ToString().StartsWith("Stloc") &&
                lastBranch.Offset > current.Offset && lastBranch.Next.Offset < loopTest.Offset, name + " stack policy insertion");
        }
        var autoStack = Method("ItemDrop", "AutoStackItems").Body.Instructions;
        var stackWrite = autoStack.First(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference f && f.Name == "m_stack");
        var guard = autoStack.TakeWhile(i => i != stackWrite).Last(i => i.Operand is Instruction);
        Require(guard.OpCode == OpCodes.Bgt_S && guard.Operand is Instruction skip && skip.Offset > stackWrite.Offset &&
            autoStack.TakeWhile(i => i != guard).Last(i => i.Operand is FieldReference f && f.Name == "m_itemData").Previous.OpCode == OpCodes.Ldloc_S,
            "world autostack guard and candidate capture");
        Require(Method("ItemDrop", "Awake").Body.Instructions.Count(i => i.OpCode == OpCodes.Stfld &&
            i.Operand is FieldReference f && f.Name == "m_dropPrefab") == 1, "upgrade data import anchor");
        Require(Calls(Method("InventoryGui", "DoCrafting"), "Inventory", "RemoveItem") &&
            Calls(Method("InventoryGui", "DoCrafting"), "Inventory", "AddItem"), "craft replacement capture");
        Require(Calls(Method("Inventory", "Save"), "ItemDrop/ItemData", "Save") &&
            Calls(Method("ItemDrop", "SaveToZDO"), "ItemDrop/ItemData", "Save"), "shared 1.0 item serialization path");
        var stats = game.GetType("Player").Methods.Single(m => m.Name == "UpdateStats" && m.Parameters.Count == 1).Body.Instructions;
        Require(Enumerable.Range(0, stats.Count - 6).Any(i => stats[i].OpCode == OpCodes.Ldarg_0 &&
            stats[i+1].Operand is FieldReference nview && nview.Name == "m_nview" &&
            stats[i+2].Operand is MethodReference getZdo && getZdo.Name == "GetZDO" &&
            stats[i+3].OpCode == OpCodes.Ldsfld && stats[i+3].Operand is FieldReference key && key.Name == "s_stamina" &&
            stats[i+4].OpCode == OpCodes.Ldarg_0 && stats[i+5].Operand is FieldReference stamina && stamina.Name == "m_stamina" &&
            stats[i+6].Operand is MethodReference set && set.Name == "Set"), "stamina injection before ZDO write");
        System.Console.WriteLine($"Metadata: {references} game/Unity member references (including embedded scripts), {bindings} FieldRef bindings, {ilChecks} IL contracts, {failures} failures.");
        return failures == 0 ? 0 : 1;
    }

    private static void CheckPluginLoadOrder(ModuleDefinition mod, Action<string> fail)
    {
        const string ves = "kg.ValheimEnchantmentSystem", epic = "randyknapp.mods.epicloot", boxes = "Azumatt.AzuCraftyBoxes";
        var plugin = mod.GetType("kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem");
        string[] dependencies = plugin.CustomAttributes.Where(a => a.AttributeType.FullName == "BepInEx.BepInDependency")
            .Select(a => (string)a.ConstructorArguments[0].Value).ToArray();
        // Real installed declarations: Epic Loot 0.14.10 requires VES first; ACB 1.8.22 requires Epic Loot first.
        // The VES side comes from the built DLL so restoring the old attribute reproduces the loader failure.
        var graph = new System.Collections.Generic.Dictionary<string, string[]> {
            [ves] = dependencies, [epic] = new[] { ves }, [boxes] = new[] { epic }
        };
        string[] Sort() => BepInEx.Utility.TopologicalSort(graph.Keys.OrderBy(k => k),
            id => graph.TryGetValue(id, out var needs) ? needs : Array.Empty<string>()).ToArray();
        try
        {
            string[] sorted = Sort();
            if (!(Array.IndexOf(sorted, ves) < Array.IndexOf(sorted, epic) && Array.IndexOf(sorted, epic) < Array.IndexOf(sorted, boxes)))
                fail("Optional plugin load order must allow VES, Epic Loot, then AzuCraftyBoxes.");
            graph.Remove(boxes);
            Sort(); // ACB absent remains a valid optional-mod setup.
            graph.Remove(epic);
            Sort(); // Neither external mod is required to load VES.
        }
        catch (Exception e) { fail("Plugin dependency regression: " + e.Message); }

        graph[epic] = new[] { ves };
        graph[boxes] = new[] { epic };
        graph[ves] = dependencies.Concat(new[] { boxes }).ToArray();
        bool reproduced = false;
        try { Sort(); }
        catch (Exception e) { reproduced = e.Message.StartsWith("Cyclic Dependency:", StringComparison.Ordinal); }
        if (!reproduced) fail("Dependency regression fixture must reproduce the reported cycle when the old VES edge is restored.");
    }
}
