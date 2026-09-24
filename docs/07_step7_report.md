# Step 7 report: lock the art style

_2026-09-23. The user tried two methods and chose **Krea 2 Turbo + image style reference**. The trained-LoRA route moved to the optional Step 11._

Boards for review:
- [`art/step7_board_cards.png`](art/step7_board_cards.png): our 3 card arts above the game art used as style reference.
- [`art/step7_board_ingame.png`](art/step7_board_ingame.png): in-game screenshots with the real card frames, the character select button and the relic.
- [`art/step7_board_relic_portrait.png`](art/step7_board_relic_portrait.png): the relic among base relics, and the portrait among the 5 base buttons.

## 1. What the game's art looks like

From the unpacked game (560 card portraits, 639 relics, the character select art):

**Card art (1000×760, RGB)**
- **Shapes:** bold graphic silhouettes, big flat colour shapes, hard-edged dark shadow shapes and dark outline accents. Brush texture is light: painted, not airbrushed.
- **Palette:** saturated complementary pairs; hot red and orange against purple, navy or teal. The background is a flat colour field or a radial burst. Each image has one bright accent: a glow, a spark or an effect.
- **Lighting:** strong rim light and glowing effects, with deep shadows. Faces are usually hidden (helmets, hoods, masks); hands, weapons and effects carry the action.
- **Framing:** one clear subject, a diagonal and dynamic composition, and often first-person hands pushing into the frame.
  **The card frame crops a lot:** attacks show a shield-shaped window with a pointed bottom, and skills a rectangle. Only about the central 75% of the width and the top ~85% show, so the subject must sit in the upper middle.

**Relics (256×256, RGBA)**
- One object filling most of the frame, often tilted, painted flat with soft brush texture and simple top-left light.
- **No painted outline.** The dark border is a separate translucent halo: pure black at alpha 128, about 12 px wide, around the silhouette.
- The game already has a plain blue **Shovel** relic, so ours has to read as gold and ceremonial (a ribbon bow).

**Character select button (132×195)**
- A head-and-shoulders bust on a **flat, saturated, single-colour background**: red for Ironclad, green for Silent, blue for Defect, pink for Necrobinder, orange for Regent.
- Chunky simplified painting with big shapes, cut to shape by the game's button mask.
- The Donald uses **mustard gold**, distinct from the Regent's orange.

## 2. The two methods

| | A: style LoRA trained on game art | B: Krea 2 + style reference (chosen) |
|---|---|---|
| **How** | ComfyUI's built-in trainer on 72 captioned game images (52 cards, 20 relics). | Krea 2 Turbo plus the `krea2_style_reference` LoRA (ostris). 1–3 game images go in as reference with each prompt. |
| **Setup** | No new tools. The right base is Krea 2 Raw, a 13.1 GB download (see below). | Nothing new: it uses your existing Krea 2 Turbo and the style reference LoRA. |
| **Cost** | ~2.4–2.6 s/step: ~65 min for 1,500 steps, then generation at the same speed as B. | ~26 s per card image (3 references), ~10 s with 1 reference. |
| **Result** | Stopped at step 91 of a 300-step check when the user chose B. | All 5 test pieces made. They read as StS2 art in the real card frames. |

The LoRA work isn't wasted. `scripts/art_train.py` (dataset, caption, train) and the measured settings are recorded in **Step 11** of the plan:
- Train on **Raw**, not Turbo. Training on Turbo without the de-distill adapter breaks its 8-step speed, which is why the first try was stopped.
- Gradient checkpointing must be depth 2. At depth 1, the 24 GB of VRAM overflow and training stalls.

## 3. The locked recipe (Method B)

**Server:** a headless ComfyUI on port 8189, run from the Comfy Desktop install. It writes only into `build/art/`, so your own ComfyUI folders stay untouched:

```
cd D:\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI
.venv\Scripts\python.exe -s main.py --extra-model-paths-config "%APPDATA%\Comfy Desktop\shared_model_paths.yaml" ^
  --output-directory C:\Users\exeet\sts2-trump-mod\build\art\comfy_out --input-directory C:\Users\exeet\sts2-trump-mod\build\art\comfy_in ^
  --port 8189 --listen 127.0.0.1 --disable-pinned-memory --disable-auto-launch
```

**Generation** (`scripts/art_gen.py JOBS.json --seeds 3`):
- Model: Krea 2 Turbo int8, the `krea2_style_reference` LoRA at 1.0, and the Qwen3-VL 4B text encoder.
- Sampler: 8 steps, euler/simple, CFG 1, shift 1.15/0.5, with `index_timestep_zero` reference latents.
- The jobs for the test pieces are in [`art/jobs/step7_styleref.json`](../art/jobs/step7_styleref.json).
- Make 3 seeds per piece and pick the best one.

| Asset | Size generated → final | Style references | Post-processing (`scripts/art_post.py`) |
|---|---|---|---|
| Card art | 1216×928 → 1000×760 | 3 game cards close to the subject (a character card for Donald cards, a monster card for Deport cards, an object card for items) | `card`: cover-crop |
| Relic | 1024×1024 → 256×256 | The game's Shovel and Golden Compass, 3× larger on a light background | `relic`: key out the background (including enclosed holes), fit, then add the game's 12 px black halo at 50% |
| Char select button | 832×1216 → 132×195 | The Silent and Necrobinder buttons, 6× larger | `portrait`: cover-crop, then the game's button mask |

**Prompt template, card art:**

> Slay the Spire card illustration. {subject and action from the card's `art` field}. {composition: low angle / diagonal}, {background: a radial burst or deep colour field in the card's colours}. Flat cel-shaded digital painting with hard-edged dark shadow shapes, dark outlines, strong readable silhouette, saturated limited palette, in the exact painting style of the reference images.

- **The Donald's description:** "Donald Trump as a comic caricature, with his iconic swooping blond comb-over, orange-tan face, pouting lips, navy suit and an extra-long red tie".
  Without the name, the likeness disappears (a generic blond hero). With it, the style drifts toward an editorial cartoon. The references pull it back.
- **Monsters:** describe the game's monster, for example "a hooded cultist with a pale teal bird-skull head, ragged dark blue robes". Deport art always shows Spire monsters, never people.
- **Short signs render well** (EXIT). Keep words to 1–2 per image.

## 4. Test pieces

| Piece | File (in the mod now) | Notes |
|---|---|---|
| Build the Wall (Skill) | `mod/images/packed/card_portraits/trump/build_the_wall.png` | Clear read in the frame. The most "cartoon" of the three; Step 8 can push it closer with more game references per image. |
| Deport (Attack) | `mod/images/packed/card_portraits/trump/deport.png` | The best match: the cultist, EXIT turnstile and suit-sleeve hand look native. |
| Net Worth (Attack) | `mod/images/packed/card_portraits/trump/net_worth.png` | A golden safe crushing a goblin in a coin burst. Matches Hand of Greed's palette. |
| Golden Shovel (relic) | `mod/images/relics/golden_shovel.png` | Gold with a red bow. Reads well at icon size in the top bar and on character select. |
| Character select button | `mod/images/packed/character_select/char_select_trump.png` (+ `_locked`) | The first try (a painterly bust on muted brown) looked too realistic. Using the game's buttons as reference fixed it. |

- Every piece was checked in game (`test.py ui`: PASS, screenshots in `build/test/ui_20260923_132411/shots`).
- The other 86 cards and other assets still use placeholders.
- Raw generations are in `build/art/test/` (3 seeds each); processed files are in `build/art/final/`.

## 5. Notes for Step 8

- **Volume:** 88 cards × 3 seeds × ~26 s ≈ 2 h of generation, plus picking.
  Batch by archetype (Wall, Deport, Deals, Tweets, General) with a fixed set of references per archetype, so the set looks consistent.
- **Keep subjects in the upper middle** of the 1000×760 image (the frame crop). Attack windows lose the bottom corners.
- **Consistency of The Donald:** same description every time. The navy suit, red tie and blond swoop are his silhouette.
- **Still to design in Step 8:**
  - the full character select screen (a large layered painting, like Ironclad's);
  - the combat character (Spine rig);
  - power and potion icons;
  - the energy orb and map marker;
  - the four Wall stage visuals and the Deport stamp.
- **Comfy Desktop and the game share the GPU.** Free ComfyUI's memory (`POST /free`) before in-game tests.

## 6. Step 7.5: the art review tool

Added by the user after Step 7: one page to review every piece of art, keep it or regenerate it.

```
python scripts/art_review.py          # opens http://127.0.0.1:8190 ; --host 0.0.0.0 to use it from a phone on the same Wi-Fi
```

- **Everything the mod needs, 152 items in 6 tabs:**

  | Tab | Items |
  |---|---|
  | Character (15) | select button, select screen, top-bar icon, map marker, combat poses (idle, attack, cast, hurt), shop and rest-site poses, 4 co-op hands, screen transition |
  | Cards (89) | 87 portraits + 2 Ancient full-art cards |
  | Relics (9) | |
  | Potions (3) | |
  | Powers (26) | |
  | UI & mechanics (10) | energy orb base, swirl and rim; energy icon; gold-cost coin; 4 Wall stages; the DENIED stamp |

- **Per item:**
  - **Keep** copies the game-ready files into `mod/` (then `build.py --install`).
  - **Regenerate** works on one item or many selected at once, ×1–4 versions each. It starts right away and the page updates live: queued, then generating with a timer, then the new image.
  - **Versions:** every past version stays and is one click (or number key) away.
  - **Prompt:** edit and save a prompt per item.
  - **Cancel** stops queued work.
- **Card previews** show the art inside the real frame: attack, skill and power windows, and the Ancient full-card border with its text box. Toggle this with "Card frames".
- **Shortcuts:**
  - Hover a tile and press **K** to keep or **R** to regenerate; both act on the whole selection when there is one.
  - **Space** selects, Shift-click selects a range, and Ctrl+A selects all shown.
  - **Enter** opens an item. In the large view, **←/→** move between items and **1–9** pick a version.
- **Filters:** tab, status (to review, not generated, kept, working, errors), card archetype and search. "Generate missing" queues everything in the current tab that has no image yet.
- **Behind it:**
  - The server starts the headless ComfyUI itself and stops it on exit.
  - Queued jobs survive a server restart.
  - Recipes (references, prompt, size, post-processing, output paths) are in `scripts/art_recipes.py`. What to draw is in `cards.json` (cards) and `docs/design/art_assets.json` (everything else).
  - State and all versions are in `build/art/review/`.
- **Tested:**
  - one image per recipe type (16 types), including keying off plain and green backgrounds, the gold-recoloured orb reference, and the 5-layer energy orb;
  - live updates, version switching, Keep, and a server restart mid-queue.
- **Speed:** about 18–26 s per image, so all 152 items take about an hour per pass.

## 7. Quality presets (added after the first full batch)

The user found the first batch low quality: muddy, blotchy, smeared detail. A controlled test (same prompt and seed, one change at a time, on Wall Slam and Mean Tweet; images in `build/art/quality/`) showed:

| Change | Effect |
|---|---|
| fp8 model instead of int8 | Practically identical, and twice as slow here. **Not the cause.** |
| 16 steps instead of 8 | Cleaner, crisper lines and shapes. |
| **Style-reference strength 0.6–0.8 instead of 1.0** | **The main fix.** Full strength with 3 references is what smears the image. |
| 1–2 references instead of 3 | Cleaner, more coherent scenes. |
| 1.5× second pass ("hires") | More detail, but doesn't cure the smearing by itself. |
| No style reference at all | Cleanest, but a generic comic look, not Slay the Spire. |

The review tool now has a **Quality** selector (next to "Per regenerate"). All presets use the int8 model:

| Preset | Settings | Time |
|---|---|---|
| Draft | 8 steps, 2 references at 0.8 | ~15 s |
| **Standard** (default) | 12 steps, 2 references at 0.75 | ~25 s |
| High | 16 steps, 2 references at 0.75, 1.5× detail pass | ~60 s |
| Max | 16 steps, 3 references at 0.75, 2× detail pass | ~2 min |
| Legacy | The first batch's settings (8 steps, 3 references at 1.0) | ~25 s |

- Every version records the preset it was made with; versions from before this change show as "Legacy (first batch)".
- **Prompts still matter:** some cards leave out part of the scene (Wall Slam's wall and enemy). Edit the prompt in the item's detail view, then regenerate.
