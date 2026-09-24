# Base game benchmarks (character pools)

## Rarity x type per character

| Character | Total | Basic | Common | Uncommon | Rare | Ancient | Attack | Skill | Power |
|---|---|---|---|---|---|---|---|---|---|
| Ironclad | 87 | 3 | 20 | 36 | 26 | 2 | 37 | 30 | 20 |
| Silent | 88 | 4 | 20 | 36 | 26 | 2 | 27 | 42 | 19 |
| Defect | 88 | 4 | 20 | 36 | 26 | 2 | 29 | 39 | 20 |
| Regent | 88 | 4 | 20 | 36 | 26 | 2 | 32 | 37 | 19 |
| Necrobinder | 88 | 4 | 20 | 36 | 26 | 2 | 35 | 35 | 18 |

## Type split per rarity (all 5 characters, average per character)

| Rarity | Attack | Skill | Power |
|---|---|---|---|
| Basic | 1.8 | 2.0 | 0.0 |
| Common | 11.0 | 9.0 | 0.0 |
| Uncommon | 11.0 | 16.4 | 8.6 |
| Rare | 7.4 | 9.0 | 9.6 |
| Ancient | 0.8 | 0.2 | 1.0 |

## Cost curve per rarity (average per character)

| Rarity | X | 0 | 1 | 2 | 3 | 4+ |
|---|---|---|---|---|---|---|
| Basic | 0.0 | 0.4 | 3.2 | 0.2 | 0.0 | 0.0 |
| Common | 0.0 | 3.6 | 13.8 | 2.4 | 0.2 | 0.0 |
| Uncommon | 0.8 | 5.8 | 20.6 | 6.2 | 2.2 | 0.4 |
| Rare | 1.0 | 4.0 | 10.0 | 6.8 | 3.8 | 0.4 |
| Ancient | 0.0 | 0.4 | 1.0 | 0.2 | 0.4 | 0.0 |

## Damage per energy, attacks with a Damage var (single target / AoE), cost >= 1

| Rarity | n | median dmg/E | p75 dmg/E | AoE n | AoE median dmg/E |
|---|---|---|---|---|---|
| Basic | 6 | 6.0 | 6.0 | 0 | - |
| Common | 35 | 9.0 | 9.0 | 7 | 6.0 |
| Uncommon | 31 | 7.0 | 9.0 | 2 | 4.7 |
| Rare | 15 | 5.0 | 10.0 | 8 | 9.5 |
| Ancient | 1 | 20.0 | 20.0 | 0 | - |

## Block per energy, cards with a Block var, cost >= 1

| Rarity | n | median block/E | p75 block/E |
|---|---|---|---|
| Basic | 6 | 5.0 | 5.0 |
| Common | 21 | 6.0 | 8.0 |
| Uncommon | 23 | 5.5 | 7.0 |
| Rare | 2 | 12.5 | 15.0 |

## Upgrade patterns (share of cards)

- damage/block up: 182 (41%)
- cost -1: 46 (10%)
- -Exhaust: 13 (3%)
- +Innate: 11 (3%)
- +Retain: 8 (2%)
- -Ethereal: 2 (0%)

## Keywords (cards per character, average)

- Exhaust: 11.8
- Ethereal: 2.2
- Sly: 1.6
- Retain: 1.2
- Innate: 0.8
- Eternal: 0.2

## Other vars used (cards per character, average)

- Damage: 27.2
- Block: 11.8
- Cards: 11.6
- CalculationBase: 7.0
- Energy: 6.6
- Repeat: 4.2
- CalculationExtra: 4.0
- ExtraDamage: 3.0
- VulnerablePower: 3.0
- WeakPower: 2.6
- Stars: 2.2
- Forge: 2.0
- Summon: 2.0
- StrengthPower: 1.8
- Power: 1.8
- OstyDamage: 1.8
- HpLoss: 1.6
- StrengthLoss: 1.2
- PoisonPower: 1.2
- Increase: 1.0
- FocusPower: 1.0
- DoomPower: 1.0
- DexterityPower: 0.8
- Shivs: 0.6
- Heal: 0.4
- PlatingPower: 0.4
- PutBack: 0.4
- VigorPower: 0.4
- Colossus: 0.2
- CrimsonMantlePower: 0.2
