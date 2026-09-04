# Enchantment skill experience

## Rules

Enchantment attempts no longer select skill EXP by the required scroll tier. With the default coefficients, the EXP for a successful attempt is:

```text
2 + 0.5 * current enchantment level + 4 * (1 - base success chance / 100)
```

The current level is captured before the attempt changes or destroys the item. Equivalently, the level term uses `target level - 1`. The base chance comes from the item's enchantment chance configuration, before skill and blessed-scroll bonuses. Base chance is clamped to 0-100 for the difficulty term. An attempt with zero or invalid final chance grants no EXP; a zero base chance with a positive final chance is eligible.

Failure EXP uses the same pre-attempt reward multiplied by `Failed Enchant Skill EXP Multiplier` (default 0.5). The existing `Skill Gain Factor` is applied by the normal skill-grant path. Directly consumed skill scrolls retain their separate `Skill Scroll EXP F` through `Skill Scroll EXP S` settings.

| Attempt | Base chance | Positive final chance | Success EXP | Failure EXP |
| --- | ---: | ---: | ---: | ---: |
| +0 to +1 | 90% | 90% | 2.4 | 1.2 |
| +4 to +5 | 74% | 74% | 5.04 | 2.52 |
| +9 to +10 | 50% | 50% | 8.5 | 4.25 |
| +14 to +15 | 25% | 25% | 12 | 6 |
| +19 to +20 | 0% | 3.5% | 15.5 | 7.75 |

## Automated checks

From the repository root in a Visual Studio Developer PowerShell, build the Release DLL once, then build and run the tests without repacking the project again:

```powershell
MSBuild.exe .\ValheimEnchantmentSystem.csproj /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU
MSBuild.exe .\Tests\ValheimEnchantmentSystem.RuleTests.csproj /restore /t:Build /p:Configuration=Release /p:BuildProjectReferences=false
.\Tests\bin\Release\net48\ValheimEnchantmentSystem.RuleTests.exe
```

The pure-rule tests cover the examples above, independent formula coefficients, custom base chance, final-chance bonuses not reducing EXP, zero-chance attempts, invalid input, negative coefficients, finite overflow caps, and success/failure multipliers. They do not execute the Unity item-consumption or skill-grant path.

## In-game integration checks

1. Check that the `4 - Skill` section exposes the three shared formula settings instead of `Enchant Skill EXP F` through `Enchant Skill EXP S`. Confirm that the seven separate `Skill Scroll EXP` settings are still present.
2. With Skill Gain Factor 1 and no other skill EXP modifiers, attempt +9 to +10 on an item whose configured base chance is 50%. Check that a success grants 8.5 EXP and a failure grants 4.25 EXP, including a failure that drops the item to +8. Confirm that exactly one selected scroll is consumed.
3. Repeat an equivalent attempt using a different required scroll tier. With the same level and base chance, the reward should be unchanged. Repeat with skill or blessed bonuses increasing the final chance; the per-outcome reward should remain unchanged.
4. Test all enabled failure modes, including destruction and protected blessed failure. Every failure uses the same pre-attempt reward and the configured failure multiplier, rather than recalculating from the resulting item level.
5. At +19 on an item with 0% base chance, use skill 0 with no positive chance bonus and check that a failed attempt grants no EXP. Add a skill or blessed chance bonus so the final chance is positive and check that attempts become eligible for EXP again.
6. Change an item's chance YAML without changing its scroll tier. Verify that its EXP follows the new base chance. Set each shared coefficient independently to zero, then all three to zero; the latter should disable enchantment EXP without disabling directly consumed skill scrolls.
7. Repeat on a dedicated server with differing local values and verify that the synchronized server coefficients and failure multiplier govern the attempt. Verify that successful and failed attempts still receive Skill Gain Factor exactly once.

These manual cases are a checklist, not a claim that in-game validation has been performed.
