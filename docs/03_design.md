# Step 3: Character Design, "The Donald" (v2)

A lighthearted caricature built around three ideas: **build a big beautiful wall**, **deport the Spire's riff-raff**, and
**make deals**. Everything below is designed against numbers mined from the five base characters ([`design/base_game_benchmarks.md`](design/base_game_benchmarks.md)).

- Full card list with every number: [`design/card_list.md`](design/card_list.md)
- Source of truth for implementation: [`design/cards.json`](design/cards.json)
- Review page: [The Donald Compendium](https://claude.ai/artifact/NbS8DJ6hybAPBYPtvt7qak)

**v2 changes (after review):**
- **The Wall is now a construction project.** It grows in height, every 10 height is a Section, milestones switch on stage perks, and it has no ceiling. It **no longer soaks damage**, so it no longer overlaps with Necrobinder's Osty.
- **Deport stands on its own:** enemies at or below **25% of their max HP**, and the line can be raised. It no longer needs the Wall.
- **More card flow** that doesn't cost gold.
- **Relics** checked against the game: 8 per character plus the starter upgrade, and 3 potions, exactly as before.

---

## 1. At a glance

| | |
|---|---|
| **Name** | The Donald |
| **Tagline** (character select) | *A tremendous businessman. The best, many people are saying. Builds walls, makes deals, sends the Spire's riff-raff packing.* |
| **HP / Gold / Energy** | 70 / 99 / 3 |
| **Card colour** | Bright metallic gold |
| **Starting deck** (10) | 4 Strike, 4 Defend, 1 **Build the Wall**, 1 **Deport** |
| **Starter relic** | **Golden Shovel**: at the start of your turn, Build 2 |
| **Signature** | **The Wall**, a construction project in front of him that grows in stages |

Decisions confirmed: the name, gold colour, 70 HP, Deport rules (elites yes, bosses no) and the edgier card names all stay.

## 2. Tone guide

The jokes land on **the persona**: bombast ("tremendous", "bigly", "sad!"), gold on everything, deal-making,
all-caps posting, golf, two scoops of ice cream, the long red tie, and "Infrastructure Week". Walls and deportation are **game mechanics
aimed at Spire monsters**, shown with slapstick visuals (security guards, EXIT turnstiles, gold buses). Off limits: real people other than him,
jokes about immigrants or any real ethnic or religious group, real tragedies, and anything mean toward real victims.

## 3. Core mechanics (exact rules)

### The Wall: a construction project
- **Build X** raises your Wall's height by X.
- **The Wall does not soak damage.** Height only goes down when you spend it (Demolition, Wrecking Ball). It resets after each fight, unless relics or cards say otherwise.
- **Every 10 height completes a Section.** There's no limit on Sections.
- **Each Section gives 3 Block at the end of your turn.** The Wall protects you in proportion to how big it is.
- **Stages** are one-time milestones each fight. They change how the wall looks and switch on a perk that **stays for the rest of the fight, even if you tear the Wall down later**:

| Height | Stage | Perk |
|---|---|---|
| 10 | Chain-link fence | The wall appears (Section Block starts) |
| 25 | Brick wall | Draw 1 extra card each turn |
| 45 | Concrete wall | Gain 1 extra Energy each turn |
| 70 | Big Beautiful Wall | Guard posts: at the end of your turn, deal 1 damage to ALL enemies for each Section |

- On screen: the wall in front of The Donald changes at each stage (fence → brick → concrete → gold), with its height and Section count on it.

*Why it's fun:* building toward the next stage, the jump in power when it lands, and deciding whether to keep building or cash in.
Stage perks give Wall decks energy and draw of their own, and Sections give defense and damage with no ceiling.

### Deport
- **Deport**: *if the enemy's HP is at or below your **Deport line** (25% of its max HP), it leaves combat.*
- The line can be raised: Border Control (+10%, stacks), Border Wall (+2% per Section), and Mass Deportation (doubles it for that card).
- Uses the game's own escape system (tested in Step 2); the fight ends normally when the last enemy leaves.
- **Deported enemies drop no Gold.** **Bosses are immune.** Elites and boss minions can be Deported.
- Not a kill: no on-death effects, and it doesn't count as *Fatal*.
- On screen: each enemy's HP bar shows a mark at the Deport line, and a red **DENIED** stamp appears once it's under the line.

*Why it's fun:* any deck that deals damage can finish enemies early. A dedicated Deport deck raises the line and chips everything under it.
The cost is always the same question: **kill for gold, or Deport for tempo?**

### Tariff (enemy debuff)
- **Tariff X**: *whenever this enemy attacks, you gain X Gold.* Stacks; lasts the combat.

### Pay X Gold
- *Can only be played if you have at least X Gold. Lose X Gold.* The card glows red when you can't afford it.

### Tweet (token card)
- **Tweet**: *0-cost Attack. Deal 3 (5) damage to ALL enemies. Exhaust.*

## 4. Play styles: each one works on its own

| Style | Cards | How it wins on its own | Grows without limit through |
|---|---|---|---|
| **The Wall** | 21 | Build stages for energy and draw; Section Block for defense; **Guard Towers** and the 70-height stage for steady damage; **Wall Slam, Demolition, Wrecking Ball** for bursts | Unlimited Sections; Infrastructure Week and Rebar every turn; Great Wall doubling; stacking Reinforced Concrete and Guard Towers; **Cornerstone** and **Permanent Structure** carry the Wall across the whole run |
| **Deport** | 11 | Chip damage plus a higher line removes whole fights: Border Patrol, Show Them the Door, Law and Order, Mass Deportation | Border Control copies stack; Border Wall grows with Sections; **So Much Winning** turns every Deport into energy and draw |
| **The Art of the Deal** | 22 | Tariffs and deals earn gold; spend it on Bribe, Hire Contractors, Make It Rain, **You're Fired!**; Net Worth and Gold Tower grow with wealth | Gold has no cap: Net Worth, Gold Tower and the Gold-Plated Toilet all scale with it |
| **Tweets** | 14 | 0-cost area damage fills the hand: Mean Tweet, Late-Night Posting, Tweetstorm, Social Media Intern | Verified Account, 3 AM Posting, Trending and Going Viral |
| General | 20 | Draw, energy, debuffs and finishers every deck wants | Bigly, Very Stable Genius |

**Bridges** (optional, never required):
- **Wall and Gold:** Wall Street, Gold-Plated Bricks and They'll Pay For It turn the Wall into gold, and Hire Contractors turns gold into Wall.
- **Wall and Deport:** Border Wall raises the Deport line per Section.
- **Tweets and the other styles:** Ratings turns Tweets into Wall, and Tweets chip every enemy under the Deport line for Law and Order.

**Tensions** that make the decisions interesting:
1. **Keep building or cash in?** Demolition and Wrecking Ball spend height, but stages you've reached stay unlocked.
2. **Kill or Deport?** Deport is faster but gives no Gold. Hostile Takeover, Leveraged Buyout and Cornerstone only pay off on a kill.
3. **Spend gold in the shop or in combat?**

## 5. Card flow (energy, draw, generated cards)

Base characters have 2–12 energy cards and 13–17 draw cards. The Donald now has card flow in **every** style, and most of it costs no gold:

| Style | Energy | Draw and card generation |
|---|---|---|
| Wall | **Concrete stage** (+1 each turn) | **Brick stage** (+1 each turn), Bricklayer, Groundbreaking |
| Deport | So Much Winning, Long Red Tie (relic) | One-Way Ticket, So Much Winning |
| Deals | Small Loan, Bribe, Gold-Plated Toilet (relic) | Casino, Campaign Donation, The Art of the Deal |
| Tweets | Tweets cost 0 | Doomscrolling, all Tweet makers |
| General | **Two Scoops**, Executive Time, Golf Weekend | Executive Time, Very Stable Genius, Sharpie, Mulligan, Covfefe, Executive Order |

## 6. Card set summary

| Rarity | Cards | Attack / Skill / Power | Base game |
|---|---|---|---|
| Basic | 4 | 2 / 2 / 0 | 4 |
| Common | 20 | 11 / 9 / 0 | 20 (11 / 9 / 0) |
| Uncommon | 36 | 11 / 16 / 9 | 36 (11 / 16 / 9) |
| Rare | 26 | 8 / 9 / 9 | 26 (7 / 9 / 10) |
| Ancient | 2 | 1 / 0 / 1 | 2 |
| **Total** | **88** + Tweet token | 33 / 36 / 19 | 87–88 (32 / 37 / 19) |

Every reward and shop rule from Step 2 passes (see card_list.md → Validation). Two co-op-only cards: Trickle Down and Coalition Wall.

**v2 card swaps:**

| Out | In | Why |
|---|---|---|
| Tremendous Punch | **Show Them the Door** (Common: 4×2, Deport) | Plain damage replaced by a Deport card at Common |
| Checkpoint | **Groundbreaking** (Common: 0 cost, Build 3, draw 1) | Card flow for Wall decks |
| Cement Mixer | **Border Wall** (Uncommon Power: +2% Deport line per Section) | Bridge between Wall and Deport |
| Pardon, Golden Parachute | **Two Scoops** (0: gain 2 Energy, Exhaust), **Doomscrolling** (draw 3, add a Tweet) | Energy and draw that don't cost gold |
| Grand Opening | **Cornerstone** (Rare: Innate; its Build grows permanently on kills) | Run-long Wall growth |
| Space Force | **Reinforced Concrete** (Rare Power: +2 Block per Section) | Space Force was a copy of Guard Towers; this is Wall defense with no ceiling |

Renamed: Watchtower → **Guard Towers** (now per Section), Tear Down This Wall → **Demolition**, the Big Beautiful Wall power → **Infrastructure Week**
(the name now belongs to the top stage). Reworded for the new rules: Barbed Wire, Wall Street, Ten Feet Higher, Border Control, Law and Order,
Mass Deportation, They'll Pay For It, You're Fired!.

**Signature cards:**
- **You're Fired!**: pay 99 Gold and Deport any non-boss enemy.
- **Demolition**: deal damage equal to your Wall and lose half of it; the stages stay.
- **Cornerstone**: its Build grows permanently with every kill.
- **National Emergency**: Build equal to the damage enemies intend to deal this turn.
- **Chapter 11**: lose all your Gold, gain Block.
- **Two Scoops**, **Covfefe** and **Hole in One**.
- **Golden Escalator** and **Make the Spire Great Again** (Ancients).

## 7. Relics and potions

The same shape as every base character, checked against the game at full unlock: 8 character relics (1 starter, 6 unlocked through the timeline, 1 shop),
plus an Ancient upgrade of the starter, plus 3 potions. The large relic and potion numbers in the game are shared pools, which The Donald also uses:
118 shared relics, 140 event and Ancient relics, and 45 shared potions.

| Relic | Rarity | Effect |
|---|---|---|
| **Golden Shovel** | Starter | At the start of your turn, Build 2. |
| **Diamond Shovel** | Ancient upgrade (Touch of Orobas) | At the start of your turn, Build 4. |
| **Hard Hat** | Common | Whenever you Build, Build 1 additional. |
| **Long Red Tie** | Uncommon | Whenever you Deport an enemy, gain 1 Energy. |
| **Gold-Plated Bricks** | Uncommon | At the end of combat, gain 2 Gold for each Section. |
| **Rubber Stamp** | Rare | Deported enemies still drop their Gold. |
| **Permanent Structure** | Rare | At the end of combat, keep a quarter of your Wall for the next combat. |
| **Late-Night Phone** | Rare | The first Tweet you play each turn is played twice. |
| **Gold-Plated Toilet** | Shop | At the start of your turn, if you have 250+ Gold, gain 1 Energy. |

| Potion | Rarity | Effect |
|---|---|---|
| **Quick-Dry Concrete** | Common | Build 12. |
| **Covfefe** | Uncommon | Add 3 Upgraded Tweets into your Hand. |
| **Deportation Draught** | Rare | Deport an enemy, whatever its HP. (Not Bosses.) |

## 8. What gets implemented (Steps 4–5)

- **Wall system:** Wall height and Sections; end-of-turn Section Block; stage tracking with perks that persist through Demolition;
  the Wall display (stage art, height, Section count, progress to the next stage). Hooks for relics and cards that add height each turn
  or carry it between fights (Permanent Structure, and Cornerstone's saved Build).
- **Deport system:** a Deport line per player (base 25%, with modifiers), escape through the game's system, boss immunity,
  the line marker and DENIED stamp on enemy HP bars.
- **New status effects:** Tariff, one per Power card, and a few temporary ones (Barbed Wire, Blocked!, Executive Time, Golf Weekend).
- **Keywords with tooltips:** Build, Wall, Section, Deport, Pay Gold, Tweet.
- **UI and game hooks:** a gold-cost badge on Pay-Gold cards; the gold card frame; Touch of Orobas → Diamond Shovel;
  Tweet as a Token card; co-op-only flags; Deport exceptions for scripted encounters (§9).

## 9. Balance reasoning and watch list

Common attacks sit at ≤ 9 damage per energy before their extra effect (base median 9). Build is priced at ~5–6 per energy.
One Section (10 height, about 2 energy) returns 3 Block every turn, so it pays back over a normal 4–6 turn fight. Stage thresholds (10/25/45/70)
are set so a dedicated Wall deck reaches Brick by turn 2–3 and Concrete by turn 3–4, with the top stage in longer elite and boss fights.

| Risk | Why | Levers |
|---|---|---|
| **Runaway Wall** | Infrastructure Week + Great Wall + stacked Reinforced Concrete → invulnerable; Demolition loops (stages persist) | Section Block value; stage thresholds; Great Wall stays Exhaust |
| **Hold the Line / Wall Slam** | Both scale with height and can be played every turn | Change "half" to "a third" if needed |
| **Gold inflation** | Tariffs + Merch Stand + Gold-Plated Bricks + They'll Pay For It | Gold amounts |
| **Deport line stacking** | Border Control copies + Border Wall at high Sections could approach 100% | Cap the line at 75%, or reduce the per-copy value |
| **Law and Order** | Automatic Deport every turn in hallway fights | Cost; bosses stay immune |
| **Deport vs scripted enemies** | Some encounters may need an enemy to *die* | Step 4 Deports every enemy in every encounter to find exceptions |
| **Tweet spam** | 3 AM Posting + Verified Account + Late-Night Phone | Tweet damage; Verified Account amount |
| **Bosses** | Deport doesn't work on bosses | The other styles all work on bosses; track win rate by boss |
