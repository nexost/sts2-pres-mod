# Setting up a PC to work on the mod

Everything needed to go from a fresh clone to a built, tested mod, then (optionally) to making art. An AI agent can
follow this page top to bottom. Each step says how to check it worked.

What's in the repo:
- **In git:** the mod's code, assets, tools and docs.
- **Not in git** (each PC makes its own):
  - `re/`: the decompiled game, which is Mega Crit's;
  - `tools/`: Godot and GDRE;
  - `build/`: all output, including the art review history;
  - `backups/`;
  - `local_settings.json`.

## 0. Requirements

| Need | Version used | Notes |
|---|---|---|
| Windows 10/11 | Windows 11 | The scripts, installer and game paths assume Windows |
| Slay the Spire 2 (Steam) | v0.107.1 | `release_info.json` in the game folder has the version. Another version: read [GAME_UPDATES.md](GAME_UPDATES.md) first |
| Git | any | |
| Python | 3.11 | `pip install pillow` (Pillow 12) is the only package |
| .NET SDK | 9.0 (9.0.312 used) | `dotnet --version`. The mod targets net9.0 like the game |
| ilspycmd | 9.1 | `dotnet tool install -g ilspycmd --version 9.1.0.7988` (newer versions should work too) |
| Godot .NET | **4.5.1 exactly**, the "mono" build | The game's engine version. Used headless to import assets and pack the PCK |
| GDRE Tools | 2.6.4 | Godot RE Tools: unpacks the game's `.pck` |
| GitHub CLI `gh` | optional | Releases (PUBLISHING.md) |
| **For art only:** ComfyUI + Krea 2 models, an NVIDIA GPU | ComfyUI 0.33.0 (Comfy Desktop), RTX 4090 24 GB | §5 |

Disk space:
- the unpacked game in `re/pck`: ~3 GB;
- tools: ~350 MB;
- art models: ~18.5 GB;
- art review history: grows ~1 GB per full pass.

## 1. Back up your saves

Tests never touch your real saves: they use their own `modded_prestest` / `modded_bal*` folders, and uninstall makes backups. Still, back up once before starting:

```
xcopy /E /I "%APPDATA%\SlayTheSpire2" "backups\saves_before_setup"
```

## 2. Tools

1. **Godot 4.5.1 .NET:**
   - Download `Godot_v4.5.1-stable_mono_win64.zip` from https://godotengine.org/download/archive/4.5.1-stable/ or the GitHub release `godotengine/godot` tag `4.5.1-stable`.
   - Unzip it so this file exists: `tools/godot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe`. `build.py` uses that exact path.
2. **GDRE Tools 2.6.4:**
   - Download the Windows zip from https://github.com/GDRETools/gdsdecomp/releases (tag `v2.6.4`).
   - Unzip it so `tools/gdre/gdre_tools.exe` exists.
3. **ilspycmd:** `dotnet tool install -g ilspycmd`. Check: `ilspycmd --version`.
4. **Python:** `pip install pillow`. Check: `python -c "import PIL; print(PIL.__version__)"`.

## 3. Paths for this PC

The scripts default to the original author's paths. Set yours in either of these places:
- `local_settings.json`: copy `local_settings.example.json` and edit it. It's git-ignored.
- Environment variables with the same names in capitals, e.g. `STS2_GAME_DIR`. These override the file.

| Setting | What | Default |
|---|---|---|
| `sts2_game_dir` | The game folder (has `SlayTheSpire2.exe`, `release_info.json`) | `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` |
| `comfy_dir` | The ComfyUI folder with `main.py` (art only) | the author's Comfy Desktop install |
| `comfy_python` | The Python that runs ComfyUI | `<comfy_dir>\.venv\Scripts\python.exe` |
| `comfy_models_yaml` | Comfy Desktop's shared model list; skipped if the file doesn't exist | `%APPDATA%\Comfy Desktop\shared_model_paths.yaml` |
| `comfy_lora_dir` | Where `art_train.py` saves LoRAs (optional Step 11) | the author's ComfyUI `models\loras` |

The C# build gets the game folder from the same setting (`build.py` passes `-p:GameDir=...`). The installer (`install.cmd`) finds the game through Steam on its own.

## 4. Extract the game, build, test

```
python scripts/extract_game.py        # re/code (decompiled C#, 10 s) + re/pck (unpacked assets, ~1 min, ~3 GB)
python scripts/game_update_check.py   # everything the mod relies on in the game exists: "All present."
python scripts/build.py --install     # placeholders, checks, C# build, Godot import, PCK, dist/, install into the game
python scripts/test.py ui             # the game starts in test mode and walks through everything (~2-3 min): "RESULT: PASS"
```

- **First launch of a modded game:** the game asks to allow mods. `test.py` runs are automatic, but do one normal launch first and accept.
- **What the tests use:** `test.py` runs the real game with `--pres-test`, a separate save folder, and your Steam login. Close the game before starting a test.
- **If `test.py ui` fails:**
  - its report and screenshots are in `build/test/ui_<time>/`;
  - `godot.log` lines starting with `!` involve the mod;
  - lines about `InputMap action "controller_..."` are the game's own and harmless.
- More tests (`cards`, `autoslay`, `coop`, `balance`) are in [FRAMEWORK.md](FRAMEWORK.md) §6.

## 5. Art setup (only to make or change art)

The art is made with **Krea 2 Turbo + a style-reference LoRA** in ComfyUI, with the game's own art as references. Full guide: [ART_PIPELINE.md](ART_PIPELINE.md).

1. **ComfyUI:** version 0.33.0 or newer, with Krea 2 support. The author used **Comfy Desktop** (https://www.comfy.org/download); a portable or git install works too.
   - The nodes used are all built in: `UNETLoader`, `CLIPLoader` (type `krea2`), `TextEncodeQwenImageEditPlus`, `FluxKontextMultiReferenceLatentMethod`, `SamplerCustomAdvanced`, and so on.
   - **No custom nodes** are needed.
2. **Models:** all four are in https://huggingface.co/Comfy-Org/Krea-2. Put each in the ComfyUI models folder named below:

   | File | Folder | Size | Role |
   |---|---|---|---|
   | `krea2_turbo_int8_convrot.safetensors` | `models/diffusion_models/` | 12.9 GB | Krea 2 Turbo, int8 (the default; needs INT8 tensor cores: RTX 30-series or newer) |
   | `qwen3vl_4b_fp8_scaled.safetensors` | `models/text_encoders/` | 5.0 GB | Qwen3-VL 4B text encoder |
   | `qwen_image_vae.safetensors` | `models/vae/` | 0.25 GB | VAE |
   | `krea2_style_reference.safetensors` | `models/loras/` | 0.4 GB | Style reference LoRA by ostris (also at https://huggingface.co/ostris/krea2_turbo_style_reference) |

   The fp8 Turbo (`krea2_turbo_fp8_scaled.safetensors`) also works (`art_recipes.FP8`); it was no better and slower here.
   The file names are set in `scripts/art_gen.py` (`UNET`, `CLIP`, `VAE`, `STYLE_LORA`).
3. **GPU:** made on an RTX 4090 (24 GB), about 25 s per image at the Standard preset. Smaller cards may work with ComfyUI's model offloading, but more slowly.
4. **Paths:** set `comfy_dir` and `comfy_python` (§3). `python scripts/art_review.py` then starts ComfyUI by itself, headless on port 8189, writing to `build/art/comfy_out` and reading from `build/art/comfy_in`.
   - To run ComfyUI yourself instead, start it on port 8189 with `--output-directory <repo>\build\art\comfy_out --input-directory <repo>\build\art\comfy_in`. The tool reads the images from there.
   - The tool uses a server that's already running.
5. **Check:** `python scripts/art_review.py` opens http://127.0.0.1:8190. Regenerate one relic and see the new version appear.
   - On a fresh clone every item shows "not generated", because the review history (`build/art/review/`) isn't in git.
   - The kept art is already in `mod/`. Generate only what you want to replace; **Keep** overwrites the file in `mod/`.

## 6. Git identity (before committing)

Check `git config user.name` and `git config user.email` in this repo before the first commit; they end up in public history. To stay private on GitHub, use your GitHub noreply address, set just for this repo:

```
git config --local user.name "<github-username>"
git config --local user.email "<id>+<github-username>@users.noreply.github.com"
```

Your id is in `gh api user --jq .id`.
