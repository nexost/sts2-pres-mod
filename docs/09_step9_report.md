# Step 9 report: polish and packaging

_2026-09-24. Game v0.107.1. Mod v1.0.0. Released on GitHub: https://github.com/nexost/sts2-pres-mod/releases/tag/v1.0.0 (not on the Steam Workshop)._

Board: [`art/step9_board.jpg`](art/step9_board.jpg) (co-op with two Donalds, co-op with Ironclad, the co-op rest site, and the full bot run).

## 1. Co-op, tested without a second player

The game has a developer option for testing co-op on one PC: `--fastmp host_standard` opens a lobby and `--fastmp join --clientId N` joins it over localhost, with no Steam lobby. `python scripts/test.py coop` uses it.

1. It launches a host and a client side by side, muted, each with its own log and test save folder.
2. Both pick their character and ready up, and the host starts a fight. In co-op the dev console commands are networked.
3. **Each side plays its own cards** through the synced action queue:
   - The Donald's `CoopTurn` hook has the host play **Coalition Wall** (ALL players Build 12).
   - The client plays **Trickle Down** (ALL players gain 12 Gold) and **Slap a Tariff**.
   - Both then play their Strikes and Defends and end their turn.
4. At the start of each of 3 turns, **both instances record the whole game state**:
   - every player's HP, Block, Gold, hand, pile sizes and powers, plus the character's own counters (Wall height, Deport line);
   - every enemy.
   - `test.py` compares the two records. Any difference is a desync.
5. Then both go to the rest site and the shop, and check that the character paintings are there.

| Party | Result |
|---|---|
| The Donald (host) + The Donald (client) | **PASS**: all card checks on both sides, 3 turns identical, 0 desync or mod lines in either log |
| The Donald (host) + Ironclad (client) | **PASS**: Coalition Wall gave Ironclad a Wall (0 → 12) and the party re-spaced; 3 turns identical, 0 desync or mod lines |

**Found and fixed along the way:**
- **Walls stood on the other player** (your screenshot).
  - The game lines players up from the center outward, 70 px apart. A Wall stands 26 px in front of its owner and is 150 px wide, so the rear player's Wall covered the front player.
  - `WallSpacingPatch` adds room after every player who shows a Wall, the way the game itself shifts the local Necrobinder to fit Osty.
  - When someone who isn't a Donald gets a Wall mid-fight from Coalition Wall, the party glides to its new places.
- **The first co-op run hung at end of turn** (your "looks like it's stuck"). The harness ended turns with a local-only command. It now sends the networked `EndPlayerTurnAction`, like the End Turn button. The bug was in the test harness, not the mod.
- **"Both instances control the same character?"** They don't: each window lists its own player first ("Test Host" vs "1001"). It looked the same because both played The Donald. The card checks confirm two separate players: each Coalition Wall and Trickle Down landed on both.

## 2. Size: 71 MB → 17 MB

Card portraits were 89 lossless images of ~0.7 MB each.
- `build.py` now imports the big paintings as lossy WebP at quality 0.9 (`IMPORT_OVERRIDES`): the card portraits, the select-screen painting and the transition mask.
- Side-by-side close-ups show no visible difference, and the in-game card library looks the same.
- Figures stay lossless, because lossy colour under their transparent pixels would bleed into the edges. Icons stay lossless because they're small and need crisp edges.

## 3. QA pass on the final build

| Test | Result |
|---|---|
| `test.py ui -c trump` | PASS (character select, run, card library, poses, all Wall stages, Deport, Pay Gold, Tweets, Touch of Orobas, shop, rest site) |
| `test.py cards PRESTEST1 -c trump` | PASS: all 89 cards base and upgraded (178 plays), 132 effect, relic and potion checks, 0 log problems (11.5 min) |
| `test.py autoslay PRESREL1 -c trump` | **Victory**: all 3 acts, the boss and the Architect, 3 min 41 s, 0 log lines about the mod |
| `test.py coop` ×2 | PASS (above) |
| Release zip on a clean `mods/` | Unzipped, `install.cmd` → installed v1.0.0; `test.py ui` PASS on that install |
| `uninstall.cmd -DryRun` | Found the one run in your own modded history that uses the mod, listed what it would remove, changed nothing |

The logs also show `InputMap action "controller_..." doesn't exist` lines. They come from the game (they're in yesterday's logs too) and have nothing to do with the mod.

## 4. Packaging

- **Version 1.0.0** (`mod.json`); `min_game_version` 0.107.1.
- `python scripts/build.py --zip` produces:
  - `build/release/sts2-pres-mod-v1.0.0.zip` (16.8 MB): one folder with `install.cmd`, `uninstall.cmd`, `README.txt` and `pres_mod/`;
  - `build/release/workshop_preview.png` (1280×720, 663 KB).
- **Player README:** the version and game version are stamped in by the build, with Windows line endings. It covers the characters, requirements, install (automatic or manual), co-op, uninstall options, and where the log is.
- **[RELEASE_NOTES.md](RELEASE_NOTES.md):** what v1.0.0 contains, co-op, requirements and known limitations:
  - sounds borrowed from the Ironclad;
  - painted poses instead of Spine;
  - no unlock timeline of his own;
  - not tested with other mods.

## 5. Where it can be published ([PUBLISHING.md](PUBLISHING.md))

- **Nexus Mods: no.**
  - Nexus has banned "mods relating to sociopolitical issues in the United States" since September 2020, extended indefinitely.
  - They enforced it on Trump and Biden mods for Marvel Rivals in January 2025.
  - A presidents mod would be removed however lighthearted.
- **Steam Workshop: yes, under Steam's content rules** (no hateful or harassing content).
  - It has been the official StS2 channel since v0.107.1; Mega Crit publishes `ModUploader.exe` for it.
  - Visibility can be public, friends-only or private.
  - Everything the uploader needs is ready except the tool itself: downloading it and publishing are your call.
- **Direct zip / GitHub release:** ready now (`build/release/`).

Sources: [Nexus file submission guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines), [Nexus ban announcement](https://www.nexusmods.com/news/14373), [PCGamesN on the 2025 removals](https://www.pcgamesn.com/marvel-rivals/trump-biden-mod), [Mega Crit's mod uploader](https://github.com/megacrit/sts2-mod-uploader).

## 6. Deferred

- **Testing with other mods** (you asked to leave it for later). The most useful target is BaseLib, the shared library most StS2 content mods depend on, plus one popular character mod.
- Worth adding at that point: a startup check that names both mods when another mod defines a class with the same model ID as ours. The game silently keeps whichever loads last.
