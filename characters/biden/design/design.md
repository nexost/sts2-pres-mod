# Character Design: "Sleepy Joe" (v1)

A lighthearted caricature of Joe Biden built around **his own sleepiness and confusion**, and the secret that comes out
when he wakes up: **Dark Brandon**. Sleepy Joe dozes and rambles; Dark Brandon snaps awake with laser eyes and says
everything at once. Every number is designed against data mined from the five base characters
([`docs/reference/base_game_benchmarks.md`](../../../docs/reference/base_game_benchmarks.md)).

- Full card list with every number: [`card_list.md`](card_list.md) (`python scripts/render_design.py -c biden`)
- Source of truth for implementation: [`cards.json`](cards.json)
- Review page: [Sleepy Joe Compendium](https://claude.ai/artifact/KhC3qpBBpx2UDJAA3KCdyv) (from [`compendium.html`](compendium.html); republish after changes). Local: [`gallery.html`](gallery.html)

**Set by the user:** Dark Brandon is the signature, and his everyday self is **Sleepy Joe**, which is also his name on the select screen (v1 review; the scaffold called him Uncle Joe).
Sleepiness and confusion are **on him**, never on the enemies. Jokes about sleepiness, confusion and gaffes are fine; jokes
about age, decline or health are not. Nothing may copy another character's or an enemy's identity (§10).

**Settled in the v1 review (2026-09-24):**
- Name: **Sleepy Joe**. HP: **70** (the starter's nap Block makes up for it).
- Left to the design, to adjust after balance testing (C5): nodding off **ends the turn on the spot** (the cost that makes you
  order your turn), and Laser Eyes deal **1× Drowsy** (a steady bonus; Laser Focus, Double Vision, the Dark Brandon Mug and
  Heavy Sleeper scale it).
- **C5 balance (2026-09-24):** both kept. The one change was the Aviator Shades' end-of-turn Doze, 2 → **3**: with 2, he
  napped in 71% of fights and struggled against Act 1 elites. With 3, he naps in 82% (1.4 naps a fight) and ends level with Ironclad.
  [C5 report](../docs/C5_balance_report.md).

---

**Settled while implementing (C4, [report](../docs/C4_content_report.md)):** Infrastructure Law is +1 Energy each turn
(no Block); Wide Awake's draw stays 1 while its Block stacks; each Double Vision adds a hit; Corvette Cruise uses the
game's Free Attack ("your next Attack costs 0"); Unleashed upgrades to cost 0; Repeat the Line only replays a card that's
in the Discard Pile; Tall Tales grows damage and Block. Sleepy cards glow red when they'd nod him off mid-turn.

---

## 1. At a glance

| | |
|---|---|
| **Name** | Sleepy Joe (who wakes up as **Dark Brandon**) |
| **Tagline** (character select) | *Here's the deal, folks: he's not asleep, he's resting his eyes. Let him nod off, and Dark Brandon wakes up.* |
| **HP / Gold / Energy** | 70 / 99 / 3 |
| **Card colour** | Royal navy (#4A55E6, frame deepened to navy) |
| **Starting deck** (10) | 4 Strike, 4 Defend, 1 **Catnap**, 1 **Here's the Deal** |
| **Starter relic** | **Aviator Shades**: at the end of your turn, Doze 3; when you nod off, gain 8 Block |
| **Signature** | **Drowsy**: he gets sleepier, nods off, and wakes as **Dark Brandon**. His cards are **Tangents** that change what they do while he rambles |

## 2. Tone guide

The jokes land on **the persona**: dozing off, "resting my eyes", losing the thread mid-sentence, rambling anecdotes,
"Here's the deal", "C'mon, man!", "Malarkey!", "No joke", the aviators, chocolate chip ice cream, Amtrak, the '67 Corvette,
and the Dark Brandon meme with its laser eyes. Sleepiness and confusion are played as slapstick (pajamas, snores, speech
bubbles wandering off), never as illness.

Off limits: jokes about age, decline or health; real people other than him (family, colleagues, other politicians);
jokes about immigrants or any real ethnic or religious group; real tragedies; anything mean toward real victims.
Enemies are always Spire monsters.

## 3. Core mechanics (exact rules)

### Drowsy: sleepiness is on him
- **Drowsy** is a counter on Joe, shown on his portrait with its **nod-off line** (10).
- **Doze X**: gain X Drowsy.
- **Sleepy Joe gets drowsier on his own: at the end of your turn, Doze 3** (not on Dark Brandon turns; 2 before the C5 balance pass). The Aviator Shades carry this, the way the Necrobinder's Bound Phylactery carries Osty, so the relic's text says it.
- **Nodding off:** when your Drowsy reaches your nod-off line (10), you nod off. **Your turn ends right away**, after the card that did it finishes.
  - Some cards say **Nod off**: you nod off now, whatever your Drowsy.
  - Crossing the line at the end of your turn costs nothing. Crossing it mid-turn wastes whatever you had left: play sleepy cards last.
- **Next turn you wake up as Dark Brandon.**

### Dark Brandon: the clarity
- **Waking up:** at the start of the turn after you nod off, or right away with a **Wake Up** card.
- **Laser Eyes** fire the moment you wake: deal **damage equal to your Drowsy to ALL enemies**. Then your Drowsy resets to 0.
  - A full nap (10+) is a 10+ damage sweep. A Wake Up card at 4 Drowsy fires a small one: the choice is timing against size.
- **For the rest of that turn** you are Dark Brandon:
  - your **Tangent cards do all their lines** (§ below);
  - **Drowsy can't build** (you're wide awake; Doze does nothing);
  - cards with "As Dark Brandon, ..." get their bonus.
- At the end of that turn you're Sleepy Joe again.

### Tangent: confusion is on him
- A **Tangent** card has 2 or 3 lines. **Only the lit line happens.** When you draw it, line (1) is lit.
- **After you play any other card, every Tangent card in your hand moves to its next line** (the last wraps back to the first).
- **As Dark Brandon, a Tangent card does all its lines.**
- So Sleepy Joe's hand keeps changing its mind. The puzzle each turn is the order: play the other cards until the Tangent says what you want, then play it. Or save it for the Dark Brandon turn and get everything.
- On screen: the lit line is gold, the others are grey, and a small (1)(2)(3) marker shows where it is.

### Keywords with tooltips
Drowsy, Doze, Nod off, Wake Up, Dark Brandon, Laser Eyes, Tangent.

## 4. Play styles: each one works on its own

| Style | Cards | How it wins on its own | Grows without limit through |
|---|---|---|---|
| **Dark Brandon** | 23 | Wake up often and make the awake turns count: Wake Up cards (Cup of Joe, Put On the Aviators, Rise and Shine, Laser Show), attacks with Dark Brandon bonuses (Finger Guns, Mic Drop at 0 cost, Dogfight), energy on awake turns (Up All Night, Game Face, Second Cup) | **No Malarkey** (Strength every wake), stacking **Laser Focus**, **Double Vision**, **Dark Brandon Rises** (awake every turn) |
| **Power Nap** | 22 | Embrace nodding off: sleepy cards are cheap Block and damage, the nap itself pays out (Power Nap Block, Snoring damage), and Drowsy-scaled hits (Groggy Haymaker, Sleeping Giant, Out Cold) land just before the line | **Heavy Sleeper** copies raise the line (bigger naps, bigger Laser Eyes), **Well Rested** (Dexterity every nap), Power Nap and Snoring copies |
| **Tangents** | 22 | Order the plays so each Tangent lands on its best line; tools to steer them (Where Was I?, Brain Freeze, Repeat the Line) and payoffs for every line change | **Tall Tales** (Tangents grow every time they change line), **Stream of Consciousness**, **Off Script**, **Moment of Clarity** |
| General | 21 | Draw, energy, debuffs and finishers every deck wants: Amtrak, ice cream, the Corvette, Malarkey! | Infrastructure Law, Gaffe Machine, Zero to Sixty |

**Bridges** (optional, never required):
- **Nap and Brandon:** every nap ends in a Dark Brandon turn. Nap decks bank big Laser Eyes; Brandon decks cash in small and often.
- **Tangents and Brandon:** Dark Brandon makes every Tangent do everything. Teleprompter makes them free on the wake turn.
- **Nap and Tangents:** Sound Asleep keeps your hand (Tangents keep cycling while you sleep on it).

**Tensions** that make the decisions interesting:
1. **Nap now, or push closer to the line?** A bigger Drowsy is a bigger Laser Eyes, but crossing mid-turn ends the turn.
2. **Wake up now, or sleep on it?** Wake Up gives you Dark Brandon this turn, with whatever Drowsy you have.
3. **Which line?** Every Tangent asks you to plan the order of your turn, or to hold it for Dark Brandon.

## 5. Card flow (energy, draw, generated cards)

Base characters have 2–12 energy cards and 13–17 draw cards. Sleepy Joe has card flow in every style, and most of it
doesn't need Drowsy or Dark Brandon:

| Style | Energy | Draw |
|---|---|---|
| Dark Brandon | Up All Night, Game Face, Second Cup (awake turns) | Put On the Aviators, Red-Eye, Morning Person, Wide Awake, '67 Corvette |
| Power Nap | Forty Winks | Counting Sheep |
| Tangents | Folks, Listen (2), Filibuster (3) | Here's the Deal (3), Let Me Finish (2), Where Was I?, Brain Freeze, Lost My Train of Thought, Index Cards |
| General | Sugar Rush, Infrastructure Law, Amtrak Pass | Full Steam Ahead, Chocolate Chip, C'mon, Man!, All Aboard, Corvette Cruise, Amtrak Joe, Build Back Better |

## 6. Card set summary

| Rarity | Cards | Attack / Skill / Power | Base game |
|---|---|---|---|
| Basic | 4 | 2 / 2 / 0 | 4 |
| Common | 20 | 11 / 9 / 0 | 20 (11 / 9 / 0) |
| Uncommon | 36 | 11 / 16 / 9 | 36 (11 / 16 / 9) |
| Rare | 26 | 8 / 9 / 9 | 26 (7 / 9 / 10) |
| Ancient | 2 | 1 / 0 / 1 | 2 |
| **Total** | **88** | 33 / 36 / 19 | 87–88 (32 / 37 / 19) |

Every reward and shop rule passes (card_list.md → Validation). One co-op-only card: Reach Across the Aisle. No token cards.

**Signature cards:**
- **Here's the Deal** (Basic): the Tangent that teaches the Tangent: damage, Block or draw 2, or all three as Dark Brandon.
- **Big Deal**: 22 damage if it's the first thing you say; the second line is "Nothing. You forgot the point."
- **Repeat the Line**: play your last card again, just like the teleprompter said.
- **Lights Out** and **Sleep In**: strong now, but you nod off (and wake up as Dark Brandon).
- **Groggy Haymaker** and **Sleeping Giant**: hit harder the sleepier he is.
- **Dark Brandon Rises**: awake every turn. Tangents always do everything, but the lasers stay small.
- **State of the Union** (Ancient): Wake Up and 14 damage to ALL enemies.

## 7. Relics and potions

The same shape as every base character: 1 starter, 1 Common, 2 Uncommon, 3 Rare, 1 Shop, plus the starter's Ancient
upgrade (Touch of Orobas), and 3 potions.

| Relic | Rarity | Effect |
|---|---|---|
| **Aviator Shades** | Starter | At the end of your turn, Doze 3. When you nod off, gain 8 Block. |
| **Dark Aviators** | Ancient upgrade | At the end of your turn, Doze 3. When you nod off, gain 14 Block. |
| **Travel Pillow** | Common | Whenever you Doze, Doze 1 additional. |
| **Ice Cream Cone** | Uncommon | Whenever you wake up, heal 3 HP. |
| **Index Cards** | Uncommon | Whenever you play a Tangent card on its last line, draw 1 card. |
| **'67 Corvette** | Rare | Whenever you wake up, draw 2 cards. |
| **Dark Brandon Mug** | Rare | Laser Eyes deal 50% more damage. |
| **Teleprompter** | Rare | Whenever you wake up, your Tangent cards cost 0 this turn. |
| **Amtrak Pass** | Shop | The first time you play 5 cards in a turn, gain 1 Energy. |

The starter answers the one real cost of nodding off: the turn ends early, maybe before you've blocked. The shades hide the nap.

| Potion | Rarity | Effect |
|---|---|---|
| **Warm Milk** | Common | Doze 8. |
| **Espresso Shot** | Uncommon | Wake Up. Draw 2 cards. |
| **Dark Roast** | Rare | Wake Up. This turn, your Attacks deal double damage. |

## 8. What gets implemented first (C3)

- **Drowsy:** the counter and its nod-off line (with modifiers: Heavy Sleeper), Doze with modifiers (Travel Pillow),
  the end-of-turn Doze 3, nodding off (end the turn after the current card; in co-op through the synced end-turn action),
  the "you nodded off" state that wakes you next turn.
- **Dark Brandon:** waking up (next turn, or Wake Up cards), Laser Eyes, the awake state for one turn, "as Dark Brandon"
  checks, and a visual: the combat body swaps to a Dark Brandon pose set (red-glowing aviators) while awake.
- **Tangent:** the line counter per card in hand, moving on after other cards are played, the lit line in the card text,
  all lines as Dark Brandon.
- **Hooks** in the Trump pattern (`Mechanics/Hooks.cs`): `IDozeModifier`, `INodOffLineModifier`, `IAfterNodOff`,
  `IAfterWakeUp`, `ILaserEyesModifier`, `IAfterTangentLineChanged`.
- **Starter set:** Strike, Defend, Catnap, Here's the Deal, Aviator Shades (and Dark Aviators via Touch of Orobas).
- **Tests** (`test.py ui -c biden`): Doze and the line, nodding off mid-turn ends the turn, waking next turn fires Laser Eyes
  for the right amount, Wake Up, a Tangent moving lines and doing all lines as Dark Brandon, Aviator Shades' Block.

## 9. Balance reasoning and watch list

A Sleepy Joe deck reaches the line about every 2–3 turns: 3 a turn on its own, plus 2–4 per sleepy card. The starter deck
wakes up first around turn 3. The C5 bot napped 1.4 times a fight, in 82% of fights, and Laser Eyes dealt about 15 damage
a fight. A Laser Eyes of 10 is about one card's worth of area damage; the rest of Dark Brandon's
value is Tangents doing everything and the "As Dark Brandon" bonuses. Tangent lines are each priced as roughly one
card (so a 3-line Tangent is ~2.5 cards on a Dark Brandon turn, ~1 card otherwise). Sleepy cards are priced at par plus
their Doze: Doze is progress toward Dark Brandon, and its cost is the risk of nodding off at the wrong moment.

| Risk | Why | Levers |
|---|---|---|
| **Dark Brandon every turn** | Dark Brandon Rises, Early Riser, Second Cup loops: Tangents doing everything every turn | Dark Brandon Rises' cost; Tangent line values; Early Riser's 5-Drowsy check |
| **Tangent all-lines stacking** | Off Script + Moment of Clarity + Teleprompter make every Tangent a triple card | Off Script to "first Tangent each turn" (already); line values |
| **Laser Eyes scaling** | Heavy Sleeper copies + Double Vision + Dark Brandon Mug + Laser Focus | Nod-off line bonus per copy; Mug to +25% |
| **Nap payoffs** | Power Nap + Snoring + Well Rested on a 2-turn nap cycle | Payoff amounts; the end-of-turn Doze |
| **Frustration** | Nodding off mid-turn by accident | The Drowsy counter shows the line; sleepy cards show a warning glow when they'd cross it |
| **Repeat the Line** | Replaying Big Deal line 1, Laser Show, Out Cold | Exclude X-cost and "Nod off" cards if needed |
| **Co-op** | A card that ends your own turn mid-action must go through the synced end-turn action | Test in C3 (Trump's Golf Weekend already ends the turn) |
| **The balance bot** | It must learn when to cross the line and which Tangent line is lit | Done in C5: `CardValueOverride` (lit line, form) and `CardValue` (Doze, Wake Up, naps) |

## 10. Why this is his own (not a copy)

| Could look like | Why it isn't |
|---|---|
| The Watcher's stances (StS1) | No stance switching at will: Drowsy is a timer you manage, the switch comes from falling asleep or a Wake Up card, and Dark Brandon lasts one turn |
| Enemies that sleep (Asleep, Slumber) | Those are enemies skipping turns until hit. Joe's nap ends his own turn and pays off the next one |
| Confused and Snecko Eye | Nothing here randomizes costs. Tangents change *what* a card does, in a fixed order you can plan around |
| Trump's Wall | The Wall only grows and never resets. Drowsy is a meter that fills, spends itself in Laser Eyes, and starts over; its cost is losing the rest of a turn |
| The "Forms" powers (Demon Form, ...) | Dark Brandon is a recurring one-turn state, not a permanent power |
| Necrobinder's Osty | No companion or summon of any kind |
