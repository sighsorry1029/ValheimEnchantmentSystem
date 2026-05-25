using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

public static class TerminalCommands
{
    private static void WriteCommandMessage(string message)
    {
        Utils.print(message);
        if (Chat.instance)
        {
            Chat.instance.m_hideTimer = 0f;
            Chat.instance.AddString(message);
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    private static class Terminal_InitTerminal_ConfigReload_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Terminal __instance)
        {
            new Terminal.ConsoleCommand("ves_reloadconfig", ConfigHotReloadRegistrar.Usage, args =>
            {
                string target = args.Length > 1 ? args[1] : "all";
                bool success = ConfigHotReloadRegistrar.TryReload(target, out string message);
                ConfigReloadPoller.ResetSnapshots();
                WriteCommandMessage(success ? message : $"Reload request completed with issues: {message}");
            });
        }
    }

    [HarmonyPatch(typeof(Terminal),nameof(Terminal.InitTerminal))]
    [ClientOnlyPatch]
    private static class Terminal_InitTerminal_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Terminal __instance)
        {
            new Terminal.ConsoleCommand("setenchant", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                int level = int.Parse(args[1]);
                ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
                if(weapon == null || !weapon.m_dropPrefab) return;
                Enchantment_Core.Enchanted en = weapon.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                en.level = level;
                en.Save();
                EnchantmentSideEffects.ApplyStateChanged(en, refreshEquipment: true);
                WriteCommandMessage("Enchantment level set to " + level);
            });
            
            new Terminal.ConsoleCommand("setenchantall", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                int level = int.Parse(args[1]);

                foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.m_inventory.Where(x => SyncedData.GetReqs(x.m_dropPrefab?.name) != null))
                {
                    Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                    en.level = level;
                    en.Save();
                }

                Enchantment_VFX.UpdateGrid();
            });
        }
    }
}
