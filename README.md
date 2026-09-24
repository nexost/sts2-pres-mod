# sts2-pres-mod: playable presidents for Slay the Spire 2

One mod (id `pres_mod`), several lighthearted caricature characters. Targets StS2 **v0.107.1** (Godot 4.5.1 .NET, .NET 9).

| Character | Id | Status | Folder |
|---|---|---|---|
| **The Donald**: builds walls, makes deals, deports the Spire's riff-raff | `trump` | Complete: 89 cards, 9 relics, 3 potions, full art | [`characters/trump/`](characters/trump/) |
| Joe Biden | `biden` | Planned | made with `scripts/new_character.py` |

**Start with [`docs/00_project_plan.md`](docs/00_project_plan.md)**: the plan, the status, the decisions and how to resume.

## Documents

| File | What |
|---|---|
| [`docs/00_project_plan.md`](docs/00_project_plan.md) | Master plan: steps, status, decision log, open items, how to resume |
| [`docs/FRAMEWORK.md`](docs/FRAMEWORK.md) | How the mod is built: shared code vs. character code, patches, scenes, build, tests, scripts |
| [`docs/ADDING_A_CHARACTER.md`](docs/ADDING_A_CHARACTER.md) | **The playbook for a new character**, from the scaffold to the final checks, with the pitfalls already hit |
| [`docs/ART_PIPELINE.md`](docs/ART_PIPELINE.md) | Making art: ComfyUI setup, what each character provides, recipes and destinations per kind, the review tool |
| [`docs/01_feasibility_report.md`](docs/01_feasibility_report.md), [`docs/02_step2_report.md`](docs/02_step2_report.md) | Steps 1–2: how the game works, the patches it needs, build, installer and uninstaller |
| [`docs/reference/`](docs/reference/) | Base-game data: balance benchmarks from the 5 characters, Ironclad's asset list |
| [`characters/trump/design/design.md`](characters/trump/design/design.md) | The Donald's design (v2): mechanics, play styles, relics, balance |
| [`characters/trump/design/cards.json`](characters/trump/design/cards.json) | **Source of truth** for The Donald's cards, relics and potions |
| [`characters/trump/docs/`](characters/trump/docs/) | The Donald's Step 4–7 reports and art boards |

## Layout

| Path | What |
|---|---|
| `mod.json` | Mod id, name, version, description, old ids the installer removes |
| `characters/<id>/` | One character: `character.json`, `design/`, `art/art_assets.json`, `localization/eng/`, `docs/` |
| `characters/_template/` | What `new_character.py` fills in for a new character |
| `mod/` | Godot asset project **and** C# project (`PresMod.csproj`); paths mirror the game's `res://` |
| `mod/pres_mod/src/` | Code: `Framework/` (shared by all characters), `Dev/` (tests, save cleanup), `Characters/<Class>/` (one folder each) |
| `mod/images`, `mod/scenes`, `mod/materials` | Assets at the exact paths the game loads for each character id |
| `scripts/` | Build, test, design, art and packaging tools; `presmod.py` is their shared config; `dist/` holds the installer and uninstaller |
| `re/`, `tools/`, `build/`, `backups/` | Decompiled game and unpacked PCK; Godot and GDRE; output; save backups (all git-ignored) |

## Commands

Scripts that work on one character take `-c <id>`; without it they use the first one (`trump`).

```bash
# New character (docs/ADDING_A_CHARACTER.md)
python scripts/new_character.py biden --class Biden --name "Uncle Joe" --full-name "Joe Biden" --primary 2F6BD8

# Build
python scripts/build.py              # build into build/dist/ (placeholders for missing art, character and ID checks)
python scripts/build.py --install    # build + install into the game

# Tests (the game runs in a test mode; saves go to modded_prestest/, never your real saves)
python scripts/test.py ui -c trump                 # walkthrough + the character's mechanics, with screenshots (~3 min)
python scripts/test.py cards PRESTEST1 -c trump    # every card base and upgraded, relics, potions, all text (~15 min)
python scripts/test.py cards PRESTEST1 relics      # just the relic and potion checks (~1 min)
python scripts/test.py autoslay SEED -c trump      # the game's bot plays a full run (god mode)
python scripts/test.py deportsweep SEED A,B        # Trump's own mode: Deport enemies one at a time in the listed encounters
python scripts/test.py balance TRUMP,IRONCLAD,SILENT 9 PREFIX 9 fullheal   # balance bot, 9 games at once, tiled + muted
python scripts/balance_report.py PREFIX            # compare balance batches
python scripts/test.py cleansaves [ID]             # clear test runs of a removed character (goes through the game for Steam Cloud)

# Design
python scripts/analyze_cards.py                    # mine base-game cards into build/analysis/ (the benchmarks)
python scripts/render_design.py -c trump           # cards.json -> card_list.md + validation
python scripts/render_gallery.py -c trump          # cards.json -> gallery.html
python scripts/render_compendium.py -c trump       # cards.json -> compendium.html (then republish)

# Art (docs/ART_PIPELINE.md)
python scripts/art_review.py [-c trump]            # review page for every character's art: keep / regenerate live (starts ComfyUI)
python scripts/make_placeholders.py [-c trump]     # stand-ins for missing art (the build runs it)
```

**Updating art:**
1. Keep the new version in the review tool; it writes into `mod/`.
2. Run `python scripts/build.py --install` and restart the game.
3. Check with `test.py ui`.

Details: [ART_PIPELINE.md](docs/ART_PIPELINE.md) §5.

**For players:**
- Install with `build/dist/install.cmd`. It also removes the old `trump_character` version.
- Remove with `build/dist/uninstall.cmd`. It also cleans saves that use the mod; options are in `scripts/dist/README.txt`.
