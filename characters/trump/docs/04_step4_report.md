# Step 4 report: core mechanics and starter set

> **Paths changed after Step 8** (Step 8.5): the mod is now sts2-pres-mod (id `pres_mod`, repo `C:\Users\exeet\sts2-pres-mod`), the code is in `mod/pres_mod/src/` (`Framework/`, `Dev/`, `Characters/Trump/`), The Donald's text and design are in `characters/trump/`, and the test flags are `--pres-*` with saves in `modded_prestest`. This report keeps the names of its time; [FRAMEWORK.md](../../../docs/FRAMEWORK.md) maps the new layout.

_2026-09-23. Game v0.107.1. Mod v0.2.0. All art is still placeholder (Steps 7–8)._

## What you can play now

Start a run as The Donald: 70 HP, 99 Gold, the **Golden Shovel**, and a starter deck of 4 Strike, 4 Defend,
**Build the Wall** and **Deport**. Card rewards can offer 3 real commons (**Slap a Tariff**, **Mean Tweet**, **Small Loan**);
the other reward slots are still filler cards until Step 5.

In combat:
- A **Wall** stands next to The Donald, showing its height, its Sections, a progress bar to the next stage, and a banner when a stage is reached.
  It changes look at each stage: survey stakes, then chain-link fence, brick, concrete, and gold brick. These are placeholder drawings.
- Each enemy HP bar has a red **Deport line** at 25% of Max HP. A **DENIED** stamp appears when the enemy is at or under it.
  Deporting shows a **DEPORTED!** popup. Bosses show no line.
- **Pay-Gold cards** show their gold price on a coin in the card's star-cost badge. It turns red when you can't afford it.

## Systems built

| System | Where | Notes |
|---|---|---|
| Wall height and Sections | `Mechanics/WallCmd`, `WallRules`, `Models/Powers/WallPower` | One Section per 10 height. **Build** applies Build modifiers. **Raise** adds height without counting as a Build (carry-over). **LoseHeight** spends height (Demolition). |
| Wall stages and perks | `Models/Powers/WallStagePower` | Stages at 10 / 25 / 45 / 70. Perks: +1 draw, +1 Energy, and end-of-turn 1 damage to ALL enemies per Section. The stage never goes down, so perks survive Demolition. End of turn: 3 Block per Section. |
| Deport | `Mechanics/DeportCmd` | Line = 25% of Max HP, changed by `IDeportLineModifier` and clamped to 0–100%. Removal uses the game's escape system, so there's no gold and no on-death effects, and combat ends if nobody is left. Primary enemies in boss rooms are immune; their minions aren't. |
| Tariff | `Models/Powers/TariffPower` | Debuff: gain Gold equal to its amount whenever that enemy attacks. |
| Pay X Gold | `Mechanics/GoldCmd`, `Models/Cards/PayGoldCard` | The card can't be played (and glows red) without the gold. It pays first, then resolves. The price is a dynamic var, so upgrades can lower it. |
| Tweet token | `Models/Cards/Tweet` | 0 cost, 3 (5) damage to ALL enemies, Exhaust. Token rarity, but shown in our card pool. |
| Mod hooks | `Mechanics/Hooks.cs` | `IBuildModifier`, `ISectionBlockModifier`, `IDeportLineModifier`, `IAfterDeport`, `IAfterPayGold`, `IAfterWallStage`. Relics and powers implement these, and Step 5 cards and relics plug in here. |
| Saved values on mod models | `ModEntry.RegisterSavedProperties` | The game only saves `[SavedProperty]` members of its own types (a generated list). We add ours, so relic counters and permanently grown cards (Permanent Structure, Cornerstone) survive save and quit. The net-ID bit size is recomputed for co-op. |
| Keyword tooltips | `Models/TrumpHoverTips`, `static_hover_tips.json`, `powers.json` | Build, Section, Deport and Pay Gold, plus the Wall, Tariff and Tweet tips. |
| Combat UI | `Nodes/NTrumpCombatUi`, `Nodes/NWallDisplay`, `Patches/CombatUiPatch` | Added to every combat room. Does nothing in fights without a mod character. |
| Gold cost badge | `Patches/PayGoldBadgePatch` | Reuses the star-cost badge and swaps its icon for a coin. Other cards are restored, so nothing leaks into base-game cards. |
| Starter upgrade | `Patches/TouchOfOrobasPatch` | Touch of Orobas: Golden Shovel becomes Diamond Shovel. |

## Content added

| Kind | Name | Effect (base / upgraded) |
|---|---|---|
| Basic Skill | Build the Wall | 1 Energy: Build 5 (8). |
| Basic Attack | Deport | 1 Energy: deal 6 (9) damage, then Deport the target if it's at or under the line. |
| Common Attack | Slap a Tariff | 1 Energy: deal 8 (10) damage, apply 2 (3) Tariff. |
| Common Attack | Mean Tweet | 1 Energy: deal 6 (9) damage, add a Tweet to your hand. |
| Common Skill | Small Loan | 0 Energy: Pay 10 Gold, gain 1 Energy, draw 1 (2). |
| Token | Tweet | 0 Energy: deal 3 (5) damage to ALL enemies. Exhaust. |
| Starter relic | Golden Shovel | At the start of your turn, Build 2. (Redesigned after the step; see below.) |
| Ancient upgrade | Diamond Shovel | At the start of your turn, Build 4. |
| Rare relic | Permanent Structure | Keep a quarter of your Wall at the end of combat and start the next one with it. Built now as the first user of the carry-over hook. |

Removed: the Step 2 Deport prototype, the placeholder relic and 4 filler cards. **13 filler cards remain** so every reward rarity
has cards; Step 5 replaces them.

## Tests

| Test | Result |
|---|---|
| `test.py ui`: 24 checks in a real fight | **PASS**, 0 mod log problems (99 s) |
| `test.py deportsweep`, all 80 encounters, every enemy Deported at once | 79/80 fights ended cleanly, **122 enemies Deported**, 12 boss fights. The 1 failure was the test itself (next section). |
| `test.py deportsweep` on 6 encounters, one enemy at a time with a full enemy turn after each | **PASS**, 0 log problems: 13 Deported, 9 enemy turns played after a Deport |
| `test.py autoslay`: full run by the game's bot | **Victory**: all 3 acts and the ending, back to the main menu in 4 min 39 s, 0 mod log problems |

The UI test checks:
- the Golden Shovel builds 2 on turn 1 and 2 more each turn after, and Max HP is 70;
- Small Loan can't be played at 5 Gold, can at 99, pays 10 and gives 1 Energy;
- Mean Tweet adds a Tweet, and the Tweet hits all enemies;
- Slap a Tariff applies 2 Tariff, and Tariff pays 2 Gold when the enemy attacks;
- stages at 11, 25, 45 and 70: 7 Sections give 21 Block, and the stage-4 perk hits for 7;
- next turn there's 4 max Energy and a 6-card hand;
- Demolition to 0 keeps stage 4;
- the Deport line is right on both sides of 25%, and the Deport card deports and ends the fight;
- Permanent Structure keeps 10 of 40 and writes it to the save data. With the Diamond Shovel swapped in, the next fight's Wall starts at 14 (4 + 10);
- the boss is immune;
- Touch of Orobas gives the Diamond Shovel.

Screenshots are in `build/test/ui_<time>/shots/`.

**The one-at-a-time Deport test** covers the fights where enemies depend on each other:
- the Queen after her Torch Head minion is Deported;
- the Kin Priest without his followers;
- the Decimillipede's linked segments;
- the three Knights;
- the Turret Operator and Living Shield;
- the Lost and the Forgotten.

The enemies left kept acting normally, and every fight ended cleanly.
Run it on any list with `python scripts/test.py deportsweep SEED A,B,C`. The full one-at-a-time version over all 80 encounters takes about 45 minutes, so it wasn't run.

Bugs found and fixed during the step:
- The card library threw an error on Small Loan. Its canonical copy has no owner, so the affordability check now skips canonical cards.
- The Wall display said "1 SECTIONS", and the chain-link mesh spilled past the fence posts. Both are fixed.
- In the test harness:
  - The stage-4 damage check read the enemy HP before the damage landed.
  - The sweep killed immune bosses without asking the game to check for victory.
  - The Test Subject (a 3-form boss that revives) needs the game's `win` command, which strips powers first.
  - AutoSlay started before the main menu and shared assets were loaded.

**Slow PC, not the mod:** midway through the step the game started running at ~8 fps. The shared asset preload took 30 s instead of 2 s. The Step 3 build was just as slow (checked in a separate worktree), so the mod wasn't the cause, and a PC restart fixed it.
The AutoSlay harness now waits for the menu and preload and allows 3× the bot's time limits (test mode only). That way a slow machine doesn't fail the regression.

## Starter relic redesign (after review)

The first Golden Shovel built 6 once, at the start of each combat. The user pointed out that this is the same as moving every stage
threshold down by 6: no decision, and nothing that grows. It now builds 2 at the start of every turn, the construction crew that never stops.
The Wall rises on its own through a fight, stage-ups happen mid-fight even without Build cards, and longer fights mean a taller Wall.
The Diamond Shovel builds 4 per turn.
It uses the same timing as the Necrobinder's Bound Phylactery: turn 1 before combat starts, later turns right after the energy reset.
Re-tested with `test.py ui`: all checks pass.

## Card frame vs the Regent

Side by side in hand (`shots/008_frame_vs_regent.png`), The Donald's frame reads as a darker mustard gold and the Regent's as a bright orange.
They're clearly different. The final metallic look (sheen, lighter highlights) is part of the Step 7 art style.

## Known placeholders and open points

- Art: card portraits, relic and power icons, and the coin are generated placeholders. The Wall is drawn in code, and the combat model is still the Ironclad.
- The energy orb and card trail are the Ironclad's.
- The DENIED stamp and the Wall display are functional but plain. Step 8 gives them real art. Layout and data stay.
- When a fight is won, the Wall display falls back to showing "Chain-link Fence 10" for a moment (stages clear with the fight). It's cosmetic, and it goes away with the Step 8 display.
- Scripted encounters: no problems found. The full one-at-a-time sweep over all 80 encounters is still available if a later step changes how Deport works.
- The game logs controller-mapping warnings (`controller_d_pad_up` and others) when a controller is connected. They come from the base game's settings file, not the mod.
