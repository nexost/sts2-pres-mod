# sts2-pres-mod release notes

## v1.1.0 (2026-09-25): Sleepy Joe

A second playable president, and a round of visual effects for The Donald. Old saves and runs still load.

**Sleepy Joe (Joe Biden)**
- 70 HP, 99 Gold. Starts with the Aviator Shades: at the end of your turn, Doze 3; when you nod off, gain 8 Block.
- **Drowsy:** Doze builds it up. At 10 he nods off and his turn ends on the spot.
- **Dark Brandon:** after a nap he wakes up as Dark Brandon, firing Laser Eyes at every enemy for his Drowsy. Dark Brandon lasts one turn, then he's Sleepy Joe again. Wake Up cards skip the nap.
- **Tangents:** cards with 2 or 3 lines. Only the lit line happens, and it moves on each time you play another card; as Dark Brandon, every line happens. A Tangent plays as what its lit line does: you aim it only when that line hits one enemy, and on a Block or draw line it's a Skill.
- Four play styles: Dark Brandon, Power Nap, Tangents and General.
- 88 cards (one co-op only: Reach Across the Aisle), 24 powers, 9 relics (including the starter's Ancient upgrade, the Dark Aviators), 3 potions.
- His own dialogue with Neow, the other Ancients and the Architect.
- Full art in the base game's style: every card, relic, potion and power; the character select screen and button; combat poses for both forms (sleepy and Dark Brandon), shop and rest-site paintings; the energy orb, map marker and co-op hands.
- His own visual effects: the red Laser Eyes beam, Z's as he dozes and a dusk that closes in near the nod-off line, a Dark Brandon glow, rambling speech bubbles, and card effects from a snore shockwave to trains, limos and an ice cream truck driving through the fight.

**The Donald**
- New visual effects: coins fly in and out with his Gold, Tariffs pop a coin over the enemy, Tweets get speech bubbles, the Wall lands harder at each stage and bursts gold, Deports send enemies flying. Card effects for You're Fired!, Fire and Fury, Make It Rain, Wrecking Ball, the golf cards, his potions and shovels.
- No rules changes.

**Co-op**
- Sleepy Joe works in any party. Nodding off ends only his own turn; the other players play on.
- Every player needs v1.1.0.

**Requirements:** Slay the Spire 2 v0.107.1 or later, Windows.

**Known limitations**
- Sleepy Joe's sounds are borrowed from the Ironclad.
- Like The Donald, his combat body is a set of painted poses with simple motion, not a Spine animation.
- No unlock timeline of their own: both characters are available from the start.
- Not tested alongside other mods yet.

**Balance:** a balance bot played Sleepy Joe, Ironclad and Silent on the same seeds, over two seed sets (about 35 runs each). He lands level with Ironclad, above Silent. He's strongest in long fights, where the Dark Brandon cycle comes round, and weakest in short, hard races.

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
