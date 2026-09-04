using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.UI;

public static class Notifications_UI
{
    private const int SuccessWebhooksOrder = 840;
    private const int FailureWebhooksOrder = 830;

    private static ConfigEntry<string> _successWebhooks = null!;
    private static ConfigEntry<string> _failureWebhooks = null!;
    private static ConfigEntry<int> _webhookMinLevel = null!;
    private static ConfigEntry<int> _notificationMinLevel = null!;
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

    internal static void Initialize()
    {
        _webhookMinLevel = ValheimEnchantmentSystem.config(
            "Notifications",
            "Webhook Minimum Enchant Level",
            NotificationSettings.DefaultMinimumLevel,
            NotificationSettings.CreateWebhookMinimumLevelDescription(),
            false);
        _successWebhooks = ValheimEnchantmentSystem.config(
            "Notifications",
            "Success Webhooks",
            "",
            NotificationDescription(
                "Comma-separated Discord webhook URLs for successful enchantment notifications. Example: URL1, URL2, URL3. Only the server uses these URLs; they are not synchronized to clients.",
                "Success Webhooks",
                SuccessWebhooksOrder),
            false);
        _failureWebhooks = ValheimEnchantmentSystem.config(
            "Notifications",
            "Failure Webhooks",
            "",
            NotificationDescription(
                "Comma-separated Discord webhook URLs for failed enchantment notifications. Example: URL1, URL2, URL3. Only the server uses these URLs; they are not synchronized to clients.",
                "Failure Webhooks",
                FailureWebhooksOrder),
            false);
        if (ValheimEnchantmentSystem.NoGraphics) return;
        _notificationMinLevel = ValheimEnchantmentSystem.ClientConfig(
            "Notifications",
            "Minimum Enchant Level",
            NotificationSettings.DefaultMinimumLevel,
            NotificationSettings.CreateNotificationMinimumLevelDescription());
        _filterConfig = ValheimEnchantmentSystem.ClientConfig(
            "Notifications",
            "Filter",
            Filter.Success,
            ConfigurationManagerDisplay.Description(
                "Filter in-game enchantment notifications by type. None hides all notifications on this client. Does not affect other players or webhook delivery. Notifications are displayed for 5 seconds.",
                ConfigurationManagerDisplay.Client,
                900,
                "Notifications - Filter"));
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

    private static ConfigDescription NotificationDescription(
        string description,
        string displayName,
        int order)
    {
        return ConfigurationManagerDisplay.Description(
            description,
            ConfigurationManagerDisplay.General,
            order,
            displayName);
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

        float duration = NotificationRules.DisplayDurationSeconds;

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

        if (!NotificationRules.ShouldShowNotification(not.Type, not.Level, _notificationMinLevel.Value, _filterConfig.Value))
        {
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

        ItemDrop itemDrop = ZNetScene.instance.GetPrefab(itemPrefab)?.GetComponent<ItemDrop>();
        if (itemDrop == null || SyncedData.GetReqs(itemPrefab) == null) return false;
        if (!SyncedData.IsLevelEnchantable(itemPrefab, prevLevel, itemDrop.m_itemData.IsWeapon())) return false;

        NotificationItemResult result = (NotificationItemResult)type;
        return result switch
        {
            NotificationItemResult.Success => level == prevLevel + 1,
            NotificationItemResult.LevelDecrease => IsValidLevelDecrease(prevLevel, level),
            NotificationItemResult.Destroyed => level == prevLevel,
            _ => false
        };
    }

    private static bool IsValidLevelDecrease(int prevLevel, int level)
    {
        int configuredDecrease = Mathf.Clamp(SyncedData.FailedEnchantLevelDecrease.Value, 1, 100);
        return level == prevLevel || level == Mathf.Max(0, prevLevel - configuredDecrease);
    }

    private static bool TryResolveSenderPlayerName(long sender, out string playerName)
    {
        playerName = "";
        if (ZRoutedRpc.instance == null) return false;
        ZNetPeer peer = ZRoutedRpc.instance.GetPeer(sender);
        if (peer == null || !peer.IsReady() || peer.m_characterID.IsNone()) return false;
        if (string.IsNullOrWhiteSpace(peer.m_playerName)) return false;

        playerName = peer.m_playerName;
        return true;
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
        ConfigEntry<string> source = type is NotificationItemResult.Success ? _successWebhooks : _failureWebhooks;
        return WebhookListConfig.ParseTargets(source.Value);
    }

    private static void TrySendDiscordNotification(string playerName, string itemPrefab, NotificationItemResult type, int prevLevel, int level)
    {
        if (!NotificationRules.MeetsMinimumLevel(level, _webhookMinLevel.Value)) return;

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

        foreach (string link in GetWebhookTargets(type))
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
                    if (!NotificationRules.ShouldShowNotification(type, level, _notificationMinLevel.Value, _filterConfig.Value)) return;

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
