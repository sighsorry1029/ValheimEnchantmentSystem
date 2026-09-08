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

Additional regression tests exercise the production consumption/transfer control flow with managed callbacks: preflight failure, 3/5-source consumption, failed or partial removal, a source moved into another stack, callback exceptions, rejected/partial output additions, misleading return values, and failed quantity observation.

On a failed removal, combining stops before creating output or touching later sources. Scrolls already removed are not automatically refunded because an external callback may have moved or otherwise changed them. A confirmed inventory transfer leaves only the observed remainder in the world. If the transferred quantity itself cannot be established, this call's prepared output is discarded through ZNetScene only when local ownership is confirmed. This can lose untransferred output; it does not refund or retry. Missing ownership or failed cleanup is logged for inspection. Network destruction is queued, so this is not an atomic transaction or guaranteed recovery across disconnects.

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
10. Inject a false return or exception at the second RemoveItem call, a true return after a partial removal, and a callback that moves the source into another stack. Verify there is no upgraded output, no later removal, and no automatic refund or retry. Record the exact remaining source quantities.
11. Test CanAddItem true followed by AddItem false with no mutation, with a partial stack merge, and after a complete transfer. Also test a callback exception after the add. Compare inventory plus dropped output totals and custom data; confirmed full transfers destroy the prepared world item, while confirmed partial transfers retain exactly the remainder. Include merging into existing scrolls with foreign custom-data keys. Repeat normal full-inventory fallback and reconnect on a host and dedicated server.
12. Inject an invalid post-add count or a failing count observation. Verify the locally owned prepared output is discarded, a possible-loss warning is logged, and no automatic retry/refund occurs. Repeat with an ownership change and a failed destruction call: ownership must not be forcibly claimed, and unresolved cleanup must be logged. Inspect replicated quantities after reconnect; console tests do not execute network destruction.
