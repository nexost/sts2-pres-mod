# Guide for AI agents working on sts2-pres-mod

This repo is a Slay the Spire 2 mod (id `pres_mod`) that adds playable, lighthearted president caricatures. The Donald is done; more are planned. It also holds every tool and document used to build it, so a new character or a game update can be handled step by step with a person reviewing each step.

## Read first

1. [README.md](README.md): layout and commands.
2. [docs/00_project_plan.md](docs/00_project_plan.md): the steps so far, status, decision log, open items.
3. Then the page for the task:

| Task | Read |
|---|---|
| Set up a new PC (tools, `re/`, paths, first build and test, ComfyUI) | [docs/SETUP.md](docs/SETUP.md) |
| Understand how the mod works (shared vs. character code, patches, scenes, build, tests, scripts) | [docs/FRAMEWORK.md](docs/FRAMEWORK.md) |
| Add a new character | [docs/ADDING_A_CHARACTER.md](docs/ADDING_A_CHARACTER.md) (phases C1–C7) |
| Make or change art (ComfyUI, models, the review tool) | [docs/ART_PIPELINE.md](docs/ART_PIPELINE.md) |
| The game updated | [docs/GAME_UPDATES.md](docs/GAME_UPDATES.md) |
| Release a version, where it may be published | [docs/PUBLISHING.md](docs/PUBLISHING.md), [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md) |
| Why something is the way it is | The step reports: `docs/01`, `02`, `09` and `characters/trump/docs/04`–`07` |
| A character's design and rules | `characters/<id>/design/design.md` and `cards.json` (the source of truth) |

## How the work is run

- **One step at a time.** Work in steps (the plan's steps, or the phases of ADDING_A_CHARACTER.md). At the end of each step, report what was delivered, update `docs/00_project_plan.md`, and stop for the person's review. Don't one-shot a whole character.
- **Ask first:**
  - before downloading anything (say the tool, the source and the size);
  - before test runs over 30 minutes;
  - before anything public (pushes to a public repo, releases, Workshop uploads).
- **Commit only when asked.** Before committing, check `git config user.name` and `git config user.email` are the identity the person wants public (see SETUP.md §6).
- **Saves:** never touch the person's real saves.
  - `test.py` runs the game with its own save folders (`modded_prestest`, `modded_bal*`), and test runs never write the global `settings.save`.
  - Steam Cloud syncs test folders too, so remove test runs with `test.py cleansaves`, not by deleting files.
- **Tests:** prefer targeted tests (`test.py ui`, `cards SEED relics`, `cards SEED A,B`) over long sweeps.
  - Balance batches run 9 games at once, tiled 3×3 on the main monitor and muted (`test.py balance ... 9`).
  - Only one normal test instance runs at a time; `build.py --install` can't replace the DLL while the game runs.
- **Tone** (a project rule): jokes target the persona only. Mechanics like walls and deportation apply to Spire monsters. No jokes about immigrants, ethnic or religious groups, real tragedies, or real people other than the character.

## Commands you'll use most

```
python scripts/build.py --install                 # build everything and install into the game
python scripts/test.py ui -c <id>                 # the main in-game check (~3 min)
python scripts/test.py cards PRESTEST1 -c <id>    # every card, relic, potion, all text (~12 min)
python scripts/new_character.py <id> --class <Class> --name "<Name>" --primary <hex>   # scaffold a character
python scripts/art_review.py -c <id>              # art: generate, review, keep (starts ComfyUI)
python scripts/extract_game.py; python scripts/game_update_check.py   # after a game update
```

## Things that already cost time once

The full list is in ADDING_A_CHARACTER.md, "Pitfalls already paid for".
- **Class names are model IDs.** A character's id must be its class name in snake_case, and class names must be unique across the base game and every character (hence `StrikeTrump`).
- **Namespace clash:** inside `PresMod.Characters.<Class>`, write `<Class>.X`, not `Characters.<Class>.X`.
- **Card pools:** a pool with only Basic cards breaks rewards and the shop; the scaffold's stub cards prevent it.
- **Co-op:** harness actions in co-op must go through the synced action queue: `TryManualPlay`, and `EndPlayerTurnAction` via `ActionQueueSynchronizer`.
- **Shell heredocs** can turn `\\n` into real newlines. Write code files with a file-editing tool or a script file.
- **Build before testing:** `test.py` tests the *installed* mod.
- **Leave generated files alone:** `card_list.md`, `gallery.html` and `compendium.html` are generated from `cards.json`.
- **Game files never go in git:** `re/` holds the decompiled game (Mega Crit's code and assets) and is git-ignored. Only small scenes copied and adapted for our characters are in `mod/`.
