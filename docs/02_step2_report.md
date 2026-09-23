# Step 2: Test Character and Build Process

**Status: done** (committed as `6f331c9`). A test character, "The Donald", is fully wired into the game and built by a single command.
It installs and uninstalls through scripts, and automated tests play it inside the real game.

## What exists now

| Piece | Where | Notes |
|---|---|---|
| Mod code (C#, ~1,150 lines) | `mod/trump_character/src/` | Character, 3 pools, 18 cards, 1 relic, 5 compatibility patches, test harness, save cleanup |
| Assets | `mod/images`, `mod/scenes`, `mod/materials` | Generated placeholders + copies of Ironclad's scenes (still using Ironclad's animations) |
| Text | `mod/trump_character/localization/eng/` | characters, cards, relics, card library |
| Build | `python scripts/build.py [--install]` | placeholders → ID check → C# → Godot import → PCK → `build/dist/` |
| Installer | `build/dist/install.cmd` | Finds the game through Steam, refuses to run while the game is open, replaces older versions |
| Uninstaller | `build/dist/uninstall.cmd` | Cleans up saves that use the mod (local **and** Steam Cloud), then removes the mod; backups kept |
| Tests | `python scripts/test.py ui` / `autoslay` | Runs in the real game in a separate save folder; screenshots + report + log scan |

## How the build works

1. `make_placeholders.py` creates any missing placeholder image or scene at the exact paths the game loads for `TRUMP`
   (card portraits are generated automatically for every card class).
2. **Model ID check**: lists every ID the mod adds and **fails the build** if a class name would override a base-game model.
   The mod repeats this check at runtime against the real game.
3. `dotnet build` against the game's own `sts2.dll` / `0Harmony.dll` (nothing from the game is copied).
4. Godot 4.5.1 headless import of the textures.
5. `pack.gd` (Godot's own `PCKPacker`) packs an **explicit file list**, so no project settings or cache files that could clash with the game go into the pack.
6. `build/dist/` = `trump_character/{manifest.json, trump_character.dll, trump_character.pck, mod_ids.txt}` + installer + uninstaller + README.

## Findings during this step

* **The game treats every `.json` in a mod folder as a manifest.** Our folder has exactly one; the ID list is `.txt`.
* **Modded play uses separate saves** (`steam/<id>/modded/profileN`); normal saves are never touched.
* **Steam Cloud re-downloads any modded save that is missing locally** (it also overwrites on any timestamp difference).
  So an uninstaller that just deletes files can't work. Ours runs the game once in `--trump-cleanup` mode, and the mod
  deletes the affected run saves through the game's save store, which removes the local and cloud copies together.
* **Card rewards and shops need a real card pool.** With only Basic cards, the first card reward threw
  `couldn't generate a valid rarity`. The shop needs 2 Attacks, 2 Skills and 1 Power, and boss rewards need 3 Rares.
  15 filler cards cover this until the real set is in (Step 5).
* **Card library**: our tab works, and the in-run library opens on our pool (it crashed before the patch).
* The Godot "RID leaked at exit" lines in the log also appear in the unmodded game; the test runner ignores them.

### Two more game-breaking spots Step 1 missed (found by the full-run test, now patched)

The game builds these names from the character's ID as text, so a search for type checks didn't find them.

| # | Where | Symptom | Fix |
|---|---|---|---|
| P6 | `ProgressSaveManager.ObtainCharUnlockEpoch` | After each act boss the game unlocks `TRUMP2_EPOCH`… which doesn't exist → exception → **no rewards, run stuck** | Skip for mod characters (our own unlock timeline can come later) |
| P7 | `TheArchitect` (final event) | Dialogue exists only for the 5 base characters and, unlike other Ancients, has **no fallback** → the Proceed button crashes → **a winning run could never finish** | Patch adds our own Architect dialogues (3 short conversations in `ancients.json`) |

I then searched the whole code base for every place that builds names from a character ID. Nothing else can break; the rest are
text lookups we now provide (`aromaPrinciple`, `goldMonologue`, banter, `SEA_GLASS.TRUMP.title` = "Gilded Glass", Colorful Philosophers).

### Deport works on the real engine
* A weakened enemy hit by the prototype **leaves combat through the game's own escape path** (alive, recorded as escaped).
* **The fight ends on its own** when the last enemy leaves, and the rewards screen appears normally.
* **Deported enemies drop no gold** (the gold reward scales with the share of enemies killed rather than escaped).
  This is a built-in cost to design around in Step 3.

### Constraints for the Step 3 design (from the game's reward code)
| Rule | Why |
|---|---|
| ≥ 8 Common cards | "Room Full of Cheese → Gorge" offers 8 Commons at once |
| ≥ 5 cards of each rarity | Sea Glass (Orobas) shows 5 cards per rarity |
| ≥ 3 Rares | Boss rewards offer 3 Rares |
| Attacks and Skills at every rarity, Powers at Uncommon/Rare | The shop stocks 2 Attacks + 2 Skills + 1 Power from the character pool (base characters have no Common Powers; corrected in Step 3) |
| Basic cards never appear in rewards | Rewards roll Common / Uncommon / Rare only |

(Base characters have ~20 Common / ~36 Uncommon / ~26 Rare, so a real set passes easily.)

## Test results

| Test | Result |
|---|---|
| Build from clean (`build.py`) | ✅ 0 warnings, ID check lists 26 IDs, no collisions |
| Install (`install.cmd`) | ✅ finds the game through Steam's registry, replaces old versions |
| **UI walkthrough** (`test.py ui`) | ✅ main menu → character select (The Donald listed, stats + relic shown) → run start → **card library mid-run opens on our tab** → combat → **Deport check** → rewards. 0 log problems |
| **Full run** (`test.py autoslay`) | ✅ **Victory as The Donald**: all 3 acts, 50 rooms (fights, elites, bosses, shops, events, rest sites, treasure), Architect dialogue, back to the main menu in 4 min 16 s. 0 log problems (5 runs total; the first 4 found the pool-size, P6 and P7 issues above) |
| Uninstall dry run on **your real modded saves** | ✅ "No saves use this mod" (your April run isn't touched) |
| Uninstall on test saves | ✅ 7 saves (1 in-progress run + 6 history) removed **locally and from Steam Cloud** through the game, backups kept; mod folder removed; overlay mod untouched |
| Reinstall + UI test | ✅ pass |

Your real saves were backed up before any testing (`backups/saves_before_step2_*`), and every test ran in the separate `modded_trumptest` save folder.

## Known placeholders (replaced later)
* Combat body, shop and rest-site poses, character select background, energy orb, card trail: **Ironclad's** (Step 8).
* Card art, icons, map marker, relic: generated placeholders (Step 8).
* Cards: Strike / Defend / Deport prototype + 18 "Filler" cards (Step 5). Relic: placeholder heal (Step 4).
* Sounds: Ironclad's (Step 8 or kept).
* Card frame: gold hue shift. Gold was confirmed as the final colour in Step 3.
* The Deport prototype (fixed 12 HP threshold) is replaced by the Step 3 v2 rule: enemies at or below 25% of their max HP.
