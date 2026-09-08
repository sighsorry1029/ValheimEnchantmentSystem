# Structural patch integration checks

These checks require Valheim. A successful build and console rule-test run do not execute Unity, Harmony game patches, or multiplayer replication. Use a disposable test world and copies of character saves.

The console output-transfer fixture reads managed `ItemData` fields with constructors bypassed; it still does not run Unity or the game. Loading that game type requires Valheim's `Managed/netstandard.dll` runtime facade (version 2.1). The test project copies it from the configured Valheim managed directory or its parent; use `-p:GameNetstandardDllPath="path\to\netstandard.dll"` to override. Compilation remains `net48`, with no binding redirect or production dependency change.

## Equipment patch state

1. Replace left/right hand equipment, put it on the back, and change each armor slot. Check both unchanged and changed item hashes, armor VFX on/off, and local and remote characters.
2. Check the installed Harmony targets, owner IDs, and priorities against the previous build. The seven VisEquipment setter patches now pass their own change flag through `__state`; refreshes from several setters must still be combined into one scheduled visual refresh.
3. With another equipment mod installed, exercise a skipped original setter and nested/repeated setters. Confirm no stale change flag leaks into a later call. ItemStand and ArmorStand refresh behavior must remain unchanged.

## Requirements and configuration

1. Compare the previous and current final requirements for the same base YAML, additional files, resource map, recipes, and biome mappings. Manual assignments must still precede automatic assignments.
2. Exercise ObjectDB Awake, CopyOtherDB, UpdateRegisters, ZNet Awake, a biome mapping edit, `ves_reloadconfig reqs`, and reconnect. Compare published requirements and rebuild counts; queued changes should still be coalesced with the existing one-frame delay.
3. Test absent/empty additional directories, mixed-case file names, duplicate item assignments, an invalid manual file, and an invalid resource map. Requirements' skip-invalid-file/first-assignment policy and other repositories' read-before-publish/override policy must not change.
4. Repeat as client, graphical host, and dedicated server. Clients must not publish authoritative requirements. Existing config sections, keys, synced field names, and item custom-data keys must remain readable.

## Notification lifecycle

1. Repeat local-host and remote-client success/failure notifications with different client-display and server-webhook thresholds. Check server sender validation and cooldown behavior.
2. Change the display filter while notifications are queued. Display-time revalidation must still suppress events excluded by the new filter.
3. Return to the main menu, join another world, and recreate ZNetScene. Client queue/display state and server request cooldowns must reset without duplicate RPC callbacks. A dedicated server must not access client-only UI settings.

## Item consumption and output

Use the existing AzuCraftyBoxes and ScrollCombineMode checklists as well. Include full inventory/world-drop fallback, partial stacks, a removal callback that fails or throws, and an AddItem callback that returns false after a partial merge. Compare inventory plus world quantities before and after each operation and after reconnecting.

This patch does not introduce a server reservation protocol for AzuCraftyBoxes containers. Simultaneous remote container writes and failures after an uncertain external mutation still require multiplayer testing; automatic retries or blind refunds must not be treated as safe recovery. Unobservable output transfers discard this call's prepared output only with confirmed local ownership, potentially losing untransferred items. Verify the queued network destruction after reconnect and the warning paths for lost ownership or failed cleanup.

## Public integration APIs

1. Call the foreign item-data adapter's non-generic `Remove` for an existing, missing, and default empty key. Check that unrelated data survives and no `Add` or generic `Remove` overload runs. The console regression fixture covers this reflection dispatch using a managed foreign implementation; repeat with an installed item-data integration.
2. Register animation-speed handlers at two different priorities, using order-sensitive operations such as addition followed by multiplication. Both groups must run in ascending priority order; handlers at the same priority retain registration order. Each group must observe the preceding group's normalized Animator speed.
3. Exercise consecutive characters, repeated fixed updates, stopped animation, a throwing handler, and another animation-speed mod. Check that existing marker guards and the finalizer still prevent duplicate processing and clear transient state. These cases require a live Unity Animator and are not covered by the console tests.
