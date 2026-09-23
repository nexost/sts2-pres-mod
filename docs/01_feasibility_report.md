# Step 1: Feasibility Report

**Game:** Slay the Spire 2 v0.107.1 (Early Access, build 59260271, June 18 2026)
**Verdict: feasible, go.** Confidence is high for the code and content, and medium for the art.

---

## 1. What the game is built with

| Part | Finding |
|---|---|
| Engine | Godot **4.5.1** (.NET build), resource pack format v3 |
| Code | C# on .NET 9, all game logic in `sts2.dll` (9 MB) |
| Content | ~27,000 files in `SlayTheSpire2.pck` (1.9 GB) |
| Character animation | **Spine 4.2.43** skeletons (`.skel` + `.atlas` + png pages), via the spine-godot plugin |
| Audio | FMOD Studio banks |
| Text / translations | Flat JSON files per language (`localization/eng/cards.json` …), text formatting via SmartFormat, colour tags like `[gold]` |
| Patching library | Harmony (`0Harmony.dll`) **ships with the game** |

The unpack was clean: GDRE converted 3,949 resources with 0 failures and rebuilt the game as a complete Godot project that opens in Godot 4.5.1. So we can inspect and copy the game's real scenes instead of guessing at their structure.

## 2. How mods work (official support)

- Mods live in `<game>/mods/<anything>/`: a `manifest.json` plus `<id>.dll` and/or `<id>.pck`, found by searching subfolders.
- Manifest fields: `id, name, author, description, version, has_dll, has_pck, dependencies[{id,min_version}], affects_gameplay, min_game_version`.
- The DLL is loaded into the game's own .NET context. On load the game calls a static method marked with `[ModInitializer("Method")]`, or runs `Harmony.PatchAll` if there isn't one.
- The PCK is mounted over `res://`, so **our files can sit exactly where the game looks for a character's assets.**
- **Mod text files are supported:** `res://<modId>/localization/<lang>/<file>.json` is merged into the game's text.
- **Game content in a mod is found automatically:** every class in a mod that inherits from `AbstractModel` (cards, relics, powers, potions, pools, characters) is created and given an ID when the game starts.
- When a mod is loaded, the **developer console's debug commands turn on automatically** (`card`, `fight`, `relic`, `power`, `gold`, `godmode`, `win`, `kill`, `energy`, `draw`, `act`, `room`, `event` …). That makes it an ideal test harness.
- Co-op: the lobby compares the list of mods that "affect gameplay". Every player needs the mod, and the game enforces that itself.
- The first time mods are present the player has to accept a one-time warning. Metrics upload is disabled while modded.

## 3. How a character is built

A character is made of about 5 small classes plus files placed at naming-convention paths. The ID comes from the class name: class `Trump` → ID `TRUMP` → paths ending in `trump`.

```
CharacterModel   → HP, gold, energy, deck, starter relic, colours, animator, 3 pools
CardPoolModel    → list of cards, card frame colour (a hue-shift material), energy icon colour
RelicPoolModel   → 8 character relics (+ starter relic)
PotionPoolModel  → 3 character potions
CardModel ×88    → ~20 lines each (cost, type, rarity, target, values, OnPlay, OnUpgrade)
PowerModel ×N    → buffs/debuffs that react to game events (172 hook methods available)
```

Reference numbers taken from the existing characters, which we'll match:

| | Ironclad | Silent | Defect | Regent | Necrobinder |
|---|---|---|---|---|---|
| Cards | 87 | 88 | 88 | 88 | 88 |
| Character relics | 8 | 8 | 8 | 8 | 8 |
| Character potions | 3 | 3 | 3 | 3 | 3 |

Ironclad's mix: 3 Basic, 20 Common, 36 Uncommon, 26 Rare, 2 Ancient; 37 Attacks, 30 Skills, 20 Powers.

## 4. What the planned mechanics can build on

| Idea | Existing engine support | Verdict |
|---|---|---|
| **Wall**: block that stays between turns | `ShouldClearBlock` hook (used by Barricade), plus `BeforeDamageReceived`, `ModifyDamage*` and `AfterBlockBroken` hooks for a separate damage layer | ✅ Easy |
| **Deport**: remove an enemy from the fight | `CreatureCmd.Escape(creature)` already exists (fleeing gremlins use it). **Escaped enemies automatically cut that fight's gold reward proportionally** (`EncounterModel.CalculateGoldProportion`), a ready-made trade-off for a gold-based play style | ✅ Easy, with a built-in balance cost |
| **Gold / deals play style** | `AfterGoldGained`, `ModifyGoldGained` hooks, `gold` command | ✅ Easy |
| Custom resource counter on screen | Precedent: Regent's Star counter (`ShouldAlwaysShowStarCounter`); powers also show a number on the character | ✅ Medium (custom UI node) |
| Character-specific Ancient dialogue | Keyed `NEOW.talk.TRUMP.*`, falls back to `ANY` lines when missing | ✅ Optional, good for comedy |

## 5. Required patches (the game's hardcoded spots)

The game hardcodes the five characters in 73 places. Nearly all of them are harmless or fall back safely. These need handling:

| # | Where | Problem | Fix | Priority |
|---|---|---|---|---|
| P1 | `ModelDb.AllCharacters` | Fixed list of 5, so our character never appears | Harmony postfix appends ours. The card, relic and potion pool lists are built from it, so they follow automatically | **Required** |
| P2 | `ProgressSaveManager.CheckFifteenElitesDefeatedEpoch` / `…BossesDefeatedEpoch` | **Throws `ArgumentOutOfRangeException` for unknown characters, so a crash when you beat an elite or boss** | Prefix that skips for our character | **Required (crash)** |
| P3 | `NCardLibrary.OnSubmenuOpened` | `_cardPoolFilters[character]`, so **a crash when opening the card library mid-run** | Add our own filter tab (also gives us our tab in the card library) | **Required (crash)** |
| P4 | `CharacterModel.AttackSfx/CastSfx/DeathSfx` | Not overridable, and they point to FMOD events that don't exist | Postfix to point them at existing sounds | Required (quality) |
| P5 | Mod node scripts | Godot doesn't know the C# scripts inside a mod DLL | Call `ScriptManagerBridge.LookupScriptsInAssembly(ourAssembly)` in the initializer (public API, same call the game uses) | Required |
| P6 | `NGeneralStatsGrid` | Stats screen lists 5 characters | Add a section | Nice to have |
| P7 | Timeline / epochs | Our character is **unlocked by default** (the unlock filter only removes the 4 locked originals) | Optional: our own unlock timeline entries later | Optional |

Harmless fallbacks we can leave alone: Yummy Cookie relic art (falls back to Ironclad's), run-history hit sounds (empty list), Necrobinder/Regent-specific checks.

**ID collision rule:** content IDs come from class names alone. A mod card class called `Barricade` would silently **replace** the game's Barricade. The build will include a check that fails on any name clash.

## 6. Asset inventory: what we have to make

Sizes measured from the game's own files (full list in `docs/ironclad_assets.csv`).

| Asset | Count | Size | Notes |
|---|---|---|---|
| Card portraits | ~88 | 1000×760 | The biggest art job |
| Card frame colour | 1 | — | Hue/saturation/value material, no new art |
| Relic icons | 9 (+ outlines) | 256×256 | 8 pool relics + starter |
| Potion icons | 3 | 256×256 | |
| Power icons | ~10–15 | 256×256 | Wall, Deport marks, Tariffs, etc. |
| Energy orb | 5 layers + 2 VFX scenes | 256×256 | + 24×24 energy icon shown in card text |
| Combat character | 1 Spine rig | ~1000×269 atlas pages | Animations: `idle_loop, attack, cast, hurt, die, relaxed_loop` |
| Character select background | 1 animated scene | 3716×2428 atlas | + button portrait 132×195 (+ locked version) |
| Screen transition | 1 | 2560×1200 | + transition material |
| Merchant pose | 1 Spine scene | ~1652×657 atlas | |
| Rest site pose | 1 Spine scene | ~489×526 atlas | |
| Top bar icon | 2 | 88×88 | normal + outline |
| Map marker | 1 | 49×64 | |
| Co-op hands | 4 | 422×1200 | rock / paper / scissors / point |
| Card trail + attack VFX | 2 scenes | — | can adapt existing ones |
| Epoch portraits | optional | ~810×500 | only if we add timeline entries |

The combat character **doesn't have to be Spine**: 8 game creatures (and a fallback scene) use plain sprites. That gives us a placeholder route and a fallback if generating the Spine rig goes badly.

## 7. Tools (installed)

| Tool | Status |
|---|---|
| ilspycmd 9.1 | already installed; decompiled 3,425 files in 11 s |
| GDRE Tools 2.6.4 | `tools/gdre`; full project recovered in 43 s |
| Godot 4.5.1 .NET | `tools/godot` (builds our PCK in Step 2) |
| .NET SDK 9.0.312, Python 3.11 + Pillow 12.3, git | installed |
| RTX 4090 (24 GB) | enough for local image models and style training (Step 7) |

Workspace: `C:\Users\exeet\sts2-trump-mod` (git repo; `re/` and `tools/` are git-ignored so no game files get committed).

## 8. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| **An Early Access patch breaks the mod** | High over months | Few Harmony patches, target members by name, `min_game_version` set, one-command rebuild; re-run the decompile and compare after each patch |
| **Art doesn't match the game's style** | Medium | Step 7 style-approval checkpoint; local model trained on the game's own art; exact sizes and palettes taken from the files |
| Generating a Spine rig with code | Medium | Spine's JSON format is documented and spine-godot loads it. Fallback: plain-sprite character animated with Godot tweens |
| **The game's auto-play bot plays randomly** (and is disabled in release builds) | Certain | For balance in Step 6: turn it back on through our mod and write a simple heuristic player that uses the real game logic. Your playtesting stays the final judge |
| Co-op testing needs 2 players | Medium | Single-player first; co-op checked in Step 9 (may need your help with a second machine/account) |
| Removing the mod mid-run breaks that run's save | Certain | Documented in the install notes (every content mod has this) |
| New sounds need FMOD Studio | Low | Reuse the game's existing sound events |
| Publishing rules for real-person / political content | — | Check Workshop/Nexus rules before any public release |

## 9. Next: Step 2 (test character and build process)

1. Set up the C# project (`net9.0`, referencing `sts2.dll`, `GodotSharp.dll`, `0Harmony.dll`) plus a Godot project for the PCK.
2. A one-command build script: compile the DLL, pack the PCK, write the manifest, copy into `mods/trump_character/`.
3. A test character with Strike, Defend, a starter relic and borrowed Ironclad visuals, plus patches P1–P5.
4. Checks: shows on character select → start a run → fight → rewards → save and quit → continue → elite/boss kill (P2) → card library (P3) → other characters still fine.
