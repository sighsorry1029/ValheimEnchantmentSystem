using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

[VES_Autoload]
public static class Notifications_UI
{
    private const int SuccessWebhook1Order = 600;
    private const int SuccessWebhook2Order = 599;
    private const int SuccessWebhook3Order = 598;
    private const int FailWebhook1Order = 500;
    private const int FailWebhook2Order = 499;
    private const int FailWebhook3Order = 498;

    public static readonly ConfigEntry<string>[] _successWebhooks = new ConfigEntry<string>[3];
    public static readonly ConfigEntry<string>[] _failWebhooks = new ConfigEntry<string>[3];
    public static ConfigEntry<int> _duration;
    private const float FadeDuration = 0.25f;


    private class Notification
    {
        public string PlayerName;
        public string ItemPrefab;
        public int Type;
        public int PrevLevel;
        public int Level;
    }

    private static readonly Queue<Notification> _notifications = new();

    private static bool IsVisible() => UI && UI.activeSelf;

    private static GameObject UI;
    private static readonly List<Image> _colorGroup = new();

    private static readonly Color SuccessColor = Color.green;
    private static readonly Color FailColor = Color.red;

    private static Image ItemIcon;
    private static Text ResultText;
    private static Text ItemNameText;
    private static Transform Scaler;
    private static Image Outline;

    [Flags]
    public enum Filter
    {
        None = 0, Success = 1, Fail = 2
    }

    public static bool HasFlagFast(this Filter value, Filter flag) => (value & flag) == flag;
    public static ConfigEntry<Filter> _filterConfig;
    private const float NotificationRequestCooldown = 0.35f;
    private static readonly Dictionary<long, float> _requestCooldownUntil = new();

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private static ConfigDescription OrderedDescription(string description, int order) => new(
        description,
        null,
        new ConfigurationManagerAttributes { Order = order });

    [UsedImplicitly]
    private static void OnInit()
    {
        _successWebhooks[0] = ValheimEnchantmentSystem.config("Notifications", "SuccessWebhook1", "", OrderedDescription("Discord webhook #1 for success notifications", SuccessWebhook1Order), false);
        _successWebhooks[1] = ValheimEnchantmentSystem.config("Notifications", "SuccessWebhook2", "", OrderedDescription("Discord webhook #2 for success notifications", SuccessWebhook2Order), false);
        _successWebhooks[2] = ValheimEnchantmentSystem.config("Notifications", "SuccessWebhook3", "", OrderedDescription("Discord webhook #3 for success notifications", SuccessWebhook3Order), false);
        _failWebhooks[0] = ValheimEnchantmentSystem.config("Notifications", "FailWebhook1", "", OrderedDescription("Discord webhook #1 for fail notifications", FailWebhook1Order), false);
        _failWebhooks[1] = ValheimEnchantmentSystem.config("Notifications", "FailWebhook2", "", OrderedDescription("Discord webhook #2 for fail notifications", FailWebhook2Order), false);
        _failWebhooks[2] = ValheimEnchantmentSystem.config("Notifications", "FailWebhook3", "", OrderedDescription("Discord webhook #3 for fail notifications", FailWebhook3Order), false);
        if (ValheimEnchantmentSystem.NoGraphics) return;
        _filterConfig = ValheimEnchantmentSystem.ClientConfig("Notifications", "Filter", Filter.Success, "Filter notifications by type");
        _duration = ValheimEnchantmentSystem.ClientConfig("Notifications", "Duration", 5, "Duration of notification");

        UI = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_EnchantmentUI_Notification"));
        UI.name = "kg_EnchantmentUI_Notification";
        UI.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(UI);

        Scaler = UI.transform.Find("Canvas/Scaler");
        ItemIcon = UI.transform.Find("Canvas/Scaler/NotificationItem/bg/Icon").GetComponent<Image>();
        ResultText = UI.transform.Find("Canvas/Scaler/NotificationText/Result").GetComponent<Text>();
        ItemNameText = UI.transform.Find("Canvas/Scaler/NotificationText/Text").GetComponent<Text>();
        Outline = UI.transform.Find("Canvas/Scaler/NotificationItem/outline").GetComponent<Image>();
        _colorGroup.AddRange(UI.GetComponentsInChildren<Image>(true).Where(t => t.name == "colorcontrol").Select(x => x.GetComponent<Image>()));
    }


    private static readonly Vector3 OriginalScale = new Vector3(1.25f, 1.25f, 1f);
    private static float _timer;
    private static float _dequeueTimer = 1f;

    public static void Update()
    {
        _dequeueTimer -= Time.deltaTime;
        if (_dequeueTimer <= 0f)
        {
            _dequeueTimer = 1f;
            if (_notifications.Count > 0 && !IsVisible() && Player.m_localPlayer)
            {
                Notification notification = _notifications.Dequeue();
                ShowNotification(notification);
            }
        }

        if (!IsVisible()) return;
        _timer += Time.deltaTime;

        int duration = _duration.Value;

        if (_timer >= duration)
            Hide();
        else if (_timer <= FadeDuration)
            Scaler.localScale = OriginalScale * (_timer / FadeDuration);
        else if (_timer >= duration - FadeDuration)
            Scaler.localScale = OriginalScale * ((duration - _timer) / FadeDuration);
        else
            Scaler.localScale = OriginalScale;
    }

    internal static void ResetTransientState()
    {
        _notifications.Clear();
        _requestCooldownUntil.Clear();
        _dequeueTimer = 1f;
        Hide();
    }

    private static void Hide()
    {
        _timer = 0f;
        if (Scaler != null)
        {
            Scaler.localScale = Vector3.zero;
        }

        if (UI != null)
        {
            UI.SetActive(false);
        }
    }

    private static void ShowNotification(Notification not)
    {
        NotificationItemResult type = (NotificationItemResult)not.Type;

        switch (type)
        {
            case NotificationItemResult.Success when !_filterConfig.Value.HasFlagFast(Filter.Success):
                _dequeueTimer = 0f;
                return;
            case NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed when !_filterConfig.Value.HasFlagFast(Filter.Fail):
                _dequeueTimer = 0f;
                return;
        }


        GameObject item = ZNetScene.instance.GetPrefab(not.ItemPrefab);
        if (!item || item.GetComponent<ItemDrop>() is not { } itemDrop) return;

        string localizedItemName = itemDrop.m_itemData.m_shared.m_name.Localize();


        _timer = 0f;
        Scaler.localScale = OriginalScale;

        ResultText.text = type switch
        {
            NotificationItemResult.Success => "$enchantment_notification_success_topic".Localize(),
            NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed => "$enchantment_notification_fail_topic".Localize(),
            _ => ""
        };
        ResultText.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;
        ItemIcon.sprite = itemDrop.m_itemData.GetIcon();

        foreach (Image image in _colorGroup)
            image.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;

        string text = type switch
        {
            NotificationItemResult.Success => "$enchantment_notification_success".Localize(not.PlayerName, localizedItemName,
                not.PrevLevel.ToString(), not.Level.ToString()),
            NotificationItemResult.LevelDecrease => "$enchantment_notification_fail".Localize(not.PlayerName, localizedItemName, not.PrevLevel.ToString(),
                not.Level.ToString()),
            NotificationItemResult.Destroyed => "$enchantment_notification_fail_destroyed".Localize(not.PlayerName, localizedItemName, not.PrevLevel.ToString()),
            _ => ""
        };
        ItemNameText.text = text;
        ItemNameText.color = type is NotificationItemResult.Success ? SuccessColor : FailColor;
        Outline.color = SyncedData.GetColor(not.ItemPrefab, not.Level, out _, true, type is NotificationItemResult.Success ? "#00FF00" : "#FF0000").IncreaseColorLight().ToColorAlpha();
        UI.SetActive(true);
    }

    public enum NotificationItemResult { Success, LevelDecrease, Destroyed }

    private static bool IsServerSender(long sender)
    {
        if (ZRoutedRpc.instance == null) return false;
        return sender == ZRoutedRpc.instance.GetServerPeerID();
    }

    private static bool IsValidNotification(int type, int prevLevel, int level, string itemPrefab)
    {
        if (!Enum.IsDefined(typeof(NotificationItemResult), type)) return false;
        if (prevLevel < 0 || level < 0 || prevLevel > 500 || level > 500) return false;
        if (string.IsNullOrWhiteSpace(itemPrefab) || !ZNetScene.instance) return false;
        return ZNetScene.instance.GetPrefab(itemPrefab)?.GetComponent<ItemDrop>() is not null;
    }

    private static bool TryResolveSenderPlayerName(long sender, out string playerName)
    {
        playerName = "";
        if (ZRoutedRpc.instance == null) return false;
        object peer = ZRoutedRpc.instance.GetPeer(sender);
        if (peer == null) return false;

        FieldInfo field = AccessTools.Field(peer.GetType(), "m_playerName");
        if (field?.GetValue(peer) is string fieldValue && !string.IsNullOrWhiteSpace(fieldValue))
        {
            playerName = fieldValue;
            return true;
        }

        PropertyInfo property = AccessTools.Property(peer.GetType(), "m_playerName");
        if (property?.GetValue(peer) is string propertyValue && !string.IsNullOrWhiteSpace(propertyValue))
        {
            playerName = propertyValue;
            return true;
        }

        return false;
    }

    private static bool IsAllowedByCooldown(long sender)
    {
        float now = Time.time;
        if (_requestCooldownUntil.TryGetValue(sender, out float until) && now < until) return false;
        _requestCooldownUntil[sender] = now + NotificationRequestCooldown;
        return true;
    }

    private static IEnumerable<string> GetWebhookTargets(NotificationItemResult type)
    {
        ConfigEntry<string>[] sources = type is NotificationItemResult.Success ? _successWebhooks : _failWebhooks;
        foreach (ConfigEntry<string> source in sources)
        {
            if (source == null) continue;
            string link = (source.Value ?? "").Trim();
            if (link.Length == 0) continue;
            yield return link;
        }
    }

    private static void TrySendDiscordNotification(string playerName, string itemPrefab, NotificationItemResult type, int prevLevel, int level)
    {
        string localizedItemName = itemPrefab;
        if (ZNetScene.instance.GetPrefab(itemPrefab)?.GetComponent<ItemDrop>() is { } itemDrop)
            localizedItemName = itemDrop.m_itemData.m_shared.m_name.Localize();

        string text = type switch
        {
            NotificationItemResult.Success => "$enchantment_notification_success".Localize(playerName, localizedItemName, prevLevel.ToString(), level.ToString()),
            NotificationItemResult.LevelDecrease => "$enchantment_notification_fail".Localize(playerName, localizedItemName, prevLevel.ToString(), level.ToString()),
            NotificationItemResult.Destroyed => "$enchantment_notification_fail_destroyed".Localize(playerName, localizedItemName, prevLevel.ToString()),
            _ => ""
        };

        foreach (string link in GetWebhookTargets(type).Distinct(StringComparer.OrdinalIgnoreCase))
            DiscordWebhook.TrySend(link, text);
    }

    private static void ServerPublishNotification(string playerName, string itemPrefab, int type, int prevLevel, int level)
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        if (!IsValidNotification(type, prevLevel, level, itemPrefab)) return;

        NotificationItemResult typedResult = (NotificationItemResult)type;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "kg_Enchantment_GlobalNotification",
            playerName ?? "No Name", itemPrefab, type, prevLevel, level);
        TrySendDiscordNotification(playerName ?? "No Name", itemPrefab, typedResult, prevLevel, level);
    }

    public static void AddNotification(string playerName, string itemPrefab, int type, int prevLevel, int level)
    {
        if (ZRoutedRpc.instance == null) return;

        if (ZNet.instance && ZNet.instance.IsServer())
        {
            ServerPublishNotification(playerName ?? "No Name", itemPrefab ?? "No Prefab", type, prevLevel, level);
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC("kg_Enchantment_GlobalNotification_Request",
            itemPrefab ?? "No Prefab", type, prevLevel, level);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [ClientOnlyPatch]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ResetTransientState();
            ZRoutedRpc.instance.Register("kg_Enchantment_GlobalNotification",
                (long sender, string playerName, string itemPrefab, int type, int prevLevel, int level) =>
                {
                    if (!IsServerSender(sender)) return;
                    switch ((NotificationItemResult)type)
                    {
                        case NotificationItemResult.Success when !_filterConfig.Value.HasFlagFast(Filter.Success):
                            return;
                        case NotificationItemResult.LevelDecrease or NotificationItemResult.Destroyed when !_filterConfig.Value.HasFlagFast(Filter.Fail):
                            return;
                    }

                    _notifications.Enqueue(new Notification
                    {
                        PlayerName = playerName,
                        ItemPrefab = itemPrefab,
                        Type = type,
                        Level = level,
                        PrevLevel = prevLevel
                    });
                });
        }
    }
    
    [HarmonyPatch(typeof(ZNetScene),nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Server_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            ResetTransientState();
            ZRoutedRpc.instance.Register("kg_Enchantment_GlobalNotification_Request",
                (long sender, string itemPrefab, int type, int prevLevel, int level) =>
                {
                    if (!ZNet.instance || !ZNet.instance.IsServer()) return;
                    if (!IsAllowedByCooldown(sender)) return;
                    if (!TryResolveSenderPlayerName(sender, out string playerName)) return;
                    ServerPublishNotification(playerName, itemPrefab ?? "No Prefab", type, prevLevel, level);
                });
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    [ClientOnlyPatch]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            ResetTransientState();
        }
    }
}
