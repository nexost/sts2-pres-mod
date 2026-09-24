# sts2-pres-mod release notes

## v1.0.0 (2026-09-24): The Donald

The first release: one playable president, built to feel like a base-game character.

**The Donald**
- 70 HP, 99 Gold. Starts with the Golden Shovel: at the start of your turn, Build 2.
- **The Wall:** Build raises it. Every 10 height is a Section worth 3 Block at the end of your turn. At 10, 25, 45 and 70 height it becomes a fence, a brick wall (+1 draw), a concrete wall (+1 Energy), then the Big Beautiful Wall (end-of-turn damage to all enemies per Section). Stage perks stay for the rest of the fight.
- **Deport:** removes an enemy at or below 25% of its max HP (bosses are immune). A DENIED stamp marks enemies you can Deport right now.
- **Deals:** Pay-Gold cards, Tariffs that pay you when enemies attack, relics that turn Gold into power.
- **Tweets:** a 0-cost token that hits all enemies, with cards that make and boost them.
- 88 cards plus the Tweet token, 26 powers, 9 relics (including the starter's Ancient upgrade, the Diamond Shovel), 3 potions.
- His own dialogue with Neow, the other Ancients and the Architect.
- Full art in the base game's style: every card, relic, potion and power; the character select screen and button; combat, shop and rest-site paintings; the energy orb, map marker and co-op hands.

**Co-op**
- Works with any mix of characters; every player needs the same mod version.
- Coalition Wall (ALL players Build) and Trickle Down (ALL players gain Gold) are co-op-only cards.
- Players are spaced so every Wall stands in its own space.

**Install and remove**
- `install.cmd` finds the game through Steam and replaces the older `trump_character` version.
- `uninstall.cmd` also removes runs and run history that use the mod, locally and from Steam Cloud, with backups.
- Modded play uses separate save profiles; normal saves are never touched.

**Requirements:** Slay the Spire 2 v0.107.1 or later, Windows.

**Known limitations**
- Sounds are borrowed from the Ironclad.
- The combat body is a set of painted poses with simple motion, not a Spine animation.
- No unlock timeline of his own: The Donald is available from the start, and his wins don't advance a timeline (optional work items P8 and P9).
- Not tested alongside other mods yet (planned).
- The download is ~17 MB: card art is stored as high-quality lossy WebP.

**Balance:** a first pass with a balance bot playing The Donald, Ironclad and Silent on the same seeds (about 190 runs in all). He lands between Silent and Ironclad: the best defense in elite and boss fights, slower in short fights.
