# Scroll combine mode

## Configuration

`5 - Scrolls` exposes one server-synced `Combine Mode` dropdown: `Off`, `Three in a row`, and `Five in a cross`. The default is `Five in a cross`. The cfg uses enum identifiers:

```ini
[Scrolls]
Combine Mode = Cross5
```

Supported values are `Off`, `Row3`, and `Cross5`. The old `Allow Combine` and `Required Shape` keys are not bound or migrated. Their previous values do not select the new mode.

## Automated checks

From the repository root in a Visual Studio Developer PowerShell:

```powershell
MSBuild.exe .\ValheimEnchantmentSystem.csproj /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU
MSBuild.exe .\Tests\ValheimEnchantmentSystem.RuleTests.csproj /restore /t:Build /p:Configuration=Release /p:BuildProjectReferences=false
.\Tests\bin\Release\net48\ValheimEnchantmentSystem.RuleTests.exe
```

Tests check enum identifiers, ordering, friendly labels, mode availability, tooltip shape markup, undefined enum values, and Configuration Manager category/order metadata. Unity inventory, rendering, input, and multiplayer checks remain manual.

## In-game checks

1. Confirm only `Combine Mode` appears in `5 - Scrolls`, after the drop settings, with the three labels above. A fresh config defaults to `Five in a cross`.
2. Select `Off` while a valid pattern is visible. Verify the indicator disappears without reopening the inventory, newly displayed tooltips omit combine instructions, and right-clicking the center consumes no scrolls and creates no upgraded scrolls. Reopen the information panel and verify that its guide omits the combine instruction without leaving an empty bullet.
3. Select `Three in a row`. Arrange exactly three identical scrolls horizontally and right-click the center. Verify the row indicator and tooltip match, the same amounts are consumed from all three stacks, and the existing next-tier output and minimum-stack behavior are unchanged.
4. Select `Five in a cross`. Arrange matching scrolls at center/left/right/up/down. Verify the cross indicator and tooltip match, and the existing five-stack consumption and next-tier output remain unchanged. Repeat with unequal stacks.
5. With the inventory open, switch `Row3` -> `Cross5` -> `Off` -> `Row3`. Verify indicators and subsequently opened tooltips follow the selected mode; switching modes alone must not consume anything.
6. Check invalid patterns: different prefabs or tiers, missing neighbors, a pattern crossing an inventory edge, and extra identical scrolls extending the pattern. Verify these remain rejected. Check reserved-top-row integration behavior for `Cross5` is unchanged.
7. Test separate inventory/container grids and top-tier scrolls. Valid combinations still produce the expected output in the correct inventory, while non-upgradeable scrolls have no combine indicator, instructions, or output.
8. On a dedicated server, set each mode server-side and verify clients receive the active mode, obey it, and cannot override locked server settings. Test joining when the server uses `Off`.
9. Start with a cfg containing only old `Allow Combine = false` and `Required Shape = Row3` entries and no `Combine Mode`. Verify the new mode defaults to `Cross5`; no legacy value is interpreted or migrated. Then set `Combine Mode = Off`, restart, and confirm it persists.
