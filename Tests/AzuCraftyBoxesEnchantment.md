# AzuCraftyBoxes normal-container enchantment integration

Test with the supplied AzuCraftyBoxes 1.8.15 and this mod on the client; repeat multiplayer cases on a dedicated server. These are manual checks, not a claim of in-game testing.

1. Without ACB installed, and with ACB disabled or personal pulling prevented, only inventory scrolls count and can be consumed. No missing-assembly errors or repeated warning spam.
2. Put normal and blessed scrolls in separate eligible player-built chests. With none in inventory, select equipment and switch blessing mode: count and availability match the selected exact prefab. Enchanting removes one matching scroll, saves the chest, and grants only the normal result/EXP.
3. Put scrolls in inventory as well: inventory is consumed first and chest stock is unchanged. Split chest stacks and multiple chests must not cause more than one removal per attempt.
4. Enable Leave One Item: a chest with one matching scroll contributes zero; two contribute one. Reserve one per source container, including split stacks. Inventory stock is not subject to this reserve.
5. Vary ACB range and YAML include/exclude restrictions. Change personal pulling, range, reserve, or access while the animation runs: execution revalidates the current settings and fails without a roll/EXP if no source remains. A full/empty inventory does not change direct chest consumption.
6. Check private chests, protected areas, WardIsLove if installed, creator-less world containers, destroyed/out-of-range chests, and a chest another player currently has open. Neither preview nor consumption may bypass access. A locally owned chest open by this player may be used.
7. Put all scrolls only in KG/makail drawers, ACB backpack sources, or gem bags: they must not make enchanting available. Skill EXP scroll interaction and inventory scroll combining remain unchanged.
8. With a remote owner and a dedicated server, consume from an eligible closed chest, then reconnect/reload and confirm the scroll remains removed. Test material removal during the animation by another player: no material means no enchant roll, skill EXP, or notification.
9. ACB's standard direct container save is not an atomic reservation across clients. Simultaneous writes to the same chest inherit that game's/mod's replication limitation; do not interpret this integration as a server-acknowledged transaction.
10. UI count refresh is evaluated when selecting/reselecting equipment and when pressing Enchant; it is not a continuously polled display. Verify instant animation duration 0 uses the same consume-before-result path.

Automated rule tests cover exact prefab matching, zero/negative stock, normal/blessed separation, reserve accounting, saturated counts, inventory-first lazy container access, one successful consumption, stock disappearing, and stopping on an uncertain mutation instead of trying another source.
