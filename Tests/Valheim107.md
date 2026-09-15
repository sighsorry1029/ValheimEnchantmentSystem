# Valheim 1.0.7 port verification

Target: Windows client build 25185596 and dedicated server build 25185644.
Use original Managed DLLs. Do not publicize or overwrite game assemblies.

## Completed checks

The Debug mod compiles against original 1.0.7 game and Unity DLLs and embeds the pinned ServerSync `valheim-1.0.7-r1`.
The existing net48 deterministic suite covers enchantment rules, shortcut state, skill XP, combine consumption, failed/partial output transfers, and foreign item-data removal.

ResourceMap coverage also exercises the actual direct-material tier selector with 1.0.7 recipe ingredients
(ShieldRoots E, HelmetLox C, GrapplingHook B, SwordGold/CapeDeepNorthMage S), the automatic Material exclusion
with managed ItemData fixtures, and finished weapon/armor classification. The actual default YAML parser checks
the three foundry-only heavy armor entries; the requirement merger/cache checks manual Material exceptions
and manual priority over automatic assignment. These tests do not initialize Unity, execute ScanRecipes against
a live ObjectDB, or initialize the game-dependent EnchantmentTierCatalog in the net48 host.

The separate .NET 10 compatibility checker reads original metadata without initializing Unity or installing Harmony patches:

- 79 attributed/deterministic Harmony targets and named argument/field bindings.
- All 707 game/Unity member references in the merged DLL and embedded VES_Scripts assembly.
- 58 typed cached field accessors.
- 71 named game method/field/property source bindings, including explicit ItemDataManager overloads.
- Nine original IL contracts: recipe label local/argument, selected recipe property, two stack loops, world autostack candidate/branch, item Awake import anchor, crafting replacement calls, common item serialization, and stamina ZDO write.

```powershell
dotnet build ValheimEnchantmentSystem.csproj -c Debug -p:DeployToGame=true
dotnet build Tests/ValheimEnchantmentSystem.RuleTests.csproj -c Debug -p:BuildProjectReferences=false
Tests/bin/Debug/net48/ValheimEnchantmentSystem.RuleTests.exe
dotnet run --project Tests/Compatibility/Compatibility.csproj
```

For server metadata, pass `-p:ValheimManagedDir=<server original Managed>` and an isolated `-p:OutputPath=<output>`; pass the repository root after `--`. The checker is separate because new game's default interface implementations cannot be fully reflected by the net48 test host.

These are compilation, deterministic tests and static contract checks, **not** execution of patched game methods. The source binding scan covers literal/nameof game lookups; it cannot prove arbitrary reflection or third-party hooks. The four optional VFX destruction selectors and ExpandWorldData runtime selection are excluded from the attributed-target count. Existing lifecycle Prepare guards/pruning and optional integration boundaries remain.

## Required in-game verification

Use a disposable world/character and matching mod DLLs on the participating machines:

1. Client/menu/world initialization: no VES module failures or Harmony skips; reopen inventory, switch containers, combine scrolls with right mouse input, use both menu shortcuts, hide/show UI repeatedly. Verify tooltip, upgrade recipe labels, hotbar/scroll overlays, split dialog blocking and destruction/recreation of UI.
2. Equipment and stands: equip/unequip, change orientation/variant, replace/remove armor and item-stand items, reload world and change network owner. Verify glow and enchantment level survive without a non-owner ZDO write.
3. Save/load and transfers: enchant, split/merge, move to/from containers, drop/pick up, logout/rejoin; compare item counts and custom data. Exercise full inventory and partial output acceptance; no duplicated output or lost remainder.
4. Vanilla crafting/upgrader: ordinary upgrade retains configured enchantment behavior; successful upgrade transfers data once; downgrade preserves data without reporting a quality upgrade; destruction refunds contain only material metadata.
5. Host + remote client and dedicated server: version/config locks, admin/non-admin changes, join initialization order, notification/skill-scroll RPC sender validation, duplicate requests, disconnect/reconnect and crossplay. Test optional mods absent and present with compatible versions.
6. Profile selection: default Debug deployment is Steam's BepInEx/plugins. Gale profiles have separate plugin directories; use the built DLL in the selected profile before judging a new log.
7. ResourceMap: on the authoritative host/server, verify the configured biome tiers after restarting with the new DLL
   (or `ves_reloadconfig reqs` for subsequent YAML edits). Check normal/blessed scroll selection and consumption for
   ShieldRoots, SwordGold, CapeDeepNorthMage, ArmorDeepNorthHeavyChest, ArmorDeepNorthHeavylegs and HelmetDNHeavy.
   Verify the 16 uncast Material weapons are absent from automatic enchantment eligibility; intentional explicit
   requirement overrides remain supported. Check remote-client synchronization and save/rejoin preservation.
   There is no migration, automatic backfill of existing configuration files, or legacy-game support in this patch.

The supplied log also contains failures from other mods. This port does not patch their bundled ServerSync or establish those mods' compatibility.

## 2026-09-15: crafting tooltip null-reference fix on 1.0.12

The supplied `asdfaf/BepInEx/LogOutput.log` records VES 1.9.16 on Valheim 1.0.12 and
repeating exceptions inside the predicate in `Utils.GetPrefabNameByItemName`, called by
`TooltipPatch.Postfix` from `InventoryGui.UpdateRecipe`. The report also describes a clean
three-mod reproduction; the attached log itself loads six plugins, so it is not evidence
of that clean-profile run. The exact offending ObjectDB entry is not identified by the log.

Original client source/metadata reference: snapshot
`client-b25253764-windows-x64-20260911T154617Z`, extraction `ilspy-9.1.0.7988-r1`,
`assembly_valheim/csharp/ObjectDB.cs` and `InventoryGui.cs` under the global Valheim references.
The installed original `assembly_valheim.dll` matches the snapshot SHA-256
`27A766A8D23A7BD8B6A54FB9AD0452A96C305FB3629B39C40527C09A1C393A84`.
`ObjectDB.UpdateRegisters` explicitly allows entries without ItemDrop. The old VES lookup
dereferenced their component without checking. In `UpdateRecipe`, GetTooltip must return
before the recipe description text is assigned, explaining how an exception can leave the
previous recipe's description visible. This explains the reported stale text but does not
prove every Scroll Station display issue has that cause.

`TooltipPatch` now uses `EnchantmentDomainHelper.ResolveItemPrefabName`. Resolution keeps
the explicit drop prefab first, then uses the game's public `GetItemPrefab(SharedData)`
index for recipe templates. The existing name fallback retains its first matching-name
policy for unregistered/shared-data copies, but skips missing/destroyed objects, missing
ItemDrop, item data and shared data; an absent database or empty name returns null.
Registered items avoid per-frame list scans, GetComponent calls and predicate allocations.
No extra cache, item mutation, Harmony target/order change, network, save or config change
is introduced. The old 1.9.15 armor VFX exception is a separate path, not repaired by this fix.

Completed: Debug build against original installed 1.0.12 DLLs, final ILRepack merge and
Steam plugins deployment; source/installed mod SHA-256 both
`818B368954A53D2D1343FDBA5193097ED2E33E44E92AC87248180336DA12B9C4`.
Existing net48 rule tests pass. Static checks pass: 79 Harmony targets/bindings, 708 member
references, 58 FieldRefs, nine IL contracts, 71 source bindings. These do not execute the
Unity lookup. An attempted headless regression fixture hit Unity native-call initialization
and the game's default-interface-method limitation in net48; it was removed, not counted
as a passing reproduction test. Gale profiles and Release packages were not modified.

Required actual-game verification (not performed in this fix):

1. Start a disposable character/world with the corrected DLL. Select Stone Axe, Hammer,
   then another recipe; repeat while watching the log. Hold Hammer selected for at least
   30 seconds. Name, description and requirements must follow the selected recipe without
   a repeating VES exception or an old description remaining.
2. At Scroll Station, switch normal/blessed scroll recipes, then return to ordinary
   crafting. Reopen inventory and reload the world. Check localized text and scroll
   requirements; unknown/unmapped items should retain their vanilla description.
3. Check an enchanted inventory weapon/armor tooltip and upgrade recipe. Enchantment
   stats/level and eligible-scroll hints must be preserved. With a test mod, check two
   distinct prefabs sharing a name: registered SharedData must resolve their own prefab.
4. In an isolated Unity test scene, exercise the fallback with null/destroyed entries,
   a GameObject without ItemDrop, incomplete item/shared data, a valid later match and no
   match; also try before ObjectDB creation/after destruction. Expect a valid name or null,
   with no exception. Do not insert synthetic entries into a persistent play profile.
5. Repeat the crafting/tooltip checks as a remote client of a host/dedicated server.
   No new server code is introduced, but actual multiplayer execution remains unverified.
