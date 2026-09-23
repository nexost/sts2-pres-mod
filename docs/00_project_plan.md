# The Donald: Project Plan and Status

**Start here.** This is the master plan for the Slay the Spire 2 character mod, kept up to date after every step.
If a chat session is lost, this file + [`README.md`](../README.md) + [`03_design.md`](03_design.md) are enough to carry on.

_Last updated: 2026-09-23. Step 3 (design, v2) is done and committed; Step 4 is waiting for the go-ahead._

---

## The brief

Create a new playable character for Slay the Spire 2: a **funny, lighthearted caricature of Donald Trump**, themed around
**deportation and wall building**. It should be well implemented, carefully balanced and super fun to play, with a full card set,
art, UI elements and a special mechanic. **The art must match the base game's style and blend in.**
Any tools may be used, downloaded or created. The work is done step by step, never in one shot.

## Working agreement

- **One step at a time.** At the end of each step, report the deliverables and **stop until the user says "continue"**.
- The user reviews designs on the published **[Compendium page](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak)**, often from their phone through Remote Control.
  Republish it (`python scripts/render_compendium.py`, then publish `docs/design/compendium.html`) whenever the design changes.
- Downloads: list the tool, source and size, and ask before downloading.
- Git: commit only when the user asks. End commits with the `Co-Authored-By: Claude` trailer.
- Never touch real saves: tests run in the separate `modded_trumptest` save folder; back up saves before anything risky.
- Tone: jokes target the **persona** only. Deportation and walls apply to Spire monsters. No jokes about immigrants, ethnic or religious groups, real tragedies, or real people other than him.

## The 9 steps

| # | Step | Status | Deliverable |
|---|---|---|---|
| 1 | Research and feasibility | ✅ Done | [`01_feasibility_report.md`](01_feasibility_report.md) |
| 2 | Test character and build process (+ installer/uninstaller) | ✅ Done (`6f331c9`) | [`02_step2_report.md`](02_step2_report.md) |
| 3 | Design document | ✅ Done (v2) | [`03_design.md`](03_design.md), [`design/card_list.md`](design/card_list.md), [`design/cards.json`](design/cards.json), [Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak) |
| 4 | Core mechanics and starter set (placeholder art) | ⏳ Next | Playable Act 1 with the starter deck |
| 5 | All the content (placeholder art) | ⬜ | Full character, playable start to finish |
| 6 | Balance and playtesting | ⬜ | Tuned version + balance report |
| 7 | Lock the art style | ⬜ | Style samples to approve |
| 8 | Make all the art | ⬜ | Finished visual version |
| 9 | Polish and packaging | ⬜ | Installable release |

### Step 1: Research and feasibility ✅
Set up the workspace and tools (ilspycmd, GDRE Tools, Godot 4.5.1 .NET, Pillow). Decompile the current build and unpack the resource pack.
Trace how a character is connected through the whole game, list every art asset needed, and write the feasibility report and risks.

### Step 2: Test character and build process ✅
- C# mod project, one-command build (`scripts/build.py`), installer and uninstaller (`scripts/dist/`).
- The uninstaller cleans up saves that use the mod, through the game itself, so the Steam Cloud copies go too.
- Test character with placeholder art. Compatibility patches P1–P7.
- In-game test harness: a UI walkthrough and full auto-play runs.
- Result: **a full winning run as The Donald**, with 0 log errors.

### Step 3: Design document ✅ (v2)
- Mined the 5 base characters for balance numbers.
- Designed 88 cards plus the Tweet token, 9 relics and 3 potions, against those benchmarks and the reward and shop rules.
- v2 after review:
  - The Wall is a construction project.
  - Deport uses a 25% HP line.
  - Every play style can win on its own and scales without limit.
  - More card flow that doesn't cost gold.

### Step 4: Core mechanics and starter set (placeholder art) ⏳ next
- **Wall system:**
  - height, Sections, and end-of-turn Section Block;
  - stages at 10/25/45/70 whose perks survive Demolition;
  - the on-screen Wall display (stage, height, Sections, progress to the next stage);
  - hooks for height carried between fights (Permanent Structure, Cornerstone).
- **Deport system:**
  - a Deport line per player (25% base, with modifiers);
  - removal through the game's escape system, with bosses immune;
  - the line marker and DENIED stamp on enemy HP bars;
  - **a test that Deports every enemy in every encounter** to find scripted fights that break.
- **Tariff** (debuff) and **Pay X Gold** (playability check plus a gold-cost badge on cards).
- **Tweet** token (Token rarity).
- Keyword tooltips: Build, Wall, Section, Deport, Tariff, Pay Gold, Tweet.
- **Starter kit:** Strike, Defend, Build the Wall, Deport, the Golden Shovel, and the Touch of Orobas → Diamond Shovel mapping.
- The real character stats (70 HP, 99 gold) and the gold card frame, checked side by side with the Regent's orange.
- Replace the Step 2 test cards (Deport prototype, fillers) as the real cards arrive; the filler cards stay until Step 5 fills the pool.
- **Deliverable:** a playable Act 1 with the starter deck, where you can feel the Wall and Deport, plus automated tests and screenshots.

### Step 5: All the content (placeholder art)
- Every card with its upgrades, all relics and potions, one power per Power card, and all the text.
- Ancient dialogue for The Donald (Neow, Darv and the others; optional but good for comedy).
- Automated tests that play every card through the developer console.
- **Deliverable:** a complete character, playable start to finish.

### Step 6: Balance and playtesting
- A smarter auto-play bot: the game's own bot picks cards at random, so write a heuristic player that uses the real game logic.
- Batches of automated runs compared with the base characters: win rate, which cards get picked and how they perform.
- The user playtests, and the numbers are tuned; repeat as needed.
- Risks to measure are listed in [`03_design.md` §9](03_design.md).
- **Deliverable:** a tuned version plus a balance report.

### Step 7: Lock the art style
- Study the game's art: palette, brushwork, lighting and framing.
- Set up a local image model on the RTX 4090, trained on the game's own art.
- Make test pieces (3 card arts, the character portrait, 1 relic icon) for the user to approve before mass production.
- The art direction for each card is in `cards.json` (`art` field).

### Step 8: Make all the art
- All card art, the character select screen, relic, potion and power icons, the energy orb, and the map, shop and rest-site art.
- **The four Wall stage visuals** and the Deport stamp.
- The combat character as a Spine rig, generated with code (idle, attack, cast, hurt, die); a plain-sprite fallback if needed.
- Sounds: reuse the game's own.
- **Deliverable:** the finished visual version.

### Step 9: Polish and packaging
- Co-op check (may need a second player), a test with other mods installed, and a full crash and QA pass.
- Note the supported game version; write install instructions and release notes.
- Check Steam Workshop and Nexus rules on real-person and political content before any public release.

## Decision log

| Date | Decision |
|---|---|
| 2026-09-23 | Start fresh from the current build (v0.107.1). The old April `sts2_decompiled_code` and `sts2_extracted` folders in the game directory are ignored. |
| 2026-09-23 | Use the game's **official mod loader** (`mods/<id>/manifest.json` + DLL + PCK) and Harmony. Mod id `trump_character`, character class `Trump` (ID `TRUMP`). |
| 2026-09-23 | The user asked for an **uninstaller** too. It uses the game's own save system to delete saves that use the mod, so Steam Cloud copies go as well. |
| 2026-09-23 | Steps 1–2 committed (`6f331c9`). |
| 2026-09-23 | Design v1 published. **Confirmed:** name "The Donald", metallic gold card colour, 70 HP, elites Deportable and bosses immune, and edgier names kept (Mass Deportation, Witch Hunt, Fake News, Fire and Fury, Chapter 11). |
| 2026-09-23 | **Design v2** after review. The Wall no longer soaks damage; it's a construction project with Sections and stages, because it was too close to Necrobinder's Osty. Deport uses a 25% HP line, because depending on the Wall forced a split build. More energy and draw that doesn't cost gold. |
| 2026-09-23 | Design principles from the user: **each play style must win on its own; scaling must have no ceiling** (like Osty's HP); card flow must not depend on gold. The user loves the Deals/gold play style. |
| 2026-09-23 | Relic and potion counts checked against the game at full unlock: 8 character relics + the starter upgrade, and 3 potions, per character. The large numbers are shared pools. |

## Open items

- P8 (stats screen section) and P9 (own unlock timeline) are optional (see the Step 1 report).
- Watch whether Steam Cloud keeps small `modded_trumptest` test files (`prefs`, `progress`); they're harmless.
- `.claude/launch.json` in the game folder serves `docs/design` on port 8765 for local page previews.

## Resuming in a new session

1. Open `C:\Users\exeet\sts2-trump-mod`. Read this file, [`README.md`](../README.md) (layout and commands) and [`03_design.md`](03_design.md).
2. Claude's memory for this project is in `C:\Users\exeet\.claude\projects\C--Program-Files--x86--Steam-steamapps-common-Slay-the-Spire-2\memory\`. It holds the plan summary and the "stop after each step" rule.
3. Check that the game version is still **v0.107.1** (`release_info.json` in the game folder). If it changed, re-decompile (`re/`) and diff before building.
4. `python scripts/build.py --install`, then `python scripts/test.py ui` to confirm everything still works.
5. Carry on with the next ⏳ step above, and stop at its end.
