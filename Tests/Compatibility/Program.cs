System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, name.Name + ".dll");
    return System.IO.File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
int contracts = ValheimEnchantmentSystem.RuleTests.GameCompatibilityChecks.Run();
string root = args.Length > 0 ? System.IO.Path.GetFullPath(args[0]) :
    System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
return contracts | ValheimEnchantmentSystem.RuleTests.MetadataChecks.Run()
    | ValheimEnchantmentSystem.RuleTests.SourceBindingChecks.Run(root);
