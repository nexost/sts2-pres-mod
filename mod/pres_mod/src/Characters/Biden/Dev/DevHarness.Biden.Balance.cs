using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Dev;

/// <summary>
/// Sleepy Joe for the balance bot (test.py balance): what his cards are worth as they actually play. Only a Tangent's lit
/// line happens (every line as Dark Brandon), multi-hit and "As Dark Brandon" cards change with his form, Doze is progress
/// until it would nod him off mid-turn (then the rest of the turn is lost), and waking fires Laser Eyes.
/// Per fight it records naps, Laser Eyes and the highest Drowsy (balance_report.py prints them).
/// </summary>
public static partial class DevHarness
{
	/// <summary>One Tangent line for the bot: the var that deals damage (hits, to all?), and the ones that give Block, cards, Energy, Weak, Vulnerable.</summary>
	private sealed record BidenLine(string? Damage = null, int Hits = 1, bool All = false, string? Block = null, string? Cards = null,
		string? Energy = null, string? Weak = null, string? Vulnerable = null);

	private static readonly Dictionary<string, BidenLine[]> BidenTangentLines = new Dictionary<string, BidenLine[]>
	{
		["HERES_THE_DEAL"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(Block: "Block"), new BidenLine(Cards: "Cards") },
		["LONG_STORY_SHORT"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(Damage: "AllDamage", All: true) },
		["ANYWAY"] = new[] { new BidenLine(Damage: "Damage", Cards: "Cards"), new BidenLine(Damage: "BigDamage") },
		["WORD_SALAD"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(Damage: "AllDamage", Hits: 2, All: true), new BidenLine(Block: "Block") },
		["LET_ME_FINISH"] = new[] { new BidenLine(Block: "Block"), new BidenLine(Cards: "Cards") },
		["FOLKS_LISTEN"] = new[] { new BidenLine(Block: "Block"), new BidenLine(Energy: "Energy"), new BidenLine(Cards: "Cards") },
		["OFF_THE_CUFF"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(Damage: "TwiceDamage", Hits: 2), new BidenLine(Damage: "AllDamage", Hits: 3, All: true) },
		["MALAPROP"] = new[] { new BidenLine(Damage: "Damage", All: true), new BidenLine(Block: "Block") },
		["FILIBUSTER"] = new[] { new BidenLine(Block: "Block"), new BidenLine(Cards: "Cards"), new BidenLine(Energy: "Energy") },
		["TALKING_POINTS"] = new[] { new BidenLine(Weak: "WeakPower"), new BidenLine(Vulnerable: "VulnerablePower"), new BidenLine(Block: "Block") },
		["RAMBLE_ON"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(Damage: "AllDamage", Hits: 2, All: true), new BidenLine(Block: "Block") },
		["BIG_DEAL"] = new[] { new BidenLine(Damage: "Damage"), new BidenLine(), new BidenLine(Damage: "AllDamage", All: true) }
	};

	/// <summary>Cards that are better as Dark Brandon: waking with them in hand is worth more.</summary>
	private static readonly HashSet<string> BidenAwakeCards = new HashSet<string>
	{
		"FINGER_GUNS", "RED_EYE", "AVIATOR_GLINT", "MORNING_PERSON", "MIC_DROP", "AIR_FORCE_ONE", "STARE_DOWN", "GAME_FACE",
		"UP_ALL_NIGHT", "DOGFIGHT", "SECOND_CUP"
	};

	private static readonly HashSet<string> BidenWakeCards = new HashSet<string>
	{
		"CUP_OF_JOE", "PUT_ON_THE_AVIATORS", "RISE_AND_SHINE", "LASER_SHOW", "UNLEASHED", "STATE_OF_THE_UNION"
	};

	private static readonly HashSet<string> BidenNodOffCards = new HashSet<string> { "LIGHTS_OUT", "SLEEP_IN", "OUT_COLD", "DEEP_SLEEP" };

	private static int _bidenLaserTotal;

	private static (int Naps, int Lasers, int LaserTotal) _bidenCombatStart;

	private static void BidenBalanceStats(Player me, Dictionary<string, object> stats)
	{
		stats["naps"] = _bidenNaps - _bidenCombatStart.Naps;
		stats["lasers"] = _bidenLasers - _bidenCombatStart.Lasers;
		stats["laserDamage"] = _bidenLaserTotal - _bidenCombatStart.LaserTotal;
		stats["maxDrowsy"] = Math.Max(stats.TryGetValue("maxDrowsy", out object? drowsy) ? (int)drowsy : 0, DrowsyCmd.GetDrowsy(me.Creature));
	}

	private static double? BidenCardValueOverride(CardModel card, FightView fight, Creature? target, int turn)
	{
		Creature joe = card.Owner.Creature;
		bool awake = DrowsyCmd.IsDarkBrandon(joe);
		DynamicVarSet v = card.DynamicVars;
		double Get(string key) => v.TryGetValue(key, out DynamicVar? var) ? (double)var.BaseValue : 0;
		int spread = Math.Max(1, fight.Enemies.Count);

		if (card is TangentCard tangent && BidenTangentLines.TryGetValue(card.Id.Entry, out BidenLine[]? lines))
		{
			IEnumerable<int> lit = tangent.DoesAllLines ? Enumerable.Range(1, lines.Length) : new[] { tangent.Line };
			return lit.Sum(line => BidenLineValue(card, lines[line - 1], fight, target));
		}
		switch (card.Id.Entry)
		{
			case "FINGER_GUNS":
				return DamageValue(card, fight, target, Get("Damage") * (awake ? 4 : 2), false);
			case "DOGFIGHT":
				return DamageValue(card, fight, target, Get("Damage") * (awake ? 5 : 3), false);
			case "AIR_FORCE_ONE":
				return DamageValue(card, fight, target, Get("Damage") * (awake ? 3 : 2), true);
			case "PILLOW_FIGHT":
			case "SLEEPWALKER":
				return DamageValue(card, fight, target, Get("Damage") * 2, false) + BidenExtraValue(card, fight);
			case "AVIATOR_GLINT":
				return DamageValue(card, fight, target, Get("Damage"), awake) + Get("WeakPower") * 2.5 * (awake ? spread : 1);
			case "RED_EYE":
				return DamageValue(card, fight, target, Get("Damage"), false) + (awake ? Get("Cards") * 2 : 0);
			case "MORNING_PERSON":
				return BlockValue(fight, Get("Block")) + (awake ? Get("Cards") * 2 : 0);
			case "GAME_FACE":
				return BlockValue(fight, Get("Block")) + (awake ? Get("Energy") * 5 : 0);
			case "STARE_DOWN":
				return Get("VulnerablePower") * 2.5 * (awake ? spread : 1);
			case "SECOND_CUP":
				return awake ? Get("Energy") * 5 : BidenWakeValue(card, fight);
			default:
				return null;
		}
	}

	private static double BidenLineValue(CardModel card, BidenLine line, FightView fight, Creature? target)
	{
		DynamicVarSet v = card.DynamicVars;
		double Get(string? key) => key != null && v.TryGetValue(key, out DynamicVar? var) ? (double)var.BaseValue : 0;
		double value = DamageValue(card, fight, target, Get(line.Damage) * line.Hits, line.All);
		value += BlockValue(fight, Get(line.Block));
		value += Get(line.Cards) * 2 + Get(line.Energy) * 5 + (Get(line.Weak) + Get(line.Vulnerable)) * 2.5;
		return value;
	}

	/// <summary>What Doze, waking and nodding off add to a card's generic value.</summary>
	private static double BidenExtraValue(CardModel card, FightView fight)
	{
		Creature joe = card.Owner.Creature;
		string id = card.Id.Entry;
		double value = 0;
		if (card is SleepyCard && card.DynamicVars.TryGetValue(CardVars.Doze, out DynamicVar? doze))
		{
			value += BidenDozeValue(card, fight, doze.BaseValue);
		}
		else if (id == "SOUND_ASLEEP")
		{
			value += BidenDozeValue(card, fight, PileType.Hand.GetPile(card.Owner).Cards.Count - 1);
		}
		if (BidenWakeCards.Contains(id))
		{
			value += BidenWakeValue(card, fight);
		}
		if (BidenNodOffCards.Contains(id))
		{
			value += BidenNapValue(card, fight, DrowsyCmd.GetDrowsy(joe));
		}
		return value;
	}

	private static int BidenEnergyAfter(CardModel card)
	{
		int energy = card.Owner.PlayerCombatState?.Energy ?? 0;
		return card.EnergyCost.CostsX ? 0 : energy - card.EnergyCost.GetAmountToSpend();
	}

	/// <summary>Doze is progress toward Dark Brandon; if it would nod him off now, it's a nap with the rest of the turn lost.</summary>
	private static double BidenDozeValue(CardModel card, FightView fight, decimal amount)
	{
		Creature joe = card.Owner.Creature;
		if (DrowsyCmd.IsDarkBrandon(joe) || DrowsyCmd.HasNoddedOff(joe) || amount <= 0)
		{
			return 0;
		}
		if (DrowsyCmd.WouldNodOff(joe, amount))
		{
			return BidenNapValue(card, fight, Math.Max(DrowsyCmd.GetNodOffLine(joe), DrowsyCmd.GetDrowsy(joe) + (int)amount));
		}
		return (double)amount;
	}

	/// <summary>A nap: next turn's Laser Eyes and Dark Brandon, the nap Block now, minus a card's worth for each Energy left unspent.</summary>
	private static double BidenNapValue(CardModel card, FightView fight, int drowsyAtNap)
	{
		Creature joe = card.Owner.Creature;
		(int damage, int hits) = DrowsyCmd.LaserFor(joe, drowsyAtNap);
		double laser = fight.Enemies.Sum(e => Math.Min(damage * hits, e.CurrentHp)) * 0.6;
		double lost = Math.Max(0, BidenEnergyAfter(card)) * 7;
		return laser + 6 + BlockValue(fight, 8) - lost;
	}

	/// <summary>Waking now: Laser Eyes for the Drowsy he has, and the Tangent and "As Dark Brandon" cards in hand get better.</summary>
	private static double BidenWakeValue(CardModel card, FightView fight)
	{
		Creature joe = card.Owner.Creature;
		if (DrowsyCmd.IsDarkBrandon(joe))
		{
			return 0;
		}
		int drowsy = DrowsyCmd.GetDrowsy(joe);
		(int damage, int hits) = DrowsyCmd.LaserFor(joe, drowsy);
		double laser = fight.Enemies.Sum(e => Math.Min(damage * hits, e.CurrentHp + e.Block)) * fight.DamageWeight;
		int helped = PileType.Hand.GetPile(card.Owner).Cards.Count(c => c != card && (c is TangentCard || BidenAwakeCards.Contains(c.Id.Entry)));
		return laser + helped * 4 + (drowsy >= 5 ? 4 : 0);
	}
}
