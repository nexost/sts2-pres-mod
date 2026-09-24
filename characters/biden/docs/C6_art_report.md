# C6 report: art

_2026-09-24. Game v0.107.1. Branch `feature/biden`. Every placeholder is replaced; the user reviewed and kept every item._

## What's in

All **147** items are made with the locked recipe from The Donald's Step 7: Krea 2 Turbo, the game's own art as style references ([ART_PIPELINE.md](../../../docs/ART_PIPELINE.md)).

| Tab | Items | Highlights |
|---|---|---|
| Character | 19 | The select screen: he leans on the green '67 Corvette on a cliff under a crescent moon, with the Spire on the horizon. The select button uses his own flat colour, royal indigo; the base game uses red, green, steel blue, pink and orange, and The Donald mustard gold. Combat poses, plus a second set for Dark Brandon (red lenses). He eats an ice cream cone in the shop and relaxes in a camp chair at the rest site. The map marker is an ice cream cone. The co-op hands wear a rolled-up light blue sleeve and a silver watch |
| Cards | 88 | Joe is on 24 cards (27%) and his hand on 5. The rest are objects, effects, monsters and scenes (below) |
| Relics, potions | 12 | Aviator Shades, Dark Aviators, Travel Pillow, Ice Cream Cone, '67 Corvette, Dark Brandon mug, … |
| Powers | 24 | One symbol each, in its own colours |
| UI | 4 | Navy energy orb (3 layers) and energy icon |

- **Numbers:** 390 versions made; 47 items kept on their first version.
- **Review:** the user reviewed every item in the tool, partly from a phone.
- **Laser Eyes are red now:** the tint over the Defect's hyperbeam went from `(1, .35, .3)` to `(1, .1, .06)`. The beam's layers add up, so any green or blue in the tint turned its white core pink.

## What changed on the way (and why)

| Problem | Fix |
|---|---|
| A 10-item sample showed three problems: "a wide toothy grin" in the persona made him grin while dozing; his lenses didn't glow as Dark Brandon; "a half-closed eye" became a ball icon | The grin moved out of the persona into the prompts that want it. Dark Brandon's cue is "the lenses of his aviators glowing solid bright red". New `card_notes` per play style (in `character.json`) add that cue to Brandon cards, and Z letters and a snoring face to Nap cards |
| He never takes the aviators off, whatever the prompt says | It became the rule, and it fits his relic's flavour ("Nobody can tell whether his eyes are open"). Sleep is shown by posture and Z letters |
| **Joe was on 80 of 88 cards** (the user's catch) | The base game and The Donald (29 of 89) show the character on 30–40% of cards and hands on about a fifth. 54 card texts were rewritten to objects, effects, monsters and scenes: trains, the limo, coffee pots, snore shockwaves, slippers walking on their own, goblins marching past a bed |
| **Power icons all looked like Thorns and Strength** (the user's catch): the same two references for every icon | A pool of 12 varied game power icons; each icon gets its own pair, at style strength 0.55, and every icon's text names its colours |
| **Five Dark Brandon cards looked the same** after rerolls: Put On the Aviators, Stare Down, Game Face, Wide Awake, Dark Brandon Rises | Each got its own framing and palette through a new per-card `art_background`: a close-up of hands and glasses on dawn orange; over the shoulder at a goblin on teal; a locker-room mirror in steel blue; a lighthouse whose lamp is an eye, on violet; a low-angle full body against a red sun |
| Content risks the model added | Finger Guns and Laser Show gave him real handguns or a gun-like device; Dogfight said "strafing"; Build Back Better cut the ribbon with a knife; Air Force One sat in flames; Wide Awake's eye over a triangle of light read as the "Illuminati" eye. All rewritten: bare hands, lasers from the eyes, ceremonial scissors, a clean jet, a sideways lighthouse beam |
| Soul of the Nation painted a fake card text box with gibberish | The Ancient-card framing no longer names the text box ("the lower half is plain background") |

## Tool changes

These are shared by every character.

- **The prompt fields above:**
  - `card_notes` per play style and `art_background` per card;
  - a varied reference pool for power icons.
- **A race when copying references:** every job copied its reference images into ComfyUI's input folder again, even while the running job was reading them ("Invalid data found when processing input"). Now an identical copy is left alone, and a changed one is replaced in one step.
- **The review tool on a phone:** `art_review.py --host 0.0.0.0` on the same Wi-Fi. The page adapts to small screens:
  - a compact header with a Filters button;
  - two columns of tiles and a selection bar at the bottom;
  - a full-screen detail view with ← · Keep · Regenerate · → at the bottom, and swipe to move between items.
- **The guides:** [ART_PIPELINE.md](../../../docs/ART_PIPELINE.md) §8 has the lessons (sample first, what the persona does, no conditionals, the card mix). [ADDING_A_CHARACTER.md](../../../docs/ADDING_A_CHARACTER.md) C6 gained the card-mix check and the sample step.

## Tests

| Test | Result |
|---|---|
| `python scripts/build.py --install` | 0 warnings, 0 errors |
| `test.py ui -c biden` | **PASS**, 0 log problems, 21 screenshots checked: select screen, button row, map, card library, the four poses, Drowsy, nodding off, the Dark Brandon pose swap, red Laser Eyes, shop, rest site |

## Next

C7: final checks.
- `test.py cards` and `autoslay` for Sleepy Joe, and the regression runs for The Donald.
- Co-op (two Sleepy Joes, and a mixed party): nodding off in co-op, Reach Across the Aisle, and the new hands.
