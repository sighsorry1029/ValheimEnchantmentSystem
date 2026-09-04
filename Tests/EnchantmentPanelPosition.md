# Enchantment panel position

## Controls and client configuration

- Hold the left mouse button on the enchantment panel title and drag to move the whole panel. Dragging is always enabled. Release to save both coordinates.
- Right-click the title to restore the default position.
- The inventory button and the separate information window do not move.
- Other client settings appear under `2 - Client` in Configuration Manager. Position offsets are hidden there, but remain saved and editable in the `[Client]` cfg section in `BepInEx/config/kg.ValheimEnchantmentSystem.cfg`:

```ini
[Client]
UI - Enchantment Panel Offset X = 100
UI - Enchantment Panel Offset Y = -50
```

Offsets are relative to the original position in canvas UI units: positive X moves right and positive Y moves up. Set both to `0` to reset. Config edits apply through the existing config reload system; close the game before editing if you want to avoid a simultaneous in-game config save.

The settings are not synced to the server. Normal panels stay entirely within the canvas; if a panel is larger than the viewport, the title remains reachable. Resolution-dependent constraints do not overwrite saved coordinates unless the player actually drags the panel.

## Automated coverage

```powershell
dotnet run --project .\Tests\ValheimEnchantmentSystem.RuleTests.csproj -c Release
```

`TestPanelPositionMath` covers both axes via shared one-dimensional math: default and signed offsets, exact boundaries, oversized panels and titles, authored off-center positions, viewport shrink/grow, nonfinite values, and reversed/extreme bounds. These checks do not run Unity's input or rendering lifecycle.

## In-game verification checklist

1. Open the enchantment menu, drag both the title text and the title background, and release. Verify that the header, body, buttons, and VFX move together without changing their relative layout.
2. Close/reopen the menu, then restart the client. Verify the saved position and both cfg coordinates persist. Connect to a dedicated server and verify another player's position remains independent.
3. Use the item-selection drag path, blessing toggle, enchant button, keyboard/gamepad shortcuts, and information window. Verify panel dragging never starts from those controls and cannot operate behind the information window.
4. Confirm Configuration Manager does not expose draggable or X/Y options. Edit X/Y in cfg while the panel is visible and hidden, and allow the existing reload poller to run. Verify no stale drag overwrites an explicit config edit.
5. Right-click the title and verify both cfg values reset to zero. Verify left-drag remains available without an enable/disable option.
6. Move to every edge, try very large offsets and nonfinite values, change window resolution/aspect ratio/canvas scale, and return to the previous resolution. Verify the title is always reachable and temporary constraints do not erase the saved layout.
7. Release outside the window, lose focus, close the panel, or open the information window during a drag. Verify movement stops and no repeated/per-frame config writes occur.

The game-only checklist must be performed in Valheim; a successful build and rule test run alone do not confirm those interactions.
