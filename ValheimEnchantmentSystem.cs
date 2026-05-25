using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Platform;
using ServerSync;
using UnityEngine.Rendering;

namespace kg.ValheimEnchantmentSystem
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com   .bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.ArcaneWard", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.Blueprint", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("expand_world_data", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValheimEnchantmentSystem : BaseUnityPlugin
    {
        private const string GUID = "kg.ValheimEnchantmentSystem";
        private const string PLUGIN_NAME = "ValheimEnchantmentSystem";
        private const string PLUGIN_VERSION = "1.9.8";
        private const string GeneralConfigSection = "General";
        private const string ClientConfigSection = "Client";
        private static readonly string ConfigFileName = GUID + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);
        
        public static ValheimEnchantmentSystem _thistype;
        public static AssetBundle _asset;
        public static readonly Harmony Harmony = new(GUID);
        public static readonly ConfigSync ConfigSync = new(GUID)
        {  
            DisplayName = PLUGIN_NAME,
            ModRequired = true,
            MinimumRequiredVersion = PLUGIN_VERSION,
            CurrentVersion = PLUGIN_VERSION
        };
        public static string ConfigFolder;
        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private enum Toggle
        {
            On = 1,
            Off = 0
        }

        public static bool NoGraphics;

        private readonly struct AutoloadFailure
        {
            public readonly Type ModuleType;
            public readonly string Reason;

            public AutoloadFailure(Type moduleType, string reason)
            {
                ModuleType = moduleType;
                Reason = reason;
            }
        }

        internal static IEnumerable<Type> GetAssemblyTypes()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Exception loaderException in ex.LoaderExceptions.Where(e => e != null))
                {
                    Utils.print($"Autoload type scan loader exception: {loaderException}", ConsoleColor.Red);
                }

                return ex.Types.Where(type => type != null)!;
            }
        }

        private static bool TryResolveAutoloadMethod(Type moduleType, VES_Autoload autoload, out MethodInfo method, out string reason)
        {
            method = null!;
            string methodName = string.IsNullOrWhiteSpace(autoload.InitMethod) ? "OnInit" : autoload.InitMethod;
            MethodInfo? candidate = moduleType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);

            if (candidate == null)
            {
                reason = $"method '{methodName}' not found";
                return false;
            }

            if (!candidate.IsStatic)
            {
                reason = $"method '{methodName}' must be static";
                return false;
            }

            if (candidate.GetParameters().Length != 0)
            {
                reason = $"method '{methodName}' must not have parameters";
                return false;
            }

            if (candidate.ReturnType != typeof(void))
            {
                reason = $"method '{methodName}' must return void";
                return false;
            }

            method = candidate;
            reason = string.Empty;
            return true;
        }

        private static HashSet<Type> RunAutoload()
        {
            List<KeyValuePair<VES_Autoload, Type>> toAutoload = GetAssemblyTypes()
                .Where(t => t.GetCustomAttribute<VES_Autoload>() != null)
                .Select(x => new KeyValuePair<VES_Autoload, Type>(x.GetCustomAttribute<VES_Autoload>(), x))
                .OrderBy(x => x.Key.priority)
                .ThenBy(x => x.Value.FullName, StringComparer.Ordinal)
                .ToList();
            HashSet<Type> knownAutoloadModules = toAutoload
                .Select(entry => GetTopLevelDeclaringType(entry.Value))
                .ToHashSet();

            HashSet<Type> failedAutoloadTypes = new();
            List<AutoloadFailure> failures = new();
            int successCount = 0;

            foreach (KeyValuePair<VES_Autoload, Type> autoload in toAutoload)
            {
                Type moduleType = autoload.Value;
                Type moduleRoot = GetTopLevelDeclaringType(moduleType);

                Type[] dependencyRoots = (autoload.Key.DependsOn ?? Array.Empty<Type>())
                    .Select(GetTopLevelDeclaringType)
                    .Distinct()
                    .ToArray();

                Type[] missingDependencies = dependencyRoots
                    .Where(dependency => !knownAutoloadModules.Contains(dependency))
                    .ToArray();
                if (missingDependencies.Length > 0)
                {
                    string dependencyError = $"unknown dependency: {string.Join(", ", missingDependencies.Select(type => type.FullName ?? type.Name))}";
                    failedAutoloadTypes.Add(moduleRoot);
                    failures.Add(new AutoloadFailure(moduleType, dependencyError));
                    Utils.print($"Autoload dependency validation failed for {moduleType.FullName}: {dependencyError}", ConsoleColor.Red);
                    continue;
                }

                Type[] failedDependencies = dependencyRoots
                    .Where(failedAutoloadTypes.Contains)
                    .ToArray();
                if (failedDependencies.Length > 0)
                {
                    string dependencyError = $"dependency failed: {string.Join(", ", failedDependencies.Select(type => type.FullName ?? type.Name))}";
                    failedAutoloadTypes.Add(moduleRoot);
                    failures.Add(new AutoloadFailure(moduleType, dependencyError));
                    Utils.print($"Autoload skipped {moduleType.FullName}: {dependencyError}", ConsoleColor.Yellow);
                    continue;
                }

                if (!TryResolveAutoloadMethod(moduleType, autoload.Key, out MethodInfo method, out string reason))
                {
                    failedAutoloadTypes.Add(moduleRoot);
                    failures.Add(new AutoloadFailure(moduleType, reason));
                    Utils.print($"Autoload validation failed for {moduleType.FullName}: {reason}", ConsoleColor.Red);
                    continue;
                }

                try
                {
                    method.Invoke(null, null);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Exception root = ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException : ex;
                    failedAutoloadTypes.Add(moduleRoot);
                    failures.Add(new AutoloadFailure(moduleType, root.Message));
                    Utils.print($"Autoload exception on method {method}. Class {moduleType}\n:{root}", ConsoleColor.Red);
                }
            }

            if (failures.Count > 0)
            {
                string failedModules = string.Join(", ", failures.Select(failure => $"{failure.ModuleType.Name} ({failure.Reason})"));
                Utils.print($"Autoload completed with failures. success={successCount}, failed={failures.Count}. Failed modules: {failedModules}", ConsoleColor.Yellow);
            }

            return failedAutoloadTypes;
        }

        internal static Type GetTopLevelDeclaringType(Type type)
        {
            Type current = type;
            while (current.DeclaringType != null)
            {
                current = current.DeclaringType;
            }

            return current;
        }

        internal static bool IsTypeBlockedByAutoloadFailure(Type type, ISet<Type> failedAutoloadTypes)
        {
            if (failedAutoloadTypes.Count == 0)
            {
                return false;
            }

            Type root = GetTopLevelDeclaringType(type);
            return failedAutoloadTypes.Contains(root);
        }
        
        private void Awake()
        { 
            bool saveOnSet = BeginBootstrap();
            try
            {
                InitializeRuntimeState();
                InitializeSharedInfrastructure();
                InitializeConfigInfrastructure();
                InitializeAssets();

                HashSet<Type> failedAutoloadTypes = RunAutoload();
                ApplyRegisteredPatches(failedAutoloadTypes);
                InitializeConfigReloadInfrastructure();
            }
            finally
            {
                EndBootstrap(saveOnSet);
            }
        } 
 
        private void Update()
        {
            ConfigReloadPoller.Update();
            if (NoGraphics) return;
            VES_UI.Update();
            Info_UI.Update();
            Notifications_UI.Update();
        }

        private void Start()
        {
        }

        private void OnDestroy()
        {
            Config.Save();
            ConfigReloadPoller.Shutdown();
        }
        
        private static AssetBundle GetAssetBundle(string filename) 
        { 
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream stream = execAssembly.GetManifestResourceStream(resourceName)!;
            return AssetBundle.LoadFromStream(stream);
        } 

        private bool BeginBootstrap()
        {
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            return saveOnSet;
        }

        private void InitializeRuntimeState()
        {
            NoGraphics = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            _thistype = this;
        }

        private static void InitializeSharedInfrastructure()
        {
            JSON.Parameters = new JSONParameters
            {
                UseExtensions = false,
                SerializeNullValues = false,
                DateTimeMilliseconds = false,
                UseUTCDateTime = true,
                UseOptimizedDatasetSchema = true,
                UseValuesOfEnums = true,
            };
            LocalizationBootstrap.Initialize();
        }

        private void InitializeConfigInfrastructure()
        {
            ConfigFolder = Path.Combine(Paths.ConfigPath, "ValheimEnchantmentSystem");
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            _serverConfigLocked = config(GeneralConfigSection, "Lock Configuration", Toggle.On, "If on, synced configuration can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        }

        private void InitializeAssets()
        {
            _asset = GetAssetBundle("kg_enchantment");
        }

        private void ApplyRegisteredPatches(HashSet<Type> failedAutoloadTypes)
        {
            PatchRegistry.Apply(Harmony, Logger, NoGraphics, failedAutoloadTypes);
        }

        private void EndBootstrap(bool saveOnSet)
        {
            Config.SaveOnConfigSet = saveOnSet;
            Config.Save();
        }

        private static string WithSyncTag(string description, bool synchronizedSetting)
        {
            const string syncedTag = "[Synced with Server]";
            const string notSyncedTag = "[Not Synced with Server]";
            if (description.Contains(syncedTag) || description.Contains(notSyncedTag))
            {
                return description;
            }

            string tag = synchronizedSetting ? syncedTag : notSyncedTag;
            return string.IsNullOrWhiteSpace(description) ? tag : $"{description} {tag}";
        }

        private void InitializeConfigReloadInfrastructure()
        {
            ConfigHotReloadRegistrar.Initialize(ConfigFileFullPath, TryReloadMainConfig);
            ConfigReloadPoller.Initialize();
        }

        private bool TryReloadMainConfig()
        {
            if (!File.Exists(ConfigFileFullPath))
            {
                Logger.LogWarning($"{ConfigFileName} does not exist; skipping config reload.");
                return false;
            }

            try
            {
                Config.Reload();
                return true;
            }
            catch
            {
                Logger.LogError($"Could not load {ConfigFileName}. Check config format and values.");
                return false;
            }
        }

        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true) 
        {
            object[] tags = description.Tags ?? Array.Empty<object>();
            ConfigDescription extendedDescription = new(
                WithSyncTag(description.Description, synchronizedSetting),
                description.AcceptableValues,
                tags
            );
            ConfigEntry<T> configEntry = _thistype.Config.Bind(group, name, value, extendedDescription);
            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting; 
            return configEntry;
        }

        public static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true) =>
            config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        // Client-only config that is not synced with server
        public static ConfigEntry<T> ClientConfig<T>(string group, string name, T value, ConfigDescription description)
        {
            // Use group as prefix in name to keep all client settings in one section
            string configName = string.IsNullOrEmpty(group) ? name : $"{group} - {name}";
            object[] tags = description.Tags ?? Array.Empty<object>();
            ConfigDescription clientDescription = new(
                WithSyncTag(description.Description, false),
                description.AcceptableValues,
                tags
            );
            ConfigEntry<T> configEntry = _thistype.Config.Bind(ClientConfigSection, configName, value, clientDescription);
            return configEntry;
        }

        public static ConfigEntry<T> ClientConfig<T>(string group, string name, T value, string description) =>
            ClientConfig(group, name, value, new ConfigDescription(description));
    }
}
