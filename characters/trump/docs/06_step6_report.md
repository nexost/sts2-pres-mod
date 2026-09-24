# Step 6 report: balance and playtesting

> **Paths changed after Step 8** (Step 8.5): the mod is now sts2-pres-mod (id `pres_mod`, repo `C:\Users\exeet\sts2-pres-mod`), the code is in `mod/pres_mod/src/` (`Framework/`, `Dev/`, `Characters/Trump/`), The Donald's text and design are in `characters/trump/`, and the test flags are `--pres-*` with saves in `modded_prestest`. This report keeps the names of its time; [FRAMEWORK.md](../../../docs/FRAMEWORK.md) maps the new layout.

_2026-09-23. Game v0.107.1. Mod v0.2.0. First pass, closed by the user: balance is good enough to move on, and more testing moves to the optional Step 10._

## How it was measured

The game's own AutoSlay bot is a crash tester: it gives itself 999 Plating and Regen, plays random cards and takes
random rewards, so it can't tell strong from weak. Step 6 adds a **balance bot** (`test.py balance`, code in
`Dev/DevHarness.Balance.cs`) that uses AutoSlay only to walk the map, events, shops and menus, and makes the real
decisions itself:

- **Combat:** no cheats. Cards are played for real (Energy is spent), and each playable card is scored in "HP saved":
  - Block counts up to the damage the enemies intend this turn.
  - A kill or a Deport is worth the attack it cancels.
  - Plain damage is weighted by how hard the enemies hit compared to the HP they have left.
  - Build is worth the future Section Block and stage perks.
  - Draw, Energy and the other characters' resources (Osty, Stars, Poison, Doom) also count.
- **Potions:** used in elite and boss fights, or at low HP.
- **Rewards:** rarer cards first, Powers slightly preferred.
- **Rest sites:** heal under half HP, otherwise upgrade.
- **Map:** rest sites when hurt, elites only when healthy. AutoSlay always takes the leftmost path.
- **Setup:** Ascension 0, the same seeds for every character. **Full-heal mode:** heal to full after every fight, the same for everyone, so runs reach Acts 2–3 and every fight can be compared on its own.

Runs go **9 at a time**, each game a muted, borderless tile in a 3×3 grid on the main monitor, from one shared queue: 54 runs take about 25 minutes.
Test games never write the global `settings.save`, so nothing leaks into your normal game.

The bot is simple and character-agnostic: absolute results (every character dies by Act 2–3) mean little.
The signal is the **comparison**, with the same bot and the same seeds for The Donald, Ironclad and Silent.

## Results before tuning (full heal, 18 runs each)

| | The Donald | Silent | Ironclad |
|---|---|---|---|
| Average floor | 23.7 | 24.1 | 30.3 |
| Act 1 hallway fights: HP lost / turns | 17.3 / 5.2 | 17.8 / 5.5 | 15.5 / 4.4 |
| Act 1 elites: HP lost | **20.3** | 28.5 | 24.6 |
| Act 1 bosses: HP lost | **30.9** | 59.8 | 53.5 |
| Act 2 hallway fights: HP lost | 26.7 | 25.7 | 27.0 |
| Act 2 elites: HP lost | **31.1** | 33.8 | 40.4 |

- **The Donald is inside the base-game range**, level with Silent and a bit below Ironclad (whose plain damage and block cards suit this bot best).
- His profile matches the design:
  - **the best defense** in elite and boss fights, where the Wall has time to pay off (Ceremonial Beast 21 HP vs Ironclad's 56);
  - **slower in short fights**, about +0.5–1.5 turns (weak Nibbits 11.7 vs 8.2), because his starter has no front-loaded damage (Ironclad has Bash).
- In fights, the Wall reached 25 in 50% of them, 45 in 23% and 70 in 10%.
- Deports happened in 59% of fights (0.67 per fight).
- The Pay-Gold cards spend about 13 Gold per fight, so Gold isn't inflating.

## Changes in this pass

| Change | Why |
|---|---|
| **Deport** 6 → **7** damage (9 → **10** upgraded) | A small nudge to early speed and Deport frequency. The starter's only non-Strike attack should feel like a signature. |
| **Deport line capped at 60%** (was 100%) | The design's watch list flagged it, and the card test confirmed it: Border Control and Border Wall with 10 Sections reached 100%. Law and Order would then Deport every non-boss enemy at full HP. At 60% Deport stays a finisher; Mass Deportation's doubled line reaches the cap on its own. |

## Results after tuning (full heal, same seeds)

| | The Donald | Silent | Ironclad |
|---|---|---|---|
| Final batch (9 each): average floor (median) | **26.4 (28)** | 21.3 (17) | 29.1 (33) |
| Act 1 hallway fights: HP lost / turns | 20.2 / 5.1 | 17.6 / 5.4 | 14.7 / 4.3 |
| Act 1 elites: HP lost | 35.1 | 37.4 | 23.5 |
| Act 1 bosses: HP lost | **43.7** | 59.6 | 53.7 |

- An earlier tuned batch of 18 Donald runs averaged floor **28.1** (median 29) with **2 wins**, the only wins by any character under this bot.
- Across all tuned runs, The Donald sits **between Silent and Ironclad**, which is the target.
- Deports happen in about half of all fights (0.6 per fight).
- The Wall reaches 25 in about a third to half of the fights.
- The UI mechanics test passed with the new 7-damage Deport. The user skipped the rest of the regression runs for this pass.

Small samples (9–18 runs per character) make single numbers noisy. The consistent pattern across every batch is what counts:
- the best defense against elites and bosses;
- slower short fights;
- overall strength between the two base characters.

## Moved to the optional Step 10

- **Your playtests.** The bot can't judge fun, synergy or how the Wall feels.
- **Style-focused runs.** `favor=Wall|Deport|Deals|Tweets` is ready in `test.py balance`, to check that every play style wins on its own.
- **Bigger baselines** (27+ runs per character) and a smarter bot.
- Watch-list items the bot can't exercise (it builds random decks): a dedicated Wall deck (Infrastructure Week, Great Wall, Reinforced Concrete), and Gold stacking (Tariffs, Merch Stand, Gold-Plated Bricks).
