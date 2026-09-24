# Sleepy Joe, C3: signature mechanics and starter set

Built and tested on 2026-09-24 (branch `feature/biden`). Design: [design.md](../design/design.md) §3 and §8.

## What you can play now

- **Starting deck:** 4 Strike, 4 Defend, **Catnap** (6 Block, Doze 3), **Here's the Deal** (Tangent: 8 damage / 7 Block / draw 2). 70 HP, 99 Gold.
- **Aviator Shades:** at the end of each Sleepy Joe turn, Doze 2; when he nods off, gain 8 Block. **Dark Aviators** (Touch of Orobas): the same with 14 Block.
- **Drowsy** shows on his portrait as a power ("7 of 10"). At 10 he nods off: a "Nodded Off" popup, the turn ends once the current card finishes, and the Aviator Shades' Block lands.
- **Waking up** at the start of the next turn: the "Dark Brandon" popup, **Laser Eyes** (the Defect's hyperbeam from his eyes, tinted) hit every enemy for his Drowsy, and Drowsy resets. His figure switches to the Dark Brandon pose set: for now a stand-in with red laser eyes.
- **Tangent cards** in hand: the lit line's number is gold and the other lines are dimmed; after every other card played the lit line moves on. As Dark Brandon every line is lit and happens.
- The 5 stub cards from the scaffold are still in the pool so rewards and the shop work; C4 replaces them.

## How it's built

| Piece | File | Notes |
|---|---|---|
| The cycle | `Mechanics/DrowsyCmd.cs` | The only code that changes it: `Doze`, `NodOff`, `WakeUp`, `FallBackAsleep`, `LaserFor`. Events `NoddedOff` and `LaserFired` for tests and balance stats |
| Hooks | `Mechanics/Hooks.cs` | `IDozeModifier`, `INodOffLineModifier`, `ILaserEyesModifier`, `IAfterNodOff`, `IAfterWakeUp`, `IAfterTangentLineChanged`, `ITangentAllLines`: what the C4 relics and powers plug into |
| States | `Powers/DrowsyPower`, `NoddedOffPower`, `DarkBrandonPower` | Drowsy is a buff on purpose, so Artifact and cleanses don't eat it. Nodded Off wakes him right after the energy reset (the Golden Shovel's and Bound Phylactery's timing). Dark Brandon removes itself at the end of the turn and switches his pose set |
| Tangent | `Cards/TangentCard.cs` | Base class: `LineCount`, `Line`, `PlayLine(n)`. Every card in every pile gets the game's hooks, so each Tangent moves itself in `AfterCardPlayed`. The text markup comes from `AddExtraArgsToDescription` (`{D1}{L1} ... {E1}` in the card text) |
| Starter relic | `Relics/AviatorsRelic.cs` | Shared by Aviator Shades and Dark Aviators. The end-of-turn Doze lives here, the way the Necrobinder's starter relic carries Osty, and the relic text says so |
| Framework | `NCharacterPoses.SetVariant(prefix, tint)` | A second pose set for a form a character takes mid-fight (`dark_combat_idle.png`, ...). Falls back to a tint when the paintings don't exist |
| Placeholders | `make_placeholders.py` | An art item's `"placeholder_eyes"` draws coloured eyes on the stand-in figure (the Dark Brandon poses) |

Choices made where the design left room:
- **Nodding off while asleep:** once he has nodded off, Doze does nothing until he wakes, so the end-of-turn Doze can't add to a nap that already happened.
- **Nod-off cards while awake** (C4's Lights Out, Sleep In): they still end the turn, and he's Dark Brandon again next turn with a 0 laser.
- **Laser Eyes damage** is unpowered, like Trump's Wall damage: Strength doesn't add to it, and its own modifiers (Laser Focus, the Mug, Double Vision) do.

## Tests

`python scripts/test.py ui -c biden` passes: 31 checks plus the shared ones, and no mod errors in the log.

| Check | Result |
|---|---|
| Max HP 70, Aviator Shades, turn 1 at 0 Drowsy | PASS |
| Here's the Deal enters the hand on line 1, is on line 2 after a Strike, and plays only line 2 (+7 Block, no damage) | PASS |
| The card text lights line 2 and dims lines 1 and 3 (markup check and screenshot of the raised card) | PASS |
| Doze 7, then Catnap's Doze 3: nodded off, Block +6 +8, the turn ended | PASS |
| Next turn: Dark Brandon, Laser Eyes once for 10, Drowsy reset | PASS |
| As Dark Brandon, Here's the Deal did all three lines (8 damage, 7 Block, draw 2); Doze does nothing | PASS |
| End of the Dark Brandon turn: Sleepy Joe again, no end-of-turn Doze | PASS |
| End of a Sleepy Joe turn: Doze 2 | PASS |
| Wake Up at 2 Drowsy: Laser Eyes for 2 | PASS |
| Touch of Orobas: Aviator Shades → Dark Aviators | PASS |

Screenshots: `build/test/ui_*/shots/` (`tangent_line2_card`, `nodded_off`, `dark_brandon_turn`, `tangent_all_lines_card`, `laser_eyes_beam`, `frame_vs_defect`).
The Donald's `test.py ui` still passes after the shared pose-controller change.

## Open points (later phases)

- **C4:** every other card, relic, power and potion; remove the 5 stubs; a warning glow on sleepy cards that would nod him off mid-turn (design.md §9).
- **C6:** the Dark Brandon poses and the power icons are stand-ins. The laser comes out pink-white over the Defect's blue beam and needs a stronger red.
- **C7:** co-op. Nodding off uses the same end-turn call as Golf Weekend, and the co-op test needs a `CoopTurn` that nods off.
