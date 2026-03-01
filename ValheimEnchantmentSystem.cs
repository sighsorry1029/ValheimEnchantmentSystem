﻿using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using LocalizationManager;
using ServerSync;
using UnityEngine.Rendering;

namespace kg.ValheimEnchantmentSystem
{
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.ArcaneWard", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("kg.Blueprint", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValheimEnchantmentSystem : BaseUnityPlugin
    {
        private const string GUID = "kg.ValheimEnchantmentSystem";
        private const string PLUGIN_NAME = "ValheimEnchantmentSystem";
        private const string PLUGIN_VERSION = "1.8.4";
        private static readonly string ConfigFileName = GUID + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);
        private const int FailFastOnAutoloadOrder = -900000;
        
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
        private static ConfigEntry<bool> _failFastOnAutoloadError = null!;
        private FileSystemWatcher? _configWatcher;
        private static readonly Dictionary<string, int> _nextOrderBySection = new(StringComparer.OrdinalIgnoreCase);

        private class ConfigurationManagerAttributes
        {
            public int? Order;
        }

        private enum Toggle
        {
            On = 1,
            Off = 0
        }

        private enum WorkingAs { Client, Server }
        public static bool NoGraphics;
        
        private void Awake()
        { 
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;

            NoGraphics = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            _thistype = this;
            WorkingAs WorkingAsType = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? WorkingAs.Server : WorkingAs.Client;
            JSON.Parameters = new JSONParameters
            {  
                UseExtensions = false,
                SerializeNullValues = false,
                DateTimeMilliseconds = false,
                UseUTCDateTime = true,
                UseOptimizedDatasetSchema = true,
                UseValuesOfEnums = true, 
            };
            Localizer.Load();

            ConfigFolder = Path.Combine(Paths.ConfigPath, "ValheimEnchantmentSystem");
            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, synced configuration can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            _failFastOnAutoloadError = ClientConfig(
                "Integrity",
                "Fail Fast On Autoload Error",
                true,
                new ConfigDescription(
                    "Stop plugin initialization when an autoload class throws. Disable only for debugging compatibility issues.",
                    null,
                    new ConfigurationManagerAttributes { Order = FailFastOnAutoloadOrder }));

            _asset = GetAssetBundle("kg_enchantment");


            IEnumerable<KeyValuePair<VES_Autoload, Type>> toAutoload = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VES_Autoload>() != null)
                .Select(x => new KeyValuePair<VES_Autoload, Type>(x.GetCustomAttribute<VES_Autoload>(), x))
                .OrderBy(x => x.Key.priority);
            foreach (KeyValuePair<VES_Autoload, Type> autoload in toAutoload)
            {
                MethodInfo method = autoload.Value.GetMethod(autoload.Key.InitMethod ?? "OnInit", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                if (method == null)
                {
                    Utils.print($"Error loading {autoload.Value.Name} class, method {autoload.Key.InitMethod} not found", ConsoleColor.Red);
                    continue;
                }
                try
                { 
                    method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Exception root = ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException : ex;
                    Utils.print($"Autoload exception on method {method}. Class {autoload.Value}\n:{root}", ConsoleColor.Red);
                    if (_failFastOnAutoloadError.Value)
                        throw new InvalidOperationException(
                            $"Autoload failed for {autoload.Value.FullName}.{method.Name}. Set [Client] Integrity - Fail Fast On Autoload Error = false to continue startup for debugging.",
                            root);
                }
            }
            
            AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Where(t => WorkingAsType switch
                {
                    WorkingAs.Client => t.GetCustomAttribute<ServerOnlyPatch>() == null, 
                    WorkingAs.Server => t.GetCustomAttribute<ClientOnlyPatch>() == null, 
                    _ => true
                }).Do(type => Harmony.CreateClassProcessor(type).Patch());

            SetupWatcher();
            if (saveOnSet)
            {
                Config.SaveOnConfigSet = true;
                Config.Save();
            }
        } 
 
        private void Update()
        {
            if (NoGraphics) return;
            VES_UI.Update();
            Info_UI.Update();
            Notifications_UI.Update();
        }

        private void Start()
        {
            Other_Mods_APIs.Start();
        }

        private void OnDestroy()
        {
            Config.Save();
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
                _configWatcher.Dispose();
                _configWatcher = null;
            }
        }
        
        private static AssetBundle GetAssetBundle(string filename) 
        { 
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream stream = execAssembly.GetManifestResourceStream(resourceName)!;
            return AssetBundle.LoadFromStream(stream);
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

        private static int NextOrderForSection(string section)
        {
            if (_nextOrderBySection.TryGetValue(section, out int current))
            {
                _nextOrderBySection[section] = current - 1;
                return current;
            }

            _nextOrderBySection[section] = -1;
            return 0;
        }

        private static bool IsOrderType(Type type) => type == typeof(int) || type == typeof(int?);

        private static bool HasOrderTag(object? tag)
        {
            if (tag == null)
            {
                return false;
            }

            Type tagType = tag.GetType();
            PropertyInfo? orderProperty = tagType.GetProperty("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (orderProperty != null && IsOrderType(orderProperty.PropertyType))
            {
                return true;
            }

            FieldInfo? orderField = tagType.GetField("Order", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return orderField != null && IsOrderType(orderField.FieldType);
        }

        private static object[] EnsureOrderTag(string section, object[]? tags)
        {
            object[] safeTags = tags ?? Array.Empty<object>();
            if (safeTags.Any(HasOrderTag))
            {
                return safeTags;
            }

            int order = NextOrderForSection(section);
            return safeTags.Concat(new object[] { new ConfigurationManagerAttributes { Order = order } }).ToArray();
        }

        private void SetupWatcher()
        {
            _configWatcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName)
            {
                IncludeSubdirectories = true,
                SynchronizingObject = ThreadingHelper.SynchronizingObject,
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ReadConfigValues;
            _configWatcher.Created += ReadConfigValues;
            _configWatcher.Renamed += ReadConfigValues;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath))
            {
                return;
            }

            try
            {
                Config.Reload();
            }
            catch
            {
                Logger.LogError($"Could not load {ConfigFileName}. Check config format and values.");
            }
        }

        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true) 
        {
            object[] orderedTags = EnsureOrderTag(group, description.Tags);
            ConfigDescription extendedDescription = new(
                WithSyncTag(description.Description, synchronizedSetting),
                description.AcceptableValues,
                orderedTags
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
            object[] orderedTags = EnsureOrderTag("Client", description.Tags);
            ConfigDescription clientDescription = new(
                WithSyncTag(description.Description, false),
                description.AcceptableValues,
                orderedTags
            );
            ConfigEntry<T> configEntry = _thistype.Config.Bind("Client", configName, value, clientDescription);
            return configEntry;
        }

        public static ConfigEntry<T> ClientConfig<T>(string group, string name, T value, string description) =>
            ClientConfig(group, name, value, new ConfigDescription(description));
    }
}
