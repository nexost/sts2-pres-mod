# The Donald's one-off helpers (Step 5)

History, kept as examples. **Don't run them:** they predate the rename (Step 8.5) and still point at
`mod/trump_character/localization/eng/`. The text now lives in `characters/trump/localization/eng/`; edit it there.

| Script | What it did |
|---|---|
| `loc_step5_cards.py` | Wrote the English text of all 88 cards from `design/cards.json`, using the base game's own card texts as templates (keyword colours such as `[gold]Build[/gold]`, `{Damage:diff()}`, plurals) |
| `loc_step5_other.py` | Wrote the powers', relics' and potions' text |
| `loc_step5_ancients.py` | Wrote The Donald's lines with every Ancient, in the line patterns `Framework/Patches/AncientDialoguePatch.cs` expects (A = the Ancient, C = the character; the last line has no button) |

For a new character, a script like these is the quickest way to write 80+ texts consistently (ADDING_A_CHARACTER.md, phase C4):
1. Read `characters/<id>/design/cards.json`.
2. Keep one small template per effect.
3. Write `characters/<id>/localization/eng/*.json`.
4. Check the result with `python scripts/test.py cards PRESTEST1 -c <id>`, which renders every text.
