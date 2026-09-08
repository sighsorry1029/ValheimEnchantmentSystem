# Changelog

## 1.9.15

- Fixed stale equipped-enchantment stat snapshots after chest-supplied enchantments and enchantment commands
- Hardened scroll combining with verified material consumption and output counts, including partial inventory merges and foreign item metadata; uncertain operations stop without automatic refunds or retries, and discarding unverified output can lose items
- Fixed foreign item-data removal calling the wrong method and animation-speed handlers skipping additional priority groups
- Simplified equipment VFX patch state, notification lifecycle resets, and requirements rebuild ownership while preserving configuration keys, save formats, public API signatures, and RPC names
- Added regression coverage for partial consumption, output transfer failures, metadata handling, and reflection-based item-data removal
- Added opt-in Debug deployment with DeployToGame=true, copying only the final merged plugin DLL after successful post-processing

## 1.9.14

- Simplified the packaged README and cleaned up obsolete project content references
- Refresh equipped-enchantment stat snapshots after enchantment level changes, including chest-supplied attempts and enchantment commands
- Verify scroll-combine removals and inventory output transfers; stop on uncertain mutations and preserve confirmed untransferred output instead of trusting inventory return values alone
- Simplified per-call equipment VFX patch state and separated notification client/server resets without changing RPC names or configuration keys
- Consolidated requirements rebuild state and shared identical YAML file enumeration while preserving per-domain loading policies
- Fixed foreign item-data removal dispatch and processing of multiple animation-speed priority groups

## 1.9.13

- Added optional AzuCraftyBoxes support for normal and blessed enchantment scrolls in nearby standard containers, using inventory scrolls first and respecting pulling, range, access, item filters, and Leave One Item settings; special drawers and backpack/gem-bag sources are excluded
- Added a configurable keyboard shortcut, default Y, and changed the default gamepad shortcut to LB + Y; either shortcut opens the inventory and enchantment panel together when the inventory is closed
- Improved shortcut handling around text input, menus, and controller input conflicts, and added localized two-line hints to the left of the enchantment button with highlighted shortcut keys
- Made the enchantment panel draggable by its title with a saved client-side position and right-click position reset; position offsets remain editable in the cfg but are hidden in Configuration Manager
- Changed Enchantment Animation Duration to a 0-1 slider, with 0 applying the result immediately
- Replaced tier-based enchanting EXP with configurable base EXP, enchantment-level scaling, and a base-success-chance difficulty bonus; failure EXP still uses its multiplier, while attempts with 0% final success chance grant no EXP
- Added integer sliders from 0 to 100 for consumable Skill Scroll EXP F through S while retaining their separate tier-based EXP values
- Combined scroll-combining options into one Combine Mode setting: Off, Row3 (three in a row), or Cross5 (five in a cross)
- Separated server/host webhook minimum enchantment level from each client's in-game notification minimum, moved webhook settings to General, and made notifications always enabled with a fixed 5-second display duration; clients can still filter or hide in-game notifications
- Reordered Configuration Manager sections to General, Client, Enchantment, Skill, Scrolls, Biome Tiers, and Scroll Recipes, with explicit priorities that preserve numbered display order
- Fixed main particle brightness at 1.5, made item upgrades always preserve enchantments, and made Jewelcrafting mirror copies always omit enchantments; removed the corresponding configuration options
- Clarified that Safety Level protects attempts based on the item's current enchantment level and does not guarantee a minimum retained level
- Removed or renamed settings are not migrated; see the README for the current cfg keys and fixed behaviors

## 1.9.12

- Updated Expand World Data integration for version 1.68 by using the current `ReadConfigs` reload hook, removing legacy `FromFile` compatibility, and eliminating repeated HarmonyX missing-method warnings

## 1.9.11

- Fixed skill EXP scrolls not being consumed with the E interaction on dedicated servers while retaining server-side prefab, distance, and duplicate-consumption validation

## 1.9.10

- Reorganized Configuration Manager settings into ordered General, Enchantment, Skill, Scrolls, Biome Tiers, Notifications, Client, and Scroll Recipes sections; scroll recipes now use the `[8 - Scroll Recipes]` cfg section and the old `[Scroll Recipes]` section is no longer read
- Replaced the six fixed webhook slots with comma-separated Success Webhooks and Failure Webhooks lists supporting any number of unique server-only targets
- Added resourcemap.yml to assign enchantment scroll requirements automatically from equipment crafting materials, with explicit EnchantmentReqs.yml entries taking priority
- Integrated built-in and Expand World Data custom biome tiers with automatic resource-map rebuilding when tier mappings change
- Grouped enchant skill EXP and skill-scroll drop/EXP settings together and hardened skill-scroll consumption with live-object and player-distance validation on the server
- Hardened global notification requests by validating the sender, enchantable item, configured minimum level, and allowed enchantment result transition
- Made chance, stat, and color YAML hot reloads atomic so an invalid base or override file cannot partially replace the active configuration
- Fixed inventory enchant and scroll-combine overlays retaining stale or duplicate state between sessions, and prevented scroll inputs from being consumed when the output prefab is unavailable
- Replaced reflection-based module autoloading with explicit dependency-aware initialization for more predictable startup and patch registration

## 1.9.9

- Blood Magic summons now inherit the damage-percent enchantment bonus from the summoning weapon
- Added StaffRedTroll and StaffSkeleton to the default enchantment requirements

## 1.9.8

- Added configurable failed-enchant level loss
- Added maxed-out UI for YAML-defined enchant caps
- Added clearer failure chance details
- Fixed enchant rolls to match the displayed two-decimal chance

## 1.9.7

- Added controller support for enchant button

## 1.9.6

- Added ZenUI compatibility for changing scroll recipes

## 1.9.5

- Improved EnchantmentReqs.yml loading behavior
- Duplicate item req entries no longer cause the entire reqs reload to fail; the first valid mapping is kept and a warning is logged
- Invalid reqs files or files with duplicate YAML keys are now skipped individually, while remaining valid reqs continue to load

## 1.9.4

- Fixed EnchantmentReqs not working
- Organized recipe configs

## 1.9.3

- Added hover text on enchantment UI for possibilities and stats
- Fixed inventory visual VFX staying in the background
- Restored crafting station animation
- Refactored code

## 1.9.2

- Fixed enchantment skill level being reset on dedicated servers
- Changed default EXP and chance for skill scrolls

## 1.9.1

- Fixed enchantment skill level being reset
- Added config option for EXP earned through using scrolls per tier

## 1.9.0

- Fixed VES not recognizing EWD biomes
- Fixed skill scrolls not working on dedicated servers
- Fixed some config section names falling back to localization tokens on some languages

## 1.8.9

- Fixed error that occurs when Expand World Data is not installed
- Fixed skill scrolls not working

## 1.8.8

- Added EWD biome support
- Added configs to adjust VFX

## 1.8.7

- Fixed some VFX not showing sometimes
- Removed duplicate config reload

## 1.8.6

- Fixed scrolls not dropping according to the configured chance

## 1.8.5

- Enabled blessed-scroll toggle even when success=0
- Made default chance YAML always include success and destroy even when the chance is 0

## 1.8.4

- Changed config structure
- Prevented altering other PieceManager-using mods' config sections
- Recommended regenerating config

## 1.8.3

- Consolidated multiple config files into a single file
- Simplified the format for EnchantmentReqs.yml
- Added EnchantmentReqs.yml support for several mods
- Note: You must regenerate your .cfg files and the BepInEx/config/ValheimEnchantmentSystem folder
- Added Tier E scrolls (Swamp and Ocean biomes)
- Added recipes to craft Blessed Scrolls using regular scrolls
- Overhauled all scroll crafting recipes
- Adjusted default enchantment success rates and equipment bonus stats
- Separated upgrade success rates for armor and weapons
- Improved scroll-related tooltips for items and within the UI
- Enhanced ServerSync stability and performance
- Resolved the error that happened when saving ItemStand with InfinityHammer
- Made recipes align alphabetically on the crafting station

## 1.8.1

- Temporary fix
- Localization file in plugins folder
- Manager update

## 1.7.4

- Fix

## 1.7.3

- Small HP regen bugfix

## 1.7.2

- Fixed a bug where OK button in Vanilla Settings would not close the tab and save changes

## 1.7.1

- Updated for new Valheim patch

## 1.7.0

- Added new stats: max_hp, max_stamina, hp_regen, stamina_regen
- Improved text visual clarity for some stats (%)

## 1.6.9

- Ashlands update

## 1.6.8

- New Valheim version update

## 1.6.5-1.6.7

- Bug fixes

## 1.6.4

- Removed wings and auras VFX. They will be added as a separate mod later
- Fixed for new Valheim version

## 1.6.3

- Bugfixes

## 1.6.2

- Enchant button now always visible
- Fixed a bug that was throwing errors when logging out / shutting down the game
- Several UI QoL bugfixes

## 1.6.0

- Added ItemFailureType option: LevelDecrease, Destroy, Combined
- Changed the .yml structure of Chances. Success and optional destroy chances must now be specified for Combined mode

## 1.5.4

- Added movement_speed enchant stat
- Fixed AUGA compatibility issues

## 1.5.3

- Enchant UI now shows enchantment chance, including skill level bonus
- Added F tier scrolls for config use
- Added scroll combination mechanic (5 or 3 scrolls into higher tier)
- Note: Please remove EnchantmentReqs.yml and main config for a fresh renewal

## 1.5.2

- Small UI fixes

## 1.5.1

- Added world notifications for enchantment success/failure
- Can be disabled globally server-side or locally in client settings

## 1.5.0

- Changed UI visuals
- Added inventory / hotbar UI VFX
- Added Enchantment Settings tab to disable VFX
- Added additional effects section to enchantment colors (wings + auras)
- Bugfixes

## 1.4.5

- Fixed FPS lag issues on servers with ServerCharacters
- Info UI fixes

## 1.4.4

- Added new stats: attack_speed and slash_wave
- Added new Info UI showing item enchant stats and chances

## 1.4.2-1.4.3

- Added new config to enable VFX for armors

## 1.4.1

- Fixed a bug with incorrect player resistances

## 1.4.0

- Added new skill: Enchantment, which increases success chance
- Skill EXP can be gained from source orbs dropped by monsters with configurable low chance

## 1.3.5-1.3.6

- Fixed enchantment bug with ItemStand items

## 1.3.4

- Fixed localization issues

## 1.3.3

- Added new enchantment modifiers to .yml (resistance_blunt, resistance_slash, etc.)

## 1.3.2

- VFX now correctly applies to items with multiple mesh parts, such as crossbows and modded items

## 1.3.1

- Fixed minor issue with UI updates when placing items in chests

## 1.3.0

- Fixed incorrect tooltip values bug
- Fixed incompatibility with Jewelcrafting and Extended inventory visual slots

## 1.2.0

- Replaced Override .yml files to affect groups of items instead of individual items
- Note: Please remove old Override_yml files so they can be recreated

## 1.1.0

- Added 4 directories for Override and Requirements additional .yml files

## 1.0.0

- Mod released
