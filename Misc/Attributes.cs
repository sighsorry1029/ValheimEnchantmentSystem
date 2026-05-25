namespace kg.ValheimEnchantmentSystem.Misc;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class VES_Autoload : Attribute
{
    public enum Priority
    {
        Init,
        First,
        Normal,
        Last
    }

    public readonly Priority priority;
    public readonly string InitMethod;
    public readonly Type[] DependsOn;

    public VES_Autoload(Priority priority = Priority.Last, string InitMethod = "OnInit", params Type[] dependsOn)
    {
        this.priority = priority;
        this.InitMethod = InitMethod;
        DependsOn = dependsOn ?? Array.Empty<Type>();
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ClientOnlyPatch : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServerOnlyPatch : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class VES_RequiresPlugin : Attribute
{
    public readonly string PluginGuid;

    public VES_RequiresPlugin(string pluginGuid)
    {
        PluginGuid = pluginGuid ?? string.Empty;
    }
}
