# Step 1: Feasibility Report

**Game:** Slay the Spire 2 v0.107.1 (Early Access, build 59260271, June 18 2026)
**Verdict: feasible, go.** Confidence is high for the code and content, and medium for the art.

> **Status (updated after Step 3):** Step 1 is complete. Since this report:
> - Step 2 built P1–P5 and found two more required patches, P6 and P7 (§5).
> - Step 3 v2 changed the Wall and Deport rules (§4). The current design is in [`03_design.md`](03_design.md).
>
> The overall plan and status are in [`00_project_plan.md`](00_project_plan.md).

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
| **Wall** (v2: construction project; every 10 height is a Section that gives Block each turn; stage perks at 10/25/45/70) | A power with a counter; end-of-turn hooks (`BeforeSideTurnEnd`) for Section Block and damage; energy and draw modifier hooks (`ModifyEnergyGain`, hand-draw hooks) for the stage perks. *(v1 used `ShouldClearBlock` and damage-layer hooks; replaced in Step 3 v2)* | ✅ Easy–Medium (display is custom UI) |
| **Deport** (v2: enemy at or below 25% of max HP; the line can be raised) | `CreatureCmd.Escape(creature)` already exists (fleeing gremlins use it). **Escaped enemies automatically cut that fight's gold reward proportionally** (`EncounterModel.CalculateGoldProportion`), a ready-made trade-off for a gold-based play style. Verified working in Step 2 | ✅ Easy, with a built-in balance cost |
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
| P6 | `ProgressSaveManager.ObtainCharUnlockEpoch` *(found in Step 2)* | After each act boss the game unlocks `TRUMP2_EPOCH`, which doesn't exist → exception → no rewards | Skip for mod characters | **Required (crash)** |
| P7 | `TheArchitect` final event *(found in Step 2)* | No dialogue for new characters and no fallback, so Proceed crashes and **a winning run can't finish** | Add our own Architect dialogues | **Required (crash)** |
| P8 | `NGeneralStatsGrid` | Stats screen lists 5 characters | Add a section | Nice to have |
| P9 | Timeline / epochs | Our character is **unlocked by default** (the unlock filter only removes the 4 locked originals) | Optional: our own unlock timeline entries later | Optional |

**Status:** P1–P7 are built and tested (Step 2, full winning run by the auto-play bot). P8 and P9 are open.
Touch of Orobas → Diamond Shovel (the starter relic upgrade) is also needed, in Step 4.

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
| **Wall stages** *(added in Step 3 v2)* | 4 stage visuals | — | Fence → brick → concrete → gold, shown in front of the character, with height and Section count |
| **Deport UI** *(added in Step 3 v2)* | line marker + stamp | — | A mark on enemy HP bars at the Deport line; a red DENIED stamp when under it |

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

## 9. Step 2 (done)

Step 2 built the C# project, the one-command build, the installer and uninstaller, and the test character with P1–P7,
and an in-game test harness. The report is in [`02_step2_report.md`](02_step2_report.md).
