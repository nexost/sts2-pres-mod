# C7 report: final checks

_2026-09-24. Game v0.107.1. Branch `feature/biden`. Sleepy Joe is complete; every test below passes for him and for The Donald._

## Results

| Test | Sleepy Joe | The Donald (regression) |
|---|---|---|
| `test.py ui` | **PASS**, 0 log problems (21 screenshots checked in C6) | **PASS**, 0 log problems. Select screen, poses and all four Wall stages unchanged |
| `test.py cards PRESTEST1` | **PASS** after one test fix (below). Every card played base and upgraded, all relics, potions and powers, all text | **PASS**: every card and relic check, and a sweep of 178 encounters with no problems |
| `test.py autoslay PRESTEST7` | **Victory**, 0 log problems, ~5 min | **Victory** |
| `test.py coop` (two of the character) | **PASS**, 0 desyncs, all 3 turns identical on both instances | **PASS**, 0 desyncs |
| `test.py coop … IRONCLAD` (mixed party) | **PASS**, 0 desyncs | — (unchanged since Step 8) |

The Donald's card and AutoSlay runs log 36 errors. All are controller warnings from the game's own input code (`NInputManager`: `The InputMap action "controller_d_pad_up" doesn't exist`), the same lines as in earlier runs. None come from the mod.

## Co-op: what's checked now

Sleepy Joe's kit gained a `CoopTurn` and a new shared hook, `CoopTurnStart`. Checks at the start of every co-op turn, after both instances recorded their state.

| Check | Result |
|---|---|
| Nodding off mid-turn (Catnap, then Sleep In) ends only that player's turn | The game's own end-turn path (`PlayerCmd.EndTurn`) marks them ready. The round waits for the other player. After napping, the player can't play more cards: the game disables local plays once your turn has ended |
| **Reach Across the Aisle** (co-op only), played after the other player ended their turn | Every other player gets +1 Energy and draws 1, a Sleepy Joe or an Ironclad. The player who played it breaks even |
| Turn 2, on both instances | Every Sleepy Joe who napped woke up as Dark Brandon, Drowsy back to 0, one 3-damage Laser Eyes each |
| Rest site and shop | Two Sleepy Joes side by side in camp chairs and with ice cream in the shop; no overlap in the line-up |

A second fix in the shared co-op harness: its scripted Strikes and Defends stop once that player's turn has ended, as the game's hand does. `TryManualPlay` doesn't check it.

## Problems found and fixed

| Problem | Cause | Fix |
|---|---|---|
| Card test: Ramble On+ "Block 0, expected +20" | As Dark Brandon it does every line; its 50 damage killed both test enemies, so the fight ended before the Block line. The card is fine: with more enemy HP it gave the full +20 | The Block check accepts a card that ended the fight, as the damage check already did |
| Co-op tests failing with a desync at the rest site, for **The Donald too** | The PC: only the second game instance crawled (asset preloads ~600 ms instead of ~20 ms), so the two drifted apart and the harness's `win` hit the slow one mid-turn. The monitors had switched from 60 to 120 Hz since the night before | A restart. Afterwards The Donald's control run and both Sleepy Joe runs passed. Noted in the plan's open items, with "run The Donald's co-op test as a control" |

During the fight, every one of the game's own checksums matched between the instances, including the naps, Reach Across, the wake-ups and the lasers. That held even in the failed runs.

## Docs

- `FRAMEWORK.md`: the `CoopTurnStart` hook, and the harness stopping scripted plays after a player's turn ends.
- `ADDING_A_CHARACTER.md` C7: use `CoopTurnStart` for what a turn caused; run The Donald's co-op test as a control before suspecting a new character.
- `00_project_plan.md`: the slow-instance symptom and the fix.

## Ready for release (your decision)

Sleepy Joe is new content that leaves old saves loadable, so by [PUBLISHING.md](../../../docs/PUBLISHING.md) he'd be a **MINOR** release: **v1.1.0**.

The release steps:
- merge `feature/biden` into `master`;
- bump `mod.json`;
- release notes;
- a tag and zip on GitHub.

Each of these is public, so nothing is done without your go-ahead.
