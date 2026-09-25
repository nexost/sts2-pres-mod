# Art pipeline

How every image in the mod is made, for any character. The method was chosen in Trump's Step 7:
**Krea 2 Turbo + image style reference, with the game's own art as the references**.
The why and the tests behind it are in [characters/trump/docs/07_step7_report.md](../characters/trump/docs/07_step7_report.md).

In short:
1. Fill in the character's art description.
2. Open the review tool.
3. Generate, keep what you like, and rebuild.

## 1. Setup

Installing ComfyUI, downloading the four model files and setting the paths for your PC are covered in [SETUP.md](SETUP.md) §3 and §5. The short version:

- **ComfyUI** (0.33.0+, no custom nodes) runs headless on port **8189**, with its own input and output folders under `build/art/`. Your own ComfyUI folders are never touched.
  - `art_review.py` starts it by itself, using the `comfy_dir` / `comfy_python` settings, and stops it on exit.
  - To run it by hand (for `art_gen.py`), from the ComfyUI folder:
    ```
    <comfy_python> -s main.py [--extra-model-paths-config "%APPDATA%\Comfy Desktop\shared_model_paths.yaml"] ^
      --output-directory <repo>\build\art\comfy_out --input-directory <repo>\build\art\comfy_in ^
      --port 8189 --listen 127.0.0.1 --disable-pinned-memory --disable-auto-launch
    ```
    The output and input folders must be those two: the tools read the images from them. Use the models-yaml option only with Comfy Desktop.
- **Models** (from https://huggingface.co/Comfy-Org/Krea-2):
  - `krea2_turbo_int8_convrot.safetensors` (diffusion model);
  - `qwen3vl_4b_fp8_scaled.safetensors` (text encoder, loaded with type `krea2`);
  - `qwen_image_vae.safetensors`;
  - the `krea2_style_reference.safetensors` LoRA by ostris.
  - The names are set in `scripts/art_gen.py`.
  - The graph (built in code by `art_gen.py`): `TextEncodeQwenImageEditPlus` with the reference images, then `FluxKontextMultiReferenceLatentMethod` (`index_timestep_zero`), then euler/simple at CFG 1.
  - fp8 was tested and is no better than int8.
- **On a fresh clone** the review tool shows every item as "not generated": the review history (`build/art/review/`) isn't in git. The kept art is already in `mod/`; **Keep** on a new version overwrites it.
- **Style references:** the unpacked game in `re/pck/`: card portraits, relics, buttons, the Ironclad bust.
- **The GPU is shared with the game.** Stop the review tool (or free ComfyUI's memory with `POST /free`) before in-game tests.

## 2. What a character's art is made from

**`characters/<id>/character.json` → `art`:**

| Field | Used for | Trump's |
|---|---|---|
| `persona` | Every prompt that shows the character (`{persona}` in art_assets.json, poses, button, select screen) | "Donald Trump as a comic caricature, with his iconic swooping blond comb-over, orange-tan face, pouting lips, navy suit and an extra-long red tie" |
| `persona_sentence` | Added to card prompts whose `art` names the character | the same, as a sentence |
| `name_in_card_art`, `full_name` | Card `art` text says "Donald ..."; the prompt swaps in the full name. **The full name is what gives the likeness**: without it the model draws a generic hero | "Donald", "Donald Trump" |
| `enemy_rule`, `enemy_rule_archetypes` | Added to cards of those play styles, or whose art mentions an enemy: enemies are Spire monsters, never people | the Deport play style |
| `card_refs` | 3 game cards per play style, used as style references (`<character>/<card>` under `re/pck/images/packed/card_portraits/`) | Wall: heirloom_hammer, inflame, bludgeon, ... |
| `card_backgrounds` | Background per play style | "radial red and orange burst background", ... |
| `card_notes` (optional) | A sentence per play style, added to card prompts that show the character | none. Biden: Z letters and a snoring face for Nap, red lenses for Brandon |
| `default_archetype` | Fallback for both | General |
| `energy_tint` | Dark, mid and light colours the Ironclad orb and energy icon are recoloured to, as references for the orb and energy icon | gold |
| `sleeve` | The co-op hands: the arm's sleeve | "a navy suit sleeve with a white shirt cuff and a gold cufflink" |

**`characters/<id>/design/cards.json`:** each card's `art` field says what its portrait shows. Name the character by `name_in_card_art`.
The likeness only comes when that name is in the text: a card that says only "Dark Brandon" gets a generic hero, so write "Joe as Dark Brandon".
An optional `art_background` replaces the play style's background for one card. Use it, with a different framing (close-up of an object,
over the shoulder, a mirror, a wide scene, a low-angle full body), when cards of one style come out looking alike.

**`characters/<id>/art/art_assets.json`:** everything else, in 5 lists: `character`, `relics`, `potions`, `powers`, `ui`.
- Each item has an `id`, a `name`, the `art` text and a `kind`.
- Items in `relics`, `potions` and `powers` need no kind: their id is the model id in snake_case (`golden_shovel`, `wall_power`).
- **Add an entry whenever a relic, power or potion class is added.** The placeholder script finds the classes by itself, but the review tool only lists what this file names.

## 3. Kinds: recipe and destination

Recipes live in `scripts/art_recipes.py` (references, prompt template, size, post-processing). Destinations are in `scripts/presmod.py` `KIND_OUTPUTS`, relative to `mod/`, where `{id}` is the character id and `{item}` the item id.

| Kind | Generated → final | Goes to | Notes |
|---|---|---|---|
| `card` | 1216×928 → 1000×760 | `images/packed/card_portraits/{id}/{item}.png` | The frame crops a lot: keep the subject in the upper middle |
| `card_ancient` | 832×1168 → 606×852 | same | Full-card art; the text box covers the lower half |
| `relic` | 1024² → 256² | `images/relics/{item}.png` | Background keyed out, the game's 12 px black halo at 50%; hover outline derived at build |
| `potion` | 1024² → 256² | `images/potions/{item}.png` | 9 px halo |
| `power` | 1024² → 256² | `images/powers/{item}.png` | 8 px halo; one bold symbol |
| `char_button` | 832×1216 → 132×195 | `images/packed/character_select/char_select_{id}.png` + `_locked` | Bust on a **flat saturated background colour** of its own (base game: red, green, blue, pink, orange; Trump: mustard gold), cut by the game's button mask |
| `fullscreen` | 1792×832 → 2560×1200 | `images/{id}/char_select_bg.png` | Only the **right 3/4** shows (the painting is shifted 640 px left): keep the subject in the right half |
| `top_icon` | 1024² → 88² | `images/ui/top_panel/character_icon_{id}.png` + `_outline` | |
| `map_marker` | 832×1088 → 49×64 | `images/packed/map/icons/map_marker_{id}.png` | |
| `figure` | 832×1216 → trimmed | `images/{id}/{item}.png` | `merchant_pose`, `rest_site_pose` (and any single figure). Green screen keyed out. **Face right**, whole body, no furniture cut by the edge. Placed by the feet. `"refs": [["combat_idle"]]` uses the kept idle as a reference, so the proportions match the combat poses |
| `figure_sheet` | 512 per pose × 1024 → cut apart | `images/{id}/{pose}.png` per name in `poses` | **Combat poses.** One image with every pose of a set side by side, so they share one head size, scale and style; `art_post.pose_sheet` cuts it at the emptiest column near each boundary, drops slivers, and gives every pose the same height (the game then shows the set at one scale). A sheet with an extra or overlapping figure is rejected with an error: regenerate. `refs`: lists of kept sprite names laid side by side on green (`[["combat_idle"]]`; a whole set: `[["combat_idle", "combat_attack", ...]]`); a name ending in `#head` gives a close-up of the head instead. A second set for a form taken mid-fight (Biden's `dark_combat_*`, `NCharacterPoses.SetVariant`) uses the first set as its reference; its stand-in can ask for coloured eyes with `"placeholder_eyes"` |
| `hand` | 640×1792 → 422×1200 | `images/ui/hands/multiplayer_hand_{id}_{gesture}.png` | Co-op rock/paper/scissors/point, arm from the bottom edge |
| `transition` | 1792×832 → 2560×1200 grey | `images/ui/transitions/{id}_transition.png` | Greyscale dissolve mask |
| `orb` | 1024² → 256² per layer | `images/ui/combat/energy_counters/{id}/{id}_orb_layer_{n}.png` | Items list their `layers` (1 base, 2–3 swirl, 4–5 rim) |
| `energy_icon` | 1024² → 24² and 74² | `images/packed/sprite_fonts/{id}_energy_icon.png`, `pres_mod/atlas_fallback/ui_atlas/card/energy_{id}.png` | Card text icon and cost gem |
| `small_icon` | 1024² → `sizes` | the item's `outputs` | e.g. Trump's gold-cost coin |
| `prop` | 1344×768 → `fit` | the item's `outputs` | Side-view object on green, e.g. Trump's Wall stages |
| `decal` | 1216×704 → `fit` | the item's `outputs` | Plain text-to-image on white (no references), e.g. the DENIED stamp |

An item can always name its own `"outputs": {"key": "images/{id}/..."}`. This is how character-specific art gets in without code in the tools: the Wall stages, the stamp, the coin.

## 4. The review tool

```
python scripts/art_review.py [-c biden] [--host 0.0.0.0]    # http://127.0.0.1:8190 (0.0.0.0: from a phone on the same Wi-Fi)
```

- **On a phone** (with `--host 0.0.0.0`, then `http://<PC's address>:8190` on the same Wi-Fi):
  - the filters sit behind a **Filters** button;
  - tiles show in two columns, and the selection bar sits at the bottom;
  - the detail view is full screen, with ← · Keep · Regenerate · → at the bottom; swipe the picture to change items.
  Windows asks once to let Python through the firewall.
- **One page for every character.** The selector at the top appears once there are two or more.
  - `-c <id>` opens the page on that character (the URL gets `?char=<id>`); otherwise it shows the character you last looked at.
  - The style filter lists that character's play styles, and card previews use its own frame colour (from `energy_tint`).
  - Each character keeps its own state and versions in `build/art/review/<id>/`.
  - Jobs from all characters share the one ComfyUI queue.
- **Items:** everything the character needs, in tabs: Character, Cards, Relics, Potions, Powers, UI & mechanics. Card art is shown inside its real frame (Ancient cards in the full-card frame).
- **Per item:**
  - **Keep** writes the game-ready files into `mod/` at the "Goes to" paths.
  - **Regenerate** takes one item or a selection, ×1–4 each, and updates live.
  - **Versions** stay one click away; version numbers are never reused.
  - **Delete** a version (it goes to a trash folder).
  - **Edit prompt** per item.
  - **Cancel** stops queued work.
- **Status:**
  - *To review* means new versions you haven't decided on.
  - *Kept*, *Not generated*, *Working* and *Errors* are the other filters.
  - Keep on the kept version marks the newer ones as seen.
- **Quality presets** (all int8):

  | Preset | Settings | Time |
  |---|---|---|
  | Draft | 8 steps, 2 refs at 0.8 | ~15 s |
  | **Standard** (default) | 12 steps, 2 refs at 0.75 | ~25 s |
  | High | 16 steps, 2 refs at 0.75, 1.5× detail pass | ~60 s |
  | Max | 16 steps, 3 refs at 0.75, 2× detail pass | ~2 min |
  | Legacy | 8 steps, 3 refs at 1.0 (the muddy first batch) | ~25 s |

  Style-reference strength matters most: 1.0 with 3 references smears the image.
- **Shortcuts:**
  - **K** keep, **R** regenerate (acting on the selection when there is one).
  - **Space** selects, Shift-click selects a range, Ctrl+A selects all shown.
  - **Enter** opens an item; **←/→** move between items and **1–9** pick a version.
- A full pass over ~150 items takes about an hour at Standard.

## 5. After keeping art

1. `python scripts/build.py --install`, then restart the game.
2. `python scripts/test.py ui -c <id>` and look at `build/test/ui_*/shots/`.

Nothing else changes: scenes and code don't hard-code image sizes.
- Figures are placed by their feet at runtime.
- Relic and potion outlines are rebuilt from the new icons.
- Card portraits, select paintings and transition masks are stored as lossy WebP (quality 0.9) in the PCK; the PNGs in `mod/` stay lossless ([FRAMEWORK.md](FRAMEWORK.md) §5).
- The Wall stages (Trump) stretch up to 1.6× and then repeat a strip.

## 6. Other tools

| Script | What |
|---|---|
| `art_gen.py JOBS.json --seeds 3` | Batch generation from a job list (e.g. `characters/trump/art/jobs/step7_styleref.json`) with the server running |
| `art_post.py card\|relic\|portrait SRC DST` | The post-processing steps on their own |
| `art_train.py dataset\|caption\|train` | Optional Step 11: a style LoRA on the game's art. Train on **Krea 2 Raw** (13.1 GB, ask before downloading), run on Turbo; gradient checkpointing depth 2 on the 4090 (~65 min per 1,500 steps) |

## 7. Lessons from The Donald

- **Say the real name** (`full_name`) for the likeness. The references pull the style back from editorial cartoon to StS2.
- **Monsters, not people:** describe Spire monsters ("a hooded cultist with a pale teal bird-skull head") for anything on the receiving end.
- **Short signs render** (EXIT, DENIED). Keep it to 1–2 words.
- **Colours drift toward the references**: the first energy orb came out red from Ironclad's. `energy_tint` fixes it, and the orb prompts say "all gold colors, no red".
- Some cards leave out half the scene (Wall Slam's wall). Edit the prompt and regenerate rather than rolling more seeds.

## 8. Lessons from Sleepy Joe

- **Test a sample first.** Ten items (button, one pose per form, one card per play style, a relic, a power) take 5 minutes and
  caught three prompt problems before the hour-long pass.
- **What's in `persona` wins.** "A wide toothy grin" in the persona made him grin while dozing, so expressions belong in the
  item's own text. And he never takes off the aviators the persona gives him, whatever a card says.
- **No conditionals.** "Whenever he's asleep, his eyes are closed" was ignored. Describe what's visible: Z letters, head
  tilted back, mouth open in a snore.
- **A form needs its one visual cue named strongly:** "the lenses of his aviators glowing solid bright red" worked where
  "his aviators glowing red" didn't.
- Power icons need a plain symbol: "a half-closed eye" came out as a ball icon.
- **Poses generated one by one never match** (head sizes and builds changed from pose to pose). Generating a whole set in one
  image fixed it: `figure_sheet`. Put the character's full description in the prompt every time, or the likeness drifts
  (his gold aviators turned into black glasses).
- **The model copies what it sees in the references, figure by figure.** A four-pose sheet plus one full figure gave
  five-figure sheets 7 times in 8; a head close-up instead (`#head`) fixed the count. Each pose also copies its column's
  reference, so a look the references don't have (Dark Brandon's red lenses) only comes out about 1 time in 3.
- **References made from kept sprites must be rebuilt after a Keep**: the tool now rebuilds them before every
  generation and stamps their links, so the page can't show (or the generator use) an old idle.
- **Power icons used the same two references for every icon**, and all 24 came out as copies of Thorns' green star and
  Strength's red. Each icon now gets its own pair from a pool of 12 varied game icons (`POWER_REF_POOL` in
  `art_recipes.py`) at style strength 0.55, and every icon's text names its colours.
- **Keep the character on about a third of the cards.** The first Biden texts put him on 80 of 88 cards. The base game
  and The Donald (29 of 89) show the character on 30–40%, hands on about a fifth, and objects, effects, monsters and
  scenes on the rest. Biden's hands use the co-op sleeve (a rolled-up light blue shirt sleeve with a silver watch).
