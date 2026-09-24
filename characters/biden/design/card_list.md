# Uncle Joe: full card list

Generated from `cards.json` by `scripts/render_design.py`. Numbers in parentheses are the upgraded values.

## Basic (2)

| # | Card | Cost | Type | Style | Text | Upgrade |
|---|---|---|---|---|---|---|
| 1 | **Strike** | 1 | Attack | General | Deal 6 (9) damage. | 6→9 |
| 2 | **Defend** | 1 | Skill | General | Gain 5 (8) Block. | 5→8 |

## Common (3)

| # | Card | Cost | Type | Style | Text | Upgrade |
|---|---|---|---|---|---|---|
| 1 | **Jab** | 1 | Attack | General | Deal 9 (12) damage. Draw 1 card. | 9→12 |
| 2 | **One-Two** | 1 | Attack | General | Deal 5 (7) damage twice. | 5→7 |
| 3 | **Brace** | 1 | Skill | General | Gain 8 (11) Block. Draw 1 card. | 8→11 |

## Uncommon (2)

| # | Card | Cost | Type | Style | Text | Upgrade |
|---|---|---|---|---|---|---|
| 1 | **Regroup** | 1 | Skill | General | Gain 5 (8) Block. Draw 2 cards. | 5→8 |
| 2 | **Resolve** | 1 | Power | General | Gain 2 (3) Strength. | 2→3 |

## By play style

- **General** (7: 2 Basic, 3 Common, 2 Uncommon): Strike, Defend, Jab, One-Two, Brace, Regroup, Resolve

## Validation

```
Total pool cards: 7 (base game: 87-88)
  Basic       2  base   4  DIFF
  Common      3  base  20  DIFF
  Uncommon    2  base  36  DIFF
  Rare        0  base  26  DIFF
  Ancient     0  base   2  DIFF
Types: Attack 3, Skill 3, Power 1  (base avg 32 / 37 / 19)
  Common    A/S/P  2/ 1/ 0   base 11/9/0
  Uncommon  A/S/P  0/ 1/ 1   base 11/16/9
  Rare      A/S/P  0/ 0/ 0   base 7/9/10
Reward/shop rules (single-player pool, co-op-only cards excluded):
  >= 8 Commons (Room Full of Cheese): 3  FAIL
  >= 5 Common (Sea Glass): 3  FAIL
  >= 5 Uncommon (Sea Glass): 2  FAIL
  >= 5 Rare (Sea Glass): 0  FAIL
  >= 3 Rares (boss rewards): 0  FAIL
  shop needs 2+ Attacks: 2  OK
  shop needs 2+ Skills: 2  OK
  shop needs 1+ Powers: 1  OK
Damage per energy, simple single-target attacks (base common median 9, p75 9):
  Basic     Strike                  6.0/E
  Common    Jab                     9.0/E
  Common    One-Two                 5.0/E
Build per energy (Block benchmark: common median 6, p75 8; Wall persists so ~5-6/E is par):
```
