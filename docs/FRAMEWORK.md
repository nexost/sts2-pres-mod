# How the mod is built: one mod, several characters

sts2-pres-mod (mod id `pres_mod`) is **one mod** holding **several playable characters**. Everything that makes a
character work in Slay the Spire 2 but isn't about that character (registration, compatibility patches, sprite-based
bodies, tests, build, art tools) is shared, so a new character only brings its own content.

To add a character, follow [ADDING_A_CHARACTER.md](ADDING_A_CHARACTER.md). The art tools are covered in
[ART_PIPELINE.md](ART_PIPELINE.md). This page explains the parts and why they are built this way.

## 1. What the game loads

- The game's `ModManager` loads `mods/pres_mod/{manifest.json, pres_mod.dll, pres_mod.pck}` and calls
  `ModEntry.Initialize` before `LocManager` and `ModelDb` start.
- **One DLL and one PCK for all characters.**
  - The game reads a mod's text from `res://<mod id>/localization/<lang>/<table>.json`, one file per table.
  - So each character keeps its own tables in `characters/<id>/localization/`, and the build merges them into one set. The build stops if two characters use the same key.
- **Characters are found automatically.** `ModContent.Characters` is every `CharacterModel` subclass in the DLL.
  - The patches, the test harness and the save cleanup all use it.
  - Nothing lists the characters by hand.
- **Model IDs come from class names alone**: `StrikeTrump` → `CARD.STRIKE_TRUMP`, class `Biden` → `CHARACTER.BIDEN`. This has consequences:
  - Class names must be **unique across all characters and the base game**. Every character suffixes its basic cards (`StrikeTrump`, `StrikeBiden`).
    - At build time, `build.py` refuses duplicates and base-game clashes.
    - At runtime, `ModEntry.CheckModelIdCollisions` refuses base-game clashes.
  - **The character's id is its class name in snake_case.** Every asset path uses it (`images/packed/card_portraits/<id>/`, `scenes/creature_visuals/<id>.tscn`, ...).
    - Class `SmokeTest` would need id `smoke_test`, not `smoketest`.
    - `new_character.py` and `build.py` both check this.

## 2. Repository layout

| Path | What |
|---|---|
| `mod.json` | The mod: id `pres_mod`, name, version, description, `legacy_ids` (older ids the installer removes: `trump_character`) |
| `characters/<id>/` | Everything about one character that isn't code or game assets: see below |
| `characters/_template/` | What `scripts/new_character.py` fills in for a new character (`{{Token}}` placeholders) |
| `mod/` | The Godot project that becomes the PCK, **and** the C# project (`PresMod.csproj`, assembly `pres_mod`). Paths inside mirror the game's `res://` |
| `mod/pres_mod/src/` | All C# (see §3) |
| `mod/pres_mod/atlas_fallback/` | Loose sprites for the game's texture atlases (§4) |
| `mod/images`, `mod/scenes`, `mod/materials` | Assets at the exact paths the game loads for each character id |
| `scripts/` | Build, test, design, art and packaging tools. `presmod.py` is their shared config |
| `docs/` | The plan, mod-wide reports, these guides; `docs/reference/` holds base-game data (benchmarks, the Ironclad asset list) |
| `re/`, `tools/`, `build/`, `backups/` | Decompiled game and unpacked PCK (`scripts/extract_game.py`; `re/game_version.json` says which game version); Godot and GDRE; all output; save backups. All git-ignored |
| `local_settings.json` | Paths for this PC (game folder, ComfyUI); git-ignored, see `local_settings.example.json` and [SETUP.md](SETUP.md) §3 |
| `AGENTS.md`, `CLAUDE.md` | Entry point for AI agents: what to read for each task, the working rules |

**One character's folder** (`characters/trump/`):

| Path | What |
|---|---|
| `character.json` | Id, class, name, colours, play styles (`archetypes`), everything the art tools need (`art`), card frame hue (`frame_hsv`), borrowed sounds (`sfx_proxy`) |
| `design/cards.json` | **Source of truth** for every card, relic and potion: text, numbers, play style, art direction |
| `design/design.md` | The design document |
| `design/card_list.md`, `gallery.html`, `compendium.html` | Generated from `cards.json` (`render_design.py`, `render_gallery.py`, `render_compendium.py`) |
| `design/compendium_template.html` | The review page's prose and layout for this character |
| `art/art_assets.json` | What to draw for everything that isn't a card portrait |
| `art/jobs/` | One-off art job lists for `art_gen.py` |
| `localization/eng/*.json` | The character's text, 9 tables, merged at build |
| `docs/` | The character's step reports and art review boards |
| `tools/` | One-off scripts used while building the character |

## 3. The code

```
mod/pres_mod/src/
  ModEntry.cs            initializer: registers Godot node scripts, [SavedProperty] members, ID collision check, Harmony
  ModContent.cs          ModContent.Characters, IsModCharacter()
  GlobalUsings.cs        game namespaces used everywhere (no character namespaces)
  Framework/             shared by every character
    IModCharacter.cs     what a character class adds to CharacterModel (sound proxy, figure heights)
    CharacterArt.cs      res://images/<id>/ paths, "art exists?" lookups, placing a figure by its feet
    Nodes/NCharacterPoses.cs   the combat body for characters without a Spine rig
    VfxRecolor.cs              recolours an instance of a game VFX scene (the Defect's hyperbeam into red Laser Eyes)
    VfxKit.cs                  small effects any character can use: screen flash, overlays, particle bursts (confetti,
                               coins, sparks), puffs (steam, dust), a sprite driving across the fight, a plane's shadow,
                               a glowing line between two points, and where a creature's painting is on screen
    Patches/             the compatibility patches (below)
  Dev/                   test harness and save cleanup, for every character
  Characters/<Class>/    one folder per character; namespace PresMod.Characters.<Class>
    <Class>.cs           the CharacterModel (+ IModCharacter)
    Pools/               card, relic and potion pools
    Cards/ Relics/ Potions/ Powers/
    Mechanics/           the character's own systems (Trump: WallCmd, DeportCmd, GoldCmd, Hooks.cs)
    Nodes/ Patches/      the character's own UI nodes and patches (Trump: the Wall display, the Pay-Gold badge)
    Dev/DevHarness.<Class>.cs  the character's test hooks (§6)
```

**Namespace pitfall:** the class `Trump` lives in the namespace `PresMod.Characters.Trump`.
- Inside that namespace, `Characters.Trump.X` means the namespace, not the class.
- Write `Trump.energyColorName`. The same applies to every character.

### Framework patches (all characters)

| Patch | Why |
|---|---|
| `CharacterRegistrationPatch` (P1) | `ModelDb.AllCharacters` is a hardcoded list of the 5 base characters; appends ours. Card, relic and potion pool lists derive from it |
| `EpochCheckPatch` (P2, P6) | Timeline unlock checks throw for unknown characters (crash after elites, bosses and act bosses); skipped for ours |
| `CardLibraryTabPatch` (P3) | Adds each character's card library tab (the library crashed mid-run otherwise). Tab tooltip: `card_library.json` `POOL_<ID>_TIP` |
| `CharacterSfxPatch` (P4) | Attack/cast/death sounds are built from the character id; points them at `IModCharacter.SfxProxyEntry` (a base character) |
| `ModEntry` (P5) | Registers our Godot node scripts (`ScriptManagerBridge.LookupScriptsInAssembly`) |
| `ArchitectDialoguePatch` (P7) | The final Architect event has no fallback dialogue, so a win couldn't finish. Reads `THE_ARCHITECT.talk.<ID>.*` |
| `AncientDialoguePatch` | Each character's own lines with Neow, Darv, Orobas, Pael, Tanx, Tezcatara, Nonupeipe and Vakuu (`ancients.json`) |
| `ArtPatches` | Sprite bodies: adds `NCharacterPoses` to a mod character's combat scene when its `Visuals` is a `Sprite2D`; places the shop and rest-site paintings (a `Sprite2D` named `CharacterSprite`) by their feet and skips the Spine calls |
| `AtlasFallbackPatch` | `ui_atlas`-style sprites (the card cost energy gem, relic and potion outlines) fall back to loose PNGs under `res://pres_mod/atlas_fallback/` |
| `TargetFilterPatch` | Lets a card or potion refuse targets (`ITargetFilter`) |

Character-specific patches live in `Characters/<Class>/Patches/`. Trump has four:
- `CombatUiPatch` adds the Wall display.
- `PayGoldBadgePatch` adds the Pay-Gold cost badge.
- `RubberStampPatch`: Deported enemies pay out Gold.
- `TouchOfOrobasPatch` maps the Golden Shovel to the Diamond Shovel.

### Saved values

The game only saves `[SavedProperty]` members of its own types, so `ModEntry` adds every mod model type to the save cache. A card or relic that grows during a run (Trump's Cornerstone, Permanent Structure) just uses `[SavedProperty]`.

## 4. The character's scenes and assets

The game loads these by name for every character (`presmod.CHARACTER_SCENES`). `new_character.py` copies them from an existing character and swaps in the new name and colour; `build.py` refuses to build if one is missing.

| Resource | Notes |
|---|---|
| `scenes/creature_visuals/<id>.tscn` | Combat body: a `Sprite2D` "Visuals" gets `NCharacterPoses` (idle/attack/cast/hurt paintings); `Bounds`, `CenterPos`, `IntentPos` markers. `NCharacterPoses.SetVariant("dark_", tint)` switches to a second pose set (`dark_combat_idle.png`, ...) for a form taken mid-fight, like Biden's Dark Brandon. A set whose images all have the same height (cut from one pose sheet) is shown at one scale; `FigureRect` gives the painting's place on screen (Biden's lasers start at the eyes) |
| `scenes/merchant/characters/<id>_merchant.tscn` | Shop figure: a `Sprite2D` "CharacterSprite", painting `images/<id>/merchant_pose.png` |
| `scenes/rest_site/characters/<id>_rest_site.tscn` | Rest-site figure, same idea (`rest_site_pose.png`); flipped for co-op seats |
| `scenes/screens/char_select/char_select_bg_<id>.tscn` | The select screen: the painting `images/<id>/char_select_bg.png`, shifted 640 px left, under ember particles |
| `scenes/combat/energy_counters/<id>_energy_counter.tscn` | The energy orb: 5 image layers + the two energy VFX scenes |
| `scenes/vfx/energy/<id>/<id>_energy_vfx_{back,front}.tscn` | Orb glow, recoloured |
| `scenes/vfx/card_trail_<id>.tscn` | Trail behind a played card, recoloured |
| `materials/transitions/<id>_transition_mat.tres` | Screen wipe; uses the mask `images/ui/transitions/<id>_transition.png` |
| `scenes/ui/character_icons/<id>_icon.tscn` | Top-bar icon (copied from Ironclad's by `make_placeholders.py`) |
| `materials/cards/frames/card_frame_<id>_mat.tres` | Card frame colour: the game's HSV shader, from `character.json` `frame_hsv` (rewritten when it changes) |

Every **image** has a fixed path per art kind: `presmod.KIND_OUTPUTS`, listed in [ART_PIPELINE.md](ART_PIPELINE.md).
- `make_placeholders.py` writes a stand-in for every image that doesn't exist yet: every card, relic, power and potion class it finds, the UI pieces, the poses and the orb.
- A new character is playable before its art exists.

**Figure heights:** paintings are trimmed cut-outs of any size. They're placed by their feet and scaled to `IModCharacter.CombatFigureHeight` (290), `MerchantFigureHeight` (330) and `RestSiteFigureHeight` (560). A character class can override these.

## 5. Build

`python scripts/build.py [--install]` runs these stages:
1. **Placeholders** for anything missing.
2. **Character check**: every character's scenes, tables and class exist; its id matches its class. It also prints how many `TODO` texts are left.
3. **Model ID check**: no duplicates, no base-game clashes.
4. **DLL** (`dotnet build`).
5. **Godot import.**
6. **PCK pack**, including the localization merge into `build/loc/`.
7. **`build/dist/`**: the mod, `install.cmd`, `uninstall.cmd`, README (with the version stamped in), and `legacy_ids.txt`.
   `--zip` also writes `build/release/sts2-pres-mod-v<version>.zip` and a Workshop preview image ([PUBLISHING.md](PUBLISHING.md)).

**Texture compression:** Godot imports images lossless by default. `build.py` switches the big paintings to lossy WebP at quality 0.9 (`IMPORT_OVERRIDES`): the card portraits, the select-screen paintings and the transition masks. That took the PCK from 71 MB to 17 MB with no visible difference. Figures stay lossless, because lossy colour under transparent pixels bleeds into their edges, and icons stay lossless because they're small and need crisp edges.

**Install** copies to `mods/pres_mod/` and removes `mods/trump_character/` (the mod's old id).

**Uninstall** starts the game once in `--pres-cleanup` mode. That deletes the run saves and run history that use the mod through the game's own save store, so the Steam Cloud copies go too.

## 6. Tests

`scripts/test.py` launches the game with `--pres-test <mode>` and collects `build/test/<mode>_<time>/`: `report.json`, screenshots, and the `godot.log` excerpt with every error.
- Test saves live in `modded_prestest/` (balance runs in `modded_bal<slot>/`), never in real profiles.
- `-c <id>` picks the character; the default is the first one.

| Mode | What | Time |
|---|---|---|
| `ui` | Character select, run start, card library, a fight with the pose checks and the character's mechanics, shop, rest site | ~3 min |
| `cards [SEED] [relics\|A,B]` | Every card base and upgraded in real fights with key-effect checks; relics, potions, turn powers; all texts rendered | ~15 min (relics only ~1 min) |
| `balance CHARS RUNS [PREFIX] [PARALLEL] [fullheal] [favor=STYLE]` | Heuristic bot runs, e.g. `BIDEN,IRONCLAD,SILENT 9 PFX 9 fullheal`: 9 at once, tiled 3×3 on the main monitor, muted | ~25 min per 54 runs |
| `autoslay [SEED]` | The game's AutoSlay bot plays a full run (god mode) | ~5 min |
| `vfx` | A character's visual effects played one after another in a fight, with a screenshot at each key moment (the character's `ExtraModes["vfx"]`: Biden, Trump) | ~2 min |
| `coop [CLIENT_CHARACTER]` | Two instances side by side play a co-op fight over localhost (the game's own `--fastmp` option, no Steam lobby): the host starts it, each plays its own cards (`CoopTurn` hook; the scripted plays stop once that player's turn has ended, as the game's hand does), both record every player and enemy at the start of each turn and the records must match (desync check), then the rest site and shop | ~4 min |
| a character's own mode | e.g. `deportsweep` (Trump) | varies |
| `cleansaves [ID,...]` | Removes test runs that use the mod or a removed character (through the game, so Steam Cloud doesn't restore them) | ~30 s |

`python scripts/balance_report.py PREFIX` compares balance batches.

**Per-character test hooks.** `Dev/CharacterTests.cs` defines the hooks. A character registers them in `Characters/<Class>/Dev/DevHarness.<Class>.cs`:

```csharp
[CharacterTestKit("BIDEN")]
private static CharacterTests BidenTests() => new CharacterTests { UiChecks = ..., CheckCardEffect = ..., CardValue = ... };
```

Every hook is optional. With none, all modes already work with the shared checks.

| Hook | Used by | Purpose |
|---|---|---|
| `UiChecks` | ui | The character's mechanics in the first fight |
| `ExtraModes` | test.py `<mode>` | Modes only this character has |
| `RelicChecks`, `PotionChecks`, `PowerChecks` | cards | Relic, potion and turn-power checks |
| `ExtraCards`, `AddCardToHand` | cards | Tokens outside the pool, and how to put them in hand |
| `PrepareCardTurn` | cards | Character state before every card (Trump: a 30 Wall) |
| `PrepareCardTurnFor` | cards | The same, knowing the card and whether it's the upgraded play (Biden: base as Sleepy Joe, upgraded as Dark Brandon) |
| `Snapshot`, `CheckCardEffect` | cards | Extra counters in the before/after snapshots; key-effect checks per card |
| `BalanceInit`, `BalanceCombatStart`, `BalanceCombatStats` | balance | Per-fight numbers in `balance.json` |
| `RemovesEnemy`, `CardValue` | balance | Teach the bot the character's non-damage effects |
| `CardValueOverride` | balance | Replace a card's whole value when its printed numbers don't all happen (Biden's Tangents: only the lit line). `DamageValue` and `BlockValue` score the parts the usual way |
| `CoopTurn` | coop | The character's co-op plays in the first turn: (is host, combat). Trump: Coalition Wall on the host, Trickle Down and Slap a Tariff on the client |
| `CoopTurnStart` | coop | Checks at the start of every turn, after both sides recorded their state: (is host, combat, turn). Biden: everyone who nodded off in turn 1 woke as Dark Brandon, with Laser Eyes |

Command-line flags (all `--pres-*`):
- `test`, `out`, `seed`, `character`, `only`, `encounters`, `fullheal`, `favor`, `tile`, `mute`, `savedir`;
- `cleanup` and `cleanup-ids` for the save cleanup.

## 7. Scripts

| Script | What |
|---|---|
| `presmod.py` | Shared config: paths and the per-PC settings (`setting()`: `GAME_DIR`, `COMFY_*`), `mod.json`, `character(id)`, `characters()`, `slug()`, `KIND_OUTPUTS`, `art_outputs()`, `CHARACTER_SCENES`, `missing_files()` |
| `extract_game.py` | Step 1, repeatable: decompile `sts2.dll` into `re/code` (ilspycmd) and unpack the PCK into `re/pck` (GDRE); keeps the previous `re/` for diffing ([GAME_UPDATES.md](GAME_UPDATES.md)) |
| `game_update_check.py` | After a game update: every patch target, member reached by name, Ancient and game file the mod relies on still exists |
| `new_character.py` | Scaffold a new character ([ADDING_A_CHARACTER.md](ADDING_A_CHARACTER.md)) |
| `build.py` | Build (and install) |
| `make_placeholders.py [-c id] [--force]` | Stand-in art for anything missing; run by the build |
| `test.py`, `balance_report.py` | Tests (§6) |
| `analyze_cards.py` | Mine the base game's cards into `build/analysis/` (the benchmarks) |
| `render_design.py`, `render_gallery.py`, `render_compendium.py` `[-c id]` | `cards.json` → card list with validation, local gallery, review page |
| `art_review.py`, `art_recipes.py`, `art_gen.py`, `art_post.py`, `art_train.py` | Art ([ART_PIPELINE.md](ART_PIPELINE.md)) |
| `asset_inventory.py` | Lists a base character's assets (made `docs/reference/ironclad_assets.csv`) |
| `dist/` | Installer, uninstaller and player README, copied into `build/dist/` |

A script that takes `-c/--character` works on one character; without it, it uses the first one (or all, for the build and placeholders).
