# C5 report: balance

_2026-09-24. Game v0.107.1. Branch `feature/biden`. Art is still placeholder (C6)._

## How it was measured

This uses the same balance bot as The Donald's Step 6 ([report](../../trump/docs/06_step6_report.md)). It plays real fights with no cheats and makes its own choices. AutoSlay only walks the map. The settings:

- Ascension 0;
- full heal after every fight;
- the same seeds for Sleepy Joe, Ironclad and Silent;
- 9 games at a time, tiled 3×3 and muted.

The bot is simple, so absolute results mean little (everyone dies in Acts 2–3). The signal is the **comparison** on the same seeds.

### What the bot had to learn for Sleepy Joe

The code is in `Characters/Biden/Dev/DevHarness.Biden.Balance.cs`.

| Effect | How it's valued |
|---|---|
| **Tangents** | Only the lit line counts, or every line as Dark Brandon. The generic value would add up every printed number, so it's replaced for these cards. |
| **Form** | Finger Guns, Dogfight and Air Force One hit more times as Dark Brandon. The "As Dark Brandon" bonuses (Red Eye, Morning Person, Game Face, Aviator Glint, Stare Down) count only when he's awake. Second Cup is Energy when he's awake and a Wake Up card when he's asleep. |
| **Doze** | 1 per Drowsy, as progress toward Dark Brandon. If it would nod him off now, it's valued as a nap instead. |
| **A nap** | Next turn's Laser Eyes at 60% (they land a turn later), plus 6 for the Dark Brandon turn, plus the Aviator Shades' 8 Block. Minus 7 for each Energy left unspent, because the turn ends. |
| **Wake Up cards** | Laser Eyes now, plus 4 for each Tangent or "As Dark Brandon" card in hand. |
| **Nod-off cards** (Lights Out, Sleep In, Out Cold, Deep Sleep) | The nap value. |

Each fight records naps, Laser Eyes, their damage and the highest Drowsy; `balance_report.py` prints them.

### Changes to the shared framework

- `CardValueOverride`: a new hook that replaces a card's whole value.
- `DamageValue` and `BlockValue`: split out of the generic value, so a character can score the parts of a card the usual way. The generic results are unchanged.
- `balance_report.py` prints any extra per-fight numbers, plus the share of fights with a nap.

## Before tuning (seeds BIDEN1, 18 runs each)

HP lost per fight / turns per fight.

| | Sleepy Joe (Doze 2) | Ironclad | Silent |
|---|---|---|---|
| Average floor (median) | 25.7 (28.5) | 29.6 (33) | 26.5 (24) |
| Act 1 hallway fights | 16.0 / 5.2 | 16.6 / 4.5 | 15.1 / 4.9 |
| Act 1 elites | **42.2** / 6.9 | 27.9 / 5.4 | 25.0 / 5.8 |
| Act 1 bosses | 45.7 / 11.3 | 53.5 / 10.2 | 54.0 / 11.3 |
| Act 2 hallway fights | 28.0 / 5.6 | 28.4 / 5.5 | 24.8 / 5.4 |
| Act 2 elites | 34.3 / 6.9 | 39.2 / 6.1 | 43.8 / 6.9 |
| Act 2 bosses | 66.4 / 8.7 | 78.3 / 8.3 | 64.7 / 9.2 |
| Naps per fight (fights with one) | 1.15 (71%) | | |

He was inside the base-game range, at the bottom:
- **Act 1 elites were the weak spot.** These are short, hard fights, and the Dark Brandon cycle often hadn't come round yet.
- **He was strong in long fights** (bosses), where the cycle comes round several times.

## The change

| Change | Why |
|---|---|
| **Aviator Shades and Dark Aviators: end-of-turn Doze 2 → 3** | The starter deck now wakes about a turn earlier, so the first Dark Brandon turn and Laser Eyes land inside a short elite fight. The design's watch list named this lever for the nap cycle, and it makes the signature show up in more fights. More HP or nap Block would also have helped, but neither makes the cycle come round more often. |

Nothing else changed. Nodding off still ends the turn on the spot, and Laser Eyes still deal 1× Drowsy.

## After tuning (Doze 3)

On the seeds it was tuned on (BIDEN1), he jumped to floor 32.1. That's more than luck alone could explain, but it could still be an overshoot. So all three characters also ran a fresh seed set (BIDEN2):

| | Sleepy Joe (Doze 3) | Ironclad | Silent |
|---|---|---|---|
| BIDEN1: average floor (median) | 32.1 (33) | 29.6 (33) | 26.5 (24) |
| BIDEN2: average floor (median) | 24.3 (21) | 24.4 (28) | 25.1 (25) |
| **Both sets: average floor (median)** | **28.1 (33)**, 35 runs | 27.1 (33), 35 runs | 25.8 (25), 33 runs |
| Runs that reached Act 3 | 6 | 3 | 2 |
| Act 1 hallway fights | 13.8 / 4.8 | 16.8 / 4.5 | 15.5 / 5.0 |
| Act 1 elites | 26.4 / 6.2 | 29.0 / 5.5 | 26.1 / 6.0 |
| Act 1 bosses | 43.5 / 12.2 | 55.5 / 10.2 | 54.3 / 11.7 |
| Act 2 hallway fights | 21.6 / 5.5 | 31.4 / 5.5 | 27.1 / 6.1 |
| Act 2 elites | 36.1 / 6.9 | 42.0 / 6.0 | 41.2 / 6.7 |
| Act 2 bosses | 65.6 / 9.8 | 77.7 / 8.1 | 69.8 / 9.2 |

The fight rows cover both sets. Per fight, over both sets:
- 1.36 naps, with a nap in **82%** of fights;
- 1.65 Laser Eyes, for 14.7 damage.

What the numbers say:
- **He's level with Ironclad by floor** (+1, well within the noise of about ±2), and a bit above Silent. The Donald's target was "between Silent and Ironclad"; Sleepy Joe is at its top edge, not past it.
- **He loses less HP per fight than both in most categories:**
  - Act 2 hallway fights, where Laser Eyes hit every enemy;
  - bosses, which last long enough for several naps.

  His 70 max HP (Ironclad has 80) is why that doesn't turn into more floors: boss fights still kill him about as often.
- **Act 1 elites went from worst (42.2) to par (26.4).** They still vary the most by elite:
  - **Bygone Effigy** is a 127 HP race. It sleeps one turn, gains 10 Strength, then slashes for 23 every turn.
  - **Phrog Parasite** is the other expensive one.

  On BIDEN2 they cost him 52 and 46 HP, against about 35 and 27 for the others. A nap that ends a turn early costs the most in a race. That's the character's intended risk, so there's no reason to design it away.
- **He swings more:** he has the most Act 3 runs, but on BIDEN2 his median is floor 21.

## Decision (recommended): keep Doze 3, change nothing else

- By floor he's level with Ironclad, the top edge of the target.
- The signature now shows up in 4 fights out of 5.
- Trimming further now would be tuning to noise; your playtests are the better judge from here.
- If he feels too strong, the next lever is the nap Block (8 → 6). It softens him without making the Dark Brandon cycle rarer.

## Other notes from the bot's data

- **Pick rates follow the bot's rule, not card strength.** It takes the rarer card first and slightly prefers Powers.
  - Every card picked under 15% of the time is a Common, so the low rates don't point to weak cards.
  - The most picked were Heavy Sleeper, Dark Brandon Rises, Build Back Better, Long-Winded, Amtrak Joe, Gaffe Machine and Power Nap.
- **Most played after the starter cards:** Here's the Deal, Catnap, Power Nap, Talking Points, Where Was I, Early Riser, Sleep In.
- **Bowlbugs** killed him 3 times on BIDEN2. That fight costs Ironclad 52 HP on average too (the Nectar bug gains 15 Strength); he just has 10 less max HP. It's not a Biden problem.
- **5 of 126 runs didn't finish.** Each stopped in the game's own AutoSlay, and they're left out of the numbers:
  - BIDEN1010 (Sleepy Joe) got stuck on the Cauldron's "Loot!" potion screen in a shop.
  - BIDEN1002 and BIDEN2017 (Silent) and BIDEN2008 (Ironclad) stopped with "Room type not assigned" at the start.
  - BIDEN1004 (Silent) stopped in a fight on floor 5.

## Tests

| Test | Result |
|---|---|
| `test.py ui -c biden` | **PASS**, 0 log problems. It now checks for Doze 3 at the end of a Sleepy Joe turn, and Laser Eyes for 3 on waking right after. |
| `test.py cards PRESTEST1 relics -c biden` | **PASS**: all relic, potion and power checks, 0 log problems. One test fix (below). |

**The test fix.** The wake-up check fires Laser Eyes at 2 × 9 to every enemy. The test fight's Ruby Raiders have 18–31 HP, and the earlier relic checks had already worn them down, so the laser could end the fight before Wide Awake and No Malarkey were checked. The check now heals the enemies first and wakes from 3 Drowsy (2 × 7).

## Next

C6: art (`art_review.py -c biden`).
