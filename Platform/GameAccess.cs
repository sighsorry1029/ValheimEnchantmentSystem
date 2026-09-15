namespace kg.ValheimEnchantmentSystem.Platform;

// Cached access to non-public original Valheim fields used by this mod. No publicized runtime DLL is required.
internal static class GameAccess
{
    private static class ArmorStandFields
    {
        internal static readonly AccessTools.FieldRef<ArmorStand, ZNetView> m_nview = AccessTools.FieldRefAccess<ArmorStand, ZNetView>("m_nview");
    }
    internal static ref ZNetView VES_m_nview(this ArmorStand instance) => ref ArmorStandFields.m_nview(instance);

    private static class CharacterFields
    {
        internal static readonly AccessTools.FieldRef<Character, HitData> m_lastHit = AccessTools.FieldRefAccess<Character, HitData>("m_lastHit");
        internal static readonly AccessTools.FieldRef<Character, ZNetView> m_nview = AccessTools.FieldRefAccess<Character, ZNetView>("m_nview");
    }
    internal static ref HitData VES_m_lastHit(this Character instance) => ref CharacterFields.m_lastHit(instance);
    internal static ref ZNetView VES_m_nview(this Character instance) => ref CharacterFields.m_nview(instance);

    private static class ChatFields
    {
        internal static readonly AccessTools.FieldRef<Chat, System.Single> m_hideTimer = AccessTools.FieldRefAccess<Chat, System.Single>("m_hideTimer");
    }
    internal static ref System.Single VES_m_hideTimer(this Chat instance) => ref ChatFields.m_hideTimer(instance);

    private static class ContainerFields
    {
        internal static readonly AccessTools.FieldRef<Container, ZNetView> m_nview = AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");
    }
    internal static ref ZNetView VES_m_nview(this Container instance) => ref ContainerFields.m_nview(instance);

    private static class EnvManFields
    {
        internal static readonly AccessTools.FieldRef<EnvMan, System.Double> m_totalSeconds = AccessTools.FieldRefAccess<EnvMan, System.Double>("m_totalSeconds");
    }
    internal static ref System.Double VES_m_totalSeconds(this EnvMan instance) => ref EnvManFields.m_totalSeconds(instance);

    private static class HotkeyBarFields
    {
        internal static readonly AccessTools.FieldRef<HotkeyBar, System.Collections.Generic.List<ItemDrop.ItemData>> m_items = AccessTools.FieldRefAccess<HotkeyBar, System.Collections.Generic.List<ItemDrop.ItemData>>("m_items");
    }
    internal static ref System.Collections.Generic.List<ItemDrop.ItemData> VES_m_items(this HotkeyBar instance) => ref HotkeyBarFields.m_items(instance);

    private static class HumanoidFields
    {
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_chestItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_chestItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_helmetItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_helmetItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_hiddenLeftItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_hiddenLeftItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_hiddenRightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_hiddenRightItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_leftItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_leftItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_legItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_legItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_rightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");
        internal static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> m_shoulderItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_shoulderItem");
    }
    internal static ref ItemDrop.ItemData VES_m_chestItem(this Humanoid instance) => ref HumanoidFields.m_chestItem(instance);
    internal static ref ItemDrop.ItemData VES_m_helmetItem(this Humanoid instance) => ref HumanoidFields.m_helmetItem(instance);
    internal static ref ItemDrop.ItemData VES_m_hiddenLeftItem(this Humanoid instance) => ref HumanoidFields.m_hiddenLeftItem(instance);
    internal static ref ItemDrop.ItemData VES_m_hiddenRightItem(this Humanoid instance) => ref HumanoidFields.m_hiddenRightItem(instance);
    internal static ref ItemDrop.ItemData VES_m_leftItem(this Humanoid instance) => ref HumanoidFields.m_leftItem(instance);
    internal static ref ItemDrop.ItemData VES_m_legItem(this Humanoid instance) => ref HumanoidFields.m_legItem(instance);
    internal static ref ItemDrop.ItemData VES_m_rightItem(this Humanoid instance) => ref HumanoidFields.m_rightItem(instance);
    internal static ref ItemDrop.ItemData VES_m_shoulderItem(this Humanoid instance) => ref HumanoidFields.m_shoulderItem(instance);

    private static class InventoryGridFields
    {
        internal static readonly AccessTools.FieldRef<InventoryGrid, System.Collections.Generic.List<InventoryElement>> m_elements = AccessTools.FieldRefAccess<InventoryGrid, System.Collections.Generic.List<InventoryElement>>("m_elements");
    }
    internal static ref System.Collections.Generic.List<InventoryElement> VES_m_elements(this InventoryGrid instance) => ref InventoryGridFields.m_elements(instance);

    private static class InventoryGuiFields
    {
        internal static readonly AccessTools.FieldRef<InventoryGui, UnityEngine.Animator> m_animator = AccessTools.FieldRefAccess<InventoryGui, UnityEngine.Animator>("m_animator");
        internal static readonly AccessTools.FieldRef<InventoryGui, System.Single> m_craftTimer = AccessTools.FieldRefAccess<InventoryGui, System.Single>("m_craftTimer");
        internal static readonly AccessTools.FieldRef<InventoryGui, Container> m_currentContainer = AccessTools.FieldRefAccess<InventoryGui, Container>("m_currentContainer");
        internal static readonly AccessTools.FieldRef<InventoryGui, UnityEngine.GameObject> m_dragGo = AccessTools.FieldRefAccess<InventoryGui, UnityEngine.GameObject>("m_dragGo");
        internal static readonly AccessTools.FieldRef<InventoryGui, ItemDrop.ItemData> m_dragItem = AccessTools.FieldRefAccess<InventoryGui, ItemDrop.ItemData>("m_dragItem");
    }
    internal static ref UnityEngine.Animator VES_m_animator(this InventoryGui instance) => ref InventoryGuiFields.m_animator(instance);
    internal static ref System.Single VES_m_craftTimer(this InventoryGui instance) => ref InventoryGuiFields.m_craftTimer(instance);
    internal static ref Container VES_m_currentContainer(this InventoryGui instance) => ref InventoryGuiFields.m_currentContainer(instance);
    internal static ref UnityEngine.GameObject VES_m_dragGo(this InventoryGui instance) => ref InventoryGuiFields.m_dragGo(instance);
    internal static ref ItemDrop.ItemData VES_m_dragItem(this InventoryGui instance) => ref InventoryGuiFields.m_dragItem(instance);

    private static class ItemStandFields
    {
        internal static readonly AccessTools.FieldRef<ItemStand, ZNetView> m_nview = AccessTools.FieldRefAccess<ItemStand, ZNetView>("m_nview");
        internal static readonly AccessTools.FieldRef<ItemStand, UnityEngine.GameObject> m_visualItem = AccessTools.FieldRefAccess<ItemStand, UnityEngine.GameObject>("m_visualItem");
        internal static readonly AccessTools.FieldRef<ItemStand, System.Int32> m_visualVariant = AccessTools.FieldRefAccess<ItemStand, System.Int32>("m_visualVariant");
    }
    internal static ref ZNetView VES_m_nview(this ItemStand instance) => ref ItemStandFields.m_nview(instance);
    internal static ref UnityEngine.GameObject VES_m_visualItem(this ItemStand instance) => ref ItemStandFields.m_visualItem(instance);
    internal static ref System.Int32 VES_m_visualVariant(this ItemStand instance) => ref ItemStandFields.m_visualVariant(instance);

    private static class LocalizationFields
    {
        internal static readonly AccessTools.FieldRef<Localization, System.Collections.Generic.Dictionary<System.String,System.String>> m_translations = AccessTools.FieldRefAccess<Localization, System.Collections.Generic.Dictionary<System.String,System.String>>("m_translations");
    }
    internal static ref System.Collections.Generic.Dictionary<System.String,System.String> VES_m_translations(this Localization instance) => ref LocalizationFields.m_translations(instance);

    private static class ObjectDBFields
    {
        internal static readonly AccessTools.FieldRef<ObjectDB, System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject>> m_itemByHash = AccessTools.FieldRefAccess<ObjectDB, System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject>>("m_itemByHash");
    }
    internal static ref System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject> VES_m_itemByHash(this ObjectDB instance) => ref ObjectDBFields.m_itemByHash(instance);

    private static class PlayerFields
    {
        internal static readonly AccessTools.FieldRef<Player, System.Collections.Generic.HashSet<System.String>> m_knownMaterial = AccessTools.FieldRefAccess<Player, System.Collections.Generic.HashSet<System.String>>("m_knownMaterial");
        internal static readonly AccessTools.FieldRef<Player, System.Collections.Generic.HashSet<System.String>> m_knownRecipes = AccessTools.FieldRefAccess<Player, System.Collections.Generic.HashSet<System.String>>("m_knownRecipes");
        internal static readonly AccessTools.FieldRef<Player, System.Single> m_stamina = AccessTools.FieldRefAccess<Player, System.Single>("m_stamina");
    }
    internal static ref System.Collections.Generic.HashSet<System.String> VES_m_knownMaterial(this Player instance) => ref PlayerFields.m_knownMaterial(instance);
    internal static ref System.Collections.Generic.HashSet<System.String> VES_m_knownRecipes(this Player instance) => ref PlayerFields.m_knownRecipes(instance);
    internal static ref System.Single VES_m_stamina(this Player instance) => ref PlayerFields.m_stamina(instance);

    private static class SkillsFields
    {
        internal static readonly AccessTools.FieldRef<Skills, System.Collections.Generic.Dictionary<Skills.SkillType,Skills.Skill>> m_skillData = AccessTools.FieldRefAccess<Skills, System.Collections.Generic.Dictionary<Skills.SkillType,Skills.Skill>>("m_skillData");
    }
    internal static ref System.Collections.Generic.Dictionary<Skills.SkillType,Skills.Skill> VES_m_skillData(this Skills instance) => ref SkillsFields.m_skillData(instance);

    private static class SpawnAbilityFields
    {
        internal static readonly AccessTools.FieldRef<SpawnAbility, ItemDrop.ItemData> m_weapon = AccessTools.FieldRefAccess<SpawnAbility, ItemDrop.ItemData>("m_weapon");
    }
    internal static ref ItemDrop.ItemData VES_m_weapon(this SpawnAbility instance) => ref SpawnAbilityFields.m_weapon(instance);

    private static class TerminalConsoleCommandFields
    {
        internal static readonly AccessTools.FieldRef<Terminal.ConsoleCommand, Terminal.ConsoleOptionsFetcher> m_tabOptionsFetcher = AccessTools.FieldRefAccess<Terminal.ConsoleCommand, Terminal.ConsoleOptionsFetcher>("m_tabOptionsFetcher");
    }
    internal static ref Terminal.ConsoleOptionsFetcher VES_m_tabOptionsFetcher(this Terminal.ConsoleCommand instance) => ref TerminalConsoleCommandFields.m_tabOptionsFetcher(instance);

    private static class VisEquipmentFields
    {
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>> m_chestItemInstances = AccessTools.FieldRefAccess<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>>("m_chestItemInstances");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentChestItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentChestItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentHelmetItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentHelmetItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentLeftBackItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentLeftBackItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentLeftItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentLeftItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentLegItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentLegItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentRightBackItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentRightBackItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentRightItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentRightItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Int32> m_currentShoulderItemHash = AccessTools.FieldRefAccess<VisEquipment, System.Int32>("m_currentShoulderItemHash");
        internal static readonly AccessTools.FieldRef<VisEquipment, UnityEngine.GameObject> m_helmetItemInstance = AccessTools.FieldRefAccess<VisEquipment, UnityEngine.GameObject>("m_helmetItemInstance");
        internal static readonly AccessTools.FieldRef<VisEquipment, UnityEngine.GameObject> m_leftBackItemInstance = AccessTools.FieldRefAccess<VisEquipment, UnityEngine.GameObject>("m_leftBackItemInstance");
        internal static readonly AccessTools.FieldRef<VisEquipment, UnityEngine.GameObject> m_leftItemInstance = AccessTools.FieldRefAccess<VisEquipment, UnityEngine.GameObject>("m_leftItemInstance");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>> m_legItemInstances = AccessTools.FieldRefAccess<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>>("m_legItemInstances");
        internal static readonly AccessTools.FieldRef<VisEquipment, ZNetView> m_nview = AccessTools.FieldRefAccess<VisEquipment, ZNetView>("m_nview");
        internal static readonly AccessTools.FieldRef<VisEquipment, UnityEngine.GameObject> m_rightBackItemInstance = AccessTools.FieldRefAccess<VisEquipment, UnityEngine.GameObject>("m_rightBackItemInstance");
        internal static readonly AccessTools.FieldRef<VisEquipment, UnityEngine.GameObject> m_rightItemInstance = AccessTools.FieldRefAccess<VisEquipment, UnityEngine.GameObject>("m_rightItemInstance");
        internal static readonly AccessTools.FieldRef<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>> m_shoulderItemInstances = AccessTools.FieldRefAccess<VisEquipment, System.Collections.Generic.List<UnityEngine.GameObject>>("m_shoulderItemInstances");
    }
    internal static ref System.Collections.Generic.List<UnityEngine.GameObject> VES_m_chestItemInstances(this VisEquipment instance) => ref VisEquipmentFields.m_chestItemInstances(instance);
    internal static ref System.Int32 VES_m_currentChestItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentChestItemHash(instance);
    internal static ref System.Int32 VES_m_currentHelmetItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentHelmetItemHash(instance);
    internal static ref System.Int32 VES_m_currentLeftBackItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentLeftBackItemHash(instance);
    internal static ref System.Int32 VES_m_currentLeftItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentLeftItemHash(instance);
    internal static ref System.Int32 VES_m_currentLegItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentLegItemHash(instance);
    internal static ref System.Int32 VES_m_currentRightBackItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentRightBackItemHash(instance);
    internal static ref System.Int32 VES_m_currentRightItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentRightItemHash(instance);
    internal static ref System.Int32 VES_m_currentShoulderItemHash(this VisEquipment instance) => ref VisEquipmentFields.m_currentShoulderItemHash(instance);
    internal static ref UnityEngine.GameObject VES_m_helmetItemInstance(this VisEquipment instance) => ref VisEquipmentFields.m_helmetItemInstance(instance);
    internal static ref UnityEngine.GameObject VES_m_leftBackItemInstance(this VisEquipment instance) => ref VisEquipmentFields.m_leftBackItemInstance(instance);
    internal static ref UnityEngine.GameObject VES_m_leftItemInstance(this VisEquipment instance) => ref VisEquipmentFields.m_leftItemInstance(instance);
    internal static ref System.Collections.Generic.List<UnityEngine.GameObject> VES_m_legItemInstances(this VisEquipment instance) => ref VisEquipmentFields.m_legItemInstances(instance);
    internal static ref ZNetView VES_m_nview(this VisEquipment instance) => ref VisEquipmentFields.m_nview(instance);
    internal static ref UnityEngine.GameObject VES_m_rightBackItemInstance(this VisEquipment instance) => ref VisEquipmentFields.m_rightBackItemInstance(instance);
    internal static ref UnityEngine.GameObject VES_m_rightItemInstance(this VisEquipment instance) => ref VisEquipmentFields.m_rightItemInstance(instance);
    internal static ref System.Collections.Generic.List<UnityEngine.GameObject> VES_m_shoulderItemInstances(this VisEquipment instance) => ref VisEquipmentFields.m_shoulderItemInstances(instance);

    private static class ZInputButtonDefFields
    {
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_heldDynamic = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_heldDynamic");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_pressedDynamic = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_pressedDynamic");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_pressedFixed = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_pressedFixed");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_releasedDynamic = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_releasedDynamic");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_releasedFixed = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_releasedFixed");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_wasPressedDynamic = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_wasPressedDynamic");
        internal static readonly AccessTools.FieldRef<ZInput.ButtonDef, System.Boolean> m_wasPressedFixed = AccessTools.FieldRefAccess<ZInput.ButtonDef, System.Boolean>("m_wasPressedFixed");
    }
    internal static ref System.Boolean VES_m_heldDynamic(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_heldDynamic(instance);
    internal static ref System.Boolean VES_m_pressedDynamic(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_pressedDynamic(instance);
    internal static ref System.Boolean VES_m_pressedFixed(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_pressedFixed(instance);
    internal static ref System.Boolean VES_m_releasedDynamic(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_releasedDynamic(instance);
    internal static ref System.Boolean VES_m_releasedFixed(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_releasedFixed(instance);
    internal static ref System.Boolean VES_m_wasPressedDynamic(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_wasPressedDynamic(instance);
    internal static ref System.Boolean VES_m_wasPressedFixed(this ZInput.ButtonDef instance) => ref ZInputButtonDefFields.m_wasPressedFixed(instance);

    private static class ZNetSceneFields
    {
        internal static readonly AccessTools.FieldRef<ZNetScene, System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject>> m_namedPrefabs = AccessTools.FieldRefAccess<ZNetScene, System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject>>("m_namedPrefabs");
    }
    internal static ref System.Collections.Generic.Dictionary<System.Int32,UnityEngine.GameObject> VES_m_namedPrefabs(this ZNetScene instance) => ref ZNetSceneFields.m_namedPrefabs(instance);

    private static class ZRoutedRpcFields
    {
        internal static readonly AccessTools.FieldRef<ZRoutedRpc, System.Int64> m_id = AccessTools.FieldRefAccess<ZRoutedRpc, System.Int64>("m_id");
    }
    internal static ref System.Int64 VES_m_id(this ZRoutedRpc instance) => ref ZRoutedRpcFields.m_id(instance);

    private static class LocalizationAddWordAccess
    {
        internal static readonly Action<Localization, string, string> Call = AccessTools.MethodDelegate<Action<Localization, string, string>>(AccessTools.Method(typeof(Localization), "AddWord", new Type[] { typeof(string), typeof(string) }));
    }
    internal static void AddWord(this Localization instance, string key, string text) => LocalizationAddWordAccess.Call(instance, key, text);

    private static class SkillsGetSkillAccess
    {
        internal static readonly Func<Skills, Skills.SkillType, Skills.Skill> Call = AccessTools.MethodDelegate<Func<Skills, Skills.SkillType, Skills.Skill>>(AccessTools.Method(typeof(Skills), "GetSkill", new Type[] { typeof(Skills.SkillType) }));
    }
    internal static Skills.Skill GetSkill(this Skills instance, Skills.SkillType type) => SkillsGetSkillAccess.Call(instance, type);

    private static class SkillsSkillGetNextLevelRequirementAccess
    {
        internal static readonly Func<Skills.Skill, float> Call = AccessTools.MethodDelegate<Func<Skills.Skill, float>>(AccessTools.Method(typeof(Skills.Skill), "GetNextLevelRequirement", new Type[] {  }));
    }
    internal static float GetNextLevelRequirement(this Skills.Skill instance) => SkillsSkillGetNextLevelRequirementAccess.Call(instance);

    private static class InventoryChangedAccess
    {
        internal static readonly Action<Inventory, bool, bool> Call = AccessTools.MethodDelegate<Action<Inventory, bool, bool>>(AccessTools.Method(typeof(Inventory), "Changed", new Type[] { typeof(bool), typeof(bool) }));
    }
    internal static void Changed(this Inventory instance, bool success = false, bool cheatedStateChanged = false) => InventoryChangedAccess.Call(instance, success, cheatedStateChanged);

    private static class ZRoutedRpcGetPeerAccess
    {
        internal static readonly Func<ZRoutedRpc, long, ZNetPeer> Call = AccessTools.MethodDelegate<Func<ZRoutedRpc, long, ZNetPeer>>(AccessTools.Method(typeof(ZRoutedRpc), "GetPeer", new Type[] { typeof(long) }));
    }
    internal static ZNetPeer GetPeer(this ZRoutedRpc instance, long uid) => ZRoutedRpcGetPeerAccess.Call(instance, uid);

    private static class ZRoutedRpcGetServerPeerIDAccess
    {
        internal static readonly Func<ZRoutedRpc, long> Call = AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID", new Type[] {  }));
    }
    internal static long GetServerPeerID(this ZRoutedRpc instance) => ZRoutedRpcGetServerPeerIDAccess.Call(instance);

    private static class ObjectDBUpdateRegistersAccess
    {
        internal static readonly Action<ObjectDB> Call = AccessTools.MethodDelegate<Action<ObjectDB>>(AccessTools.Method(typeof(ObjectDB), "UpdateRegisters", new Type[] {  }));
    }
    internal static void UpdateRegisters(this ObjectDB instance) => ObjectDBUpdateRegistersAccess.Call(instance);

    private static class ItemDropSaveAccess
    {
        internal static readonly Action<ItemDrop> Call = AccessTools.MethodDelegate<Action<ItemDrop>>(AccessTools.Method(typeof(ItemDrop), "Save", new Type[] {  }));
    }
    internal static void Save(this ItemDrop instance) => ItemDropSaveAccess.Call(instance);

    private static class ContainerSaveAccess
    {
        internal static readonly Action<Container> Call = AccessTools.MethodDelegate<Action<Container>>(AccessTools.Method(typeof(Container), "Save", new Type[] {  }));
    }
    internal static void Save(this Container instance) => ContainerSaveAccess.Call(instance);

    private static class ContainerLoadAccess
    {
        internal static readonly Func<Container, bool> Call = AccessTools.MethodDelegate<Func<Container, bool>>(AccessTools.Method(typeof(Container), "Load", new Type[] {  }));
    }
    internal static bool Load(this Container instance) => ContainerLoadAccess.Call(instance);

    private static class InventoryGuiSetupDragItemAccess
    {
        internal static readonly Action<InventoryGui, ItemDrop.ItemData, Inventory, int> Call = AccessTools.MethodDelegate<Action<InventoryGui, ItemDrop.ItemData, Inventory, int>>(AccessTools.Method(typeof(InventoryGui), "SetupDragItem", new Type[] { typeof(ItemDrop.ItemData), typeof(Inventory), typeof(int) }));
    }
    internal static void SetupDragItem(this InventoryGui instance, ItemDrop.ItemData item, Inventory inventory, int amount) => InventoryGuiSetupDragItemAccess.Call(instance, item, inventory, amount);

    private static class InventoryGridGetButtonPosAccess
    {
        internal static readonly Func<InventoryGrid, GameObject, Vector2i> Call = AccessTools.MethodDelegate<Func<InventoryGrid, GameObject, Vector2i>>(AccessTools.Method(typeof(InventoryGrid), "GetButtonPos", new Type[] { typeof(GameObject) }));
    }
    internal static Vector2i GetButtonPos(this InventoryGrid instance, GameObject go) => InventoryGridGetButtonPosAccess.Call(instance, go);

    private static class ValheimUIRadialBaseInstantCloseAccess
    {
        internal static readonly Action<Valheim.UI.RadialBase> Call = AccessTools.MethodDelegate<Action<Valheim.UI.RadialBase>>(AccessTools.Method(typeof(Valheim.UI.RadialBase), "InstantClose", new Type[] {  }));
    }
    internal static void InstantClose(this Valheim.UI.RadialBase instance) => ValheimUIRadialBaseInstantCloseAccess.Call(instance);

    private static class StaticFields
    {
        internal static readonly FieldInfo LocalizationInstance = AccessTools.Field(typeof(Localization), "m_instance");
        internal static readonly FieldInfo TerminalInitialized = AccessTools.Field(typeof(Terminal), "m_terminalInitialized");
        internal static readonly FieldInfo Commands = AccessTools.Field(typeof(Terminal), "commands");
        internal static readonly FieldInfo LastInteractFrame = AccessTools.Field(typeof(UIGamePad), "m_lastInteractFrame");
    }
    internal static Localization ExistingLocalization => (Localization)StaticFields.LocalizationInstance.GetValue(null);
    internal static bool TerminalInitialized => (bool)StaticFields.TerminalInitialized.GetValue(null);
    internal static Dictionary<string, Terminal.ConsoleCommand> TerminalCommands => (Dictionary<string, Terminal.ConsoleCommand>)StaticFields.Commands.GetValue(null);
    internal static void SetLastInteractFrame(int frame) => StaticFields.LastInteractFrame.SetValue(null, frame);

    private static class InputAccess
    {
        internal static readonly Func<ZInput.InputSource, bool> Accept = AccessTools.MethodDelegate<Func<ZInput.InputSource, bool>>(AccessTools.Method(typeof(ZInput), "ShouldAcceptInputFromSource", new[] { typeof(ZInput.InputSource) }));
    }
    internal static bool ShouldAcceptInputFromSource(ZInput.InputSource source) => InputAccess.Accept(source);

    private static class HotkeyElements
    {
        internal static readonly FieldInfo Elements = AccessTools.Field(typeof(HotkeyBar), "m_elements");
        internal static readonly FieldInfo GameObject = AccessTools.Field(Elements.FieldType.GetGenericArguments()[0], "m_go");
        internal static readonly FieldInfo Used = AccessTools.Field(Elements.FieldType.GetGenericArguments()[0], "m_used");
    }
    internal static IList GetHotkeyElements(this HotkeyBar bar) => (IList)HotkeyElements.Elements.GetValue(bar);
    internal static GameObject GetHotkeyElementObject(object element) => (GameObject)HotkeyElements.GameObject.GetValue(element);
    internal static bool IsHotkeyElementUsed(object element) => (bool)HotkeyElements.Used.GetValue(element);


}
