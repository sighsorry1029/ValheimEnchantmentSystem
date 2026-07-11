# Changelog

| Version | UpdateNotes |
|---|---|
| 1.9.10 | - Reorganized Configuration Manager settings into ordered General, Enchantment, Skill, Scrolls, Biome Tiers, Notifications, Client, and Scroll Recipes sections; scroll recipes now use the `[8 - Scroll Recipes]` cfg section and the old `[Scroll Recipes]` section is no longer read<br/>- Replaced the six fixed webhook slots with comma-separated Success Webhooks and Failure Webhooks lists supporting any number of unique server-only targets<br/>- Added resourcemap.yml to assign enchantment scroll requirements automatically from equipment crafting materials, with explicit EnchantmentReqs.yml entries taking priority<br/>- Integrated built-in and Expand World Data custom biome tiers with automatic resource-map rebuilding when tier mappings change<br/>- Grouped enchant skill EXP and skill-scroll drop/EXP settings together and hardened skill-scroll consumption with live-object and player-distance validation on the server<br/>- Hardened global notification requests by validating the sender, enchantable item, configured minimum level, and allowed enchantment result transition<br/>- Made chance, stat, and color YAML hot reloads atomic so an invalid base or override file cannot partially replace the active configuration<br/>- Fixed inventory enchant and scroll-combine overlays retaining stale or duplicate state between sessions, and prevented scroll inputs from being consumed when the output prefab is unavailable<br/>- Replaced reflection-based module autoloading with explicit dependency-aware initialization for more predictable startup and patch registration |
| 1.9.9 | - Blood Magic summons now inherit the damage-percent enchantment bonus from the summoning weapon<br/>- Added StaffRedTroll and StaffSkeleton to the default enchantment requirements |
| 1.9.8 | - Added configurable failed-enchant level loss<br/>- Added maxed-out UI for YAML-defined enchant caps<br/>- Added clearer failure chance details<br/>- Fixed enchant rolls to match the displayed two-decimal chance |
| 1.9.7 | - Added controller support for enchant button |
| 1.9.6 | - Added ZenUI compatibility for changing scroll recipes |
| 1.9.5 | - Improved EnchantmentReqs.yml loading behavior<br/>- Duplicate item req entries no longer cause the entire reqs reload to fail; the first valid mapping is kept and a warning is logged<br/>- Invalid reqs files or files with duplicate YAML keys are now skipped individually, while remaining valid reqs continue to load |
| 1.9.4 | - Fixed EnchantmentReqs not working<br/>- Organized recipe configs |
| 1.9.3 | - Added hover text on enchantment UI for possibilities and stats<br/>- Fixed inventory visual VFX staying in the background<br/>- Restored crafting station animation<br/>- Refactored code |
| 1.9.2 | - Fixed enchantment skill level being reset on dedicated servers<br/>- Changed default EXP and chance for skill scrolls |
| 1.9.1 | - Fixed enchantment skill level being reset<br/>- Added config option for EXP earned through using scrolls per tier |
| 1.9.0 | - Fixed VES not recognizing EWD biomes<br/>- Fixed skill scrolls not working on dedicated servers<br/>- Fixed some config section names falling back to localization tokens on some languages |
| 1.8.9 | - Fixed error that occurs when Expand World Data is not installed<br/>- Fixed skill scrolls not working |
| 1.8.8 | - Added EWD biome support<br/>- Added configs to adjust VFX |
| 1.8.7 | - Fixed some VFX not showing sometimes<br/>- Removed duplicate config reload |
| 1.8.6 | - Fixed scrolls not dropping according to the configured chance |
| 1.8.5 | - Enabled blessed-scroll toggle even when success=0<br/>- Made default chance YAML always include success and destroy even when the chance is 0 |
| 1.8.4 | - Changed config structure<br/>- Prevented altering other PieceManager-using mods' config sections<br/>- Recommended regenerating config |
| 1.8.3 | - Consolidated multiple config files into a single file<br/>- Simplified the format for EnchantmentReqs.yml<br/>- Added EnchantmentReqs.yml support for several mods<br/>- Note: You must regenerate your .cfg files and the BepInEx/config/ValheimEnchantmentSystem folder<br/><br/>- Added Tier E scrolls (Swamp and Ocean biomes)<br/>- Added recipes to craft Blessed Scrolls using regular scrolls<br/>- Overhauled all scroll crafting recipes<br/>- Adjusted default enchantment success rates and equipment bonus stats<br/>- Separated upgrade success rates for armor and weapons<br/><br/>- Improved scroll-related tooltips for items and within the UI<br/>- Enhanced ServerSync stability and performance<br/>- Resolved the error that happened when saving ItemStand with InfinityHammer<br/>- Made recipes align alphabetically on the crafting station |
| 1.8.1 | - Temporary fix<br/>- Localization file in plugins folder<br/>- Manager update |
| 1.7.4 | - Fix |
| 1.7.3 | - Small HP regen bugfix |
| 1.7.2 | - Fixed a bug where OK button in Vanilla Settings would not close the tab and save changes |
| 1.7.1 | - Updated for new Valheim patch |
| 1.7.0 | - Added new stats: max_hp, max_stamina, hp_regen, stamina_regen<br/>- Improved text visual clarity for some stats (%) |
| 1.6.9 | - Ashlands update |
| 1.6.8 | - New Valheim version update |
| 1.6.5-1.6.7 | - Bug fixes |
| 1.6.4 | - Removed wings and auras VFX. They will be added as a separate mod later<br/>- Fixed for new Valheim version |
| 1.6.3 | - Bugfixes |
| 1.6.2 | - Enchant button now always visible<br/>- Fixed a bug that was throwing errors when logging out / shutting down the game<br/>- Several UI QoL bugfixes |
| 1.6.0 | - Added ItemFailureType option: LevelDecrease, Destroy, Combined<br/>- Changed the .yml structure of Chances. Success and optional destroy chances must now be specified for Combined mode |
| 1.5.4 | - Added movement_speed enchant stat<br/>- Fixed AUGA compatibility issues |
| 1.5.3 | - Enchant UI now shows enchantment chance, including skill level bonus<br/>- Added F tier scrolls for config use<br/>- Added scroll combination mechanic (5 or 3 scrolls into higher tier)<br/>- Note: Please remove EnchantmentReqs.yml and main config for a fresh renewal |
| 1.5.2 | - Small UI fixes |
| 1.5.1 | - Added world notifications for enchantment success/failure<br/>- Can be disabled globally server-side or locally in client settings |
| 1.5.0 | - Changed UI visuals<br/>- Added inventory / hotbar UI VFX<br/>- Added Enchantment Settings tab to disable VFX<br/>- Added additional effects section to enchantment colors (wings + auras)<br/>- Bugfixes |
| 1.4.5 | - Fixed FPS lag issues on servers with ServerCharacters<br/>- Info UI fixes |
| 1.4.4 | - Added new stats: attack_speed and slash_wave<br/>- Added new Info UI showing item enchant stats and chances |
| 1.4.2-1.4.3 | - Added new config to enable VFX for armors |
| 1.4.1 | - Fixed a bug with incorrect player resistances |
| 1.4.0 | - Added new skill: Enchantment, which increases success chance<br/>- Skill EXP can be gained from source orbs dropped by monsters with configurable low chance |
| 1.3.5-1.3.6 | - Fixed enchantment bug with ItemStand items |
| 1.3.4 | - Fixed localization issues |
| 1.3.3 | - Added new enchantment modifiers to .yml (resistance_blunt, resistance_slash, etc.) |
| 1.3.2 | - VFX now correctly applies to items with multiple mesh parts, such as crossbows and modded items |
| 1.3.1 | - Fixed minor issue with UI updates when placing items in chests |
| 1.3.0 | - Fixed incorrect tooltip values bug<br/>- Fixed incompatibility with Jewelcrafting and Extended inventory visual slots |
| 1.2.0 | - Replaced Override .yml files to affect groups of items instead of individual items<br/>- Note: Please remove old Override_yml files so they can be recreated |
| 1.1.0 | - Added 4 directories for Override and Requirements additional .yml files |
| 1.0.0 | - Mod released |
