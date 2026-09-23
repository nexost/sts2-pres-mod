# The Donald: Slay the Spire 2 character mod

Workspace for the mod. Targets STS2 **v0.107.1** (Godot 4.5.1 .NET, .NET 9).

**Start with [`docs/00_project_plan.md`](docs/00_project_plan.md)**: the 9-step plan, current status, decision log and how to resume.

## Documents

| File | What |
|---|---|
| [`docs/00_project_plan.md`](docs/00_project_plan.md) | Master plan, step status, decisions, open items, how to resume |
| [`docs/01_feasibility_report.md`](docs/01_feasibility_report.md) | Step 1: how the game works, the patches it needs, the asset list, risks |
| [`docs/02_step2_report.md`](docs/02_step2_report.md) | Step 2: build pipeline, installer and uninstaller, tests, findings |
| [`docs/03_design.md`](docs/03_design.md) | Step 3: the character design (v2): mechanics, play styles, relics, balance |
| [`docs/04_step4_report.md`](docs/04_step4_report.md) | Step 4: Wall, Deport, Tariff, Pay Gold and Tweet systems, starter kit, tests |
| [`docs/design/cards.json`](docs/design/cards.json) | **Source of truth** for every card, relic and potion (text, numbers, art direction) |
| [`docs/design/card_list.md`](docs/design/card_list.md) | Card tables and validation, generated from `cards.json` |
| [`docs/design/base_game_benchmarks.md`](docs/design/base_game_benchmarks.md) | Balance numbers mined from the 5 base characters |
| [`docs/design/compendium.html`](docs/design/compendium.html) | The review page, published as the [Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak) |
| [`docs/design/gallery.html`](docs/design/gallery.html) | Local card gallery (open in a browser) |

## Layout

| Path | What |
|---|---|
| `mod/` | Godot asset project **and** C# project (`TrumpMod.csproj`). Paths inside mirror the game's `res://` layout |
| `mod/trump_character/src/` | Mod code: `ModEntry.cs` (initializer), `Mechanics/` (Wall, Deport, Gold commands and mod hooks), `Models/` (character, pools, cards, relics, powers), `Nodes/` (combat UI), `Patches/` (Harmony), `Dev/` (test harness, save cleanup) |
| `mod/trump_character/localization/eng/` | Text, merged into the game's tables |
| `mod/images`, `mod/scenes`, `mod/materials` | Assets at the exact paths the game loads for character `TRUMP` |
| `scripts/` | Build, test, design and packaging scripts (below); `dist/` holds the installer and uninstaller; `templates/` holds the Compendium page |
| `docs/` | Plan, step reports and design |
| `re/` | Decompiled game code (`re/code`) and recovered Godot project (`re/pck`); git-ignored |
| `tools/` | GDRE Tools, Godot 4.5.1 .NET; git-ignored |
| `build/` | Output (git-ignored): `dist/` mod + installer, `test/` test runs, `analysis/` base-game card data |
| `backups/` | Save backups taken before testing; git-ignored |

## Commands

```bash
python scripts/build.py              # build into build/dist/
python scripts/build.py --install    # build + install into the game
python scripts/test.py ui            # scripted walkthrough + every Step 4 mechanic, with screenshots (~3 min)
python scripts/test.py deportsweep   # fights all 80 encounters and Deports every enemy (bosses killed) (~30 min)
python scripts/test.py autoslay SEED # the game's AutoSlay bot plays a full run as our character (god mode)

python scripts/analyze_cards.py      # mine base-game cards into build/analysis/ (benchmarks, per-character lists)
python scripts/render_design.py      # cards.json -> docs/design/card_list.md + validation report
python scripts/render_gallery.py     # cards.json -> docs/design/gallery.html
python scripts/render_compendium.py  # cards.json -> docs/design/compendium.html (then republish the Compendium)
```

Test output lands in `build/test/<mode>_<time>/` (report.json, screenshots, godot.log).
Test runs use save folder `modded_trumptest/`, never your real saves.

Install for players: run `build/dist/install.cmd`. Remove: `build/dist/uninstall.cmd` (options in `scripts/dist/README.txt`).

## How the mod hooks in

* The game's `ModManager` loads `mods/trump_character/{manifest.json, trump_character.dll, trump_character.pck}` and calls `ModEntry.Initialize`.
* All `AbstractModel` subclasses in the DLL are registered automatically; IDs come from class names (`StrikeTrump` → `CARD.STRIKE_TRUMP`).
  The build and the mod both refuse class names that clash with base game models.
* Harmony patches (`Patches/`); numbers match the Step 1 report:
  * P1 `CharacterRegistrationPatch`: adds our character to the hardcoded `ModelDb.AllCharacters`.
  * P2 and P6 `EpochCheckPatch`: skips timeline unlock checks that throw for unknown characters (crash after elites, bosses and act bosses).
  * P3 `CardLibraryTabPatch`: adds our card library tab (the library crashed mid-run otherwise).
  * P4 `CharacterSfxPatch`: points attack/cast/death sounds at existing FMOD events.
  * P5 `ModEntry`: registers our Godot node scripts (`ScriptManagerBridge.LookupScriptsInAssembly`).
  * P7 `ArchitectDialoguePatch`: gives our character dialogue in the final Architect event (it had none, so a win couldn't finish).
  * `AtlasFallbackPatch`: lets `ui_atlas` sprites fall back to loose PNGs under `res://trump_character/atlas_fallback/`.
  * `TouchOfOrobasPatch`: the Ancient's starter-relic upgrade maps Golden Shovel to Diamond Shovel.
  * `PayGoldBadgePatch`: Pay-Gold cards show their gold cost in the star-cost badge, with a coin icon.
  * `CombatUiPatch`: adds `Nodes/NTrumpCombatUi` to every combat room (Wall display, Deport line and stamp, DEPORTED! popup).
* `ModEntry` also registers our `[SavedProperty]` members with the save system (the game only knows its own types),
  so relic counters and permanently grown cards survive save and quit.
* Mechanics go through `Mechanics/WallCmd`, `DeportCmd` and `GoldCmd`; cards and relics plug in through the interfaces in `Mechanics/Hooks.cs`
  (`IBuildModifier`, `ISectionBlockModifier`, `IDeportLineModifier`, `IAfterDeport`, `IAfterPayGold`, `IAfterWallStage`).
* `Dev/DevHarness.cs` only runs with `--trump-test`; `Dev/SaveCleanup.cs` only with `--trump-cleanup` (used by the uninstaller).
