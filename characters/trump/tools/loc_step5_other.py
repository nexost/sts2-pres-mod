"""
One-off helper for Step 5: adds the English text of the new powers, relics and potions to the mod's
localization files (powers.json, relics.json, potions.json), keeping everything already there.
Temporary Strength-down powers (Sharpie, Low Energy) take their title from the card and the base game's text.
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOC = os.path.join(ROOT, "mod", "trump_character", "localization", "eng")

B = "[gold]Build[/gold]"
SECTION = "[gold]Section[/gold]"
TWEET = "[gold]Tweet[/gold]"
HAND = "[gold]Hand[/gold]"
GOLD = "[gold]Gold[/gold]"
DEPORT = "[gold]Deport[/gold]"
AMT = "[blue]{Amount}[/blue]"

# key: (title, description (no numbers), smartDescription (with {Amount}))
POWERS = {
    "BARBED_WIRE_POWER": ("Barbed Wire",
                          f"Whenever you are attacked this turn, deal damage back, plus 1 for each {SECTION}.",
                          f"Whenever you are attacked this turn, deal {AMT} damage back, plus 1 for each {SECTION}."),
    "BLOCKED_POWER": ("Blocked!",
                      f"This turn, whenever you play a {TWEET}, gain [gold]Block[/gold].",
                      f"This turn, whenever you play a {TWEET}, gain {AMT} [gold]Block[/gold]."),
    "REBAR_POWER": ("Rebar", f"At the start of your turn, {B}.", f"At the start of your turn, {B} {AMT}."),
    "INFRASTRUCTURE_WEEK_POWER": ("Infrastructure Week", f"At the start of your turn, {B}.", f"At the start of your turn, {B} {AMT}."),
    "GUARD_TOWERS_POWER": ("Guard Towers",
                           f"At the end of your turn, deal damage to a random enemy for each {SECTION}.",
                           f"At the end of your turn, deal {AMT} damage to a random enemy for each {SECTION}."),
    "BORDER_CONTROL_POWER": ("Border Control", f"Your {DEPORT} line is higher.", f"Your {DEPORT} line is [blue]{{Amount}}%[/blue] higher."),
    "BORDER_WALL_POWER": ("Border Wall",
                          f"Your {DEPORT} line is higher for each {SECTION}.",
                          f"Your {DEPORT} line is [blue]{{Amount}}%[/blue] higher for each {SECTION}."),
    "LAW_AND_ORDER_POWER": ("Law and Order", f"At the end of your turn, {DEPORT} ALL enemies.", f"At the end of your turn, {DEPORT} ALL enemies."),
    "SO_MUCH_WINNING_POWER": ("So Much Winning",
                              "Whenever an enemy dies or is Deported, gain Energy and draw cards.",
                              f"Whenever an enemy dies or is Deported, gain {AMT} {{energyPrefix:energyIcons(1)}} and draw {AMT} {{Amount:plural:card|cards}}."),
    "REINFORCED_CONCRETE_POWER": ("Reinforced Concrete",
                                  f"Each {SECTION} gives more [gold]Block[/gold].",
                                  f"Each {SECTION} gives {AMT} more [gold]Block[/gold]."),
    "PROTECTIONISM_POWER": ("Protectionism",
                            "At the start of your turn, apply [gold]Tariff[/gold] to ALL enemies.",
                            f"At the start of your turn, apply {AMT} [gold]Tariff[/gold] to ALL enemies."),
    "MERCH_STAND_POWER": ("Merch Stand", f"At the end of combat, gain {GOLD}.", f"At the end of combat, gain {AMT} {GOLD}."),
    "THEYLL_PAY_FOR_IT_POWER": ("They'll Pay For It",
                                f"Whenever an enemy attacks you and deals no damage to your HP, gain {GOLD}.",
                                f"Whenever an enemy attacks you and deals no damage to your HP, gain {AMT} {GOLD}."),
    "ART_OF_THE_DEAL_POWER": ("The Art of the Deal",
                              f"Whenever you Pay {GOLD}, draw cards.",
                              f"Whenever you Pay {GOLD}, draw {AMT} {{Amount:plural:card|cards}}."),
    "GOLD_TOWER_POWER": ("Gold Tower",
                         f"At the end of your turn, gain [gold]Block[/gold] for the {GOLD} you have.",
                         f"At the end of your turn, gain {AMT} [gold]Block[/gold] for every [blue]30[/blue] {GOLD} you have."),
    "SOCIAL_MEDIA_INTERN_POWER": ("Social Media Intern",
                                  f"At the start of your turn, add a {TWEET} into your {HAND}.",
                                  f"At the start of your turn, add {{Amount:plural:a {TWEET}|[blue]{{}}[/blue] [gold]Tweets[/gold]}} into your {HAND}."),
    "THREE_AM_POSTING_POWER": ("3 AM Posting",
                               f"At the start of your turn, add [gold]Tweets[/gold] into your {HAND}.",
                               f"At the start of your turn, add {{Amount:plural:a {TWEET}|[blue]{{}}[/blue] [gold]Tweets[/gold]}} into your {HAND}."),
    "RATINGS_POWER": ("Ratings", f"Whenever you play a {TWEET}, {B}.", f"Whenever you play a {TWEET}, {B} {AMT}."),
    "VERIFIED_ACCOUNT_POWER": ("Verified Account",
                               "[gold]Tweets[/gold] deal additional damage.",
                               f"[gold]Tweets[/gold] deal {AMT} additional damage."),
    "EXECUTIVE_ORDER_POWER": ("Executive Order",
                              "The first Skill you play each turn costs 0.",
                              "The first {Amount:plural:Skill|[blue]{}[/blue] Skills} you play each turn {Amount:plural:costs|cost} 0 {energyPrefix:energyIcons(1)}."),
    "MAKE_THE_SPIRE_GREAT_AGAIN_POWER": ("Make the Spire Great Again",
                                         f"At the start of your turn, {B} [blue]4[/blue], gain [blue]4[/blue] {GOLD} and add a {TWEET} into your {HAND}.",
                                         f"At the start of your turn, {B} [blue]4[/blue], gain [blue]4[/blue] {GOLD} and add a {TWEET} into your {HAND}."),
}

RELICS = {
    "HARD_HAT": ("Hard Hat", f"Whenever you {B}, {B} [blue]{{Build}}[/blue] additional.", "Worn over the hair. Carefully."),
    "LONG_RED_TIE": ("Long Red Tie", f"Whenever you {DEPORT} an enemy, gain {{Energy:energyIcons()}}.", "Reaches well past the belt. On purpose."),
    "GOLD_PLATED_BRICKS": ("Gold-Plated Bricks", f"At the end of combat, gain [blue]{{Gold}}[/blue] {GOLD} for each {SECTION} of your [gold]Wall[/gold].",
                           "Structurally questionable. Aesthetically tremendous."),
    "RUBBER_STAMP": ("Rubber Stamp", f"Enemies you {DEPORT} still drop their {GOLD}.", "It only has one word on it."),
    "LATE_NIGHT_PHONE": ("Late-Night Phone", f"The first {TWEET} you play each turn is played twice.", "Battery at 1%. Opinions at 100%."),
    "GOLD_PLATED_TOILET": ("Gold-Plated Toilet", f"At the start of your turn, if you have [blue]{{Gold}}[/blue] or more {GOLD}, gain {{Energy:energyIcons()}}.",
                           "The merchant swears it's never been used."),
}

POTIONS = {
    "QUICK_DRY_CONCRETE": ("Quick-Dry Concrete", f"{B} [blue]{{Build}}[/blue]."),
    "COVFEFE_POTION": ("Covfefe", f"Add [blue]{{Cards}}[/blue] Upgraded [gold]Tweets[/gold] into your {HAND}."),
    "DEPORTATION_DRAUGHT": ("Deportation Draught", f"{DEPORT} an enemy, whatever its HP. (Not Bosses.)"),
}


def update(name, entries):
    path = os.path.join(LOC, name)
    loc = {}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as fh:
            loc = json.load(fh)
    loc.update(entries)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(loc, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    print(f"{name}: {len(entries)} keys added or updated, {len(loc)} total")


def main():
    powers = {}
    for key, (title, desc, smart) in POWERS.items():
        powers.update({f"{key}.title": title, f"{key}.description": desc, f"{key}.smartDescription": smart})
    update("powers.json", powers)
    relics = {}
    for key, (title, desc, flavor) in RELICS.items():
        relics.update({f"{key}.title": title, f"{key}.description": desc, f"{key}.flavor": flavor})
    update("relics.json", relics)
    update("potions.json", {k: v for key, (title, desc) in POTIONS.items() for k, v in ((f"{key}.title", title), (f"{key}.description", desc))})


if __name__ == "__main__":
    main()
