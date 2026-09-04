using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Integrations;
using kg.ValheimEnchantmentSystem.Items_Structures;
using kg.ValheimEnchantmentSystem.Platform;
using ServerSync;
using UnityEngine.Rendering;

namespace kg.ValheimEnchantmentSystem
{
    [BepInPlugin(GUID, PLUGIN_NAME, ModVersion)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.ArcaneWard", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.Blueprint", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("expand_world_data", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Azumatt.AzuCraftyBoxes", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValheimEnchantmentSystem : BaseUnityPlugin
    {
        private const string GUID = "kg.ValheimEnchantmentSystem";
        private const string PLUGIN_NAME = "ValheimEnchantmentSystem";
        public const string ModVersion = "1.9.13";
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
            MinimumRequiredVersion = ModVersion,
            CurrentVersion = ModVersion
        };
        public static string ConfigFolder;
        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private enum Toggle
        {
            On = 1,
            Off = 0
        }

        public static bool NoGraphics;

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
                    Utils.print($"Patch type scan loader exception: {loaderException}", ConsoleColor.Red);
                }

                return ex.Types.Where(type => type != null)!;
            }
        }

        private static HashSet<Type> InitializeModules()
        {
            HashSet<Type> failedModules = new();

            TryInitializeModule(typeof(External_AsmLoad), External_AsmLoad.Initialize, failedModules);
            TryInitializeModule(typeof(SyncedData), SyncedData.Initialize, failedModules);
            TryInitializeModule(typeof(Enchantment_Skill), Enchantment_Skill.Initialize, failedModules);
            TryInitializeModule(typeof(ScrollDropService), ScrollDropService.BindConfiguration, failedModules);
            TryInitializeModule(typeof(IntegrationRegistry), IntegrationRegistry.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(BiomeTierResolver), BiomeTierResolver.Initialize, failedModules, typeof(IntegrationRegistry));
            TryInitializeModule(typeof(ResourceMapRequirementResolver), ResourceMapRequirementResolver.Initialize, failedModules,
                typeof(SyncedData), typeof(BiomeTierResolver));
            TryInitializeModule(typeof(ScrollDropService), ScrollDropService.Initialize, failedModules, typeof(BiomeTierResolver));
            TryInitializeModule(typeof(SkillScrollService), SkillScrollService.Initialize, failedModules, typeof(Enchantment_Skill));
            TryInitializeModule(typeof(EquippedEnchantmentSnapshotService), EquippedEnchantmentSnapshotService.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(VfxInstanceRegistry), VfxInstanceRegistry.Initialize, failedModules);
            TryInitializeModule(typeof(Notifications_UI), Notifications_UI.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(Enchantment_VFX), Enchantment_VFX.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(VES_UI), VES_UI.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(Info_UI), Info_UI.Initialize, failedModules, typeof(SyncedData), typeof(VES_UI));
            TryInitializeModule(typeof(InventoryOverlayVfx), InventoryOverlayVfx.Initialize, failedModules, typeof(SyncedData));
            TryInitializeModule(typeof(Enchantment_Core), Enchantment_Core.Initialize, failedModules,
                typeof(SyncedData), typeof(Enchantment_Skill), typeof(Notifications_UI), typeof(Enchantment_VFX));
            TryInitializeModule(typeof(ScrollItems), ScrollItems.Initialize, failedModules,
                typeof(VES_UI), typeof(Enchantment_Skill), typeof(SkillScrollService));

            return failedModules;
        }

        private static void TryInitializeModule(Type moduleType, Action initializer, ISet<Type> failedModules, params Type[] dependencies)
        {
            Type moduleRoot = GetTopLevelDeclaringType(moduleType);
            if (failedModules.Contains(moduleRoot))
            {
                return;
            }

            Type[] failedDependencies = dependencies
                .Select(GetTopLevelDeclaringType)
                .Where(failedModules.Contains)
                .ToArray();
            if (failedDependencies.Length > 0)
            {
                failedModules.Add(moduleRoot);
                Utils.print($"Initialization skipped {moduleType.FullName}: dependency failed: {string.Join(", ", failedDependencies.Select(type => type.FullName ?? type.Name))}", ConsoleColor.Yellow);
                return;
            }

            try
            {
                initializer();
            }
            catch (Exception ex)
            {
                failedModules.Add(moduleRoot);
                Utils.print($"Initialization failed for {moduleType.FullName}: {ex}", ConsoleColor.Red);
            }
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

                HashSet<Type> failedAutoloadTypes = InitializeModules();
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

            _serverConfigLocked = config(
                GeneralConfigSection,
                "Lock Configuration",
                Toggle.On,
                ConfigurationManagerDisplay.Description(
                    "If on, synced configuration can be changed by server admins only.",
                    ConfigurationManagerDisplay.General,
                    1000,
                    "Lock Configuration"));
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
