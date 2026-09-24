# The Donald: Project Plan and Status

**Start here.** This is the master plan for the Slay the Spire 2 character mod, kept up to date after every step.
If a chat session is lost, this file + [`README.md`](../README.md) + [`03_design.md`](03_design.md) are enough to carry on.

_Last updated: 2026-09-23. Step 8 (all the art) is done and not committed yet; it waits for the user's review. Step 9 waits for the go-ahead._

---

## The brief

Create a new playable character for Slay the Spire 2: a **funny, lighthearted caricature of Donald Trump**, themed around
**deportation and wall building**. It should be well implemented, carefully balanced and super fun to play, with a full card set,
art, UI elements and a special mechanic. **The art must match the base game's style and blend in.**
Any tools may be used, downloaded or created. The work is done step by step, never in one shot.

## Working agreement

- **One step at a time.** At the end of each step, report the deliverables and **stop until the user says "continue"**.
- The user reviews designs on the published **[Compendium page](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak)**, often from their phone through Remote Control.
  Republish it (`python scripts/render_compendium.py`, then publish `docs/design/compendium.html`) whenever the design changes.
- Downloads: list the tool, source and size, and ask before downloading.
- Git: commit only when the user asks. End commits with the `Co-Authored-By: Claude` trailer.
- Never touch real saves: tests run in the separate `modded_trumptest` save folder; back up saves before anything risky.
- Tone: jokes target the **persona** only. Deportation and walls apply to Spire monsters. No jokes about immigrants, ethnic or religious groups, real tragedies, or real people other than him.

## The steps

| # | Step | Status | Deliverable |
|---|---|---|---|
| 1 | Research and feasibility | ✅ Done | [`01_feasibility_report.md`](01_feasibility_report.md) |
| 2 | Test character and build process (+ installer/uninstaller) | ✅ Done (`6f331c9`) | [`02_step2_report.md`](02_step2_report.md) |
| 3 | Design document | ✅ Done (v2) | [`03_design.md`](03_design.md), [`design/card_list.md`](design/card_list.md), [`design/cards.json`](design/cards.json), [Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak) |
| 4 | Core mechanics and starter set (placeholder art) | ✅ Done (`0586dce`, shovel redesign `670e337`) | [`04_step4_report.md`](04_step4_report.md) |
| 5 | All the content (placeholder art) | ✅ Done (`bed48ed`) | [`05_step5_report.md`](05_step5_report.md) |
| 6 | Balance and playtesting | ✅ First pass (`6964d20`) | [`06_step6_report.md`](06_step6_report.md) |
| 7 | Lock the art style | ✅ Done (`328beee`) | [`07_step7_report.md`](07_step7_report.md), boards in [`art/`](art/) |
| 7.5 | Art review tool | ✅ Done (`328beee`) | `python scripts/art_review.py`, see [`07_step7_report.md`](07_step7_report.md) §6 |
| 8 | Make all the art | ✅ Done (not committed) | [`art/step8_ingame.png`](art/step8_ingame.png); updating art: README "Updating art later" |
| 9 | Polish and packaging | ⏳ Next | Installable release |
| 10 | Extra balance testing (optional) | ⬜ | More measured tuning, if wanted |
| 11 | Custom style LoRA art (optional) | ⬜ | Art regenerated with a LoRA trained on the game's art, if wanted |

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
Result: everything below is built and tested. `test.py ui` runs 24 checks. The Deport sweep covered all 80 encounters, plus a one-enemy-at-a-time run on 6 linked fights. An AutoSlay full run won. See the [report](04_step4_report.md).
Permanent Structure (Rare relic) was built early as the first user of the carry-over hook.

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
- Replace the Step 2 test cards (Deport prototype, fillers) as the real cards arrive; the filler cards stay until Step 5 fills the pool.
- **Deliverable:** a playable Act 1 with the starter deck, where you can feel the Wall and Deport, plus automated tests and screenshots.

### Step 5: All the content (placeholder art) ✅
Result: 88 cards, 26 powers, 9 relics, 3 potions and Ancient dialogue, all playable. `test.py cards` plays every card base and upgraded and checks the relics, potions and turn powers; an AutoSlay run with the full pool won. See the [report](05_step5_report.md).

- Build on the Step 4 systems: new cards and relics plug into `Mechanics/Hooks.cs`, `WallCmd`, `DeportCmd` and `GoldCmd`.
  Saved values (Cornerstone's growth) use `[SavedProperty]`, already registered for mod types.
- Replace the 13 remaining filler cards.
- Every card with its upgrades, all relics and potions, one power per Power card, and all the text.
- Ancient dialogue for The Donald (Neow, Darv and the others; optional but good for comedy).
- Automated tests that play every card through the developer console.
- **Deliverable:** a complete character, playable start to finish.

### Step 6: Balance and playtesting ✅ (first pass)
Result: a balance bot (`test.py balance`) plays The Donald, Ironclad and Silent on the same seeds, 9 games at once. The Donald sits inside the base-game range (level with Silent, a bit below Ironclad), with the best defense in elite and boss fights and slower short fights. Tuned: Deport 6 → 7 damage, Deport line capped at 60%. The user decided this is enough balance for now; more goes in the optional Step 10. See the [report](06_step6_report.md).

- A smarter auto-play bot: the game's own bot picks cards at random, so write a heuristic player that uses the real game logic.
- Batches of automated runs compared with the base characters: win rate, which cards get picked and how they perform.
- The user playtests, and the numbers are tuned; repeat as needed.
- Risks to measure are listed in [`03_design.md` §9](03_design.md).
- **Deliverable:** a tuned version plus a balance report.

### Step 7: Lock the art style ✅
Result:
- **The method:** Krea 2 Turbo + image style reference in a headless ComfyUI. The game's own art goes in as the reference images.
- **Tools:** `scripts/art_gen.py` generates, `scripts/art_post.py` produces the game-ready files.
- **Test pieces**, now in the mod and checked in game: Build the Wall, Deport, Net Worth, the Golden Shovel and the character select button.
- The style guide, the recipe per asset type and the method comparison are in the [report](07_step7_report.md).
- The trained-LoRA method moved to the optional Step 11.

- Study the game's art: palette, brushwork, lighting and framing.
- Try both methods the user suggested: a style LoRA trained on the game's art, and Krea 2 with image style reference.
- Make test pieces (3 card arts, the character portrait, 1 relic icon) for the user to approve before mass production.
- The art direction for each card is in `cards.json` (`art` field).

### Step 7.5: Art review tool ✅
Added by the user after Step 7: one page to review every piece of art, keep it or regenerate it, with live updates.
- `python scripts/art_review.py` opens a local web page (starts the headless ComfyUI itself).
- It lists **everything the mod needs**, 152 items:
  - 89 card portraits (2 of them Ancient full-art);
  - 9 relics, 3 potions and 26 power icons;
  - the character items: select button, select screen, top-bar icon, map marker, 4 combat poses, the shop and rest-site poses, 4 co-op hands, and the screen transition;
  - the UI items: 3 energy orb pieces, the energy and gold icons, 4 Wall stages and the DENIED stamp.
- **Per item:** Keep (copies the game-ready files into `mod/`), Regenerate (several at once, ×1–4 each), switch between versions, edit the prompt, cancel. Card art shows inside its real frame.
- The recipes (references, prompts, sizes, post-processing, output paths) are in `scripts/art_recipes.py`. What to draw is in `cards.json` (cards) and `docs/design/art_assets.json` (everything else).

### Step 8: Make all the art ✅
Result: the user generated and kept all 152 items in the review tool, and everything is wired into the game.
- `test.py ui` passes with the real art: the combat poses, all 4 Wall stages, the stamp, the shop, the rest site and character select.
- **Quality presets:** the tool gained presets (Standard/High fix the muddy first batch) and a delete option; see the [Step 7 report](07_step7_report.md) §7.
- **Updating art later:** Keep in the tool, then `build.py --install` (README, "Updating art later").
- **Second review pass:** the user redid the shop pose and all 4 Wall stages; in-game check passed (`build/test/ui_20260923_230300`). More re-rolls are optional.

How it's wired:
- **Combat:** `scenes/creature_visuals/trump.tscn` is a sprite. `NTrumpPoses` swaps the idle, attack, cast and hurt paintings on the game's animation triggers (`ArtPatches`), with lunge, hop, flinch, breathing and death tweens. This is the plain-sprite route; no Spine rig.
- **Shop and rest site:** sprites placed by feet at runtime (`ArtPatches`). The shop's Spine calls are skipped, and the rest site flips for co-op seats.
- **Character select:** a full-screen painting under the kept ember particles, recoloured gold.
- **Energy orb and trail:** the orb uses our 5 layers plus gold copies of the energy VFX; the card trail and transition mask are ours.
- **Wall:** drawn from the 4 stage paintings (a cap plus a repeated strip).
- **Deport stamp:** the stamp image, plus a stamp slam on Deport.
- **Game VFX reused:** dust on Build, rubble on stage-up and Demolition, themed hit effects on 14 attack cards.
- **Outlines:** relic and potion hover outlines are derived at build time.
- **Stand-ins:** `make_placeholders.py` writes stand-ins only for art paths nothing has filled yet.
- **Tests:** `test.py ui` now also checks the poses and screenshots the shop and rest site.
- Use the Step 7 recipe (see the [report](07_step7_report.md) §3 and §5): 3 seeds per piece, pick the best, and batch cards by archetype with fixed references.
- All card art, the character select screen, relic, potion and power icons, the energy orb, and the map, shop and rest-site art.
- **The four Wall stage visuals** and the Deport stamp.
- Generate and review everything in the Step 7.5 tool, then wire the new files in:
  - the character select screen (a static painting instead of the copied Ironclad scene);
  - the energy orb layers;
  - the transition material;
  - the combat, shop and rest-site figures;
  - the Wall stages and the stamp.
- **VFX** (Godot scenes, not image generation):
  - the card trail and energy orb glow recoloured gold;
  - a dust-and-bricks puff on Build;
  - the stamp slam and poof on Deport.
  All built from the game's own particle textures.
- The combat character as a Spine rig, generated with code (idle, attack, cast, hurt, die); a plain-sprite fallback if needed.
- Sounds: reuse the game's own.
- **Deliverable:** the finished visual version.

### Step 9: Polish and packaging
- Co-op check (may need a second player), a test with other mods installed, and a full crash and QA pass.
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
  The download is `krea2_raw_fp8_scaled.safetensors` (13.1 GB) from `huggingface.co/Comfy-Org/Krea-2`, which is ungated. Ask before downloading. bf16 Raw (26.3 GB) doesn't fit in 24 GB and trains about 2–3× slower.
- **Ready to use:**
  - `scripts/art_train.py` (captioning plus ComfyUI's built-in trainer, no extra tools);
  - a captioned set of 72 game images (52 cards, 20 relics) in `build/art/comfy_in/sts2_style`, rebuilt by the snippet in the [Step 7 report](07_step7_report.md).
- **Measured on the 4090:**
  - about 2.4–2.6 s/step with an fp8 base, gradient checkpointing depth 2 and bypass mode, so ~65 min for 1,500 steps;
  - depth 1 overflows the 24 GB of VRAM and stalls.
- Then regenerate a few cards with the LoRA, compare them with the Step 8 art, and swap in the better set.

## Decision log

| Date | Decision |
|---|---|
| 2026-09-23 | Start fresh from the current build (v0.107.1). The old April `sts2_decompiled_code` and `sts2_extracted` folders in the game directory are ignored. |
| 2026-09-23 | Use the game's **official mod loader** (`mods/<id>/manifest.json` + DLL + PCK) and Harmony. Mod id `trump_character`, character class `Trump` (ID `TRUMP`). |
| 2026-09-23 | The user asked for an **uninstaller** too. It uses the game's own save system to delete saves that use the mod, so Steam Cloud copies go as well. |
| 2026-09-23 | Steps 1–2 committed (`6f331c9`). |
| 2026-09-23 | Design v1 published. **Confirmed:** name "The Donald", metallic gold card colour, 70 HP, elites Deportable and bosses immune, and edgier names kept (Mass Deportation, Witch Hunt, Fake News, Fire and Fury, Chapter 11). |
| 2026-09-23 | **Design v2** after review. The Wall no longer soaks damage; it's a construction project with Sections and stages, because it was too close to Necrobinder's Osty. Deport uses a 25% HP line, because depending on the Wall forced a split build. More energy and draw that doesn't cost gold. |
| 2026-09-23 | Design principles from the user: **each play style must win on its own; scaling must have no ceiling** (like Osty's HP); card flow must not depend on gold. The user loves the Deals/gold play style. |
| 2026-09-23 | Relic and potion counts checked against the game at full unlock: 8 character relics + the starter upgrade, and 3 potions, per character. The large numbers are shared pools. |
| 2026-09-23 | Step 4: Deported enemies leave through the game's **escape** system (no gold, no on-death effects). Boss immunity = primary enemies in boss rooms; their minions can be Deported. The DENIED stamp marks an enemy that is Deportable right now. |
| 2026-09-23 | Step 4 committed (`0586dce`). **Golden Shovel redesigned** after review: a flat Build 6 at the start of combat only moved the stage thresholds. It is now "at the start of your turn, Build 2" (the construction crew); the Diamond Shovel builds 4 per turn. Chosen over three other options: compound growth per Section, draw on stage-up, and Gold per Section. |
| 2026-09-23 | Step 5 committed (`bed48ed`). Step 6: balance bot runs, 9 games in parallel, tiled 3×3 and muted (the user's request). First tuning pass: Deport 7 (10) damage, Deport line cap 60%. The user called balance good enough for now and added an optional Step 10 for more testing. |
| 2026-09-23 | Step 5: where the design text left room, choices are listed in the [Step 5 report](05_step5_report.md). Examples: Guard Towers fires one shot per Section, Law and Order Deports after end-of-turn damage, and You're Fired! and the Deportation Draught can't target immune bosses. |
| 2026-09-23 | Step 6 committed (`6964d20`). Step 7: the user asked to try two methods, training a style LoRA and Krea 2 with image style reference. The user chose **Krea 2 Turbo + style reference** (ComfyUI, the game's own art as reference images). The LoRA route moved to the optional Step 11, to be trained on Krea 2 Raw rather than Turbo. |
| 2026-09-23 | Step 4: mod models get `[SavedProperty]` support (`SavedPropertiesTypeCache.InjectTypeIntoCache` at startup), so run-long growth (Permanent Structure, Cornerstone) survives save and quit. |

## Open items

- P8 (stats screen section) and P9 (own unlock timeline) are optional (see the Step 1 report).
- Watch whether Steam Cloud keeps small `modded_trumptest` test files (`prefs`, `progress`); they're harmless.
- `.claude/launch.json` in the game folder serves `docs/design` on port 8765 for local page previews.
- If the game suddenly runs at ~8 fps in tests (the shared preload in `godot.log` takes ~30 s instead of ~2 s), the PC needs a restart. It isn't the mod (checked on 2026-09-23).
- The full one-at-a-time Deport sweep over all 80 encounters (~45 min) hasn't been run. Targeted lists are the default: `test.py deportsweep SEED A,B,C`.

## Resuming in a new session

1. Open `C:\Users\exeet\sts2-trump-mod`. Read this file, [`README.md`](../README.md) (layout and commands) and [`03_design.md`](03_design.md).
2. Claude's memory for this project is in `C:\Users\exeet\.claude\projects\C--Program-Files--x86--Steam-steamapps-common-Slay-the-Spire-2\memory\`. It holds the plan summary and the "stop after each step" rule.
3. Check that the game version is still **v0.107.1** (`release_info.json` in the game folder). If it changed, re-decompile (`re/`) and diff before building.
4. `python scripts/build.py --install`, then `python scripts/test.py ui` to confirm everything still works.
5. Carry on with the next ⏳ step above, and stop at its end.
