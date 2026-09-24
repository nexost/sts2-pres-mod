# Sleepy Joe, C4: all the content and text

Built and tested on 2026-09-24 (branch `feature/biden`). Design: [design.md](../design/design.md), [cards.json](../design/cards.json).

## What's in

- **88 cards** (33 Attacks, 36 Skills, 19 Powers), matching `cards.json` one to one. The card pool is generated from it, in its order. The 5 scaffold stubs are gone.
- **24 powers** (21 new): Brandon, Nap, Tangent and General families in `Powers/*Powers.cs`, plus Drowsy, Dark Brandon and Nodded Off from C3.
- **9 relics**: Aviator Shades and Dark Aviators (C3), Travel Pillow, Ice Cream Cone, Index Cards, '67 Corvette, Dark Brandon Mug, Teleprompter, Amtrak Pass.
- **3 potions**: Warm Milk, Espresso Shot, Dark Roast.
- **All text**: every card, power, relic and potion, the select screen and banter lines, Colorful Philosophers ("Navy"), and his side of all 59 Ancient and Architect lines. `build.py` counts 0 `TODO`s.
- **Art list**: every relic, potion and power has an entry in `art/art_assets.json`, so the review tool lists them (C6).

## How it's built

- **Card families:** `TangentCard` (lines, lit line, all lines as Dark Brandon) and `SleepyCard` (Doze cards).
  - A sleepy card **glows red** when playing it now would nod him off mid-turn, the warning from design.md §9.
  - "As Dark Brandon" cards **glow gold** while he's awake, and so do Tangents that would do every line.
- **Reused game pieces:**
  - Double Damage (Unleashed, Dark Roast);
  - Retain Hand (Sound Asleep);
  - Free Attack (Corvette Cruise);
  - Strength and Dexterity;
  - Weak and Vulnerable;
  - the combat target RNG for random hits and the card selection RNG for Brain Freeze.
- **Costs:** Mic Drop (0 while awake), Zero to Sixty (1 less per card played) and the Teleprompter (Tangents free on a wake turn) use the game's cost hook, so the cost gem shows the real price.
- **Repeat the Line** replays the owner's last card this turn with the game's own `AutoPlay`, at the same target when it's still alive.

### Choices made where the design left room (design.md and cards.json updated)

| Card or relic | Choice | Why |
|---|---|---|
| Infrastructure Law | +1 Energy each turn; no Block | A power holds one number, and energy is the effect worth a 3-cost Rare |
| Wide Awake | Block stacks with copies; the draw stays 1 | Same reason |
| Double Vision | Each copy adds a hit | Stacks without limit, like the design asks |
| Corvette Cruise | "Your next Attack costs 0" (no "this turn") | It's the game's own Free Attack |
| Unleashed | Upgrade: costs 0 | The design had no upgrade for it |
| Repeat the Line | Only if the last card is in the Discard Pile | Exhausted cards and played Powers are gone; X-cost cards are skipped |
| Tall Tales | Damage and Block grow, not draw counts | +1 card per line change would snowball |
| Brain Freeze | A random line isn't a "line change" | So it doesn't trigger Tall Tales or Stream of Consciousness |
| Soul of the Nation | Its extra card is part of the hand draw | Same as Amtrak Joe |

## Tests

| Test | Result |
|---|---|
| `test.py ui -c biden` | PASS, the 31 C3 checks |
| `test.py cards PRESTEST1 -c biden` | **257 checks PASS**; every card base and upgraded, every text rendered, 0 mod errors in the log. The first run's 8 failures were my own energy expectations (the test plays cards for free); fixed and rerun for those cards: PASS |
| `test.py cards PRESTEST1 relics -c biden` | PASS, 36 checks: each relic and potion, the power triggers (on waking, on nodding off, on a Tangent's line change, at turn start), Mic Drop's cost, Up All Night's playability, Zero to Sixty's discount |
| `test.py autoslay BIDENC4 -c biden` | **Victory**: all three acts and the Architect with his lines, 4 min 16 s, no exceptions |
| `test.py cards PRESTEST1 relics -c trump` | PASS (the shared harness changed) |

How the cards test plays him:
- Base versions are played as Sleepy Joe at 3 Drowsy, and upgraded versions as Dark Brandon. So every card is seen in both forms.
- His powers are cleared before each card, so exact Block and Drowsy checks aren't thrown off by earlier Power cards.
- This uses the new shared hook `PrepareCardTurnFor(ctx, card, upgraded)` (FRAMEWORK.md §6).

## Open points (later phases)

- **C5 balance:** none of the numbers have been through the balance bot yet, and the bot needs `CardValue` hooks for Doze, waking, nodding off and Tangent lines.
- **C6 art:** every portrait and icon is a stand-in.
- **C7 co-op:** Reach Across the Aisle and nodding off in a co-op fight.
