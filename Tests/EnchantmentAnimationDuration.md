# Enchantment animation duration

## Client configuration

Configuration Manager exposes `UI - Enchantment Animation Duration` under `2 - Client` as a numeric 0-1 slider, not a percentage. The default is 1 second. The existing cfg section and key remain unchanged:

```ini
[Client]
EnchantmentAnimationDuration = 0.5
```

- `0`: the same validated enchantment result is processed immediately, without the waiting animation or waiting sound. Success/failure effects, result sounds, and the result/OK step remain.
- `0.5`: half a second of animation.
- `1`: one second of animation (the default).

Values outside the range are clamped to 0-1; previous settings of 2-5 seconds now become 1 second. Changes apply to the next attempt, not an attempt already in progress. This remains a client-only setting: no enchantment rules or server-synced values are changed.

## Automated checks

Run `dotnet run --project .\Tests\ValheimEnchantmentSystem.RuleTests.csproj -c Release`. Timing tests cover zero, fractional values, range boundaries, nonfinite inputs, bounded progress, and numeric slider metadata. The existing enchantment rule tests still cover result decisions. These tests do not execute Unity rendering, audio, or input.

## In-game checks

1. Set 0 and attempt an enchantment with valid materials. Verify that the result appears immediately, exactly one required scroll is consumed, and the normal success/failure VFX and result sound play without the waiting effect/sound.
2. Confirm that the next press acknowledges the result instead of performing another enchantment. Then attempt again; verify the normal validation, blessing selection, skill EXP, failure rules, and notifications.
3. Try missing materials, a capped item, and an item no longer in the inventory. Verify immediate mode does not bypass existing validation or consume materials.
4. Set 0.5 and 1. Verify the expected duration and that the waiting sound stops before the result sound; cancellation still works for nonzero durations.
5. Change the setting while an animation is running. Verify the current attempt finishes at its original duration and the next attempt uses the new value.
6. Check the slider shows numeric seconds rather than percentages, edit the unchanged cfg key, and verify settings persist after restarting the client.
