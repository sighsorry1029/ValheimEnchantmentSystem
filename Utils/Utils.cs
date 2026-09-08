using System.Reflection.Emit;
using BepInEx.Logging;
using ItemDataManager;
using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem;

public static class Utils
{
    public static bool IsDebug_VES => Player.m_debugMode || ZNet.IsSinglePlayer;
    public static bool IsDebug_Strict => Player.m_debugMode;

    public static void print(object obj, ConsoleColor color = ConsoleColor.DarkGreen)
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ConsoleManager.SetConsoleColor(color);
            ConsoleManager.StandardOutStream.WriteLine($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] {obj}");
            ConsoleManager.SetConsoleColor(ConsoleColor.White);
            foreach (ILogListener logListener in BepInEx.Logging.Logger.Listeners)
                if (logListener is DiskLogListener { LogWriter: not null } bepinexlog)
                    bepinexlog.LogWriter.WriteLine($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] {obj}");
        }
        else
        {
            MonoBehaviour.print($"[{DateTime.Now}] [kg.ValheimEnchantmentSystem] " + obj);
        }
    }

    public static void arr_print(IEnumerable arr, ConsoleColor color = ConsoleColor.DarkGreen)
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ConsoleManager.SetConsoleColor(color);
            ConsoleManager.StandardOutStream.WriteLine($"[ValheimEnchantmentSystem] printing array: {arr}");
            int c = 0;
            foreach (object item in arr)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[{c++}] {item}");
                foreach (ILogListener logListener in BepInEx.Logging.Logger.Listeners)
                    if (logListener is DiskLogListener { LogWriter: not null } bepinexlog)
                        bepinexlog.LogWriter.WriteLine($"[{c++}] {item}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }
        else
        {
            MonoBehaviour.print("[ValheimEnchantmentSystem] " + arr);
            int c = 0;
            foreach (object item in arr)
            {
                MonoBehaviour.print($"[{c++}] {item}");
            }
        }
    }

    public static void WriteFile(this string path, string data)
    {
        File.WriteAllText(path, data);
    }

    public static string ReadFile(this string path)
    {
        return File.ReadAllText(path);
    }

    public static string Localize(this string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        try
        {
            return Localization.instance?.Localize(text) ?? text;
        }
        catch
        {
            return text;
        }
    }

    public static string Localize(this string text, params string[] args)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        try
        {
            return Localization.instance?.Localize(text, args) ?? text;
        }
        catch
        {
            return text;
        }
    }

    public static bool StlocIndex(this object obj, int index) =>
        obj is LocalBuilder builder && builder.LocalIndex == index;

    public static Color ToColorAlpha(this string colorString)
    {
        if (colorString[0] == '#')
        {
            colorString = colorString.Substring(1);
        }

        if (!uint.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, null, out uint colorValue))
            return Color.white;
        switch (colorString.Length)
        {
            case 8:
            {
                float r = ((colorValue >> 24) & 0xFF) / 255.0f;
                float g = ((colorValue >> 16) & 0xFF) / 255.0f;
                float b = ((colorValue >> 8) & 0xFF) / 255.0f;
                float a = (colorValue & 0xFF) / 255.0f;
                Color color = new Color(r, g, b, a);
                return color;
            }
            case 6:
            {
                float r = ((colorValue >> 16) & 0xFF) / 255.0f;
                float g = ((colorValue >> 8) & 0xFF) / 255.0f;
                float b = (colorValue & 0xFF) / 255.0f;
                Color color = new Color(r, g, b, 1f);
                return color;
            }
            default:
                return Color.white;
        }
    }

    public static int CustomCountItemsNoLevel(string prefab)
    {
        int num = 0;
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab)
            {
                num += itemData.m_stack;
            }
        }

        return num;
    }

    public static void CustomRemoveItemsNoLevel(string prefab, int amount)
    {
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab)
            {
                int num = Mathf.Min(itemData.m_stack, amount);
                itemData.m_stack -= num;
                amount -= num;
                if (amount <= 0)
                    break;
            }
        }

        Player.m_localPlayer.m_inventory.m_inventory.RemoveAll(x => x.m_stack <= 0);
        Player.m_localPlayer.m_inventory.Changed();
    }

    public static string IncreaseColorLight(this string color)
    {
        if (!ColorUtility.TryParseHtmlString(color, out Color c)) return color;
        Color.RGBToHSV(c, out float h, out float s, out float v);
        v = 1f;
        c = Color.HSVToRGB(h, s, v);
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static Color IncreaseColorLight(this Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        v = 1f;
        c = Color.HSVToRGB(h, s, v);
        return c;
    }

    public static string GetPrefabNameByItemName(string itemname)
    {
        GameObject find = ObjectDB.instance.m_items.FirstOrDefault(x =>
            x.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == itemname);
        if (find == null) return null;
        return find.name;
    }

    public static bool TryDeserializeYAML<T>(string text, out T obj, out string error)
    {
        obj = default!;
        error = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "content is empty";
                return false;
            }

            T parsed = new DeserializerBuilder().Build().Deserialize<T>(text);
            if (parsed is null)
            {
                error = "deserialized to null";
                return false;
            }

            obj = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    public static bool TryFromYAML<T>(this string path, out T obj, out string error)
    {
        obj = default!;

        try
        {
            if (!File.Exists(path))
            {
                error = "file does not exist";
                return false;
            }

            string text = File.ReadAllText(path);
            return TryDeserializeYAML(text, out obj, out error);
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    public static T FromYAML<T>(this string path)
    {
        if (path.TryFromYAML(out T obj, out string error))
            return obj;

        throw new InvalidOperationException($"Error while deserializing {path}: {error}");
    }

    public static IEnumerable<Enchantment_Core.Enchanted> EquippedEnchantments(this Player p) =>
        p.m_inventory.GetEquippedItems().Select(x => x.Data().Get<Enchantment_Core.Enchanted>()).Where(x => x?.level > 0);

    private static IEnumerator DelayedAction(Action invoke, int skipFrames)
    {
        for (int i = 0; i < skipFrames; i++)
            yield return null;

        invoke();
    }

    public static void DelayedInvoke(this MonoBehaviour mb, Action invoke, int skipFrames)
    {
        mb.StartCoroutine(DelayedAction(invoke, skipFrames));
    }

    public static double RoundOne(this float f)
    {
        return Math.Round(f, 1);
    }


    public static void IncreaseSkillEXP(Skills.SkillType skillType, float expToAdd)
    {
        Player localPlayer = Player.m_localPlayer;
        if (localPlayer == null || expToAdd <= 0f)
        {
            return;
        }

        Skills.Skill skill = localPlayer.m_skills.GetSkill(skillType);

        if (skill != null)
        {
            float gainFactor = Mathf.Max(0f, skill.m_info?.m_increseStep ?? 1f);
            expToAdd *= gainFactor;

            while (expToAdd > 0)
            {
                float nextLevelRequirement = skill.GetNextLevelRequirement();
                if (skill.m_accumulator + expToAdd >= nextLevelRequirement)
                {
                    expToAdd -= nextLevelRequirement - skill.m_accumulator;
                    skill.m_accumulator = 0;
                    skill.m_level++;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                }
                else
                {
                    skill.m_accumulator += expToAdd;
                    expToAdd = 0;
                }
            }
        }
    }

    public static Transform FindChild(Transform aParent, string aName)
    {
        Stack<Transform> stack = new Stack<Transform>();
        Transform transform = aParent;
        do
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                stack.Push(transform.GetChild(i));
            }

            if (stack.Count <= 0)
            {
                return null;
            }

            transform = stack.Pop();
        } while (transform.name != aName);
        return transform;
    }

    public static bool IsEnemy(this Character c)
    {
        if (c == Player.m_localPlayer) return false;
        if (c.IsPlayer())
        {
            return Player.m_localPlayer.IsPVPEnabled() && c.IsPVPEnabled();
        }

        return !c.m_baseAI || c.m_baseAI.IsEnemy(Player.m_localPlayer);
    }

    public static void InstantiateItem(GameObject prefab, int count, int level, Inventory overrideInventory = null)
    {
        Player p = Player.m_localPlayer;
        if (!p || !prefab || count <= 0 || ZNetScene.instance == null) return;

        Inventory inventory = overrideInventory ?? p.m_inventory;

        if (prefab.GetComponent<ItemDrop>() is not { } item) return;
        
        if (item.m_itemData.m_shared.m_maxStackSize > 1)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab,
                p.transform.position + p.transform.forward * 1.5f + Vector3.up * 1.5f, Quaternion.identity);
            ItemDrop itemDrop = go.GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_quality = level;
            itemDrop.m_itemData.m_stack = count;
            itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
            itemDrop.Save();
            go.SetActive(true);
            TryTransferPreparedItem(itemDrop, inventory);
        }
        else
        {
            for (int i = 0; i < count; ++i)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab,
                    p.transform.position + p.transform.forward * 1.5f + Vector3.up * 1.5f, Quaternion.identity);
                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                itemDrop.m_itemData.m_quality = level;
                itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
                itemDrop.Save();
                go.SetActive(true);
                if (!TryTransferPreparedItem(itemDrop, inventory)) return;
            }
        }
    }

    private static bool TryTransferPreparedItem(ItemDrop itemDrop, Inventory inventory)
    {
        ItemDrop.ItemData prepared = itemDrop.m_itemData;
        string name = prepared.m_shared.m_name;
        int quality = prepared.m_quality;
        int worldLevel = prepared.m_worldLevel;
        bool known = TryAddItemAndGetRemainder(prepared.m_stack,
            () => CountOutputTransferItems(inventory.GetAllItems(), name, quality, worldLevel),
            // AddItem may retain its argument in the inventory. Keep the prepared world item's data independent.
            () => inventory.CanAddItem(itemDrop.gameObject) && inventory.AddItem(prepared.Clone()),
            out int remaining, out string failure);
        if (!known)
        {
            // The prepared output belongs to this call. Revoke only our currently owned output; never guess an inventory refund.
            // Local deactivation alone would leave its full stack in the replicated ZDO.
            try
            {
                ZNetView view = itemDrop.GetComponent<ZNetView>();
                if (ZNetScene.instance != null && ZDOMan.instance != null && view != null && view.IsValid() && view.IsOwner())
                {
                    string outputId = view.GetZDO().m_uid.ToString();
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
                    print($"Scroll output transfer could not be confirmed: {failure} Owned prepared output {outputId} was discarded to avoid duplicate output. Untransferred items may be lost; no refund or retry was created.", ConsoleColor.Yellow);
                }
                else
                {
                    print($"Scroll output transfer could not be confirmed: {failure} Prepared output could not be discarded because local network ownership was not confirmed. Check inventory/world quantities before reuse; no refund or retry was created.", ConsoleColor.Yellow);
                }
            }
            catch (Exception error)
            {
                print($"Scroll output transfer could not be confirmed: {failure} Discarding its prepared output also failed: {error.Message} Check inventory/world quantities before reuse; no refund or retry was created.", ConsoleColor.Yellow);
            }
            return false;
        }
        if (!string.IsNullOrEmpty(failure))
            print($"Scroll output transfer reported a problem: {failure} Confirmed untransferred amount: {remaining}.", ConsoleColor.Yellow);

        if (remaining == 0)
        {
            ZNetScene.instance.Destroy(itemDrop.gameObject);
        }
        else
        {
            prepared.m_stack = remaining;
            itemDrop.Save();
            itemDrop.gameObject.SetActive(true);
        }
        return true;
    }

    internal static long CountOutputTransferItems(IEnumerable<ItemDrop.ItemData> items, string name, int quality, int worldLevel)
    {
        long count = 0;
        foreach (ItemDrop.ItemData item in items)
        {
            // Observe vanilla's stack identity; AddItem/ItemDataManager decides whether and how custom values merge.
            // A valid TryStack can retain foreign metadata or produce a new value, so raw dictionary equality is not required.
            if (item?.m_shared?.m_name != name || item.m_quality != quality || item.m_worldLevel != worldLevel)
                continue;
            if (item.m_stack < 0)
                throw new InvalidOperationException("An output stack has an invalid count.");
            count += item.m_stack;
        }
        return count;
    }

    internal static bool TryAddItemAndGetRemainder(int requested, Func<long> readCount, Func<bool> addItem,
        out int remaining, out string failure)
    {
        remaining = 0;
        failure = string.Empty;
        try
        {
            long before = readCount();
            if (requested <= 0 || before < 0)
                throw new InvalidOperationException("The output amount or initial inventory count is invalid.");
            bool added = false;
            try
            {
                added = addItem();
            }
            catch (Exception error)
            {
                // Changed callbacks can throw after an add. Observe the inventory before deciding what remains.
                failure = error.Message;
            }
            long after = readCount();
            if (after < before || after - before > requested)
                throw new InvalidOperationException("Inventory output counts changed by an unexpected amount.");
            if (added && after - before != requested)
                // A Changed callback can move successfully added output elsewhere. A net deficit is not proof of a world remainder.
                throw new InvalidOperationException("AddItem reported success without the complete output remaining in the target inventory.");
            remaining = requested - (int)(after - before);
            if (!added && remaining != requested)
                failure = string.IsNullOrEmpty(failure) ? "AddItem's result differs from the observed transfer." : failure;
            return true;
        }
        catch (Exception error)
        {
            failure = error.Message;
            return false;
        }
    }

    /*public static float RoundOne(this float f)
    {
        return f < 100 ? Mathf.Round(f * 10.0f) * 0.1f : Mathf.Round(f);
    }*/
}
