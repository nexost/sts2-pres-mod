# The Donald: Slay the Spire 2 character mod

Workspace for the mod. Targets STS2 **v0.107.1** (Godot 4.5.1 .NET, .NET 9).

## Layout

| Path | What |
|---|---|
| `mod/` | Godot asset project **and** C# project (`TrumpMod.csproj`). Paths inside mirror the game's `res://` layout |
| `mod/trump_character/src/` | Mod code: `ModEntry.cs` (initializer), `Models/` (character, pools, cards, relics), `Patches/` (Harmony), `Dev/` (test harness, save cleanup) |
| `mod/trump_character/localization/eng/` | Text, merged into the game's tables |
| `mod/images`, `mod/scenes`, `mod/materials` | Assets at the exact paths the game loads for character `TRUMP` |
| `scripts/` | `build.py`, `test.py`, `make_placeholders.py`, `pack.gd`, `dist/` (installer + uninstaller) |
| `docs/` | Step reports and design docs |
| `re/` | Decompiled game code (`re/code`) and recovered Godot project (`re/pck`); git-ignored |
| `tools/` | GDRE Tools, Godot 4.5.1 .NET; git-ignored |
| `build/dist/` | Output: `trump_character/` mod folder + `install.cmd` / `uninstall.cmd` |

## Commands

```bash
python scripts/build.py              # build into build/dist/
python scripts/build.py --install    # build + install into the game
python scripts/test.py ui            # scripted walkthrough with screenshots (~40 s)
python scripts/test.py autoslay SEED # the game's AutoSlay bot plays a full run as our character (god mode)
```

Test output lands in `build/test/<mode>_<time>/` (report.json, screenshots, godot.log).
Test runs use save folder `modded_trumptest/`, never your real saves.

## How the mod hooks in

* The game's `ModManager` loads `mods/trump_character/{manifest.json, trump_character.dll, trump_character.pck}` and calls `ModEntry.Initialize`.
* All `AbstractModel` subclasses in the DLL are registered automatically; IDs come from class names (`StrikeTrump` → `CARD.STRIKE_TRUMP`).
  The build and the mod both refuse class names that clash with base game models.
* Harmony patches (`Patches/`):
  * `CharacterRegistrationPatch`: adds our character to the hardcoded `ModelDb.AllCharacters`.
  * `EpochCheckPatch`: skips timeline unlock checks that throw for unknown characters (crash after elites and after act bosses).
  * `ArchitectDialoguePatch`: gives our character dialogue in the final Architect event (it had none, so a win couldn't finish).
  * `CardLibraryTabPatch`: adds our card library tab (the library crashed mid-run otherwise).
  * `CharacterSfxPatch`: points attack/cast/death sounds at existing FMOD events.
  * `AtlasFallbackPatch`: lets `ui_atlas` sprites fall back to loose PNGs under `res://trump_character/atlas_fallback/`.
* `Dev/DevHarness.cs` only runs with `--trump-test`; `Dev/SaveCleanup.cs` only with `--trump-cleanup` (used by the uninstaller).
