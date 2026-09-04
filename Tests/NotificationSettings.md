# Notification settings

## Configuration and routing

Notifications are always enabled. The server validates and relays valid enchantment events independently of any display threshold. Each output then applies its own minimum to the resulting enchantment level:

| Configuration Manager entry | Scope | Default |
| --- | --- | ---: |
| `1 - General` / `Webhook Minimum Enchant Level` | Only the server's webhook delivery; not synced | 6 |
| `2 - Client` / `Notification Minimum Enchant Level` | Only that client's in-game display; not synced | 6 |

The client notification minimum is an integer slider from 0 through 100; the separate webhook minimum remains 0 through 500. `Success Webhooks` and `Failure Webhooks` appear in General and use only the server's comma-separated URL lists. A local host uses independent webhook and in-game thresholds. `Notifications - Filter` continues to select success/failure types locally; `None` disables local display without disabling webhooks or other clients' notifications. Notification display duration is fixed at five seconds.

## Automated checks

The rule tests cover inclusive threshold boundaries, zero/negative minimum handling, independent webhook/display outcomes in both directions, every success/failure filter combination, rejection of undefined event types, fixed duration, threshold slider/display metadata, the new seven-category ordering, and propagation of hidden-setting metadata without losing existing tags.

The shared default and description factories live in the Unity-independent `Configs.NotificationSettings` class and are used by both runtime bindings and tests. The filter matrix passes actual enum values without reflecting over or boxing the surrounding UI class's nested enums, keeping this .NET Framework test harness independent of Unity's runtime dependencies.

They do not exercise Unity UI initialization, RPC sender validation, actual Discord delivery, or Configuration Manager rendering. Build the Release project, then build and run `Tests/ValheimEnchantmentSystem.RuleTests.csproj` using the existing test workflow.

## In-game integration checklist

1. With fresh server/client configs, confirm both minimums default to 6 and are marked not synced. Set them independently, reconnect, and confirm the client value is not replaced by the server value. Confirm the categories are General, Client, Enchantment, Skill, Scrolls, Biome Tiers, and Scroll Recipes, numbered 1 through 7.
2. On a dedicated server, set webhook minimum 15, receiver A's notification minimum 6, and receiver B's minimum 15. Generate a valid result at +8. A should display it; B and the webhook should not. At +15, both clients and the webhook should receive it, subject to each local filter.
3. Reverse the thresholds: server webhook minimum 6 and receiving client minimum 15. A +8 result should reach the webhook but not that client's screen. Repeat with a local host to confirm its webhook and its own screen have independent thresholds.
4. Set the enchanting sender's local minimum to 100 and filter to None. Generate a valid +8 result while the server webhook minimum and another recipient's minimum are 6. The sender should display nothing, but the server webhook and the other recipient must still receive the event. The sender's settings must not suppress outbound events. Verify a client cfg minimum above 100 clamps to 100 while a webhook minimum above 100 remains valid up to 500.
5. Test Success, LevelDecrease, Destroyed, and protected no-change failures. Compare the resulting level against each minimum, independently of the previous/target level. Verify the success/fail filter works both on receipt and when queued notifications are shown after a setting change.
6. Put different webhook URLs on a dedicated server and a normal client. Only the dedicated server's URLs should be called. Repeat with a local host: only the host's URLs should be called. Use test endpoints you control and verify multiple comma-separated URLs still work.
7. Confirm notifications remain visible for five seconds, including existing fades. Confirm no `Enable Enchantment Notifications` or notification duration option is exposed or bound. Old entries must not control behavior or migrate into new entries.
8. Confirm the panel is always draggable, no draggable toggle appears, and offset X/Y remain saved in cfg but hidden from Configuration Manager. Move the panel, restart, and verify its position persists; right-click the drag handle to reset it.
9. Repeat malformed-event and sender-authorization checks: unknown result types, negative/out-of-range levels, invalid item prefabs, impossible level transitions, non-server global messages, and unready peers must still be rejected. Existing request throttling must remain in effect.
10. Verify previously customized scroll recipe values still load despite the displayed section changing to `7 - Scroll Recipes`; no duplicate fresh recipe config should replace those values.

This is a manual checklist, not a claim that multiplayer or in-game validation has been performed.
