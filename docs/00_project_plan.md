# sts2-pres-mod: Project Plan and Status

**Start here.** This is the master plan for the mod, kept up to date after every step.
If a chat session is lost, this file + [`README.md`](../README.md) + [`FRAMEWORK.md`](FRAMEWORK.md) are enough to carry on.
For a new character, follow [`ADDING_A_CHARACTER.md`](ADDING_A_CHARACTER.md).

_Last updated: 2026-09-24. Step 8.5 (multi-character framework and the rename to sts2-pres-mod) is done and not committed yet; it waits for the user's review. Step 9 waits for the go-ahead._

---

## The brief

Create a new playable character for Slay the Spire 2: a **funny, lighthearted caricature of Donald Trump**, themed around
**deportation and wall building**. It should be well implemented, carefully balanced and super fun to play, with a full card set,
art, UI elements and a special mechanic. **The art must match the base game's style and blend in.**
Any tools may be used, downloaded or created. The work is done step by step, never in one shot.

**Extended after Step 8:**
- The mod became **sts2-pres-mod**, a mod of playable presidents.
- **Joe Biden** comes later, reusing every technique and tool built for The Donald. Adding a character should be significantly faster than the first one was.

## Working agreement

- **One step at a time.** At the end of each step, report the deliverables and **stop until the user says "continue"**.
- The user reviews designs on the published **[Compendium page](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak)**, often from their phone through Remote Control.
  Whenever the design changes, republish it: run `python scripts/render_compendium.py -c <id>`, then publish `characters/<id>/design/compendium.html`.
- Downloads: list the tool, source and size, and ask before downloading.
- Git: commit only when the user asks. End commits with the `Co-Authored-By: Claude` trailer.
- **Never touch real saves:**
  - Tests run in the separate `modded_prestest` save folder, and balance runs in `modded_bal<slot>`.
  - Test games never write the global `settings.save`.
  - Back up saves before anything risky.
- **Tests:** prefer targeted ones (a few representative cases); ask before runs of 30+ minutes. Balance batches run 9 games at a time, tiled 3×3 on the main monitor and muted.
- **Tone:** jokes target the **persona** only. Mechanics such as deportation and walls apply to Spire monsters. No jokes about immigrants, ethnic or religious groups, real tragedies, or real people other than the character himself.

## The steps

Steps 1–8 built The Donald together with the tools. Step 8.5 made those tools work for any character. Steps 9–11 are for the whole mod.
A new character follows the phases C1–C7 of [`ADDING_A_CHARACTER.md`](ADDING_A_CHARACTER.md), which replace Steps 2–8 for it.

| # | Step | Status | Deliverable |
|---|---|---|---|
| 1 | Research and feasibility | ✅ Done | [`01_feasibility_report.md`](01_feasibility_report.md) |
| 2 | Test character and build process (+ installer/uninstaller) | ✅ Done (`6f331c9`) | [`02_step2_report.md`](02_step2_report.md) |
| 3 | Design document (The Donald) | ✅ Done (v2) | [`design.md`](../characters/trump/design/design.md), [`card_list.md`](../characters/trump/design/card_list.md), [`cards.json`](../characters/trump/design/cards.json), [Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak) |
| 4 | Core mechanics and starter set (placeholder art) | ✅ Done (`0586dce`, shovel redesign `670e337`) | [`04_step4_report.md`](../characters/trump/docs/04_step4_report.md) |
| 5 | All the content (placeholder art) | ✅ Done (`bed48ed`) | [`05_step5_report.md`](../characters/trump/docs/05_step5_report.md) |
| 6 | Balance and playtesting | ✅ First pass (`6964d20`) | [`06_step6_report.md`](../characters/trump/docs/06_step6_report.md) |
| 7 | Lock the art style | ✅ Done (`328beee`) | [`07_step7_report.md`](../characters/trump/docs/07_step7_report.md), boards in [`art/`](../characters/trump/docs/art/) |
| 7.5 | Art review tool | ✅ Done (`328beee`) | `python scripts/art_review.py`, see [`ART_PIPELINE.md`](ART_PIPELINE.md) §4 |
| 8 | Make all the art | ✅ Done (`99fba6b`) | [`step8_ingame.png`](../characters/trump/docs/art/step8_ingame.png); updating art: [`ART_PIPELINE.md`](ART_PIPELINE.md) §5 |
| 8.5 | Multi-character framework, rename to sts2-pres-mod | ✅ Done (not committed) | [`FRAMEWORK.md`](FRAMEWORK.md), [`ADDING_A_CHARACTER.md`](ADDING_A_CHARACTER.md), [`ART_PIPELINE.md`](ART_PIPELINE.md), `scripts/new_character.py` |
| 9 | Polish and packaging | ⏳ Next | Installable release |
| 10 | Extra balance testing (optional) | ⬜ | More measured tuning, if wanted |
| 11 | Custom style LoRA art (optional) | ⬜ | Art regenerated with a LoRA trained on the game's art, if wanted |

**Characters:**

| Character | Status |
|---|---|
| The Donald (`trump`) | Complete (Steps 3–8) |
| Joe Biden (`biden`) | Planned: after Step 9 or whenever the user asks, starting with phase C1 |

### Step 1: Research and feasibility ✅
Set up the workspace and tools (ilspycmd, GDRE Tools, Godot 4.5.1 .NET, Pillow). Decompile the current build and unpack the resource pack.
Trace how a character is connected through the whole game, list every art asset needed, and write the feasibility report and risks.

### Step 2: Test character and build process ✅
- C# mod project, one-command build (`scripts/build.py`), installer and uninstaller (`scripts/dist/`).
- The uninstaller cleans up saves that use the mod, through the game itself, so the Steam Cloud copies go too.
- Test character with placeholder art. Compatibility patches P1–P7.
- In-game test harness: a UI walkthrough and full auto-play runs.
- Result: **a full winning run as The Donald**, with 0 log errors.

### Step 3: Design document ✅ (v2)
- Mined the 5 base characters for balance numbers.
- Designed 88 cards plus the Tweet token, 9 relics and 3 potions, against those benchmarks and the reward and shop rules.
- v2 after review:
  - The Wall is a construction project.
  - Deport uses a 25% HP line.
  - Every play style can win on its own and scales without limit.
  - More card flow that doesn't cost gold.

### Step 4: Core mechanics and starter set (placeholder art) ✅
Result: everything below is built and tested. See the [report](../characters/trump/docs/04_step4_report.md).
- `test.py ui` runs 24 checks.
- The Deport sweep covered all 80 encounters, plus a one-enemy-at-a-time run on 6 linked fights.
- An AutoSlay full run won.
- Permanent Structure (Rare relic) was built early as the first user of the carry-over hook.

- **Wall system:**
  - height, Sections, and end-of-turn Section Block;
  - stages at 10/25/45/70 whose perks survive Demolition;
  - the on-screen Wall display (stage, height, Sections, progress to the next stage);
  - hooks for height carried between fights (Permanent Structure, Cornerstone).
- **Deport system:**
  - a Deport line per player (25% base, with modifiers);
  - removal through the game's escape system, with bosses immune;
  - the line marker and DENIED stamp on enemy HP bars;
  - **a test that Deports every enemy in every encounter** to find scripted fights that break.
- **Tariff** (debuff) and **Pay X Gold** (playability check plus a gold-cost badge on cards).
- **Tweet** token (Token rarity).
- Keyword tooltips: Build, Wall, Section, Deport, Tariff, Pay Gold, Tweet.
- **Starter kit:** Strike, Defend, Build the Wall, Deport, the Golden Shovel, and the Touch of Orobas → Diamond Shovel mapping.
- The real character stats (70 HP, 99 gold) and the gold card frame, checked side by side with the Regent's orange.

### Step 5: All the content (placeholder art) ✅
Result: all playable. See the [report](../characters/trump/docs/05_step5_report.md).
- 88 cards, 26 powers, 9 relics, 3 potions and Ancient dialogue.
- `test.py cards` plays every card base and upgraded, and checks the relics, potions and turn powers.
- An AutoSlay run with the full pool won.

### Step 6: Balance and playtesting ✅ (first pass)
Result: a balance bot (`test.py balance`) plays The Donald, Ironclad and Silent on the same seeds, 9 games at once. See the [report](../characters/trump/docs/06_step6_report.md).
- **Where he sits:** inside the base-game range, level with Silent and a bit below Ironclad.
  - He has the best defense in elite and boss fights.
  - Short fights are slower.
- **Tuned:** Deport 6 → 7 damage, and the Deport line capped at 60%.
- The user decided this is enough balance for now; more goes in the optional Step 10.

### Step 7: Lock the art style ✅
- **The method:** Krea 2 Turbo + image style reference in a headless ComfyUI. The game's own art goes in as the reference images.
- **Test pieces**, checked in game: Build the Wall, Deport, Net Worth, the Golden Shovel and the character select button.
- The style guide, the recipe per asset type and the method comparison are in the [report](../characters/trump/docs/07_step7_report.md).
- The trained-LoRA method moved to the optional Step 11.

### Step 7.5: Art review tool ✅
Added by the user after Step 7: one page to review every piece of art, keep it or regenerate it, with live updates.
- `python scripts/art_review.py` lists everything a character needs: 152 items for The Donald.
- **Per item:** keep, regenerate (several at once), versions, prompt editing, quality presets and delete.
- Since Step 8.5 it handles every character, with a selector at the top. Full guide: [ART_PIPELINE.md](ART_PIPELINE.md).

### Step 8: Make all the art ✅
Result: the user generated and kept all 152 items in the review tool, and everything is wired into the game (`99fba6b`).
- **Tests:** `test.py ui` passes with the real art: the combat poses, all 4 Wall stages, the stamp, the shop, the rest site and character select.
- **Combat:** `scenes/creature_visuals/trump.tscn` is a sprite. `NCharacterPoses` (Framework) swaps the idle, attack, cast and hurt paintings on the game's animation triggers.
  - Motion tweens on top: lunge, hop, flinch, breathing and death.
  - No Spine rig.
- **Shop and rest site:** sprites placed by their feet at runtime.
- **Character select:** a full-screen painting under ember particles recoloured to the character's colour.
- **Energy orb, trail and transition:** ours, recoloured copies of the game's VFX.
- **Wall and Deport stamp:** the 4 stage paintings (a cap plus a repeated strip) and the stamp image with a stamp slam.
- **Game VFX reused:** dust on Build, rubble on stage-up and Demolition, and themed hit effects on 14 attack cards.

### Step 8.5: Multi-character framework, rename to sts2-pres-mod ✅
Added by the user after Step 8: make a second character (Joe Biden, later) significantly faster to add, reusing every technique used for The Donald.
- **Rename:**
  - repo `sts2-pres-mod`, mod id `pres_mod`, `PresMod.csproj`, namespace `PresMod`;
  - test flags `--pres-*` and the save folder `modded_prestest`.
  - The installer removes the old `mods/trump_character` (`mod.json` `legacy_ids`).
- **Code split:**
  - `Framework/` holds everything shared: patches, sprite bodies (`NCharacterPoses`, `ArtPatches`), `CharacterArt`, `IModCharacter`, and the starter upgrade (`IUpgradableStarterRelic`).
  - `Characters/Trump/` holds his own content and mechanics.
  - `Dev/` is the harness, with per-character hooks (`CharacterTests`, `[CharacterTestKit("ID")]`).
- **Data split:**
  - `characters/<id>/` holds `character.json`, the design, the art list and the localization. The build merges every character's tables.
  - `scripts/presmod.py` is the shared config for every script.
  - Every script takes `-c <id>`.
- **Art tools:**
  - The recipes read the character's `character.json` (persona, references, backgrounds, tint, sleeve). Output paths come from the art kind or the item.
  - The review tool has a character selector and a workspace per character. The existing state was migrated, and the 124 stored prompts are identical after the refactor.
- **Scaffold:**
  - `scripts/new_character.py` creates a playable character from `characters/_template/` in about a minute: class, pools, Strike and Defend, 5 stub cards, a stub starter, all 9 text tables, recoloured scenes and placeholders.
  - The build checks every character has what the game loads by name.
  - `test.py cleansaves` removes a deleted character's test runs.
- **Verified:**
  - A throwaway character from the scaffold built, booted and passed `test.py ui` (select, run, poses, shop, rest site).
  - It was then removed.
  - The Donald passes `test.py ui` and `test.py cards ... relics` after the refactor.
- **Docs:** [FRAMEWORK.md](FRAMEWORK.md), [ADDING_A_CHARACTER.md](ADDING_A_CHARACTER.md), [ART_PIPELINE.md](ART_PIPELINE.md) and a new README.

### Step 9: Polish and packaging
- A co-op check (may need a second player), a test with other mods installed, and a full crash and QA pass, for every character.
- **The PCK is 71 MB**, mostly the full-size paintings. Try lossy compression for the select screen and figures.
- Note the supported game version; write install instructions and release notes.
- Check Steam Workshop and Nexus rules on real-person and political content before any public release.

### Step 10: Extra balance testing (optional)
Added by the user during Step 6: balance is good enough to move on, and more testing can come at the end.
- **Style-focused runs** check the design principle that every play style wins on its own. `test.py balance TRUMP 18 PREFIX 9 fullheal favor=Wall` (or Deport, Deals, Tweets) makes the bot favor that style's cards.
- **Bigger baselines** (27+ runs per character) for tighter numbers.
- **A smarter bot**, if needed. For example, it doesn't plan around the Wall stages or card synergies.
- Tune from the user's own playtests.

### Step 11: Custom style LoRA art (optional)
Added by the user during Step 7: the art is made with Krea 2 + style reference for now. Later, a LoRA trained on the game's own art can replace it if it looks better.
- **Base model:** train on **Krea 2 Raw** (the undistilled base) and run the LoRA on Turbo at 8 steps. Training on Turbo without the de-distill adapter breaks its 8-step speed.
  - The download is `krea2_raw_fp8_scaled.safetensors` (13.1 GB) from `huggingface.co/Comfy-Org/Krea-2`, which is ungated. Ask before downloading.
  - bf16 Raw (26.3 GB) doesn't fit in 24 GB and trains about 2–3× slower.
- **Ready to use:**
  - `scripts/art_train.py` (captioning plus ComfyUI's built-in trainer, no extra tools);
  - a captioned set of 72 game images (52 cards, 20 relics) in `build/art/comfy_in/sts2_style`, rebuilt by the snippet in the [Step 7 report](../characters/trump/docs/07_step7_report.md).
- **Measured on the 4090:**
  - about 2.4–2.6 s/step with an fp8 base, gradient checkpointing depth 2 and bypass mode, so ~65 min for 1,500 steps;
  - depth 1 overflows the 24 GB of VRAM and stalls.
- Then regenerate a few cards with the LoRA, compare them with the Step 8 art, and swap in the better set.

## Decision log

| Date | Decision |
|---|---|
| 2026-09-23 | Start fresh from the current build (v0.107.1). The old April `sts2_decompiled_code` and `sts2_extracted` folders in the game directory are ignored. |
| 2026-09-23 | Use the game's **official mod loader** (`mods/<id>/manifest.json` + DLL + PCK) and Harmony. Mod id `trump_character` (renamed `pres_mod` in Step 8.5), character class `Trump` (ID `TRUMP`). |
| 2026-09-23 | The user asked for an **uninstaller** too. It uses the game's own save system to delete saves that use the mod, so Steam Cloud copies go as well. |
| 2026-09-23 | Steps 1–2 committed (`6f331c9`). |
| 2026-09-23 | Design v1 published. **Confirmed:** name "The Donald", metallic gold card colour, 70 HP, elites Deportable and bosses immune, and edgier names kept (Mass Deportation, Witch Hunt, Fake News, Fire and Fury, Chapter 11). |
| 2026-09-23 | **Design v2** after review. The Wall no longer soaks damage; it's a construction project with Sections and stages, because it was too close to Necrobinder's Osty. Deport uses a 25% HP line, because depending on the Wall forced a split build. More energy and draw that doesn't cost gold. |
| 2026-09-23 | Design principles from the user: **each play style must win on its own; scaling must have no ceiling** (like Osty's HP); card flow must not depend on gold. The user loves the Deals/gold play style. |
| 2026-09-23 | Relic and potion counts checked against the game at full unlock: 8 character relics + the starter upgrade, and 3 potions, per character. The large numbers are shared pools. |
| 2026-09-23 | Step 4: Deported enemies leave through the game's **escape** system (no gold, no on-death effects). Boss immunity = primary enemies in boss rooms; their minions can be Deported. The DENIED stamp marks an enemy that is Deportable right now. |
| 2026-09-23 | Step 4 committed (`0586dce`). **Golden Shovel redesigned** after review: a flat Build 6 at the start of combat only moved the stage thresholds. It is now "at the start of your turn, Build 2" (the construction crew); the Diamond Shovel builds 4 per turn. Chosen over three other options: compound growth per Section, draw on stage-up, and Gold per Section. |
| 2026-09-23 | Step 5 committed (`bed48ed`). Step 6: balance bot runs, 9 games in parallel, tiled 3×3 and muted (the user's request). First tuning pass: Deport 7 (10) damage, Deport line cap 60%. The user called balance good enough for now and added an optional Step 10 for more testing. |
| 2026-09-23 | Step 5: where the design text left room, choices are listed in the [Step 5 report](../characters/trump/docs/05_step5_report.md). Examples: Guard Towers fires one shot per Section, Law and Order Deports after end-of-turn damage, and You're Fired! and the Deportation Draught can't target immune bosses. |
| 2026-09-23 | Step 6 committed (`6964d20`). Step 7: the user asked to try two methods, training a style LoRA and Krea 2 with image style reference. The user chose **Krea 2 Turbo + style reference** (ComfyUI, the game's own art as reference images). The LoRA route moved to the optional Step 11, to be trained on Krea 2 Raw rather than Turbo. |
| 2026-09-23 | Step 4: mod models get `[SavedProperty]` support (`SavedPropertiesTypeCache.InjectTypeIntoCache` at startup), so run-long growth (Permanent Structure, Cornerstone) survives save and quit. |
| 2026-09-23 | Steps 7 + 7.5 committed (`328beee`), Step 8 committed (`99fba6b`). Art quality: presets added after the first batch (style-reference strength 0.75 instead of 1.0 was the main fix). |
| 2026-09-24 | **Step 8.5:** the mod becomes **sts2-pres-mod** (id `pres_mod`), a mod of playable presidents; Joe Biden comes later. **One mod with several characters**, not one mod per character. The game reads one set of text tables per mod, so each character keeps its own tables and the build merges them. The test flags became `--pres-*`, and the installer removes the old `trump_character` folder. |
| 2026-09-24 | A new character's id must be its class name in snake_case (the game derives every asset path from the class). The scaffold adds 5 stub cards so rewards and shops work before the real cards exist. Touch of Orobas became generic (`IUpgradableStarterRelic`). |

## Open items

- P8 (stats screen section) and P9 (own unlock timeline) are optional (see the Step 1 report).
- Steam Cloud also syncs the test save folders (`modded_prestest`). Files deleted by hand come back; use `test.py cleansaves` (it goes through the game).
- `.claude/launch.json` in the game folder serves `characters/` on port 8765 for local page previews (e.g. `/trump/design/gallery.html`).
- If the game suddenly runs at ~8 fps in tests (the shared preload in `godot.log` takes ~30 s instead of ~2 s), the PC needs a restart. It isn't the mod (checked on 2026-09-23).
- The full one-at-a-time Deport sweep over all 80 encounters (~45 min) hasn't been run. Targeted lists are the default: `test.py deportsweep SEED A,B,C`.

## Resuming in a new session

1. Open `C:\Users\exeet\sts2-pres-mod`. Read this file, [`README.md`](../README.md) (layout and commands) and [`FRAMEWORK.md`](FRAMEWORK.md). For a character's design, read `characters/<id>/design/design.md`.
2. Claude's memory for this project is in `C:\Users\exeet\.claude\projects\C--Program-Files--x86--Steam-steamapps-common-Slay-the-Spire-2\memory\`. It holds the plan summary and the working rules.
3. Check that the game version is still **v0.107.1** (`release_info.json` in the game folder). If it changed, re-decompile (`re/`) and diff before building.
4. `python scripts/build.py --install`, then `python scripts/test.py ui -c trump` to confirm everything still works.
5. Carry on with the next ⏳ step above, and stop at its end.
