# Step 5 report: all the content

> **Paths changed after Step 8** (Step 8.5): the mod is now sts2-pres-mod (id `pres_mod`, repo `C:\Users\exeet\sts2-pres-mod`), the code is in `mod/pres_mod/src/` (`Framework/`, `Dev/`, `Characters/Trump/`), The Donald's text and design are in `characters/trump/`, and the test flags are `--pres-*` with saves in `modded_prestest`. This report keeps the names of its time; [FRAMEWORK.md](../../../docs/FRAMEWORK.md) maps the new layout.

_2026-09-23. Game v0.107.1. Mod v0.2.0. All art is still placeholder (Steps 7–8). Numbers are the Step 3 design; Step 6 tunes them._

## What's in

The Donald is now a complete character. Everything in [`design/cards.json`](../design/cards.json) is playable, and the Step 2 filler cards are gone.

| | Count | Notes |
|---|---|---|
| Cards | **88** + the Tweet token | 4 Basic, 20 Common, 36 Uncommon, 26 Rare, 2 Ancient. By style: Deals 22, Wall 21, General 20, Tweets 14, Deport 11 (plus shared cards) |
| Powers | **26** | One per Power card plus the temporary ones: Barbed Wire, Blocked!, Sharpie and Low Energy |
| Relics | **9** | Golden Shovel, Diamond Shovel, Hard Hat, Long Red Tie, Gold-Plated Bricks, Rubber Stamp, Permanent Structure, Late-Night Phone, Gold-Plated Toilet |
| Potions | **3** | Quick-Dry Concrete, Covfefe, Deportation Draught |
| Ancient dialogue | **8 Ancients** | Neow, Darv, Orobas, Pael, Tanx, Tezcatara, Nonupeipe and Vakuu each have a first meeting and a repeat line for The Donald, in their own voice. The Architect had his since Step 2. |
| Co-op cards | 2 | Trickle Down and Coalition Wall appear only in multiplayer. Coalition Wall gives the other players a Wall, and it shows next to them. |

Every card, power, relic and potion has English text, a placeholder icon or portrait, and upgrade values from the design.

## How it's built

- **One class per card**, following the base game's own patterns:
  - Wall Slam, Demolition and Net Worth work like Body Slam;
  - Fire and Fury and Tweetstorm are X-cost, like Whirlwind;
  - Bigly works like Rampage;
  - Cornerstone works like Genetic Algorithm and Feed;
  - Alternative Facts works like Hologram, and Mulligan like Acrobatics;
  - Trending works like Right Hand Hand;
  - Late-Night Phone works like Throwing Axe.
  The live numbers ("Deals 35 damage") show on the cards in combat.
- **Hooks from Step 4** carry the relics and powers:
  - Hard Hat modifies Builds;
  - Reinforced Concrete modifies Section Block;
  - Border Control and Border Wall raise the Deport line;
  - Long Red Tie and So Much Winning trigger on Deports;
  - The Art of the Deal triggers when you Pay Gold.
- **New patches:**
  - `TargetFilterPatch`: You're Fired! and the Deportation Draught can't target immune bosses, so 99 Gold is never wasted.
  - `RubberStampPatch`: Deported enemies count as defeated for the Gold reward.
  - `AncientDialoguePatch`: the Ancient lines.
- **Cornerstone's growth is saved**, using the saved-property support from Step 4.
- **`GlobalUsings.cs`** keeps the ~120 model files short.

### Choices made while implementing (where the design text left room)

| Card or relic | How it works |
|---|---|
| Guard Towers | One shot per Section, each at a random enemy (like Sword Boomerang). |
| Barbed Wire | Damage back = its number + your Sections **when you're hit**, so building more that turn raises it. |
| Gold Tower | Block = Gold × 2 ÷ 30 (× 3 ÷ 30 upgraded), which is 1 per 15 (10) Gold. Copies add up exactly. |
| Great Wall | Builds the current height, so Build bonuses like Hard Hat apply once. |
| Law and Order | Deports after the end-of-turn damage (Big Beautiful Wall, Guard Towers), so those hits can put enemies under the line first. |
| Demolition, Wrecking Ball | If the hit ends the fight, the Wall isn't halved (the fight is over anyway). |
| Executive Order | The first Skill each turn costs 0. Two copies make the first two free. |
| Very Stable Genius | Upgrades last for the combat only, like Armaments. |
| Hole in One | Unplayable once any card has been played that turn (it glows when it's playable). |

## Tests

New test mode: `python scripts/test.py cards`. Options: `cards SEED relics` (relics, potions and powers only, ~1 min), `cards SEED A,B` (chosen cards).

| Check | Result |
|---|---|
| **Every card played**, base and upgraded (178 plays) in real fights, choice screens answered by the game's own bot selector | All played, **no errors**. The per-card log (energy, Gold, Wall, Block, hand, enemy HP, powers) matched the card text for every card; highlights below. |
| Key effects checked with numbers (about 40 cards) | Pass after 4 test fixes (below) |
| **All text rendered**: every card (library, in hand, upgraded), every tooltip, every power on screen, every relic and potion | No missing variables, no errors |
| Relics: Hard Hat, Long Red Tie, Late-Night Phone, Gold-Plated Toilet, Gold-Plated Bricks, Rubber Stamp | All pass |
| Potions: Quick-Dry Concrete, Covfefe, Deportation Draught | All pass |
| Turn powers over a real turn | All pass:<br>• Reinforced Concrete: 3 Sections gave 15 Block.<br>• Guard Towers dealt 6 damage.<br>• Rebar + Infrastructure Week + Make the Spire Great Again + Golden Shovel built exactly 16.<br>• Social Media Intern + 3 AM Posting + MSGA added exactly 4 Tweets.<br>• Protectionism put a Tariff on every enemy.<br>• Ratings and Verified Account work on a Tweet.<br>• Merch Stand paid +15 Gold. |
| Ancient dialogue | All 8 Ancients have The Donald's dialogues, and every line renders |
| `test.py ui` (Step 4 mechanics) | **PASS**, 0 log problems |
| `test.py autoslay`: full run with the whole card pool | **Victory** in 4 min 5 s, 0 mod log problems. Met Pael and Nonupeipe, whose new dialogue played without errors |

Highlights from the card log:
- Border Control took the Deport line from 25% to 35%, then 50% with the upgrade. Border Wall with 10 Sections took it to 70%, then to the 100% cap.
- Chapter 11 turned 300 Gold into 75 Block (100 upgraded).
- Great Wall doubled 30 → 60 → 120.
- Wall Street paid 40 Gold for 10 Sections (60 upgraded).
- Hold the Line gave 36 Block at Wall 73.
- Net Worth hit for 35 at 300 Gold.
- You're Fired! removed a full-HP enemy for 99 Gold (75 upgraded).
- Golf Weekend ended the turn and paid off the next one.
- Leveraged Buyout paid 40 Gold on a kill.
- So Much Winning gave Energy and cards when enemies fell.

Test fixes during the step (none were game bugs):
- God mode gives the player 999,999,999 Strength, which one-shot every target. The card test now heals instead.
- Trade War and Low Energy hit ALL enemies, so the check now looks at every enemy.
- Tweetstorm's check now allows for the 10-card hand limit.
- The Tweet token can't be added from the console, so the test creates it directly.

## Not done / next

- **Balance** is Step 6: all numbers are the design's first pass. The watch list is in [`design.md` §9](../design/design.md).
- The full one-at-a-time Deport sweep over all 80 encounters is still optional (Step 4 checked 6 linked fights).
- Art for all of it is Steps 7–8.
