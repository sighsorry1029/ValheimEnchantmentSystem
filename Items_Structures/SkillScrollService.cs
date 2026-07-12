using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem.Items_Structures;

public static class SkillScrollService
{
    private const float InitializationRetryDelaySeconds = 0.2f;
    private const string SkillScrollPrefabPrefix = "kg_EnchantSkillScroll_";
    private const string RoutedRequestConsume = "VES_RequestConsumeExpScroll_Server";
    private const string RoutedGrantConsume = "VES_GrantConsumeExpScroll_Client";
    private const string SkillScrollConsumedZdoKey = "VES_Consumed";
    private const string ValidSkillScrollTiers = "FEDCBAS";
    private const float MaxConsumeDistance = 6f;
    private static readonly Dictionary<char, ConfigEntry<int>> BookXpByTier = new();
    private static readonly List<GameObject> SkillScrollPrefabs = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        int skillScrollExpOrder = 600;
        foreach (char tier in EnchantmentTierCatalog.AllTiers)
        {
            BookXpByTier[tier] = ValheimEnchantmentSystem.config(
                "Skill Scrolls",
                $"Skill EXP Scroll {tier}",
                EnchantmentTierCatalog.GetDefaultSkillScrollExp(tier),
                ConfigurationManagerDisplay.Description(
                    $"Skill EXP granted by a tier {tier} skill scroll.",
                    ConfigurationManagerDisplay.Skill,
                    skillScrollExpOrder,
                    $"Skill Scroll EXP {tier}"));
            skillScrollExpOrder -= 10;
        }
    }

    public static void RegisterSkillScrollPrefab(GameObject skillScrollPrefab)
    {
        if (skillScrollPrefab == null)
            return;

        if (skillScrollPrefab.GetComponent<ExpScroll>() == null)
            skillScrollPrefab.AddComponent<ExpScroll>();
        if (!SkillScrollPrefabs.Contains(skillScrollPrefab))
            SkillScrollPrefabs.Add(skillScrollPrefab);
    }

    private static bool TryGetConfiguredExpValue(GameObject item, out int expValue)
    {
        expValue = 0;

        string prefabName = global::Utils.GetPrefabName(item);
        if (string.IsNullOrEmpty(prefabName))
        {
            return false;
        }
        if (!TryGetSkillScrollTier(prefabName, out char tier))
        {
            return false;
        }
        if (!BookXpByTier.TryGetValue(tier, out ConfigEntry<int> exp))
        {
            return false;
        }

        expValue = exp.Value;
        if (expValue <= 0)
        {
            return false;
        }

        return true;
    }

    internal static bool TryGetSkillScrollTier(string? prefabName, out char tier)
    {
        tier = default;
        if (string.IsNullOrEmpty(prefabName) ||
            prefabName.Length != SkillScrollPrefabPrefix.Length + 1 ||
            !prefabName.StartsWith(SkillScrollPrefabPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        tier = prefabName[SkillScrollPrefabPrefix.Length];
        return ValidSkillScrollTiers.IndexOf(tier) >= 0;
    }

    private static bool TryGetCurrentZdoIdText(ZNetView znv, out string zdoIdText)
    {
        zdoIdText = string.Empty;
        if (znv == null || !znv.IsValid())
            return false;

        ZDO zdo = znv.GetZDO();
        if (zdo == null)
            return false;

        zdoIdText = zdo.m_uid.ToString();
        return !string.IsNullOrWhiteSpace(zdoIdText);
    }

    private static bool TryParseZdoId(string text, out ZDOID zdoId)
    {
        zdoId = ZDOID.None;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] split = text.Split(':');
        if (split.Length < 2)
            return false;

        if (!long.TryParse(split[0], out long userId))
            return false;
        if (!uint.TryParse(split[1], out uint id))
            return false;

        zdoId = new ZDOID(userId, id);
        return true;
    }

    private static bool TryResolveSkillScrollZdo(string zdoIdText, out ZDO zdo, out GameObject prefab)
    {
        zdo = null!;
        prefab = null!;

        if (!TryParseZdoId(zdoIdText, out ZDOID zdoId))
        {
            return false;
        }

        if (ZDOMan.instance == null)
        {
            return false;
        }

        zdo = ZDOMan.instance.GetZDO(zdoId);
        if (zdo == null)
        {
            return false;
        }

        if (ZNetScene.instance == null)
        {
            return false;
        }

        int prefabHash = zdo.GetPrefab();
        if (prefabHash == 0)
        {
            return false;
        }

        prefab = ZNetScene.instance.GetPrefab(prefabHash);
        if (prefab == null)
        {
            return false;
        }

        if (!TryGetSkillScrollTier(prefab.name, out _) || prefab.GetComponent<ExpScroll>() == null)
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            foreach (GameObject prefab in SkillScrollPrefabs)
            {
                if (prefab == null)
                    continue;
                if (!__instance.m_prefabs.Contains(prefab))
                    __instance.m_prefabs.Add(prefab);

                __instance.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
            }

            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.Register<string>(RoutedRequestConsume, RPC_RequestConsume_Server);
                ZRoutedRpc.instance.Register<int>(RoutedGrantConsume, RPC_GrantConsume_Client);
            }
        }
    }

    private static void RPC_RequestConsume_Server(long sender, string zdoIdText)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
            return;

        if (!TryResolveSkillScrollZdo(zdoIdText, out ZDO zdo, out GameObject prefab))
        {
            return;
        }

        if (!CanSenderConsumeScroll(sender, zdo))
        {
            return;
        }

        TryConsumeFromServer(zdo, prefab, sender);
    }

    private static bool CanSenderConsumeScroll(long sender, ZDO scrollZdo)
    {
        if (scrollZdo == null || ZRoutedRpc.instance == null || ZDOMan.instance == null)
        {
            return false;
        }

        // Dedicated servers may know the scroll ZDO without loading its live GameObject near a remote player.
        if (!TryGetSenderPosition(sender, out Vector3 senderPosition))
        {
            return false;
        }

        return (senderPosition - scrollZdo.GetPosition()).sqrMagnitude <= MaxConsumeDistance * MaxConsumeDistance;
    }

    private static bool TryGetSenderPosition(long sender, out Vector3 position)
    {
        position = default;
        if (ZRoutedRpc.instance == null || ZDOMan.instance == null)
        {
            return false;
        }

        if (sender == ZRoutedRpc.instance.m_id)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return false;
            }

            position = localPlayer.transform.position;
            return true;
        }

        ZNetPeer peer = ZRoutedRpc.instance.GetPeer(sender);
        if (peer == null || !peer.IsReady() || peer.m_characterID.IsNone())
        {
            return false;
        }

        ZDO playerZdo = ZDOMan.instance.GetZDO(peer.m_characterID);
        if (playerZdo == null)
        {
            return false;
        }

        position = playerZdo.GetPosition();
        return true;
    }

    private static void RPC_GrantConsume_Client(long sender, int expValue)
    {
        if (ZRoutedRpc.instance != null && sender != ZRoutedRpc.instance.GetServerPeerID())
            return;

        ExpScroll.GrantConsumeLocal(expValue);
    }

    private static void TryConsumeFromServer(ZDO zdo, GameObject prefab, long sender)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
            return;

        if (zdo == null)
        {
            return;
        }

        if (zdo.GetBool(SkillScrollConsumedZdoKey))
            return;

        if (!TryGetConfiguredExpValue(prefab, out int expValue))
        {
            return;
        }

        zdo.Set(SkillScrollConsumedZdoKey, true);
        zdo.SetOwner(ZDOMan.GetSessionID());

        if (ZRoutedRpc.instance != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, RoutedGrantConsume, expValue);
        }

        if (ZNetScene.instance == null || ZDOMan.instance == null)
            return;

        GameObject liveInstance = ZNetScene.instance.FindInstance(zdo.m_uid);
        if (liveInstance != null)
        {
            ZNetScene.instance.Destroy(liveInstance);
            return;
        }

        ZDOMan.instance.DestroyZDO(zdo);
    }

    public sealed class ExpScroll : MonoBehaviour, Interactable, Hoverable
    {
        private const string ZdoCreationTime = "CreationTime";
        private const int MaxDuration = 120;

        private ZNetView _znv = null!;
        private bool _expirationInitialized;

        private int CreationTime
        {
            get => _znv.GetZDO().GetInt(ZdoCreationTime);
            set => _znv.GetZDO().Set(ZdoCreationTime, value);
        }

        private void Awake()
        {
            _znv = GetComponent<ZNetView>();
            TryInitialize();
        }

        private void TryInitialize()
        {
            CancelInvoke(nameof(TryInitialize));

            if (_znv == null)
            {
                _znv = GetComponent<ZNetView>();
            }

            if (_znv == null)
            {
                ScheduleInitializationRetry();
                return;
            }

            if (!_znv.IsValid())
            {
                ScheduleInitializationRetry();
                return;
            }

            if (_expirationInitialized || !_znv.IsOwner())
            {
                return;
            }

            _expirationInitialized = true;
            if (!TryGetCurrentSeconds(out double currentSeconds))
            {
                Invoke(nameof(ScheduleExpiration), 1f);
                return;
            }

            if (CreationTime == 0)
            {
                CreationTime = (int)Math.Floor(currentSeconds);
            }

            ScheduleExpiration();
        }

        private void ScheduleInitializationRetry()
        {
            Invoke(nameof(TryInitialize), InitializationRetryDelaySeconds);
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(TryInitialize));
            CancelInvoke(nameof(ScheduleExpiration));
            CancelInvoke(nameof(ExpireIfNeeded));
        }

        private void ScheduleExpiration()
        {
            CancelInvoke(nameof(ScheduleExpiration));
            CancelInvoke(nameof(ExpireIfNeeded));

            if (_znv == null || !_znv.IsValid() || !_znv.IsOwner())
                return;

            ZDO zdo = _znv.GetZDO();
            if (zdo == null || zdo.GetBool(SkillScrollConsumedZdoKey))
                return;
            if (!TryGetCurrentSeconds(out double currentSeconds))
            {
                Invoke(nameof(ScheduleExpiration), 1f);
                return;
            }

            double remainingSeconds = MaxDuration - (currentSeconds - CreationTime);
            if (remainingSeconds <= 0f)
            {
                ExpireIfNeeded();
                return;
            }

            Invoke(nameof(ExpireIfNeeded), (float)remainingSeconds);
        }

        private void ExpireIfNeeded()
        {
            if (_znv == null || !_znv.IsValid() || !_znv.IsOwner())
                return;

            ZDO zdo = _znv.GetZDO();
            if (zdo == null || zdo.GetBool(SkillScrollConsumedZdoKey))
                return;
            if (!TryGetCurrentSeconds(out double currentSeconds))
            {
                Invoke(nameof(ScheduleExpiration), 1f);
                return;
            }

            if (currentSeconds - CreationTime < MaxDuration)
            {
                ScheduleExpiration();
                return;
            }

            if (ZNetScene.instance == null)
                return;

            _znv.ClaimOwnership();
            ZNetScene.instance.Destroy(gameObject);
        }

        private static bool TryGetCurrentSeconds(out double currentSeconds)
        {
            currentSeconds = 0d;
            if (EnvMan.instance == null)
                return false;

            currentSeconds = EnvMan.instance.m_totalSeconds;
            return true;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (_znv == null || !_znv.IsValid())
                TryInitialize();

            if (_znv == null || !_znv.IsValid())
            {
                return false;
            }
            if (hold)
                return false;
            if (_znv.GetZDO() == null || _znv.GetZDO().GetBool(SkillScrollConsumedZdoKey))
                return false;

            if (ZRoutedRpc.instance == null)
            {
                return false;
            }

            if (!TryGetCurrentZdoIdText(_znv, out string zdoIdText))
            {
                return false;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(0L, RoutedRequestConsume, zdoIdText);
            return true;
        }

        internal static void GrantConsumeLocal(int expValue)
        {
            if (expValue <= 0)
                return;

            Utils.IncreaseSkillEXP(Enchantment_Skill.SkillType_Enchantment, expValue);

            if (ValheimEnchantmentSystem.NoGraphics || ZNetScene.instance == null)
                return;

            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            GameObject fx = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
            if (fx != null)
                Object.Instantiate(fx, player.transform.position, Quaternion.identity);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return "<b>$enchantment_skill_scroll</b>\n\n[<color=yellow><b>$KEY_Use</b></color>] $enchantment_skill_scroll_use".Localize();
        }

        public string GetHoverName()
        {
            return string.Empty;
        }
    }
}
