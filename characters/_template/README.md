# Character template

`scripts/new_character.py` copies this folder to make a new character. It isn't a character itself: `presmod.py`
skips folders starting with `_`, and nothing here is compiled or packed.

- Files outside `src/` go to `characters/<id>/`.
- `src/` goes to `mod/pres_mod/src/Characters/<Class>/`.
- File names and contents have `{{Token}}` placeholders, filled from the script's options.
- The script also writes `localization/eng/ancients.json` (every key an existing character has), copies the compendium template, and copies and recolours the scenes (`presmod.CHARACTER_SCENES`).

| Token | Example | From |
|---|---|---|
| `{{id}}`, `{{ID}}`, `{{Class}}` | `biden`, `BIDEN`, `Biden` | the id and `--class` |
| `{{Name}}`, `{{FullName}}`, `{{FirstName}}`, `{{Initial}}` | `Uncle Joe`, `Joe Biden`, `Joe`, `B` | `--name`, `--full-name` |
| `{{Starter}}`, `{{StarterName}}`, `{{STARTER_ID}}`, `{{starter_id}}` | `AviatorShades`, `Aviator Shades`, `AVIATOR_SHADES`, `aviator_shades` | `--starter`, `--starter-name` |
| `{{primary}}`, `{{secondary}}`, `{{accent}}`, `{{text}}` | `2F6BD8`, ... | `--primary` etc. |
| `{{dark}}`, `{{deck}}`, `{{map}}`, `{{dialogue}}` | shades of primary | outline, deck entry, map drawing, dialogue colours |
| `{{tint_dark}}`, `{{tint_mid}}`, `{{tint_light}}` | shades of primary | `art.energy_tint` |
| `{{frame_h}}` | `0.62` | card frame hue from primary |
| `{{color_word}}`, `{{ColorWordTitle}}`, `{{VfxColor}}` | `blue`, `Blue`, `Blue` | `--color-word` or guessed from primary |
| `{{Gender}}`, `{{he}}`, `{{him}}`, `{{his}}`, `{{his_pronoun}}` | `Masculine`, `he`, `him`, `his`, `his` | `--gender` |
| `{{hp}}`, `{{gold}}`, `{{sfx}}` | `75`, `99`, `ironclad` | `--hp`, `--gold`, `--sfx` |

When a new character needs something every character would need, add it here (and to `presmod.CHARACTER_SCENES` or `LOC_TABLES` if the game loads it by name), so the next character gets it for free.
