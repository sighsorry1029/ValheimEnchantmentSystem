using BepInEx.Bootstrap;

namespace kg.ValheimEnchantmentSystem.Integrations;

// This adapter only discovers ordinary Container inventories. ACB's drawer, backpack,
// and gem-bag wrappers are deliberately not queried or consumed through its API.
internal sealed class AzuCraftyBoxesIntegration : IOptionalIntegration
{
    internal const string PluginGuid = "Azumatt.AzuCraftyBoxes";
    public static readonly AzuCraftyBoxesIntegration Instance = new();

    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const string PullingStatusKey = "ACB_PreventPulling";
    private FieldInfo? _modEnabled;
    private FieldInfo? _range;
    private FieldInfo? _leaveOne;
    private FieldInfo? _containers;
    private FieldInfo? _containersToAdd;
    private FieldInfo? _containersToRemove;
    private FieldInfo? _yamlData;
    private MethodInfo? _canItemBePulled;
    private MethodInfo? _hasAccess;
    private bool _bound;
    private bool _bindingFailed;
    private bool _warningLogged;

    public bool IsAvailable() => Chainloader.PluginInfos.ContainsKey(PluginGuid);

    public void Initialize() => EnsureBindings();

    internal bool IsEligibleContainer(Container container, Player player, string itemPrefabName, out bool leaveOne)
    {
        return GetEligibleContainers(player, itemPrefabName, out leaveOne).Contains(container);
    }

    internal IReadOnlyList<Container> GetEligibleContainers(Player player, string itemPrefabName, out bool leaveOne)
    {
        leaveOne = false;
        if (!player || player != Player.m_localPlayer || string.IsNullOrWhiteSpace(itemPrefabName) || !EnsureBindings())
        {
            return Array.Empty<Container>();
        }

        try
        {
            if (!ReadToggle(_modEnabled!) || !IsPullingAllowed(player) || _yamlData!.GetValue(null) == null)
            {
                return Array.Empty<Container>();
            }

            float range = ReadConfig(_range!) is float configuredRange
                ? configuredRange
                : throw new InvalidOperationException("Container Range is not a float.");
            if (float.IsNaN(range) || float.IsInfinity(range))
            {
                return Array.Empty<Container>();
            }

            leaveOne = ReadToggle(_leaveOne!);
            // ACB 1.8.15 compares squared distance to range * range, including negative ranges.
            double rangeSquared = (double)range * range;
            Vector3 position = player.transform.position;
            CraftingStation station = player.GetCurrentCraftingStation();
            string stationName = station ? global::Utils.GetPrefabName(station.gameObject) : string.Empty;
            List<Container> result = new();

            foreach (Container container in SnapshotRegisteredContainers())
            {
                if (!container || !container.gameObject.activeInHierarchy || !container.m_nview ||
                    !container.m_nview.IsValid() || container.GetInventory() == null ||
                    container.m_nview.GetZDO().GetLong("creator".GetStableHashCode(), 0L) == 0L ||
                    container.GetComponent<TombStone>() || container.GetComponentInParent<Player>() ||
                    (container.m_wagon && container.m_wagon.InUse()) ||
                    (container.IsInUse() && !container.IsOwner()))
                {
                    continue;
                }

                Vector3 delta = container.transform.position - position;
                double distanceSquared = (double)delta.x * delta.x + (double)delta.y * delta.y + (double)delta.z * delta.z;
                if (double.IsNaN(distanceSquared) || distanceSquared > rangeSquared ||
                    _hasAccess!.Invoke(null, new object[] { container }) is not true)
                {
                    continue;
                }

                // Keep ACB's live include/exclude groups and crafting-station restrictions.
                string containerName = global::Utils.GetPrefabName(container.gameObject);
                if (_canItemBePulled!.Invoke(null, new object[] { containerName, itemPrefabName, stationName }) is true)
                {
                    result.Add(container);
                }
            }

            result.Sort((left, right) =>
            {
                int distanceOrder = (left.transform.position - position).sqrMagnitude.CompareTo(
                    (right.transform.position - position).sqrMagnitude);
                return distanceOrder != 0 ? distanceOrder : left.GetInstanceID().CompareTo(right.GetInstanceID());
            });
            return result;
        }
        catch (Exception exception)
        {
            WarnOnce(exception);
            leaveOne = false;
            return Array.Empty<Container>();
        }
    }

    private bool EnsureBindings()
    {
        if (_bound)
        {
            return true;
        }
        if (_bindingFailed || !Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo plugin) || !plugin.Instance)
        {
            return false;
        }

        try
        {
            Assembly assembly = plugin.Instance.GetType().Assembly;
            Type pluginType = RequireType(assembly, "AzuCraftyBoxes.AzuCraftyBoxesPlugin");
            Type boxesType = RequireType(assembly, "AzuCraftyBoxes.Util.Functions.Boxes");
            Type functionsType = RequireType(assembly, "AzuCraftyBoxes.Util.Functions.MiscFunctions");
            _modEnabled = RequireField(pluginType, "ModEnabled", typeof(ConfigEntryBase));
            _range = RequireField(pluginType, "mRange", typeof(ConfigEntryBase));
            _leaveOne = RequireField(pluginType, "leaveOne", typeof(ConfigEntryBase));
            _yamlData = RequireField(pluginType, "yamlData", typeof(IDictionary));
            _containers = RequireField(boxesType, "Containers", typeof(IEnumerable<Container>));
            _containersToAdd = RequireField(boxesType, "ContainersToAdd", typeof(IEnumerable<Container>));
            _containersToRemove = RequireField(boxesType, "ContainersToRemove", typeof(IEnumerable<Container>));
            _canItemBePulled = RequireMethod(boxesType, "CanItemBePulled", typeof(string), typeof(string), typeof(string));
            _hasAccess = RequireMethod(functionsType, "HasAccessToContainer", typeof(Container));
            _bound = true;
            return true;
        }
        catch (Exception exception)
        {
            _bindingFailed = true;
            WarnOnce(exception);
            return false;
        }
    }

    private Container[] SnapshotRegisteredContainers()
    {
        // Reading the registry directly avoids ACB's frame-global nearby-query cache,
        // whose first query can have a different source/radius. Include deferred changes
        // without flushing or modifying ACB's registry from this read-only adapter.
        HashSet<Container> snapshot = new(ReadContainers(_containers!));
        snapshot.UnionWith(ReadContainers(_containersToAdd!));
        snapshot.ExceptWith(ReadContainers(_containersToRemove!));
        return snapshot.ToArray();
    }

    private static IEnumerable<Container> ReadContainers(FieldInfo field) =>
        field.GetValue(null) as IEnumerable<Container> ?? throw new InvalidOperationException($"{field.Name} is unavailable.");

    private static bool IsPullingAllowed(Player player)
    {
        // Match ACB's default-on semantics without writing its per-player custom data.
        return !player.m_customData.TryGetValue(PullingStatusKey, out string status) ||
               !int.TryParse(status, out int value) || value == 1;
    }

    private static object ReadConfig(FieldInfo field) =>
        (field.GetValue(null) as ConfigEntryBase)?.BoxedValue ??
        throw new InvalidOperationException($"{field.Name} configuration is unavailable.");

    private static bool ReadToggle(FieldInfo field)
    {
        object value = ReadConfig(field);
        if (!value.GetType().IsEnum || !Enum.IsDefined(value.GetType(), value))
        {
            throw new InvalidOperationException($"{field.Name} is not a supported toggle.");
        }
        return string.Equals(value.ToString(), "On", StringComparison.Ordinal);
    }

    private static Type RequireType(Assembly assembly, string name) =>
        assembly.GetType(name, throwOnError: false) ?? throw new TypeLoadException(name);

    private static FieldInfo RequireField(Type type, string name, Type valueType)
    {
        FieldInfo field = type.GetField(name, StaticMembers) ?? throw new MissingFieldException(type.FullName, name);
        if (!valueType.IsAssignableFrom(field.FieldType))
        {
            throw new InvalidOperationException($"{type.FullName}.{name} has an unsupported type.");
        }
        return field;
    }

    private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters)
    {
        MethodInfo method = type.GetMethod(name, StaticMembers, null, parameters, null) ??
                            throw new MissingMethodException(type.FullName, name);
        if (method.ReturnType != typeof(bool))
        {
            throw new InvalidOperationException($"{type.FullName}.{name} has an unsupported return type.");
        }
        return method;
    }

    private void WarnOnce(Exception exception)
    {
        if (_warningLogged)
        {
            return;
        }
        _warningLogged = true;
        Exception cause = exception is TargetInvocationException { InnerException: not null } invocation
            ? invocation.InnerException
            : exception;
        Utils.print($"AzuCraftyBoxes chest lookup was unavailable; using player inventory only. {cause.GetType().Name}: {cause.Message}", ConsoleColor.Yellow);
    }
}
