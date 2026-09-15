using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace ValheimEnchantmentSystem.RuleTests;

internal static class SourceBindingChecks
{
    internal static int Run(string root)
    {
        var modules = new[] { "assembly_valheim", "assembly_utils", "assembly_guiutils" }
            .Select(n => ModuleDefinition.ReadModule(Path.Combine(AppContext.BaseDirectory, n + ".dll"))).ToArray();
        var types = modules.SelectMany(m => m.GetTypes()).ToArray();
        var aliases = new Dictionary<string, string> {
            ["int"]="System.Int32", ["long"]="System.Int64", ["float"]="System.Single",
            ["bool"]="System.Boolean", ["string"]="System.String", ["byte"]="System.Byte",
            ["GameObject"]="UnityEngine.GameObject", ["Texture2D"]="UnityEngine.Texture2D" };
        string Canonical(string name) => aliases.TryGetValue(name, out var value) ? value : name.Replace("/", ".");
        var pattern = new Regex(@"AccessTools\.(?<kind>DeclaredMethod|Method|DeclaredField|Field|Property)\(typeof\((?<type>[\w.]+)\),\s*(?:nameof\((?<nameof>[\w.]+)\)|""(?<name>[^""]+)"")(?<args>,\s*new(?:\s+Type)?\[\]\s*\{[^}]*\})?");
        int failures = 0, bindings = 0;
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file);
            if (new[] { "Tests", ".tmp", "bin", "obj", "_valheim_decomp", "_tmp_allmanagers_template", "%TEMP%" }
                .Any(folder => relative.StartsWith(folder + Path.DirectorySeparatorChar))) continue;
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                string ownerName = match.Groups["type"].Value;
                var owner = types.FirstOrDefault(t => Canonical(t.FullName) == ownerName);
                if (owner == null) continue; // self/optional external type, not a game contract
                string name = match.Groups["name"].Success ? match.Groups["name"].Value : match.Groups["nameof"].Value.Split('.').Last();
                string kind = match.Groups["kind"].Value;
                bool valid;
                if (kind.EndsWith("Method"))
                {
                    var candidates = owner.Methods.Where(m => m.Name == name);
                    if (match.Groups["args"].Success)
                    {
                        string[] args = Regex.Matches(match.Groups["args"].Value, @"typeof\(([\w.]+)\)")
                            .Select(m => Canonical(m.Groups[1].Value)).ToArray();
                        candidates = candidates.Where(m => m.Parameters.Select(p => Canonical(p.ParameterType.FullName)).SequenceEqual(args));
                    }
                    valid = candidates.Any();
                }
                else valid = kind == "Property" ? owner.Properties.Any(p => p.Name == name) : owner.Fields.Any(f => f.Name == name);
                ++bindings;
                if (!valid) { ++failures; System.Console.Error.WriteLine(relative + ": unresolved " + match.Value); }
            }
        }
        foreach (var module in modules) module.Dispose();
        System.Console.WriteLine($"Source bindings: {bindings} named game method/field/property accesses, {failures} failures.");
        return failures == 0 ? 0 : 1;
    }
}
