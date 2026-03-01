using ItemManager;
using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.Misc;

[VES_Autoload(VES_Autoload.Priority.Init)]
public static class External_AsmLoad
{
    [UsedImplicitly]
    private static void OnInit()
    {
        LoadAsm("VES_Scripts");
    }
    private static void LoadAsm(string name)
    {
        string[] manifestResourceNames = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
        if (manifestResourceNames.Length == 0)
        {
            Utils.print("No resources found", ConsoleColor.Red);
            return;
        }
        string resourceName = manifestResourceNames.Single(str => str.EndsWith(name + ".dll"));
        using System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        byte[] buffer = new byte[stream.Length];
        // ReSharper disable once MustUseReturnValue
        stream.Read(buffer, 0, buffer.Length); 
        try
        {
            Assembly.Load(buffer);
            stream.Dispose();
        }
        catch(Exception ex)
        {
            Utils.print($"Error loading {name} assembly\n:{ex}", ConsoleColor.Red);
        }
    }


}
