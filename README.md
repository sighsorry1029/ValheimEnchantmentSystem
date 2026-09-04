# Uploaded a temporal fix with original author's approvement
# AI did all the fix so support can be limited.

## Guides and description here: https://kg.sayless.eu/ves/

Original: https://thunderstore.io/c/valheim/p/KGvalheim/Valheim_Enchantment_System/
Like the mod? Support original author please: `war3spells@gmail.com` (Paypal)

## Enchantment skill EXP

Enchanting grants EXP based on the attempted enchantment level and the item's base success chance, not the scroll tier. The server-synchronized settings appear under **4 - Skill** in Configuration Manager. In the cfg file:

```ini
[Enchantment]
Enchant Skill EXP Base = 2
Enchant Skill EXP Per Level = 0.5
Enchant Skill EXP Difficulty Bonus = 4
FailedEnchantSkillExpMultiplier = 0.5
```

Success EXP = Base + Per Level × (target enchant level − 1) + Difficulty Bonus × (1 − base success chance / 100).

- The target is the level being attempted, before success or failure changes the item. For +9 → +10 with a 50% base chance, the defaults grant 8.5 EXP on success or 4.25 EXP on failure.
- The base chance comes from the item's YAML chance settings, including overrides, clamped to 0–100%. Skill and blessed-scroll bonuses do not reduce EXP.
- Attempts with a final success chance of 0% still follow the normal consumption/failure rules but grant no EXP. A 0% base chance can grant EXP when skill or blessed bonuses make the final chance positive.
- Failure EXP uses the configured failure multiplier. Skill Gain Factor applies afterward to both success and failure EXP.
- Set all three EXP settings to 0 to disable EXP from enchanting. The old `Enchant Skill EXP F` through `S` settings are no longer used; any leftover entries in an existing cfg can be removed.
- Consumable skill scrolls retain their separate tier-based `Skill Scroll EXP F` through `S` settings in Configuration Manager.

## AzuCraftyBoxes: enchantment scrolls from normal containers

When AzuCraftyBoxes is installed and enabled, enchanting can use normal or blessed requirement scrolls from eligible nearby standard `Container` inventories. The player's own inventory is used first. The enchantment panel includes only usable external scrolls in its material count, and checks the source again when the animation finishes. No new configuration is required and AzuCraftyBoxes is an optional dependency; without it, enchanting remains inventory-only.

- Uses AzuCraftyBoxes' current pulling toggle, range, YAML restrictions, access/ward checks, and **Leave One Item** setting. The reserved last item is excluded from the displayed available count.
- Supports its normal-container registry, including mod-added chests using the same standard inventory system. ItemDrawers, backpack/gem-bag integration sources, tombstones, and containers in use by another player are not used.
- The target equipment must still be in the player's inventory. EXP-scroll consumption and scroll combining are unchanged.
- A missing/changed integration API safely disables external sources. A confirmed one-item removal and container save must finish before the enchantment roll and skill EXP are applied.
- Container writes follow AzuCraftyBoxes' normal replicated-container behavior, not a new server reservation protocol. This does not add atomic cross-client transactions; avoid simultaneous use of the same chest's last scroll from multiple clients.

## Scroll combining

The server-synchronized **Combine Mode** setting under **5 - Scrolls** controls both availability and the required shape:

```ini
[Scrolls]
Combine Mode = Cross5
```

- `Off`: disable combining, inventory combine indicators, and combine tooltips.
- `Row3` (Three in a row): arrange three matching scrolls horizontally and right-click the center.
- `Cross5` (Five in a cross, default): arrange five matching scrolls in a cross and right-click the center.

The former `Allow Combine` and `Required Shape` cfg entries are not read or migrated. Set `Combine Mode` explicitly if you want a mode other than the default `Cross5`; leftover old entries may be removed.

## Notifications and client settings

Notification events are always enabled. The server validates and relays accepted events without applying a minimum level to the relay. Webhook delivery and each client's in-game display have independent minimum levels (both default to 6):

```ini
[Notifications]
Webhook Minimum Enchant Level = 6
Success Webhooks =
Failure Webhooks =

[Client]
Notifications - Minimum Enchant Level = 6
Notifications - Filter = Success
```

- **1 - General** contains the webhook minimum and the comma-separated Success/Failure Webhooks lists. Only the dedicated server or local host's values are used for webhook delivery; these values are not synchronized to clients. Empty URL lists disable webhook delivery.
- **2 - Client** contains the in-game notification minimum (integer range 0–100) and filter. These values affect only this client's display and never suppress events sent to the server, other players, or webhooks. The local host can choose a different minimum for its own screen and webhooks. Set the filter to `None` to hide in-game notifications. The separate webhook minimum retains its 0–500 range.
- Minimum levels use the resulting enchantment level; destruction uses the level before destruction. For example, a minimum of 6 includes success from +5 to +6, but excludes a failed decrease from +6 to +5.
- In-game notifications always last 5 seconds. The former enable switch, shared minimum-level setting, and notification duration setting are no longer read or migrated. Existing sender validation and request rate limits remain in effect.
- The enchantment panel is always draggable by its title. Right-click the title to reset its position. Offset X/Y entries remain in the local cfg for persistence and direct editing but are hidden in Configuration Manager. The former draggable switch is no longer used.
- Main particle brightness is fixed at its former default of 1.5. The old `MainVFXParticleBrightness` cfg entry is no longer used. Weapon/armor VFX toggles, main light intensity, main tint intensity, and armor tint intensity remain configurable.

Configuration Manager sections are now **1 - General**, **2 - Client**, **3 - Enchantment**, **4 - Skill**, **5 - Scrolls**, **6 - Biome Tiers**, and **7 - Scroll Recipes**. Display categories are separate from cfg storage sections; scroll recipes continue using the existing `[8 - Scroll Recipes]` cfg section so saved recipe values are retained without migration.

Explicit category priorities keep these sections in numbered order in ConfigManager, independent of the order settings were registered. Each section's existing option order is unchanged.

## Enchantment menu shortcut

The shortcuts under **2 - Client** are local settings and are not synchronized with the server. In the cfg file:

```ini
[Client]
UI - Keyboard Shortcut = Y
UI - Gamepad Shortcut = JoyButtonY
UI - Gamepad Shortcut Modifier = JoyLBumper
```

- With the inventory closed, press keyboard `Y`, or hold controller **LB** and press **Y**, to open both the inventory and the enchantment panel. With the inventory already open, the same shortcut toggles only the enchantment panel. Neither shortcut requires focus on the repair/crafting gamepad group.
- Set the keyboard shortcut to `None` to disable it. Modifier combinations such as `Y + LeftControl` are supported; unrelated held keys do not suppress the shortcut.
- The gamepad shortcut has separate button and modifier settings. Set **UI - Gamepad Shortcut** to `Disabled` to disable it, or set only **UI - Gamepad Shortcut Modifier** to `Disabled` to use a single button. A single-button shortcut can conflict with normal game controls, so the default **LB + Y** is recommended. Y without LB retains its normal game behavior, including opening the inventory when assigned to it. Holding the shortcut does not repeatedly toggle the panel; release and press Y again for another toggle.
- On activation, the gamepad combination suppresses its overlapping native input for that activation. An open radial menu is canceled without activating its selected item. LB pressed on its own retains its normal behavior; actions already triggered before Y is pressed cannot be undone. Avoid combinations assigned to other mods.
- Text input, settings windows, menus, inventory dialogs, the large map, the store, the enchantment info panel, and dead/cutscene player states block the shortcuts. Blocked presses are not queued. After changing a binding, release the shortcut buttons before trying the new binding. Escape, Tab, Space, and Return already have panel actions, so avoid conflicting keyboard bindings.

The old `[Client] UI - GamepadShortcut` entry (without the space before `Shortcut`) is no longer read or migrated. The new gamepad entries default to LB + Y even if the old entry contains X; leftover old entries may be removed.

Hovering the enchantment button shows its name to the left, with a smaller second line such as “Press Y to open the panel”. The key name is orange; the rest of the second line is muted. The hint follows the configured shortcut and changes to “close” when the panel is open. Controller focus shows the configured gamepad combination instead, with both button names in orange. Disabling a shortcut hides its second line. English and Korean text are included.

## Fixed upgrade and mirror behavior

Item quality upgrades always keep the existing enchantment. Jewelcrafting mirror copies never inherit enchantment; the original item's enchantment is unchanged. The former `DropEnchantmentOnUpgrade` and `AllowJewelcraftingMirrorCopyEnchant` options are no longer bound or displayed, and leftover cfg entries are ignored rather than migrated.
