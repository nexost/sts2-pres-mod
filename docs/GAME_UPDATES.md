# When the game updates

Slay the Spire 2 is in Early Access and patches often. The mod reaches into the game in many places: Harmony patches,
private fields, scene and asset paths, the Ancient list, balance numbers. This is the routine after every game update,
redoing the parts of Step 1 (research) that depend on the game.

## 1. Notice

- The game's version is in `release_info.json` in the game folder. `mod.json` `min_game_version` is the version the mod was last checked against.
- Steam updates the game on its own. After an update the mod may not load, may crash at startup, or may misbehave in one screen.

## 2. Re-extract the game

```
python scripts/extract_game.py
```

- Decompiles `sts2.dll` into `re/code` and unpacks `SlayTheSpire2.pck` into `re/pck`, then records the version in `re/game_version.json`.
- The previous extraction is kept as `re_<old version>/`, so you can see what changed. Delete it when done: it's ~3 GB.
- The commands behind it:
  - `ilspycmd -p --nested-directories -o re/code "<game>/data_sts2_windows_x86_64/sts2.dll"`;
  - `tools/gdre/gdre_tools.exe --headless --recover="<game>/SlayTheSpire2.pck" --output=re/pck`.

## 3. Check what the mod relies on

```
python scripts/game_update_check.py
```

| Section | What a MISSING line means | What to do |
|---|---|---|
| Versions | `re/` is older than the installed game | Run step 2 |
| Harmony patch targets | A patched method or property was renamed, moved or removed | Find its replacement in `re/code` (search the old name, or diff the class against `re_<old>/code`). Update the `[HarmonyPatch]`, and check the patch logic still fits the new code |
| Members reached by name | A private field or method used through `Traverse` / `AccessTools` changed | Same: find the new name in the class, update the string |
| Ancients | A new Ancient appeared, or one was removed | New: add its dialogue pattern to `Framework/Patches/AncientDialoguePatch.cs` (look at its `DefineDialogues`: A = the Ancient speaks, C = the character), then lines for every character in `characters/<id>/localization/eng/ancients.json`. Removed: delete the entry and its lines |
| Game files | A scene or image the tools copy or use as an art reference moved | Find the new path in `re/pck`; update `make_placeholders.py`, `art_recipes.py` or the character's `card_refs` |

Also diff what changed, even if the check passes. A method can keep its name and change what it does:

```
git diff --no-index --stat re_<old>/code re/code
git diff --no-index re_<old>/code/MegaCrit/sts2/Core/Nodes/Rooms/NCombatRoom.cs re/code/MegaCrit/sts2/Core/Nodes/Rooms/NCombatRoom.cs
```

Look closest at the classes the mod patches (the list the check prints), and at these:

| Area | Why it matters |
|---|---|
| `CharacterModel`, `ModelDb`, `CardPoolModel`, `RelicPoolModel`, `PotionPoolModel` | Our characters and pools subclass them. New abstract members break the build; new virtual ones may need values |
| `CardModel`, `RelicModel`, `PowerModel`, `PotionModel`, the `*Cmd` commands, `DynamicVar`s | Every card and relic uses them |
| `ProgressSaveManager`, epochs/timeline | P2/P6: checks that crash for unknown characters (`EpochCheckPatch`) |
| `NCombatRoom.PositionPlayersAndPets`, `NCreature`, `NCreatureVisuals` | Sprite bodies and the co-op Wall spacing |
| `NMerchantCharacter`, `NRestSiteCharacter`, `NCharacterSelectButton`, `NCardLibrary` | Our scenes and tabs |
| `TheArchitect`, `AncientEventModel` subclasses | Dialogue (a win can't finish without the Architect's) |
| `Modding/ModManager`, `ModSettings`, `SavedPropertiesTypeCache` | How mods load, and our saved properties |
| `localization/eng/*.json` in `re/pck` | Keywords our text uses (`[gold]`, `{Damage:diff()}`, ...) |

A new base-game model can also take a name the mod uses, e.g. a new card class called `Bribe`. The build's model ID check (`build.py`) then stops with "ID collision with base game": rename our class. The model ID changes with it, so update its localization keys and art file names too.

## 4. Build and test

```
python scripts/build.py --install
python scripts/test.py ui -c <id>                 # every character
python scripts/test.py cards PRESTEST1 -c <id>    # every card, relic, potion, and all text (~12 min per character)
python scripts/test.py autoslay SEED -c <id>      # a full run must still win
python scripts/test.py coop -c <id>               # co-op still in sync
```

The C# build compiles against the game's current `sts2.dll` (`GameDir`), so API changes show up as compile errors first. `godot.log` lines marked `!` in the test output involve the mod.

## 5. Balance and design numbers

The base game's balance can shift in a patch:
- `python scripts/analyze_cards.py` mines the base characters' cards again into `build/analysis/`. Compare with `docs/reference/base_game_benchmarks.md` and update it if the numbers moved.
- A balance batch against the base characters shows whether our characters are still in range (FRAMEWORK.md §6, ADDING_A_CHARACTER.md C5).
- New base-game mechanics or keywords are worth a look in `re/pck/localization/eng/cards.json`: a new keyword could clash with ours.

## 6. Release the update

- Set `min_game_version` in `mod.json` to the new version.
- Bump the mod version and add a release notes entry.
- Follow the release checklist in [PUBLISHING.md](PUBLISHING.md).
