# Adding a character

The playbook for the next president (Joe Biden is planned). The Donald took eight steps to build, and most of that time
went into things that are now done once for every character:
- the game patches;
- sprite bodies without Spine;
- the test harness and the balance bot;
- the build, the installer and the save cleanup;
- the art recipes and the review tool.

A new character starts **playable in minutes**. All the remaining work is the character itself: its design, its code,
its jokes and its art.

Before starting on a new PC: [SETUP.md](SETUP.md) (tools, `re/`, paths, a first passing `test.py ui`; §5 for art).
How the pieces fit together: [FRAMEWORK.md](FRAMEWORK.md). Art: [ART_PIPELINE.md](ART_PIPELINE.md).
The worked example throughout is The Donald: [`characters/trump/`](../characters/trump/) and
[`mod/pres_mod/src/Characters/Trump/`](../mod/pres_mod/src/Characters/Trump/).

## The phases at a glance

| Phase | What | Trump's step | Tools that do the heavy lifting | Machine time |
|---|---|---|---|---|
| C1 | Scaffold: a playable character with placeholders | 2 | `new_character.py`, `build.py`, `test.py ui` | ~5 min |
| C2 | Design: mechanics, cards, relics, potions | 3 | `render_design.py` (benchmark checks), `render_gallery.py`, `render_compendium.py` | – |
| C3 | Signature mechanics and starter set | 4 | Framework patches, the test kit hooks | ui test ~3 min |
| C4 | All the content and text | 5 | `test.py cards` | ~15 min per full pass |
| C5 | Balance | 6 | `test.py balance`, `balance_report.py` | ~25 min per 54 runs |
| C6 | Art | 7–8 | `art_review.py` (recipes already locked) | ~1 h per full pass |
| C7 | Final checks and docs | – | `test.py ui/cards/autoslay` | ~25 min |

Stop after each phase for review, as with Trump (the working agreement in [00_project_plan.md](00_project_plan.md)).

---

## C1. Scaffold

**Decide first:**

| Choice | Notes |
|---|---|
| **Class name** (`Biden`) and **id** (`biden`) | The id is the class name in snake_case, and must be: the game names every asset after it. Unique across the base game and the mod |
| **Display name** (`"Sleepy Joe"`) and full name for art (`"Joe Biden"`) | The full name goes in art prompts (that's what gives the likeness) |
| **Primary colour** | Card frame, orb, trail, text. Pick one far from the 5 base characters and from existing mod characters (Trump: mustard gold `F2B92E`) |
| Gender | Pronouns in the game's texts (`--gender masculine\|feminine\|neutral`) |
| HP, Gold | Base game: 70–80 HP, 99 Gold. Final numbers come from the design |
| Starter relic class and name | A stub is made; its real effect comes from the design |
| Sounds | Borrowed from a base character (`--sfx`, default ironclad) until the mod has its own |

**Run:**

```bash
python scripts/new_character.py biden --class Biden --name "Sleepy Joe" --full-name "Joe Biden" --primary 4A55E6 --color-word navy --starter AviatorShades --starter-name "Aviator Shades"
python scripts/build.py --install
python scripts/test.py ui -c biden
```

Add `--dry-run` first to see the file list. Other options: `--secondary`, `--accent`, `--text`, `--color-word`, `--hp`, `--gold`, `--sfx`, `--from` (the script's docstring lists them).

**What you get** (36 files plus about 40 placeholder images; nothing existing is touched):
- `characters/biden/`:
  - `character.json`;
  - `design/`: a starter `cards.json`, a `design.md` skeleton, and a copy of Trump's compendium template;
  - `art/art_assets.json`: the 19 standard art items with generic prompts;
  - `localization/eng/`: all 9 tables, including every Ancient and Architect line key, as `TODO` placeholders.
- `mod/pres_mod/src/Characters/Biden/`:
  - the character class and its pools;
  - Strike and Defend;
  - **5 stub cards** (Jab, One-Two, Brace, Regroup, Resolve), so card rewards (3 different cards) and the shop (Attacks, Skills and a Power) work;
  - the stub starter relic (6 Block; Touch of Orobas gives Circlet);
  - an empty test kit.
- **Scenes:** combat body, shop, rest site, character select, energy orb and its VFX, card trail, transition. They're copied from Trump's, with names replaced and every saturated colour re-hued to the primary.
- **Placeholder art** for everything, generated card list and gallery.

**Check:** `build.py` lists the character and how many `TODO` texts remain. The ui test must PASS: select screen, run start, card library, the 4 poses, shop and rest site. The screenshots are in `build/test/ui_*/shots/`.

**To undo a scaffold:**
1. Delete `characters/<id>/`, `mod/pres_mod/src/Characters/<Class>/` and the files with `<id>` in their name under `mod/scenes`, `mod/materials` and `mod/images`.
2. Rebuild.
3. Run `python scripts/test.py cleansaves <ID>`. The test saves keep the removed character's runs otherwise, and the next test fails on "model not found".

## C2. Design

Write `characters/<id>/design/design.md` (the skeleton has the sections Trump's design used) and `design/cards.json`.

- **Numbers:** design against [`docs/reference/base_game_benchmarks.md`](reference/base_game_benchmarks.md), mined from the 5 base characters.
  - Refresh it with `python scripts/analyze_cards.py` after a game update.
  - `python scripts/render_design.py -c biden` writes the card tables and checks counts per rarity and type against the reward and shop rules.
- **Principles the user set for Trump, which apply to every character:**
  - each play style must win on its own;
  - scaling must have no ceiling;
  - card flow (energy, draw) must not depend on the gimmick resource;
  - no overlap with a base character's signature (the Wall v1 was too close to Necrobinder's Osty), an enemy's mechanic, or another mod character's (Biden's design.md §10 checks each look-alike);
  - the character's traits play out on the character, not the enemies (Biden's sleepiness is his own meter, not a way to put monsters to sleep).
- **Counts** (as the base game at full unlock):
  - about 88 cards;
  - 8 character relics plus the starter's Ancient upgrade;
  - 3 potions (common, uncommon, rare).
- **Play styles** go in `character.json` `archetypes`; each card's `arch` uses them. The balance bot's `favor=STYLE` and the art's per-style references and backgrounds use them too.
- **Tone:** the jokes target the persona only. Mechanics aimed at people are aimed at Spire monsters. Off limits: other real people, any ethnic or religious group, immigrants, real tragedies, real victims.
- **Review page:** rewrite `design/compendium_template.html`. It is Trump's copy, whose prose explains his mechanics. Then:
  1. Run `python scripts/render_compendium.py -c biden`.
  2. Publish `design/compendium.html` for the user.
  3. Republish whenever the design changes.

## C3. Signature mechanics and starter set

**Where the code goes:** `mod/pres_mod/src/Characters/<Class>/`, namespace `PresMod.Characters.<Class>`. Trump's layout, which is worth copying:

| Folder | Trump's | Pattern |
|---|---|---|
| `Mechanics/` | `WallCmd`, `DeportCmd`, `GoldCmd`, `WallRules`, `Hooks.cs` | A static command class per resource (the only code that changes it); small interfaces in `Hooks.cs` that cards, powers and relics implement to modify it (`IBuildModifier`, `IAfterDeport`, ...) |
| `Powers/` | `WallPower`, `TariffPower`, one power per Power card | Resources shown on the creature are powers; turn effects are power hooks |
| `<Class>HoverTips.cs` | Build, Deport, Pay Gold, Tweet | Keyword tooltips; text in `static_hover_tips.json` as `<ID>_<KEYWORD>.title/.description` |
| `Nodes/` + `Patches/CombatUiPatch.cs` | the Wall display | Extra combat UI: a node added to every combat room by a patch |
| `Cards/` | `CardVars.cs`, `PayGoldCard` base class | Shared dynamic-var names and base classes for card families |

Things the Framework already does:
- **The starter's Ancient version:** the starter relic implements `IUpgradableStarterRelic`. Trump: Golden Shovel → Diamond Shovel. Without it, Touch of Orobas gives Circlet.
- **Cards that can't target some enemies:** `ITargetFilter` (Trump's You're Fired! skips immune bosses).
- **Values that must survive save and quit:** `[SavedProperty]` on the model.
- **Game VFX** can be reused by path (`vfx/vfx_attack_blunt`, dust, rubble), or through their node classes (Biden's Laser Eyes are the Defect's `NHyperbeamVfx`, tinted).
- **A form taken mid-fight:** `NCharacterPoses.SetVariant(prefix, tint)` swaps in a second pose set (Biden's Dark Brandon).
- **Card text that changes in hand:** override `AddExtraArgsToDescription` to pass values into the card's text; hand cards redraw after every action (Biden's `TangentCard`).

**Tests:** put the mechanic's checks in `Dev/DevHarness.<Class>.cs` → `UiChecks`. Trump's `TrumpMechanicsChecks` is the model: set up a state with console commands, play a card, assert, screenshot. Then run `test.py ui -c <id>`.

Replace the scaffold's starter relic and the starting deck in `<Class>.cs` with the designed ones.

## C4. All the content and text

- **Every card:** a class in `Cards/`, listed in `Pools/<Class>CardPool.cs`, plus the texts and an entry in `cards.json`.
- **Remove the 5 stub cards** once real commons, uncommons and a power exist: their classes, pool entries, `cards.json` entries and text.
- **Relics and potions:** list them in their pools. The starter and its upgrade are `Starter` rarity, and never drop.
- **Every relic, power and potion** also needs an entry in `art/art_assets.json`, or the review tool won't list it.
- **Text formats** (the game's own; copy from `re/pck/localization/eng/` when in doubt):
  - `CARD_ID.title` / `.description`, with `{Damage:diff()}`, `{Block:diff()}`, `{Cards:plural:card|cards}`;
  - colour tags `[gold]keyword[/gold]` and `[blue]number[/blue]`;
  - `\n` for line breaks.
  - Relics also have `.flavor`; powers have `.description` and `.smartDescription`.
- **The rest of the text:**
  - `characters.json`: select screen text, pronouns, gold and event lines, co-op banter.
  - `events.json`: the Colorful Philosophers option.
  - `ancients.json`: every Ancient's lines. The keys are already there; the patterns are in `Framework/Patches/AncientDialoguePatch.cs`, and the Architect's lines are required for a win to finish.
  - The build counts the remaining `TODO`s.
- **Writing 80+ texts:** a small one-off script that fills the tables from `cards.json` keeps them consistent. The Donald's are kept as examples in `characters/trump/tools/` (see its README).
- **Tests:** `python scripts/test.py cards PRESTEST1 -c <id>` plays every card base and upgraded, checks key effects (`CheckCardEffect`), relics, potions and powers (`RelicChecks`, ...), and renders every text.
  - `cards SEED A,B` reruns a few cards.
  - `cards SEED relics` runs only the relic and potion checks.
- **Full run:** `python scripts/test.py autoslay SEED -c <id>` must end in a win, with 0 mod errors in the log.

## C5. Balance

```bash
python scripts/test.py balance BIDEN,IRONCLAD,SILENT 18 BIDEN1 9 fullheal
python scripts/balance_report.py BIDEN1
```

- The bot plays the real game logic 9 games at a time: tiled 3×3 on the main monitor, muted, on one shared queue.
- It values damage and block by itself. Teach it the character's other effects with the `CardValue` hook (Trump values Build, Tariff and Gold paid), and `RemovesEnemy` if something removes enemies without killing them.
- Add per-fight numbers with `BalanceCombatStats`; they show up in the report.
- `favor=STYLE` makes the bot prefer one play style's cards, to check that each style wins on its own.
- Ask the user before runs over 30 minutes.

## C6. Art

1. **Fill in `character.json` → `art`** (fields explained in [ART_PIPELINE.md](ART_PIPELINE.md) §2):
   - `persona` and `persona_sentence`;
   - `card_refs` and `card_backgrounds` per play style;
   - `energy_tint` (the scaffold derives it from the primary colour);
   - `sleeve`.
2. **Give `art_assets.json` the character's jokes:**
   - the select screen scene, props in the poses, the map marker, the transition;
   - the character's own art items with their `outputs` (like Trump's Wall stages).
3. **Run `python scripts/art_review.py -c biden`:**
   - "Generate missing" per tab at **Standard**;
   - review;
   - regenerate at High where it matters (select screen, button, poses).
4. `python scripts/build.py --install`, then `python scripts/test.py ui -c biden`, and look at the screenshots.

Things to know:
- The select painting shows only its right 3/4.
- Figures face right, whole body, no furniture.
- The button background is one flat colour the base characters don't use.
- Keep card subjects in the upper middle.

## C7. Final checks and docs

- `test.py ui`, `test.py cards`, `test.py autoslay` all PASS for the new character, **and for the other characters** (their shared code may have moved).
- **Co-op:** `test.py coop -c <id>` (two of the new character) and `test.py coop -c <id> IRONCLAD` (a mixed party) PASS. Give the kit a `CoopTurn` if the character has cards that affect allies. Two things to check on the screenshots: anything drawn beside the character must fit the co-op line-up (Trump's Walls needed `WallSpacingPatch`), and co-op-only cards (`mp` in cards.json) must play.
- The release steps are in [PUBLISHING.md](PUBLISHING.md).
- Write the character's reports in `characters/<id>/docs/`; update [00_project_plan.md](00_project_plan.md) and the README's character table.
- Commit when the user asks.

---

## Pitfalls already paid for

| Pitfall | What happens | Guard |
|---|---|---|
| Id isn't the class name in snake_case | Class `SmokeTest` is `SMOKE_TEST` to the game, so nothing named `smoketest` loads: missing select screen, crash on run start | `new_character.py` and `build.py` refuse it |
| Two classes with the same name (any character, or the base game) | One model silently replaces the other | `build.py` ID check; `ModEntry.CheckModelIdCollisions` |
| `Characters.Biden.X` inside `PresMod.Characters.Biden` | Resolves to the namespace, compile error | Write `Biden.X` |
| Only Basic cards in the pool | Card rewards and the shop throw ("Can't generate valid rarity for merchant card type Attack") | The 5 stub cards |
| No Architect lines | The final event has no dialogue and a win can't finish | The scaffold writes every key |
| A character removed or renamed | Its runs in the test saves break the next test ("Model id=CHARACTER.X not found"); deleting the files by hand doesn't help, because Steam Cloud restores them | `test.py cleansaves <ID>` |
| God mode in card tests | Hides damage and HP-loss effects | The cards test plays without it |
| Style reference at full strength | Muddy, smeared art | Presets Standard and up |
| Figures facing left or cut by the frame | They look wrong in combat and in the shop | Recipe prompts; check the ui screenshots |
| Shell heredocs with `\n` in Python or C# | Some tools turn `\\n` into a real newline | Write code files with an editor or a script file |
