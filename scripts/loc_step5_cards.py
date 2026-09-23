"""
One-off helper for Step 5: writes the English text of every card into mod/trump_character/localization/eng/cards.json,
keeping the entries that are already there and dropping the Step 2 filler cards. Card names and effects follow
docs/design/cards.json; the templates follow the base game's own cards (Body Slam, Whirlwind, Hologram, ...).
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, "mod", "trump_character", "localization", "eng", "cards.json")

B = "[gold]Build[/gold]"
WALL = "[gold]Wall[/gold]"
DEPORT = "[gold]Deport[/gold]"
TWEET = "[gold]Tweet[/gold]"
TWEETS = "[gold]Tweets[/gold]"
HAND = "[gold]Hand[/gold]"
SECTION = "[gold]Section[/gold]"


def pay(var="GoldCost"):
    return f"Pay [gold]{{{var}:diff()}} Gold[/gold]."


CARDS = {
    # Wall
    "BRICK_TOSS": ("Brick Toss", f"Deal {{Damage:diff()}} damage.\n{B} {{Build:diff()}}."),
    "WALL_SLAM": ("Wall Slam", f"Deal damage equal to half your {WALL}.{{InCombat:\n(Deals {{CalculatedDamage:diff()}} damage)|}}"),
    "BRICKLAYER": ("Bricklayer", f"{B} {{Build:diff()}}.\nDraw {{Cards:diff()}} {{Cards:plural:card|cards}}."),
    "GROUNDBREAKING": ("Groundbreaking", f"{B} {{Build:diff()}}.\nDraw {{Cards:diff()}} {{Cards:plural:card|cards}}."),
    "POUR_CONCRETE": ("Pour Concrete", f"{B} {{Build:diff()}}."),
    "BARBED_WIRE": ("Barbed Wire", f"{B} {{Build:diff()}}.\nWhenever you are attacked this turn, deal {{BarbedWirePower:diff()}} damage back, plus 1 for each {SECTION}."),
    "TEN_FEET_HIGHER": ("Ten Feet Higher", f"{B} {{Build:diff()}}.\nIf your {WALL} is 25 or more, {B} {{Build:diff()}} more."),
    "HOLD_THE_LINE": ("Hold the Line", f"Gain [gold]Block[/gold] equal to half your {WALL}.{{InCombat:\n(Gains {{CalculatedBlock:diff()}} [gold]Block[/gold])|}}"),
    "DEMOLITION": ("Demolition", f"Deal damage equal to your {WALL}.\nLose half your {WALL}.{{InCombat:\n(Deals {{CalculatedDamage:diff()}} damage)|}}"),
    "SECURITY_DETAIL": ("Security Detail", f"Deal {{Damage:diff()}} damage.\n{B} {{Build:diff()}}."),
    "REBAR": ("Rebar", f"At the start of your turn, {B} {{RebarPower:diff()}}."),
    "GUARD_TOWERS": ("Guard Towers", f"At the end of your turn, deal {{GuardTowersPower:diff()}} damage to a random enemy for each {SECTION}."),
    "WRECKING_BALL": ("Wrecking Ball", f"Deal damage equal to your {WALL} to ALL enemies.\nLose half your {WALL}.{{InCombat:\n(Deals {{CalculatedDamage:diff()}} damage)|}}"),
    "CORNERSTONE": ("Cornerstone", f"Deal {{Damage:diff()}} damage.\n{B} {{Build:diff()}}.\nIf [gold]Fatal[/gold], permanently increase this card's {B} by {{Increase:diff()}}."),
    "GREAT_WALL": ("Great Wall", f"Double your {WALL}."),
    "COALITION_WALL": ("Coalition Wall", f"ALL players {B} {{Build:diff()}}."),
    "NATIONAL_EMERGENCY": ("National Emergency", f"{B} equal to the total damage enemies intend to deal this turn."),
    "INFRASTRUCTURE_WEEK": ("Infrastructure Week", f"At the start of your turn, {B} {{InfrastructureWeekPower:diff()}}."),
    "REINFORCED_CONCRETE": ("Reinforced Concrete", f"Each {SECTION} gives {{ReinforcedConcretePower:diff()}} more [gold]Block[/gold]."),
    "GOLDEN_ESCALATOR": ("Golden Escalator", f"Deal {{Damage:diff()}} damage to ALL enemies.\n{B} {{Build:diff()}}."),
    # Deport
    "ESCORT_OUT": ("Escort Out", f"Deal {{Damage:diff()}} damage.\n{DEPORT}."),
    "SHOW_THEM_THE_DOOR": ("Show Them the Door", f"Deal {{Damage:diff()}} damage twice.\n{DEPORT}."),
    "BORDER_PATROL": ("Border Patrol", f"Deal {{Damage:diff()}} damage to ALL enemies.\n{DEPORT} ALL."),
    "ONE_WAY_TICKET": ("One-Way Ticket", f"Deal {{Damage:diff()}} damage.\n{DEPORT}.\nIf it was Deported, draw {{Cards:diff()}} cards."),
    "BACKGROUND_CHECK": ("Background Check", f"Enemy loses {{HpLoss:diff()}} HP.\n{DEPORT}."),
    "BORDER_CONTROL": ("Border Control", f"Your {DEPORT} line is {{BorderControlPower:diff()}}% higher."),
    "BORDER_WALL": ("Border Wall", f"Your {DEPORT} line is {{BorderWallPower:diff()}}% higher for each {SECTION}."),
    "MASS_DEPORTATION": ("Mass Deportation", f"Deal {{Damage:diff()}} damage to ALL enemies.\n{DEPORT} ALL, with your {DEPORT} line doubled."),
    "LAW_AND_ORDER": ("Law and Order", f"At the end of your turn, {DEPORT} ALL enemies."),
    "SO_MUCH_WINNING": ("So Much Winning", f"Whenever an enemy dies or is Deported, gain {{energyPrefix:energyIcons(1)}} and draw 1 card."),
    "YOURE_FIRED": ("You're Fired!", f"{pay()}\n{DEPORT} the enemy, whatever its HP.\n(Not Bosses.)"),
    # Deals
    "HOSTILE_TAKEOVER": ("Hostile Takeover", "Deal {Damage:diff()} damage.\nIf [gold]Fatal[/gold], gain {Gold:diff()} [gold]Gold[/gold]."),
    "CAMPAIGN_DONATION": ("Campaign Donation", "Gain {Gold:diff()} [gold]Gold[/gold].\nDraw {Cards:diff()} {Cards:plural:card|cards}."),
    "TRADE_WAR": ("Trade War", "Deal {Damage:diff()} damage to ALL enemies.\nApply {TariffPower:diff()} [gold]Tariff[/gold] to ALL enemies."),
    "NET_WORTH": ("Net Worth", "Deal {CalculationBase:diff()} damage, plus 1 for every 10 [gold]Gold[/gold] you have.{InCombat:\n(Deals {CalculatedDamage:diff()} damage)|}"),
    "HIRE_CONTRACTORS": ("Hire Contractors", f"{pay()}\n{B} {{Build:diff()}}."),
    "BRIBE": ("Bribe", f"{pay()}\nGain {{Energy:energyIcons()}}."),
    "WALL_STREET": ("Wall Street", f"Gain {{Gold:diff()}} [gold]Gold[/gold] for each {SECTION}."),
    "CASINO": ("Casino", f"{pay()}\nDraw {{Cards:diff()}} cards."),
    "PROTECTIONISM": ("Protectionism", "At the start of your turn, apply {ProtectionismPower:diff()} [gold]Tariff[/gold] to ALL enemies."),
    "MERCH_STAND": ("Merch Stand", "At the end of combat, gain {MerchStandPower:diff()} [gold]Gold[/gold]."),
    "TARIFF_MAN": ("Tariff Man", "Deal {Damage:diff()} damage.\nDouble the enemy's [gold]Tariff[/gold]."),
    "LEVERAGED_BUYOUT": ("Leveraged Buyout", "Deal {Damage:diff()} damage.\nIf [gold]Fatal[/gold], gain {Gold:diff()} [gold]Gold[/gold]."),
    "MAKE_IT_RAIN": ("Make It Rain", f"{pay()}\nDeal {{Damage:diff()}} damage to ALL enemies {{Repeat:diff()}} times."),
    "CHAPTER11": ("Chapter 11", "Lose all your [gold]Gold[/gold].\nGain 1 [gold]Block[/gold] for every {GoldPerBlock:diff()} [gold]Gold[/gold] lost.{InCombat:\n(Gains {CalculatedBlock:diff()} [gold]Block[/gold])|}"),
    "LAWYER_UP": ("Lawyer Up", f"{pay()}\nGain {{IntangiblePower:diff()}} [gold]Intangible[/gold]."),
    "THEYLL_PAY_FOR_IT": ("They'll Pay For It", "Whenever an enemy attacks you and deals no damage to your HP, gain {TheyllPayForItPower:diff()} [gold]Gold[/gold]."),
    "ART_OF_THE_DEAL": ("The Art of the Deal", "Whenever you Pay [gold]Gold[/gold], draw {ArtOfTheDealPower:diff()} card."),
    "GOLD_TOWER": ("Gold Tower", "At the end of your turn, gain 1 [gold]Block[/gold] for every {GoldPerBlock:diff()} [gold]Gold[/gold] you have."),
    "TRICKLE_DOWN": ("Trickle Down", "ALL players gain {Gold:diff()} [gold]Gold[/gold]."),
    # Tweets
    "ALL_CAPS": ("ALL CAPS", "Deal {Damage:diff()} damage to ALL enemies twice."),
    "LATE_NIGHT_POSTING": ("Late-Night Posting", f"Add {{Cards:diff()}} {TWEETS} into your {HAND}."),
    "GOING_VIRAL": ("Going Viral", f"Deal {{ExtraDamage:diff()}} damage for each {TWEET} played this combat.{{InCombat:\n(Deals {{CalculatedDamage:diff()}} damage)|}}"),
    "RALLY_SPEECH": ("Rally Speech", f"Deal {{Damage:diff()}} damage to ALL enemies.\nAdd a {TWEET} into your {HAND}."),
    "TWEETSTORM": ("Tweetstorm", f"Add X+1 {{IfUpgraded:show:Upgraded |}}{TWEETS} into your {HAND}."),
    "BLOCKED": ("Blocked!", f"This turn, whenever you play a {TWEET}, gain {{BlockedPower:diff()}} [gold]Block[/gold].\nAdd a {TWEET} into your {HAND}."),
    "DOOMSCROLLING": ("Doomscrolling", f"Draw {{Cards:diff()}} cards.\nAdd a {TWEET} into your {HAND}."),
    "SOCIAL_MEDIA_INTERN": ("Social Media Intern", f"At the start of your turn, add a {TWEET} into your {HAND}."),
    "RATINGS": ("Ratings", f"Whenever you play a {TWEET}, {B} {{RatingsPower:diff()}}."),
    "TRENDING": ("Trending", f"Deal {{Damage:diff()}} damage to ALL enemies.\nWhenever you play a {TWEET}, return this to your {HAND} from the [gold]Discard Pile[/gold]."),
    "BURNER_ACCOUNTS": ("Burner Accounts", f"Add {{Cards:diff()}} Upgraded {TWEETS} into your {HAND}."),
    "VERIFIED_ACCOUNT": ("Verified Account", f"{TWEETS} deal {{VerifiedAccountPower:diff()}} additional damage."),
    "THREE_AM_POSTING": ("3 AM Posting", f"At the start of your turn, add {{ThreeAmPostingPower:diff()}} {TWEETS} into your {HAND}."),
    # General
    "SAD": ("Sad!", "Deal {Damage:diff()} damage.\nApply {WeakPower:diff()} [gold]Weak[/gold]."),
    "MULLIGAN": ("Mulligan", "Deal {Damage:diff()} damage.\nDiscard 1 card, then draw {Cards:diff()} card."),
    "POWER_HANDSHAKE": ("Power Handshake", "Deal {Damage:diff()} damage.\nApply {VulnerablePower:diff()} [gold]Vulnerable[/gold]."),
    "LOW_ENERGY": ("Low Energy", "ALL enemies lose {StrengthLoss:diff()} [gold]Strength[/gold] this turn."),
    "ALTERNATIVE_FACTS": ("Alternative Facts", f"Gain {{Block:diff()}} [gold]Block[/gold].\nPut a card from your [gold]Discard Pile[/gold] into your {HAND}."),
    "FIRE_AND_FURY": ("Fire and Fury", "Deal {Damage:diff()} damage to ALL enemies X times."),
    "BIGLY": ("Bigly", "Deal {Damage:diff()} damage.\nIncrease this card's damage by {Increase:diff()} this combat."),
    "WITCH_HUNT": ("Witch Hunt", "Deal {Damage:diff()} damage.\nIf the enemy is [gold]Weak[/gold], hits twice."),
    "COVFEFE": ("Covfefe", f"Add a random Colorless card into your {HAND}. It's free to play this turn."),
    "TWO_SCOOPS": ("Two Scoops", "Gain {Energy:energyIcons()}.{IfUpgraded:show:\nDraw {Cards:diff()} card.|}"),
    "SHARPIE": ("Sharpie", "Enemy loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.\nDraw {Cards:diff()} card."),
    "EXECUTIVE_TIME": ("Executive Time", "Draw {Cards:diff()} cards.\nNext turn, gain {Energy:energyIcons()}."),
    "FAKE_NEWS": ("Fake News", "Apply {WeakPower:diff()} [gold]Weak[/gold].\nApply {VulnerablePower:diff()} [gold]Vulnerable[/gold]."),
    "EXECUTIVE_ORDER": ("Executive Order", "The first Skill you play each turn costs 0 {energyPrefix:energyIcons(1)}."),
    "HOLE_IN_ONE": ("Hole in One", "Can only be played as the first card each turn.\nDeal {Damage:diff()} damage."),
    "GOLF_WEEKEND": ("Golf Weekend", "End your turn.\nNext turn, gain {Energy:energyIcons()} and draw {Cards:diff()} cards."),
    "VERY_STABLE_GENIUS": ("Very Stable Genius", f"[gold]Upgrade[/gold] ALL cards in your {HAND} for the rest of combat.\nDraw {{Cards:diff()}} cards."),
    "MAKE_THE_SPIRE_GREAT_AGAIN": ("Make the Spire Great Again", f"At the start of your turn, {B} {{Build:diff()}}, gain {{Gold:diff()}} [gold]Gold[/gold] and add a {TWEET} into your {HAND}."),
}

EXTRA = {
    "ALTERNATIVE_FACTS.selectionScreenPrompt": "Choose a card to put back in your Hand.",
}


def main():
    with open(PATH, encoding="utf-8") as fh:
        loc = json.load(fh)
    loc = {k: v for k, v in loc.items() if not k.startswith("PLACEHOLDER_")}
    for key, (title, description) in CARDS.items():
        loc[f"{key}.title"] = title
        loc[f"{key}.description"] = description
    loc.update(EXTRA)
    with open(PATH, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(loc, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    print(f"{len(CARDS)} cards written, {len(loc)} keys total")


if __name__ == "__main__":
    main()
