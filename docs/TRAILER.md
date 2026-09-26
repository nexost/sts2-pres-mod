# The trailer: how it was made and how to update it

*Presidents of the Spire*, 1:22.6, 2560×1440 at 60 fps, both characters (Step 12 of the [plan](00_project_plan.md)).
**Watch it: https://youtu.be/co-SsNDz3go**
Everything that made it is in this repo: the director bot in the mod, the sound and picture tools in `trailer/tools/`,
and the edit in `trailer/edit/` (Remotion). Generated media live in `build/trailer/` (git-ignored). Game files never go
in git.

- **Deliverables:** `build/trailer/export/` (below). Publishing anywhere is the user's call ([PUBLISHING.md](PUBLISHING.md)).
- **Review pages** (artifacts): treatment (T1), sound (T3), dailies (T4), cinematics (T5), edit (T6), final (T7); links in
  the plan.
- **Tone:** the project's rules apply to every line, joke and image (see [AGENTS.md](../AGENTS.md)). Painted shots stay
  in the game's painted style, never photoreal; the narrator is a generic voice; the characters speak in on-screen text.

## Deliverables

`python trailer/tools/export.py trailer/edit/out/trailer_master.mov` writes `build/trailer/export/` from the ProRes
master (rendered from PNG frames; the final x264 encodes run in the project's ffmpeg):

| File | For |
|---|---|
| `presidents_of_the_spire_1440p60.mp4` | YouTube: the master picture (H.264, BT.709), AAC 384 kbps |
| `presidents_of_the_spire.en.srt` | YouTube closed captions (upload with the video) |
| `presidents_of_the_spire_1080p60.mp4` | Reddit and general sharing (~15 Mbps; Reddit's limit is 1 GB) |
| `presidents_of_the_spire_1080p60_captions.mp4` | The same with the narration burned in, for muted autoplay |
| `thumbnail_1280x720.jpg`, `thumbnail_1920x1080.png` | YouTube thumbnail (under 2 MB) and a full-size copy |
| `presidents_of_the_spire_mix.wav` | The mastered sound: 48 kHz 24-bit, −14 LUFS, true peak ≤ −1 dBTP |

The end card says *Free on GitHub · github.com/nexost/sts2-pres-mod* and *A fan-made parody mod. Not affiliated
with Mega Crit.* Not Nexus (their ban on US sociopolitical mods); the Workshop would need the end card changed.

## How it's put together

| Phase | What | Tools | Output |
|---|---|---|---|
| T1 | Treatment: script, beats, shot list, tone check | `trailer/treatment/` | the storyboard page |
| T2 | Toolchain; in-game recorder chosen | `scripts/trailer_capture.py`, `Dev/TrailerRecorder.cs` | |
| T3 | Narration (ElevenLabs), music (MiniMax Music 3), SFX, the radio cut | `eleven.py`, `music.py`, `mix.py` | `build/trailer/audio/` |
| T4 | Director bot stages and records every gameplay shot, then its sound | `trailer_capture.py`, `dailies.py` | `build/trailer/capture/` |
| T5 | Painted shots (MiniMax H3), upscaled and smoothed; key art; title font | `h3.py`, `upscale.py`, `art_gen.py` | `build/trailer/ai/`, `media/ai/` |
| T6 | The edit: shots, graphics, hits, collection beat, title, the button | `trailer/edit/`, `moments.py`, `prep_edit.py` | `trailer/edit/out/` |
| T7 | Mastering, exports, subtitles, thumbnail | `mix.py`, `export.py`, `subtitles.py` | `build/trailer/export/` |

### One timeline

`trailer/edit/src/timeline.json` is the single source for sound and picture (times in seconds):

- `music`, `vo`, `sfx`: the radio cut. Each clip: `file`/`line`/`name`, `at`, `from`/`to` in the source, `gain`, fades,
  and the "deep-fried" controls (`speed`, `pitch`, `bass`, `reverb`, `drive`, `tapeStop`). A cue can be split and a
  span repeated (the finale repeats two bars of its drop).
- `mix`: `duckDb` (music and game under the narrator), `busDb` per bus, `gameTrimDb` (the audio pass's level).
- `shots`: the picture. `src` is a captured clip (`build/trailer/media/clips/<src>.mp4`), `ai/<tag>` a painted shot,
  `split`, or `graphics`; `from` the in-point; `rate` (slow motion), `freeze`; `cam` `[scale, x, y]` frames the 4K
  source (x, y = the visible centre, 0..1) and `camTo` moves it; `look` a grade (`cold`, `mono`, `dusk`, `brandon`);
  `sound` false or `{src, from}` to borrow another clip's game sound.
- `graphics`: `name`, `stamp`, `callout` (a card render beside the action), `kinetic`, `lightning`, `glint`,
  `collection`, `title`, `button`.

`mix.py` places each shot's own game sound under it, so re-cutting the picture re-cuts the game sound.

## Recipes

### Change a cut, a graphic or the timing
1. Edit `timeline.json` (the shot list is also on the edit review page).
2. `python trailer/tools/mix.py` if any time moved (it also writes `vo_markers.json` for subtitles).
3. Check frames: `cd trailer/edit && node stills.mjs Trailer 12.9 15.3 --scale 0.5` (a few seconds each).
4. Render (below). `npx remotion studio` in `trailer/edit/` previews interactively.

To pick in-points, `python trailer/tools/moments.py CLIP` draws a contact sheet (a frame every 0.25 s with the time) and
the clip's sound with its loudest hits marked, in `build/trailer/moments/`. Cuts sit on the music's beats; librosa's
beat grid is a start, but check the tempo by correlating a bar against the next (the finale's real bar is 1.4275 s,
168 BPM, where librosa said 172).

### Re-record a gameplay shot
1. Shots are C# in `Characters/<Class>/Dev/DevHarness.<Class>.Trailer.cs` (`TrailerShots`), helpers in
   `Dev/DevHarness.Trailer.cs`. Each shot sets up a fight (`TrailerFight(act, encounter)`), a hand, and plays cards
   with `PlayFromHand`; `Takes` records a `_full` take (as played) and a `_clean` take (no interface) from fresh fights.
2. `python scripts/build.py --install`, then `python scripts/trailer_capture.py SHOT[,SHOT] -c <id>` (4K at a fixed
   60 fps; each clip is piped straight to ffmpeg, no dropped frames). Co-op: `trailer_capture.py coop full|clean`.
3. Sound: `trailer_capture.py SHOTS -c <id> --method audio` replays in real time with the music off and records the PC's
   output. Keep the Windows volume where it was (the mix's `gameTrimDb` assumes peaks around −33 dBFS) or change the trim.
4. `python trailer/tools/dailies.py CAPTURE_DIRS...` (review page, later folders win), then
   `python trailer/tools/prep_edit.py` to link the new takes into the edit.

Lessons: set a Deport target's HP at the line but above the card's damage; the last enemy leaving ends the fight;
park the mouse or tooltips open; one co-op take per launch. Clean takes keep text effects ("YOU'RE FIRED!", SAD!) and
hide the played-card display, so card callouts use them.

### Narration, music, sound effects
- Voice: `python trailer/tools/eleven.py vo VOICE_ID [--takes N]` renders the lines in `trailer/audio/narration.json` (narrator
  David; key `elevenlabs_api_key` in `local_settings.json`, never committed; Starter plan, mp3 128 kbps; the whole
  trailer used about 3,300 of 40,000 monthly credits). `eleven.py sfx` for `sfx.json`, `eleven.py credits`.
- Music: `python trailer/tools/music.py [CUE,...] [--takes N]` (MiniMax Music 3 through a running ComfyUI) from
  `music.json`; `audio_report.py` (levels and spectrogram sheets) and `reel.py` (listening reels) to compare takes.
- Master: `mix.py` normalises to −14 LUFS into a 4x-oversampled true-peak limiter and iterates until both the
  loudness and the −1 dBTP ceiling hold.

### Painted shots
- `python trailer/tools/h3.py trailer/ai/shots.json [--only NAME] [--seeds 1,2] [--preset quality|quality_sage]` renders
  the painted shots (MiniMax H3 image-to-video from the mod's own paintings). Settings that beat the turbo workflow: res_multistep, 20 steps, shift 12 video / 3 audio,
  guidance 1, native 1344×768, no acceleration. Explore takes with Sage attention (same look, twice as fast), re-render
  the pick without it. Prompts follow MiniMax's official format (the minimax-h3-prompt skill).
- `python trailer/tools/upscale.py TAG...`: SeedVR2 7B fp16 to 1440p (block swap 36 + batch 21 to fit 24 GB), then
  RIFE v4.26 ×5 with every other frame kept (24 → 60 fps), ~10 min a shot. Output `build/trailer/media/ai/`.
- ComfyUI must run with `PYTHONIOENCODING=utf-8` (art_review.py sets it); SeedVR2's flash_attn shim is patched locally.

### Render and export
```bash
cd trailer/edit
npx remotion render src/index.ts Trailer out/trailer_v2.mp4 --crf=14 --concurrency=12   # review (~9 min)
npx remotion render src/index.ts Trailer out/trailer_master.mov --codec=prores --prores-profile=hq --image-format=png --color-space=bt709 --muted --concurrency=12
npx remotion still src/index.ts Thumbnail out/thumbnail.png
cd ../..
python trailer/tools/subtitles.py
python trailer/tools/export.py trailer/edit/out/trailer_master.mov
python trailer/tools/edit_page.py trailer/edit/out/trailer_v2_final.mp4 --title "Trailer Edit v2" --poster 74.5   # review page
```
Review renders take the mix by remuxing `build/trailer/media/mix.wav` onto the video (the render may have started
before the last mix).

## Adding a character to the trailer
1. Shots: a `TrailerShots` dictionary for the new class (copy one of the two), capture and audio pass.
2. Painted shots and key art from the character's own paintings (`shots.json`, `keyart_jobs.json`).
3. Script and music: a new section in `narration.json` and `music.json`, a new beat in `timeline.json`; re-check the
   tone rules for every new line.
4. The collection beat counts cards, relics and potions from every character's `cards.json` automatically
   (`prep_edit.py` writes `collection.json`).

## Pitfalls already paid for
- Real-time screen capture (ddagrab) saw only the desktop on this PC; the in-game recorder is the way.
- The audio pass recorded at a low PC volume: every clip peaked at ~−33 dBFS, inaudible at the −12 dB bus until
  `gameTrimDb` 27.
- SeedVR2 7B at batch 41 spilled 18 GB into shared memory (90 W, 10+ minutes on one batch); block swap fixed it.
- RIFE blends what jumps between frames (lightning, a laser appearing) for one 1/60 s frame; the 24 fps upscales are
  kept if a shot needs them.
- Text: a transformed inline-block can't share its parent's `background-clip: text` (each letter carries the foil);
  Spectral's space collapses in an inline-block (give spaces a width).
- Card model ids to file names: `SlapATariff` → `slap_a_tariff` (split before a capital that starts a word).
- A shell `cd` inside a background command didn't take once; use absolute paths for long renders.
