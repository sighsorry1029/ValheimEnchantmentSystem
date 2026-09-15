using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ValheimEnchantmentSystem.RuleTests;

// Resolves the real patch contracts against original game assemblies without starting Unity.
internal static class GameCompatibilityChecks
{
    internal static int Run()
    {
        int failures = 0, targets = 0;
        Assembly mod = typeof(kg.ValheimEnchantmentSystem.Enchantment_Core).Assembly;
        Type[] types;
        try { types = mod.GetTypes(); }
        catch (ReflectionTypeLoadException e)
        {
            failures += e.LoaderExceptions.Length;
            foreach (Exception loader in e.LoaderExceptions) System.Console.Error.WriteLine(loader.Message);
            types = e.Types.Where(t => t != null).ToArray();
        }
        foreach (Type type in types.Where(t => t.Namespace != null &&
                     t.Namespace.StartsWith("kg.ValheimEnchantmentSystem")))
        {
            try
            {
                HarmonyPatch[] attributes = type.GetCustomAttributes<HarmonyPatch>().ToArray();
                if (attributes.Length == 0) continue;
                HarmonyMethod info = attributes[0].info;
                IEnumerable<MethodBase> originals;
                if (info.declaringType != null)
                    originals = new[] { FindMethod(info.declaringType, info.methodName, info.argumentTypes) };
                else
                {
                    // Optional external-mod hooks require that mod's own runtime, not a synthetic substitute.
                    if (type.FullName.Contains("ExpandWorldDataIntegration")) continue;
                    if (type.Name == "ModifyArmor" || type.Name == "ModifyDamage")
                        originals = new[] { FindMethod(typeof(ItemDrop.ItemData), type.Name == "ModifyArmor" ? "GetArmor" : "GetDamage", Type.EmptyTypes) };
                    else if (type.Name == "Humanoid_EquipState_Patch")
                        originals = new[] { FindMethod(typeof(Humanoid), "EquipItem", null), FindMethod(typeof(Humanoid), "UnequipItem", null) };
                    else if (type.Name.Contains("Unregister")) continue;
                    else throw new Exception("Unreviewed dynamic target selector");
                }                foreach (MethodBase original in originals)
                {
                    if (original == null)
                    {
                        if (type.Name.Contains("Unregister")) continue; // guarded Prepare, registry also prunes destroyed Unity objects
                        throw new Exception("Target did not resolve");
                    }
                    ++targets;
                    foreach (MethodInfo patch in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Where(m => m.Name == "Prefix" || m.Name == "Postfix" || m.Name == "Finalizer"))
                        foreach (ParameterInfo p in patch.GetParameters())
                        {
                            if (p.Name.StartsWith("___"))
                            {
                                FieldInfo field = FindField(original.DeclaringType, p.Name.Substring(3));
                                if (field == null) throw new Exception("Missing injected field " + p.Name);
                                CheckType(p, field.FieldType);
                            }
                            else if (!p.Name.StartsWith("__"))
                            {
                                ParameterInfo arg = original.GetParameters().FirstOrDefault(a => a.Name == p.Name);
                                if (arg == null) throw new Exception("Missing argument " + p.Name + " on " + original);
                                CheckType(p, arg.ParameterType);
                            }
                        }
                }
            }
            catch (Exception e) { ++failures; System.Console.Error.WriteLine(type.FullName + ": " + e.GetBaseException().Message); }
        }
        System.Console.WriteLine($"Compatibility: {targets} patch targets and argument bindings, {failures} failures.");
        return failures == 0 ? 0 : 1;
    }

    private static MethodInfo FindMethod(Type type, string name, Type[] args)
    {
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == name);
        if (args != null) methods = methods.Where(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(args));
        return methods.SingleOrDefault();
    }
    private static FieldInfo FindField(Type type, string name)
    {
        for (; type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field;
        }
        return null;
    }
    private static void CheckType(ParameterInfo patch, Type source)
    {
        Type target = patch.ParameterType.IsByRef ? patch.ParameterType.GetElementType() : patch.ParameterType;
        if (source.IsByRef) source = source.GetElementType();
        if (!target.IsAssignableFrom(source))
            throw new Exception($"Argument {patch.Name}: {source} cannot bind to {target}");
    }
}
