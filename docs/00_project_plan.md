# sts2-pres-mod: Project Plan and Status

**Start here.** This is the master plan for the mod, kept up to date after every step.
If a chat session is lost, this file + [`README.md`](../README.md) + [`FRAMEWORK.md`](FRAMEWORK.md) are enough to carry on.
For a new character, follow [`ADDING_A_CHARACTER.md`](ADDING_A_CHARACTER.md).

_Last updated: 2026-09-25. **v1.1.0 is released** (Sleepy Joe, and visual effects for The Donald): https://github.com/nexost/sts2-pres-mod/releases/tag/v1.1.0. The repo is public at https://github.com/nexost/sts2-pres-mod. Not on the Steam Workshop ([PUBLISHING.md](PUBLISHING.md)).
Joe Biden is complete (C1–C7, [Sleepy Joe Compendium](https://claude.ai/artifact/KhC3qpBBpx2UDJAA3KCdyv)) and merged into `master`; the full release test set passed for both characters on 2026-09-25._

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
- Git: commit only when the user asks. End commits with the `Co-Authored-By: Claude` trailer. **Commits use the `nexost` identity with GitHub's noreply email** (set in this repo's local git config): never the global git identity, which holds the user's real name and work email.
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
| 2 | Test character and build process (+ installer/uninstaller) | ✅ Done (`c248d56`) | [`02_step2_report.md`](02_step2_report.md) |
| 3 | Design document (The Donald) | ✅ Done (v2) | [`design.md`](../characters/trump/design/design.md), [`card_list.md`](../characters/trump/design/card_list.md), [`cards.json`](../characters/trump/design/cards.json), [Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak) |
| 4 | Core mechanics and starter set (placeholder art) | ✅ Done (`5192aff`, shovel redesign `ae17284`) | [`04_step4_report.md`](../characters/trump/docs/04_step4_report.md) |
| 5 | All the content (placeholder art) | ✅ Done (`b6f2c2c`) | [`05_step5_report.md`](../characters/trump/docs/05_step5_report.md) |
| 6 | Balance and playtesting | ✅ First pass (`cbafe5c`) | [`06_step6_report.md`](../characters/trump/docs/06_step6_report.md) |
| 7 | Lock the art style | ✅ Done (`533f196`) | [`07_step7_report.md`](../characters/trump/docs/07_step7_report.md), boards in [`art/`](../characters/trump/docs/art/) |
| 7.5 | Art review tool | ✅ Done (`533f196`) | `python scripts/art_review.py`, see [`ART_PIPELINE.md`](ART_PIPELINE.md) §4 |
| 8 | Make all the art | ✅ Done (`63c91dc`) | [`step8_ingame.png`](../characters/trump/docs/art/step8_ingame.png); updating art: [`ART_PIPELINE.md`](ART_PIPELINE.md) §5 |
| 8.5 | Multi-character framework, rename to sts2-pres-mod | ✅ Done (`f27d1bc`) | [`FRAMEWORK.md`](FRAMEWORK.md), [`ADDING_A_CHARACTER.md`](ADDING_A_CHARACTER.md), [`ART_PIPELINE.md`](ART_PIPELINE.md), `scripts/new_character.py` |
| 9 | Polish and packaging | ✅ Done (`eca8ecb`) | v1.0.0: `build.py --zip`, [RELEASE_NOTES.md](RELEASE_NOTES.md), [PUBLISHING.md](PUBLISHING.md) |
| 10 | Extra balance testing (optional) | ⬜ | More measured tuning, if wanted |
| 11 | Custom style LoRA art (optional) | ⬜ | Art regenerated with a LoRA trained on the game's art, if wanted |
| 12 | Trailer (1:22.6, both characters) | ⏳ T7 final in review | Phases T1–T7 below; code in `trailer/`, media in `build/trailer/` |

**Characters:**

| Character | Status |
|---|---|
| The Donald (`trump`) | Complete (Steps 3–8) |
| Joe Biden (`biden`) | Complete (C1–C7), released in **v1.1.0** |

**Joe Biden's phases:**

| Phase | Status | Notes |
|---|---|---|
| C1 Scaffold | ✅ Done | "Uncle Joe", class `Biden`, 75 HP / 99 Gold (placeholders until C2), stub starter Aviator Shades (6 Block), Ironclad's sounds. `test.py ui -c biden` passes |
| C2 Design | ✅ v1 reviewed | [design.md](../characters/biden/design/design.md), [cards.json](../characters/biden/design/cards.json), [Compendium](https://claude.ai/artifact/KhC3qpBBpx2UDJAA3KCdyv). Sleepy Joe / Dark Brandon: **Drowsy** (he nods off at 10 and his turn ends; he wakes as Dark Brandon with Laser Eyes) and **Tangent** cards (only the lit line happens; as Dark Brandon, all of them). Styles: Dark Brandon, Power Nap, Tangents, General. 88 cards, 9 relics, 3 potions |
| C3 Mechanics and starter set | ✅ Done (`f818dfb`) | [Report](../characters/biden/docs/C3_mechanics_report.md). Drowsy, nodding off, Dark Brandon and Laser Eyes, Tangent cards (lit line in the card text); Catnap, Here's the Deal, Aviator Shades / Dark Aviators. `test.py ui -c biden` passes (31 checks) |
| C4 All content and text | ✅ Done (`0d273c7`) | [Report](../characters/biden/docs/C4_content_report.md). 88 cards, 24 powers, 9 relics, 3 potions, every text and Ancient line; stubs removed. `test.py cards` 257 checks PASS, AutoSlay **victory** |
| C5 Balance | ✅ Done (`2c1d7b3`) | [Report](../characters/biden/docs/C5_balance_report.md). The bot learned Tangent lines, his two forms, Doze and naps. One change: Aviator Shades' end-of-turn Doze 2 → **3**. Over two seed sets (35 runs each), the average floor is Sleepy Joe **28.1**, Ironclad 27.1, Silent 25.8. He naps in 82% of fights. `test.py ui` and the relic/potion/power checks pass |
| C6 Art | ✅ Done (`4712ed4`) | [Report](../characters/biden/docs/C6_art_report.md). All 147 items made and kept by the user: select screen (the '67 Corvette), poses for both forms, 88 cards (Joe on 24, his hand on 5, the rest objects, effects, monsters and scenes), relics, potions, powers, energy orb. Laser Eyes tinted red. `test.py ui` PASS |
| C7 Final checks | ✅ Done, in review | [Report](../characters/biden/docs/C7_final_report.md). ui, cards, autoslay (victory), co-op (two Sleepy Joes, and with an Ironclad: 0 desyncs) all PASS; The Donald's ui, cards, autoslay and co-op PASS. New co-op hook `CoopTurnStart` |

### Step 1: Research and feasibility ✅
Set up the workspace and tools (ilspycmd, GDRE Tools, Godot 4.5.1 .NET, Pillow). Decompile the current build and unpack the resource pack.
This part is now one command, `python scripts/extract_game.py`, rerun after every game update ([GAME_UPDATES.md](GAME_UPDATES.md)). Setting up a new PC: [SETUP.md](SETUP.md).
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
Result: the user generated and kept all 152 items in the review tool, and everything is wired into the game (`63c91dc`).
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

### Step 9: Polish and packaging ✅
Result: **v1.0.0**, a tested, installable release (`python scripts/build.py --zip` → `build/release/sts2-pres-mod-v1.0.0.zip`).
- **Co-op, tested solo:** the game's own developer option `--fastmp` connects two instances over localhost without Steam.
  - `test.py coop` runs a host and a client side by side. Both pick characters, fight 3 turns playing their own cards and co-op cards, and record every player and enemy at each turn start.
  - The two records must match, which catches desyncs. Then both visit the rest site and the shop.
  - **Two Donalds** and **Donald + Ironclad** both pass, with 3 turns identical on both sides and no desync or mod errors in either log.
  - **Found and fixed:** in co-op each Wall stood on the next player. `WallSpacingPatch` makes room in the game's party line-up, the way the game makes room for Osty, and re-spaces the party when Coalition Wall gives an ally a Wall mid-fight.
  - **Also fixed:** the harness ended turns locally; in co-op it now sends the networked end-turn action like the button does.
- **Size:** the PCK went from 71 MB to 17 MB.
  - Card portraits, select paintings and transition masks are now lossy WebP at 0.9, set by `build.py`. No visible difference.
  - Figures and icons stay lossless.
- **QA pass on the final build:**
  - `test.py ui` passes;
  - `test.py cards` passes: all 89 cards base and upgraded, 132 checks, 0 log problems;
  - `test.py autoslay` is a **victory** (all 3 acts and the Architect, 3 min 41 s, 0 log lines about the mod);
  - **the release zip**, unzipped and installed into a clean `mods/`, passes `test.py ui`;
  - `uninstall.cmd -DryRun` finds the one run in the user's own modded history that uses the mod and changes nothing.
- Report: [09_step9_report.md](09_step9_report.md).
- **Packaging:**
  - version 1.0.0 (`mod.json`);
  - the player README gets the version and game version stamped in, with co-op and troubleshooting notes;
  - [RELEASE_NOTES.md](RELEASE_NOTES.md);
  - `build.py --zip` makes the zip and a Workshop preview image.
- **Publishing rules** ([PUBLISHING.md](PUBLISHING.md)):
  - **Nexus Mods is out**: an indefinite ban on US sociopolitical mods since 2020, enforced on Trump and Biden mods in 2025.
  - **Steam Workshop is the official channel**, through Mega Crit's `ModUploader.exe`, under Steam's content rules.
  - Publishing needs the user's go-ahead (and the uploader download).
- **Deferred at the user's request:** testing with other mods (e.g. BaseLib, the common library most StS2 content mods use).

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

### Step 12: Trailer
Added by the user on 2026-09-25: a ~1 minute trailer showing both characters, epic and funny, edited like a big-studio game
trailer, that makes people want to download the mod. Any tools; paid APIs (voice, music) are fine if not too expensive, with the
user's go-ahead. One phase at a time, stopping for review after each.

| Phase | What | Status |
|---|---|---|
| T0 | Plan | ✅ Approved |
| T1 | Creative treatment: title, script, beat sheet, shot list, jokes (tone-checked), music brief, storyboard page | ✅ [Trailer treatment](https://claude.ai/artifact/KneX9Nm4JGDJe5YbGWiC78) v2 (source `trailer/treatment/`) |
| T2 | Toolchain and capture test: ffmpeg, Remotion, Godot Movie Maker (`--write-movie`) vs OBS on one scripted shot | ✅ In-game recorder chosen (below) |
| T3 | Audio backbone: music (MiniMax Music 3 locally), narrator casting and takes, SFX kit, the audio-only "radio cut" | ✅ Picks locked ([Trailer Sound Review](https://claude.ai/artifact/3gpByBPAY78JaGPY14Lzyp)); the timeline and mix are in `trailer/edit/src/timeline.json` |
| T4 | Director bot and gameplay capture: a `trailer` harness mode that stages each shot, all shots recorded | ✅ [Trailer Dailies](https://claude.ai/artifact/GgKYJuNYaZqb6tZvt1RzDr) (49 clips with game sound, 354 card images) |
| T5 | Cinematic shots and graphics: MiniMax H3 image-to-video on the mod's paintings, title logo, end card | ✅ [Trailer Cinematics](https://claude.ai/artifact/X3hkwFYv1nq8cAkwsBfB1E): five picks finished at 2520x1440, 60 fps (SeedVR2 + RIFE), Spectral title, backtoback s11 thumbnail art |
| T6 | Edit v1 in Remotion: cut to the radio cut, motion graphics, transitions, sound design, grade | ✅ [Trailer Edit](https://claude.ai/artifact/YJG1SAdnEcX38PMqPhnzNC) v2, 1:22.6 (v1 was 1:12.6; masters in `trailer/edit/out/`) |
| T7 | Notes, final mix and master, exports (16:9 master, optional vertical cut, thumbnail), `docs/TRAILER.md` | ⏳ In review: [Trailer Final](https://claude.ai/artifact/A11JbQLKhEhJEKrd12yLKy): YouTube 1440p60, 1080p60, 1080p60 with captions, .srt, thumbnail, mix; [TRAILER.md](TRAILER.md). Vertical cut not made (maybe later) |

Rules: AI video stays in the game's painted style (never photoreal), no imitation of the real people's voices, and the
project's tone rules apply to every line. Publishing the trailer anywhere is the user's call.

Set by the user (T0 review): a generic narrator voice, the characters speak in on-screen text only. For YouTube and Reddit,
mastered at 2560×1440 (a Shorts/TikTok cut maybe later). Downloads approved for T2: ffmpeg, Remotion, librosa.

**T1 decisions:** title *Presidents of the Spire*, narrator style A (epic, straight-faced), final joke = the Tweet plus Joe
cut off mid-word. The user added a **collection beat** (the mod's cards and relics shown off in 3D, with card callouts
during gameplay), so the runtime is 1:12.

**T2, the toolchain** (all local, nothing in git but code):
- `tools/ffmpeg/` (gyan.dev 9.0.2 essentials, NVENC); path setting `ffmpeg` in `presmod.py`.
- `trailer/edit/`: the Remotion project (4.0.529, `npm install` there). Media come from `build/trailer/media/`;
  `npx remotion render src/index.ts <Composition> out/<file>.mp4`. A 5.9 s 1440p60 test renders in 42 s.
- librosa (pip) for the music's beats.
- **Capture: `python scripts/trailer_capture.py SHOTS -c <id>`**, mode `trailer` in the harness
  (`Dev/DevHarness.Trailer.cs`, `Dev/TrailerRecorder.cs`, shots in `CharacterTests.TrailerShots`). The engine runs at
  `--fixed-fps 60` and the harness pipes each clip's frames to ffmpeg (H.264 4:4:4, NVENC): 4K, no dropped frames,
  ~37 fps capture speed, clips only (no startup footage). Waits inside shots use game time (`Wait`), never `Task.Delay`.
- Compared on the same shot: Movie Maker (`--write-movie`) works in the shipped game and matches frame for frame, but
  records the whole session (a 583 MB AVI per run) at ~20 fps. Real-time screen capture (ffmpeg ddagrab) sees only
  the desktop, not the game, on this PC (4090 + Intel iGPU displays).
- No capture method gets the game's sound: it is FMOD in real time, and the PC has no loopback device.

**T4, the footage** (masters in `build/trailer/capture/`, 4K 60 fps, H.264 4:4:4; ~2.6 GB):
- **Director code:** `Dev/DevHarness.Trailer.cs` (clips, takes, fights in any act, setup helpers; pre-run shots `menu_spire`
  and `select`; shared `gallery` and `cards`), `Dev/TrailerUi.cs` (clean looks through the game's own trailer-mode flags,
  version labels, mouse parked), `Characters/<Class>/Dev/DevHarness.<Class>.Trailer.cs` (the shots), `Dev/DevHarness.Trailer.Coop.cs`
  (co-op, mode `trailer_coop`). Every moment is taken twice from a fresh fight: `_full` (as played) and `_clean` (no interface).
- **Scenery** from the probe gallery: Glory's castle (the Wall, gold, knights, the Queen), Underdocks (Deport, Tweets, the laser
  on the docks), Hive (Mass Deportation, Fire and Fury, Laser Show, Motorcade), Overgrowth (Sleepy Joe, golf, the Vantom cave).
- **Co-op:** `trailer_capture.py coop full|clean`, one take per launch (a second take in the same run desynced the client).
  The host (The Donald) records full screen at a fixed 60 fps; the client (Sleepy Joe) is a small window on the second screen,
  set up only through networked commands and card plays, reacting to the shared state (its Wall, its own card).
- **Game sound:** `--method audio` replays the shots in real time with the music off (`--pres-nomusic`) and records the PC's
  output (WASAPI loopback, gaps filled against the clock); every clip's sound lines up within ~0.1 s.
- **Card images:** all 177 cards base and upgraded, 1000x1360 PNG with transparency (`cards/<character>/`).
- **Review page:** `trailer/tools/dailies.py CAPTURE_DIRS...` (later folders replace earlier takes).
- Lessons: set a Deport target's HP at the line but above the card's damage, or it just dies; the last enemy leaving ends the
  fight (the Loot screen fades in about 1.5 s later); enemy hover tooltips open under a parked mouse unless it's moved aside.

**T5, the painted shots** (`trailer/ai/shots.json`, `trailer/tools/h3.py`, output `build/trailer/ai/`):
- The user's earlier H3 workflow gave weak results, so the settings were researched and tested. The released quality
  configuration is res_multistep, 20 steps (21 sigmas), shift 12 video / 3 audio, guidance 1, no acceleration, the native
  1344x768 canvas (the user's workflow: turbo LoRA at 6 steps, euler, Spectrum forecasting, 0.4 MP). A/B on the same seed:
  the quality run moves more and stays crisp; the turbo run barely moves and grains the texture. Sage attention is
  visually identical (under 4% pixel difference) at half the time (~4.5 min a clip): takes are explored with it, the
  chosen ones re-rendered without it. The pruned int8 model was kept (the 34 GB full int8 is the next step if needed).
- Prompts follow MiniMax's official format (the minimax-h3-prompt skill): the alignment line, one shot, observable
  action, soundscape. First frames are the mod's own paintings and card art, cropped to 1.75:1.
- Key art: Krea 2 through `scripts/art_gen.py trailer/ai/keyart_jobs.json` (the mod's art pipeline, style references =
  both select paintings and the Spire plate), 1920x1088.
- Title drafts: Remotion stills (`TitleDraft`) in the game's own OFL fonts (Kreon, Spectral, Fira Sans Extra Condensed,
  copied from the game's files to `build/trailer/media/fonts/`, never committed).
- Picks (the user): a1_donald_hero s7, a2_joe_eyes s3, a3_brandon_rises s1, a3_brandon_unleashed s2, a4_title_walk s22 s2;
  title font A (Spectral); thumbnail from key art backtoback s11.
- **Finishing** (`trailer/tools/upscale.py TAG...`, output `build/trailer/media/ai/<TAG>.mp4`, 2520x1440 at 60 fps with
  H3's audio): SeedVR2 7B fp16 (numz's ComfyUI node) to 1440p, then RIFE v4.26 (ComfyUI's built-in FrameInterpolate)
  x5 with every other frame kept, 24 to exactly 60 fps. SeedVR2 is a one-step model, so block swap (all 36 blocks and the
  I/O layers in system RAM) costs one weight transfer per batch; without it the 7B at 1440p spilled 18 GB into shared
  memory and ran at 90 W for 10+ minutes on one batch. Batch 21 peaks at ~12 GB (DiT) and ~18 GB (tiled VAE decode);
  ~70 s per 21 frames. Against a Lanczos upscale: clean line work on glasses, hair and teeth, still painted, not photoreal.
- Local patch: SeedVR2's `src/optimization/compatibility.py` stubbed `flash_attn` when it's missing, which makes
  transformers 5.x fail (KeyError 'flash_attn'); the stub is skipped on ImportError. Its dependencies were installed with
  a constraints file pinning torch, safetensors, huggingface_hub, transformers and numpy. ComfyUI must run with
  `PYTHONIOENCODING=utf-8` or the node fails to import on its emoji output (art_review.py now sets it).

**T7, master and exports** (`trailer/tools/export.py`, `subtitles.py`; output `build/trailer/export/`; how-to in
[TRAILER.md](TRAILER.md)):
- Mastering in `mix.py`: gain to -14 LUFS into a 4x-oversampled true-peak limiter, measured with ffmpeg's EBU R128
  meter and corrected until both -14.0 LUFS and <= -1 dBTP hold (v2's mix was -14.5 LUFS, -0.8 dBTP).
- The master picture is a ProRes HQ intermediate rendered from PNG frames; Remotion's own ffmpeg quit on a full-length
  x264 slow crf 10 encode (and ProRes rejects a CRF, so `remotion.config.ts` no longer sets one). `export.py` encodes
  YouTube's file (x264 slow crf 12, closed GOP, keyframe every 30 frames, BT.709, AAC 384 kbps), a 1080p60 file for
  Reddit, and a captioned 1080p60 (libass with Kreon; the lines already shown as big type are left out).
- Thumbnail: the `Thumbnail` still (key art backtoback s11, the title in the trailer's gold Spectral, a "SLAY THE
  SPIRE 2 · FREE MOD" ribbon), 1280x720 JPG; readable at search size (320x180).

**T6, the edit** (`trailer/edit/src/trailer/`, the Remotion composition `Trailer`; data in `timeline.json`):
- **Workflow:** `python trailer/tools/prep_edit.py` (hard-links the clips, game sound, card renders, relic and potion art
  and the roster portraits and sprites into `build/trailer/media/` and `build/trailer/audio/game/`), `python
  trailer/tools/moments.py [CLIP...]` (a time-stamped contact sheet per clip with its sound's hits, to set cuts), edit
  `timeline.json`, `python trailer/tools/mix.py`, `node stills.mjs Trailer T1 T2...` in `trailer/edit/` (checks framing
  in seconds), then `npx remotion render src/index.ts Trailer out/trailer_v1.mp4` (~8 min at 1440p60, 12 tabs) and
  remux `mix.wav` onto it. `python trailer/tools/edit_page.py RENDER` makes the review page.
- **timeline.json** now holds the picture too: `shots` (source clip, in-point, speed, freeze, camera `[scale, x, y]`
  into the 4K master and `camTo` for moves, grade `look`) and `graphics` (name cards, the Wall's stamps, card callouts,
  kinetic lines, lightning, glint, the collection beat, title, the button). The mix places each shot's own game sound
  under it (`shot_sounds`, slowed with slow motion; `sound` overrides, e.g. the co-op shots borrow the solo takes').
- Cuts sit on the music's beats (librosa beat grid of the placed cues: ~161 BPM Donald, ~172 Joe, ~112 Brandon; the
  finale is really 168 BPM, a bar of 1.4275 s found by correlating the drop against itself, which also gives seamless
  bar-aligned repeats: `music` entries can repeat a span of a cue). Every big sound effect kicks the picture (`fx.ts`: zoom punch, shake, flash, a few deep-fried frames).
- The captures are 4K, so shots punch in up to 2x without upscaling past the master; the co-op crops keep the dev
  player names ("Test Host") out. Callout shots use the `_clean` takes so the game's own played-card display doesn't
  double the callout; clean takes keep the text effects ("YOU'RE FIRED!", SAD!).
- Game sound: the audio pass recorded at the PC's volume (peaks ~-33 dBFS on every clip), so `mix.gameTrimDb` 27 brings
  it up before the -12 dB bus: about 18 dB under the music.
- Fonts: a transformed inline-block can't share its parent's `background-clip: text` (each letter carries the foil),
  and Spectral's space collapses in an inline-block (spaces get an explicit width).

**T3, the sound** (`trailer/audio/*.json` hold the briefs; `trailer/tools/` the tools; output in `build/trailer/audio/`):
- **ElevenLabs** (`trailer/tools/eleven.py`): key `elevenlabs_api_key` in `local_settings.json` (git-ignored; needs the
  voices, models and user read permissions). Starter plan: 40,000 credits a month, mp3 128 kbps only. `cast`, `vo VOICE_ID`,
  `sfx`, `credits`. Casting added six library voices to the account (it holds 10 custom voices in all).
- **Music** (`trailer/tools/music.py`): MiniMax Music 3 through ComfyUI's API, the official template's graph; ~22 s a take.
  Five cues (`music.json`), four takes each. Captions follow the model's three parts (Global Metadata, Vocal Details, Arrangement).
- **Mix** (`trailer/tools/mix.py`): music, narration, effects and the game's audio placed from `trailer/edit/src/timeline.json`,
  music ducked under the narrator, peak limiter, −14 LUFS / −1 dBFS. Remotion only plays the mix (it can't duck or limit).
- **Animatic** (`Animatic` composition): the mix over the storyboard frames with name cards and subtitles, from the same timeline.
- `audio_report.py` measures takes (tempo, key, loudness shape, hits) and draws spectrograms; `reel.py` makes labelled listening reels.
- The user asked for the game's own sound, quietly under everything: T4 records it in a real-time pass per shot
  (PyAudioWPatch, WASAPI loopback) and lines it up with the silent 4K clips.

## Decision log

| Date | Decision |
|---|---|
| 2026-09-23 | Start fresh from the current build (v0.107.1). The old April `sts2_decompiled_code` and `sts2_extracted` folders in the game directory are ignored. |
| 2026-09-23 | Use the game's **official mod loader** (`mods/<id>/manifest.json` + DLL + PCK) and Harmony. Mod id `trump_character` (renamed `pres_mod` in Step 8.5), character class `Trump` (ID `TRUMP`). |
| 2026-09-23 | The user asked for an **uninstaller** too. It uses the game's own save system to delete saves that use the mod, so Steam Cloud copies go as well. |
| 2026-09-23 | Steps 1–2 committed (`c248d56`). |
| 2026-09-23 | Design v1 published. **Confirmed:** name "The Donald", metallic gold card colour, 70 HP, elites Deportable and bosses immune, and edgier names kept (Mass Deportation, Witch Hunt, Fake News, Fire and Fury, Chapter 11). |
| 2026-09-23 | **Design v2** after review. The Wall no longer soaks damage; it's a construction project with Sections and stages, because it was too close to Necrobinder's Osty. Deport uses a 25% HP line, because depending on the Wall forced a split build. More energy and draw that doesn't cost gold. |
| 2026-09-23 | Design principles from the user: **each play style must win on its own; scaling must have no ceiling** (like Osty's HP); card flow must not depend on gold. The user loves the Deals/gold play style. |
| 2026-09-23 | Relic and potion counts checked against the game at full unlock: 8 character relics + the starter upgrade, and 3 potions, per character. The large numbers are shared pools. |
| 2026-09-23 | Step 4: Deported enemies leave through the game's **escape** system (no gold, no on-death effects). Boss immunity = primary enemies in boss rooms; their minions can be Deported. The DENIED stamp marks an enemy that is Deportable right now. |
| 2026-09-23 | Step 4 committed (`5192aff`). **Golden Shovel redesigned** after review: a flat Build 6 at the start of combat only moved the stage thresholds. It is now "at the start of your turn, Build 2" (the construction crew); the Diamond Shovel builds 4 per turn. Chosen over three other options: compound growth per Section, draw on stage-up, and Gold per Section. |
| 2026-09-23 | Step 5 committed (`b6f2c2c`). Step 6: balance bot runs, 9 games in parallel, tiled 3×3 and muted (the user's request). First tuning pass: Deport 7 (10) damage, Deport line cap 60%. The user called balance good enough for now and added an optional Step 10 for more testing. |
| 2026-09-23 | Step 5: where the design text left room, choices are listed in the [Step 5 report](../characters/trump/docs/05_step5_report.md). Examples: Guard Towers fires one shot per Section, Law and Order Deports after end-of-turn damage, and You're Fired! and the Deportation Draught can't target immune bosses. |
| 2026-09-23 | Step 6 committed (`cbafe5c`). Step 7: the user asked to try two methods, training a style LoRA and Krea 2 with image style reference. The user chose **Krea 2 Turbo + style reference** (ComfyUI, the game's own art as reference images). The LoRA route moved to the optional Step 11, to be trained on Krea 2 Raw rather than Turbo. |
| 2026-09-23 | Step 4: mod models get `[SavedProperty]` support (`SavedPropertiesTypeCache.InjectTypeIntoCache` at startup), so run-long growth (Permanent Structure, Cornerstone) survives save and quit. |
| 2026-09-23 | Steps 7 + 7.5 committed (`533f196`), Step 8 committed (`63c91dc`). Art quality: presets added after the first batch (style-reference strength 0.75 instead of 1.0 was the main fix). |
| 2026-09-24 | **Step 8.5:** the mod becomes **sts2-pres-mod** (id `pres_mod`), a mod of playable presidents; Joe Biden comes later. **One mod with several characters**, not one mod per character. The game reads one set of text tables per mod, so each character keeps its own tables and the build merges them. The test flags became `--pres-*`, and the installer removes the old `trump_character` folder. |
| 2026-09-24 | Step 8.5 committed (`f27d1bc`). **Step 9:** co-op is tested with two local instances (`--fastmp`); big paintings are stored lossy (PCK 71 → 17 MB); version 1.0.0. Nexus Mods bans US-politics mods, so the release targets the Steam Workshop (only when the user decides) or a direct zip. The user deferred testing with other mods. |
| 2026-09-24 | GitHub: the repo is https://github.com/nexost/sts2-pres-mod, branch `master`, release `v1.0.0`, made public. Before going public all commits were rewritten from the global git identity (real name, work email) to `nexost` + GitHub noreply email, and the history went to a fresh repo. The old private repo was renamed `sts2-pres-mod-old`, for the user to delete. Mod author and Harmony ID are `nexost`. |
| 2026-09-24 | Docs for other people and their AI agents: `AGENTS.md`, `SETUP.md` (tools, models, paths) and `GAME_UPDATES.md`. Per-PC paths moved to `local_settings.json` / environment variables. Step 1 is repeatable with `extract_game.py`, and `game_update_check.py` checks what the mod relies on. |
| 2026-09-24 | A new character's id must be its class name in snake_case (the game derives every asset path from the class). The scaffold adds 5 stub cards so rewards and shops work before the real cards exist. Touch of Orobas became generic (`IUpgradableStarterRelic`). |
| 2026-09-24 | **Joe Biden, C1**, on branch `feature/biden`. Display name "Uncle Joe", starter relic Aviator Shades (a stub until C2), 75 HP / 99 Gold until the design sets them. **Colour:** primary `4A55E6` (royal indigo, readable for the name and targeting line), with the card frame deepened to navy (`frame_hsv` h 0.68, s 1.0, v 1.0). It was checked with the game's own frame shader against the Defect's teal (h 0.55): clearly different. The colour word is "navy", because the Defect already has "Blue" in the Colorful Philosophers event. **Test harness:** the game's "Ascensions Unlocked!" popup ignores the tutorials-off setting and covered every ui screenshot once a test run had met the Architect. The harness now marks it as seen in the test profile (`DisableTutorials`). |
| 2026-09-24 | **Joe Biden, C2 v1.** The user chose **Dark Brandon** as the signature, with **Sleepy Joe** as his other side (over Bipartisan and Build Back Better). Rules from the user: **sleepiness and confusion are on him, never on the enemies**, and **his identity must not copy another character or an enemy**. An earlier idea (putting enemies to sleep, using the game's Confused) was dropped for this. Tone: sleepiness, confusion and gaffes are fair game; age, decline and health are not. Result: **Drowsy** (a meter on him; nodding off ends his turn and he wakes as Dark Brandon) and **Tangent** cards (his cards change what they do while he rambles). design.md §10 lists the look-alikes and why each is different. |
| 2026-09-24 | **Biden C2 review:** name **Sleepy Joe** (not Uncle Joe), **70 HP**. Left to the design and revisited in C5: nodding off ends the turn on the spot; Laser Eyes deal 1× Drowsy. |
| 2026-09-24 | **Biden C3.** The Aviator Shades carry Sleepy Joe's end-of-turn Doze 2 (like Bound Phylactery carries Osty), and their text says so. Laser Eyes reuse the Defect's hyperbeam, tinted. Tangent text: the lit line's number is gold and the others are dimmed (`[color]` BBCode through `AddExtraArgsToDescription`). Framework: `NCharacterPoses.SetVariant` for a second pose set (Dark Brandon), with `placeholder_eyes` stand-ins. |
| 2026-09-24 | C3 committed (`f818dfb`). **Biden C4:** all content. Implementation choices (listed in the C4 report and design.md): Infrastructure Law is energy only, Corvette Cruise uses the game's Free Attack, Repeat the Line replays from the Discard Pile only, Tall Tales grows damage and Block. The cards test got a shared `PrepareCardTurnFor` hook. |
| 2026-09-24 | C4 committed (`0d273c7`). **Biden C5 (balance):** the Aviator Shades' end-of-turn Doze went from 2 to **3**. With 2, he was at the bottom of the base-game range (floor 25.7 vs 29.6 / 26.5) and weak against Act 1 elites. With 3, over two seed sets, he's level with Ironclad (28.1 vs 27.1, Silent 25.8) and naps in 82% of fights. Nodding off still ends the turn on the spot, and Laser Eyes stay at 1× Drowsy. If playtests find him too strong, the next lever is the nap Block (8 → 6). The balance bot got a `CardValueOverride` hook (Tangents do only their lit line), and `DamageValue` / `BlockValue` became shared helpers. Lesson: confirm a tuning change on fresh seeds; 18 runs move 2–3 floors from luck alone. |
| 2026-09-24 | C5 committed (`2c1d7b3`). **Biden C6 (art):** all 147 items, reviewed and kept by the user. From the user's review: Joe was on 80 of 88 cards, and the base game's mix (character on 30–40%, hands on about a fifth) became the rule, with 54 cards rewritten. Power icons had copied their two fixed references, so each now gets its own pair from a pool, at lower strength. Five look-alike Dark Brandon cards got their own framing and palette. The model's riskier ideas (real handguns, a knife, a burning Air Force One, an "Illuminati" eye) were rewritten. Tool changes: `card_notes`, per-card `art_background`, the reference-copy race fixed, and the review page works on a phone over Wi-Fi (`--host 0.0.0.0`). |
| 2026-09-24 | C6 committed (`4712ed4`). **Biden C7 (final checks):** every test passes for Sleepy Joe and The Donald. Co-op now checks that nodding off ends only that player's turn (the game blocks their plays), that Reach Across the Aisle reaches every player, and that each napper wakes as Dark Brandon on both instances (new `CoopTurnStart` hook). The co-op desyncs first seen were the PC: the second game instance crawled until a restart. The Donald's co-op test failed the same way, and is now the control. By the versioning rules, releasing Sleepy Joe would be **v1.1.0**, when the user decides. |
| 2026-09-24 | **Biden polish, before release (user's review).** Poses generated one by one never matched, so each form's four combat poses are now one generated image cut apart by the tool (`figure_sheet`); every pose shows at one scale in game. Regular Joe looks sleepy (yawning idle, tired smiles). The mega laser is back: the Defect's hyperbeam recoloured part by part (`VfxRecolor`); a whole-effect tint had multiplied its cyan to near-black. It fires from the eyes of the pose shown. Kept out on purpose: code that repaints Dark Brandon's lenses red (tools stay generic). |
| 2026-09-24 | **Biden visual effects** (the user picked 6 of 13 ideas): nodding off (the game's sleeping Z's), the Dark Brandon wake-up (red burst, then a glow and embers for the turn), Doze (Z's, and a dusk vignette near the line, local player only), Tangent speech bubbles, Snore/Snoring (the scream wave in blue), Laser Show (the sweeping beam in red). All visual only, so co-op state is untouched. design.md §11 lists them and the effects still to do. |
| 2026-09-24 | **VFX overhaul, both characters (user's request).** Biden: the other 7 ideas too (Double Vision and Laser Focus beams, Mic Drop meteor, Air Force One shadow, vehicles driving across with generated side-view art, Reach Across the Aisle lines, Ice Cream Cone sprinkles, potion effects). The Donald: coins in and out with his Gold, Tariff coin pops, Tweet bubbles, harder Wall stage-ups and gold bursts, a Deport swoosh, and card effects (You're Fired!, Fire and Fury, Make It Rain, Wrecking Ball, golf, potions, shovels). New shared `Framework/VfxKit` and a `test.py vfx` showcase mode. All visual only; ui, targeted card and co-op tests pass for both. |
| 2026-09-25 | **Playtest fix (user):** Here's the Deal on its Block line still asked for an enemy target. Now every Tangent aims and counts as its lit line: one-enemy lines need a target; all-enemy, Block, draw and Energy lines don't; an Attack Tangent on a no-damage line is a Skill (frame and type redraw in the hand). A one-enemy line played without a target (its line changed while queued) hits the first enemy standing. |
| 2026-09-25 | **Released v1.1.0** (MINOR: new character, old saves still load). Full release test set passed for both characters (ui, full cards, AutoSlay victory, co-op ×2 and with Ironclad), the zip was installed on a clean mod folder and both characters played; the uninstaller's dry run was clean. `feature/biden` fast-forwarded into `master`, tagged `v1.1.0`, GitHub release with the zip (30.7 MB). |
| 2026-09-25 | **Step 12, trailer.** Plan approved: 7 phases, one stop each. Generic narrator (ElevenLabs), no voice imitation, characters in on-screen text; painted-style AI shots only; master 2560×1440 60 fps for YouTube and Reddit. T1 treatment published: 1:05, epic straight-faced narration undercut by the footage (brass for The Donald, a lullaby for Sleepy Joe, a synthwave drop for Dark Brandon), co-op "bipartisan infrastructure" beat, title and a final joke. The mod's "riff-raff" phrasing is left out of the trailer (reads as a real-world jab out of context). |
| 2026-09-25 | **Trailer T1 and T2.** T1 decisions: *Presidents of the Spire*, narrator A, the Tweet ending, and a collection beat for the cards and relics (user's request), 1:12. T2: ffmpeg in `tools/ffmpeg`, Remotion in `trailer/edit`, librosa. Trailer footage is recorded **in the game** at `--fixed-fps 60` (harness mode `trailer`, `scripts/trailer_capture.py`): every frame of each clip at 4K, none dropped; Movie Maker works too but is slower and records the whole session; real-time screen capture can't see the game here. Clips are silent (FMOD). |
| 2026-09-25 | **Trailer T3.** ElevenLabs Starter for the narrator (six library voices cast, Don provisional) and 22 effects × 2; music made locally with MiniMax Music 3 (5 cues × 4 takes); the sound is mixed in Python from the edit's timeline (ducking, limiter, −14 LUFS) and Remotion only plays it. The cold open had ~9.9 s of speech in 10 s, so the three words "Warriors. Assassins. Machines." are placed one per hit and The Donald starts at 0:10.2. The collection mock now shows both decks (user). The game's own audio goes under the mix, quietly (user). |
| 2026-09-25 | **Trailer T3, round 2 (user).** Narrator David; Spire take 4 and Brandon take 2 kept. Donald, the lullaby and the finale were "too high and happy": new D minor variants (brass trap, brass phonk, dark hybrid; slowed or dark lullaby; hybrid, orchestral phonk, orchestral dubstep finale), and the mixer can deep-fry any clip (speed, pitch, bass, reverb, drive). The edit goes to a fast "holy shit" pace: cuts on the beat, zoom punches and blown-out frames on the big hits (in the animatic already); T4 records alternates of every shot. |
| 2026-09-25 | **Trailer T3 done.** Final sound: narrator David; music spire take 4, Donald brass phonk (take 13), lullaby slowed (take 13), Dark Brandon take 2, finale orchestral dubstep (take 12, its drop on the wave of cards at 0:54). The final joke holds Joe's line ~1.5 s before the cut (1:12.6). User notes for T4: the character-select shot hides the game's random "?" button; the "two presidents" shot must be The Donald with Sleepy Joe (the storyboard frame was an Ironclad stand-in). |
| 2026-09-25 | **Trailer T4.** The director bot records every shot in the game at 4K 60 fps (full and clean takes), over four acts' scenery; co-op with both presidents (host records, client reacts to shared state); the game's own sound in a real-time pass with the music off; all 354 card images rendered by the game. 49 clips on the dailies page. |
| 2026-09-26 | **Trailer T5.** The user's earlier H3 workflow was not trusted (their results were weak): the released quality configuration (res_multistep 20 steps, shift 12/3, guidance 1, native 1344x768) beat the turbo+Spectrum workflow in an A/B on the same seed; Sage attention kept (identical look, half the time) for exploring, final takes re-rendered without it. Five painted shots from the mod's own art (Donald hero, Joe dozing into Dark Brandon, Dark Brandon Rises, Unleashed, the title walk) and new key art of both presidents (Krea 2). Title in the game's own OFL fonts. Upscaling (SeedVR2) and 24-to-60 fps interpolation (RIFE) proposed, pending the user's OK for the downloads. |
| 2026-09-26 | **Trailer T5 finished.** The user picked a take per shot, the Spectral title and the backtoback s11 key art for the thumbnail, and approved the SeedVR2 and RIFE downloads. Picks re-rendered without Sage, then SeedVR2 7B fp16 to 2520x1440 (block swap and batch 21 to stay inside 24 GB; the first try at batch 41 spilled into shared memory) and RIFE v4.26 to 60 fps, ~10 min a shot. Upscaling beat a plain Lanczos resize clearly (line work, still painted); RIFE is clean on camera moves and coins and blends only things that jump between frames (lightning, a laser, a brick entering), for one 1/60 s frame. The 24 fps upscaled frames stay available per shot for the edit. |
| 2026-09-26 | **Trailer T6, edit v1.** The picture edit lives in `timeline.json` next to the sound (shots and graphics), rendered by Remotion from the 4K captures, the painted shots and the mod's own card, relic and character art. Moment maps (contact sheets with each clip's sound hits) and the music's beat grid set every cut; the captures are punched in rather than shown whole. New gags inside the approved script: the Wall's stages stamped and struck through, CANDIDATE No. 1 / No. 2 / No. 2 (AWAKE) name cards, a freeze on SAD! under the record scratch. End card: Free on GitHub with the repo link; small print now says "parody". Game sound raised by a 27 dB trim (it was recorded at a low PC volume) to sit ~18 dB under the music. No burned-in subtitles (a captions switch exists for a Reddit cut). |
| 2026-09-26 | **Trailer T6, edit v2.** The user: the hero cards (0:55) and the relic ring (1:01) went by too fast. Each hero card now holds a full bar (1.43 s) and plays a full bar; the ring holds 4.9 s. The finale gets two bars of its drop repeated in the mix (bar 1.4275 s, found by correlation; librosa's 172 BPM was really 168) and runs one more bar, so the cuts stay on the beat: Golden Escalator slams on the drop's return, the repeat re-hits on Laser Show, the title lands before the track's bass fades. Runtime 1:22.6. |
| 2026-09-26 | **Trailer T7.** The user approved v2. Final: the mix mastered to -14.0 LUFS / -1.3 dBTP (true-peak limiter), the picture re-rendered from PNG frames through a ProRes HQ master, exports for YouTube (1440p60), Reddit (1080p60, with and without burned-in captions), YouTube captions (.srt), the thumbnail and the mix; `docs/TRAILER.md` tells how to update or remake it. No vertical cut (the user's "maybe later"). Nothing uploaded anywhere: publishing is the user's call. |

## Open items

- P8 (stats screen section) and P9 (own unlock timeline) are optional (see the Step 1 report).
- Steam Cloud also syncs the test save folders (`modded_prestest`). Files deleted by hand come back; use `test.py cleansaves` (it goes through the game).
- `.claude/launch.json` in the game folder serves `characters/` on port 8765 for local page previews (e.g. `/trump/design/gallery.html`).
- If the game suddenly runs at ~8 fps in tests (the shared preload in `godot.log` takes ~30 s instead of ~2 s), the PC needs a restart. It isn't the mod (checked on 2026-09-23).
  The same on 2026-09-24, in co-op: only the second instance crawled (`Preloading 'Act=…'` ~600 ms instead of ~20 ms), the two drifted apart and
  the test failed with a desync at the rest site. The monitors had switched from 60 to 120 Hz (listed at the top of `godot.log`). The Donald's co-op test failed
  the same way, so run it as a control; after a restart both passed.
- The full one-at-a-time Deport sweep over all 80 encounters (~45 min) hasn't been run. Targeted lists are the default: `test.py deportsweep SEED A,B,C`.
- **Other mods** (deferred by the user): test alongside BaseLib and a popular character mod. Worth adding then: a startup check that logs a clear error when another mod defines a class with the same model ID as ours. The game silently keeps the last one loaded.
- Workshop users can't run `uninstall.cmd`'s save cleanup; the Workshop description should ask them to finish runs before unsubscribing.

## Resuming in a new session

1. Open the repo (the author's copy is `C:\Users\exeet\sts2-pres-mod`). Read [`AGENTS.md`](../AGENTS.md), this file, [`README.md`](../README.md) and [`FRAMEWORK.md`](FRAMEWORK.md). For a character's design, read `characters/<id>/design/design.md`.
2. On a new PC, follow [SETUP.md](SETUP.md) first. On the author's PC, Claude also has a project memory with the plan summary and working rules (they're in AGENTS.md too).
3. Check the game version (`release_info.json` in the game folder) against `re/game_version.json`. If the game updated, follow [GAME_UPDATES.md](GAME_UPDATES.md) before building.
4. `python scripts/build.py --install`, then `python scripts/test.py ui -c trump` to confirm everything still works.
5. Carry on with the next ⏳ step above, and stop at its end.
