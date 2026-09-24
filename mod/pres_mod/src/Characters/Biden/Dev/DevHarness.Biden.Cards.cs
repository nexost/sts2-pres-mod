using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Dev;

/// <summary>
/// Sleepy Joe in the cards test (test.py cards): the setup and snapshots for every card, the key-effect checks, and the
/// relic, potion and power checks. Base versions play as Sleepy Joe with 3 Drowsy; upgraded versions as Dark Brandon.
/// </summary>
public static partial class DevHarness
{
	private static int _bidenNaps;

	private static int _bidenLasers;

	private static int _bidenLastLaser;

	private static bool _bidenCounting;

	private static void BidenCountEvents()
	{
		if (_bidenCounting)
		{
			return;
		}
		_bidenCounting = true;
		DrowsyCmd.NoddedOff += _ => _bidenNaps++;
		DrowsyCmd.LaserFired += (_, damage, hits) =>
		{
			_bidenLasers += hits;
			_bidenLastLaser = damage;
			_bidenLaserTotal += damage * hits;
		};
	}

	private static void BidenSnapshot(Player me, Dictionary<string, decimal> extra)
	{
		Creature joe = me.Creature;
		extra["drowsy"] = DrowsyCmd.GetDrowsy(joe);
		extra["awake"] = DrowsyCmd.IsDarkBrandon(joe) ? 1 : 0;
		extra["naps"] = _bidenNaps;
		extra["lasers"] = _bidenLasers;
		extra["hpBlock"] = joe.CombatState?.HittableEnemies.Sum(e => e.CurrentHp + e.Block) ?? 0;
	}

	/// <summary>A known starting point: every power on Joe removed, then Sleepy Joe with this much Drowsy, or Dark Brandon at 0.</summary>
	private static async Task BidenResetState(PlayerChoiceContext ctx, bool awake, int drowsy)
	{
		Creature joe = Me.Creature;
		foreach (PowerModel power in joe.Powers.ToList())
		{
			await PowerCmd.Remove(power);
		}
		if (awake)
		{
			await DrowsyCmd.WakeUp(ctx, joe);
		}
		else if (drowsy > 0)
		{
			await DrowsyCmd.Doze(ctx, joe, drowsy);
		}
	}

	private static async Task BidenTrimHand()
	{
		foreach (CardModel extra in PileType.Hand.GetPile(Me).Cards.Skip(3).ToList())
		{
			await CardPileCmd.Add(extra, PileType.Discard);
		}
	}

	// ---------------------------------------------------------------- key effects

	/// <summary>Base versions were played as Sleepy Joe at 3 Drowsy, upgraded ones as Dark Brandon (Tangents do all lines, Doze does nothing).</summary>
	private static void BidenCheckCardEffect(string id, bool up, CardSnapshot b, CardSnapshot a, Action<bool, string> Expect)
	{
		void Doze(int amount) => Expect(up ? a.I("drowsy") == b.I("drowsy") : a.I("drowsy") - b.I("drowsy") == amount,
			$"Drowsy {b.I("drowsy")} -> {a.I("drowsy")} (expected {(up ? "no change as Dark Brandon" : "+" + amount)})");
		void Block(int amount) => Expect(a.Block - b.Block == amount, $"Block {b.Block} -> {a.Block} (expected +{amount})");
		void Damage(int atLeast) => Expect(b.I("hpBlock") - a.I("hpBlock") >= atLeast || a.Enemies < b.Enemies,
			$"enemy HP + Block {b.I("hpBlock")} -> {a.I("hpBlock")} (expected at least -{atLeast})");
		void Woke() => Expect(a.I("awake") == 1 && (up || a.I("lasers") - b.I("lasers") == 1) && a.I("drowsy") == 0,
			$"awake {a.I("awake")}, Laser Eyes {b.I("lasers")} -> {a.I("lasers")}, Drowsy {a.I("drowsy")}");
		void Napped() => Expect(a.I("naps") - b.I("naps") == 1 && a.Round > b.Round && a.I("awake") == 1,
			$"nodded off {a.I("naps") - b.I("naps")} time(s), round {b.Round} -> {a.Round}, awake {a.I("awake")}");
		void Drew(int cards) => Expect(a.Hand - b.Hand == cards - 1, $"hand {b.Hand} -> {a.Hand} (expected {cards} drawn, the card itself left)");
		void Energy(int gain) => Expect(a.Energy - b.Energy == gain, $"Energy {b.Energy} -> {a.Energy} (expected {gain:+#;-#;0}; the test plays cards for free)");
		void MyPower(string power) => Expect(a.MyPowers.Contains(power), $"powers {a.MyPowers}");

		switch (id)
		{
			// Sleepy cards: Sleepy Joe Dozes, Dark Brandon doesn't.
			case "CATNAP": Doze(3); Block(up ? 9 : 6); break;
			case "SLEEPWALK": Doze(2); Damage(up ? 10 : 7); break;
			case "SNORE": Doze(2); Damage(up ? 6 : 4); break;
			case "RESTING_MY_EYES": Doze(3); Block(up ? 6 : 4); break;
			case "COUNTING_SHEEP": Doze(2); Drew(up ? 3 : 2); break;
			case "PILLOW_FIGHT": Doze(3); Damage(up ? 12 : 10); break;
			case "FIVE_MORE_MINUTES": Doze(4); Block(up ? 12 : 9); break;
			case "FORTY_WINKS": Doze(4); Energy(1); break;
			case "PILLOW_FORT": Doze(3); Block(up ? 18 : 14); break;
			case "SLEEPWALKER": Doze(4); Damage(up ? 24 : 18); break;
			case "HIBERNATE": Doze(6); Block(up ? 24 : 18); break;
			case "SOUND_ASLEEP": Doze(b.Hand - 1); MyPower("RETAIN_HAND_POWER"); break;
			// Drowsy-scaled hits (3 Drowsy asleep, 0 awake).
			case "GROGGY_HAYMAKER": Damage(up ? 7 : 5 + 3); break;
			case "SLEEPING_GIANT": Damage(up ? 10 : 8 + 2 * 3); break;
			// Wake Up: Laser Eyes for the 3 Drowsy, then Dark Brandon.
			case "CUP_OF_JOE": Woke(); break;
			case "PUT_ON_THE_AVIATORS": Woke(); Drew(up ? 3 : 2); break;
			case "RISE_AND_SHINE": Woke(); Damage(up ? 11 : 8); break;
			case "LASER_SHOW": Woke(); break;
			case "UNLEASHED": Woke(); MyPower("DOUBLE_DAMAGE_POWER"); break;
			case "STATE_OF_THE_UNION": Woke(); Damage(up ? 20 : 14); break;
			case "SECOND_CUP":
				if (up)
				{
					Energy(3);
				}
				else
				{
					Woke();
				}
				break;
			// Nod off: the turn ended (the after snapshot is the next turn, awake).
			case "LIGHTS_OUT":
			case "SLEEP_IN":
			case "OUT_COLD":
			case "DEEP_SLEEP":
				Napped();
				break;
			// Dark Brandon bonuses (the upgraded play is awake).
			case "FINGER_GUNS": Damage(up ? 5 * 4 : 4 * 2); break;
			case "DOGFIGHT": Damage(up ? 8 * 5 : 6 * 3); break;
			case "RED_EYE": Damage(up ? 10 : 8); Drew(up ? 2 : 0); break;
			case "MORNING_PERSON": Block(up ? 10 : 7); Drew(up ? 2 : 0); break;
			case "GAME_FACE": Block(up ? 11 : 8); Energy(up ? 1 : 0); break;
			case "MIC_DROP": Damage(up ? 20 : 15); break;
			case "UP_ALL_NIGHT": Energy(up ? 3 : 2); break;
			case "AVIATOR_GLINT":
				Expect(up ? a.EnemiesWithPower("WEAK_POWER") == a.Enemies : a.TargetPowers.Contains("WEAK_POWER"),
					$"Weak: target {a.TargetPowers}, {a.EnemiesWithPower("WEAK_POWER")} of {a.Enemies} enemies");
				break;
			case "STARE_DOWN":
				Expect(up ? a.EnemiesWithPower("VULNERABLE_POWER") == a.Enemies : a.TargetPowers.Contains("VULNERABLE_POWER"),
					$"Vulnerable: target {a.TargetPowers}, {a.EnemiesWithPower("VULNERABLE_POWER")} of {a.Enemies} enemies");
				break;
			// Tangents: line 1 asleep, every line awake.
			case "HERES_THE_DEAL": Damage(up ? 11 : 8); Block(up ? 10 : 0); Drew(up ? 2 : 0); break;
			case "LONG_STORY_SHORT": Damage(up ? 12 + 6 : 9); break;
			case "ANYWAY": Damage(up ? 7 + 15 : 5); Drew(1); break;
			case "WORD_SALAD": Damage(up ? 18 : 14); Block(up ? 13 : 0); break;
			case "LET_ME_FINISH": Block(up ? 11 : 8); Drew(up ? 3 : 0); break;
			case "FOLKS_LISTEN": Block(up ? 8 : 6); Energy(up ? 1 : 0); Drew(up ? 3 : 0); break;
			case "FILIBUSTER": Block(up ? 16 : 12); Energy(up ? 2 : 0); Drew(up ? 4 : 0); break;
			case "MALAPROP": Damage(up ? 13 : 10); Block(up ? 18 : 0); break;
			case "OFF_THE_CUFF": Damage(up ? 13 + 14 : 10); break;
			case "RAMBLE_ON": Damage(up ? 26 : 20); Block(up ? 20 : 0); break;
			case "BIG_DEAL": Damage(up ? 28 : 22); break;
			case "TALKING_POINTS":
				Expect(a.TargetPowers.Contains("WEAK_POWER") && (!up || (a.TargetPowers.Contains("VULNERABLE_POWER") && a.Block - b.Block == 11)),
					$"target {a.TargetPowers}, Block {b.Block} -> {a.Block}");
				break;
			// General.
			case "CHOCOLATE_CHIP": Block(up ? 5 : 3); Drew(1); break;
			case "FULL_STEAM_AHEAD": Damage(up ? 10 : 8); Drew(1); break;
			case "SUGAR_RUSH": Energy(1); Drew(up ? 2 : 1); break;
			case "ALL_ABOARD": Block(up ? 6 : 4); Drew(up ? 3 : 2); break;
			case "WHERE_WAS_I": Drew(up ? 2 : 1); break;
			case "BRAIN_FREEZE": Drew(up ? 2 : 1); break;
			case "LOST_MY_TRAIN_OF_THOUGHT":
			case "BUILD_BACK_BETTER":
				Expect(a.Hand == b.Hand - 1, $"hand {b.Hand} -> {a.Hand} (expected the same number of cards back, minus this one)");
				break;
			case "THE_BEAST": Damage(up ? 20 : 15); Block(up ? 8 : 6); break;
			case "MOTORCADE": Damage(up ? 18 : 14); Block(up ? 14 : 10); break;
			case "COME_ON_MAN": Expect(a.TargetPowers.Contains("VULNERABLE_POWER"), $"target {a.TargetPowers}"); Drew(1); break;
			case "CORVETTE_CRUISE": MyPower("FREE_ATTACK_POWER"); Drew(up ? 3 : 2); break;
			case "MOMENT_OF_CLARITY": MyPower("MOMENT_OF_CLARITY_POWER"); break;
			// Power cards put their power on him.
			case "WIDE_AWAKE": MyPower("WIDE_AWAKE_POWER"); break;
			case "LASER_FOCUS": MyPower("LASER_FOCUS_POWER"); break;
			case "EARLY_RISER": MyPower("EARLY_RISER_POWER"); break;
			case "POWER_NAP": MyPower("POWER_NAP_POWER"); break;
			case "SNORING": MyPower("SNORING_POWER"); break;
			case "STREAM_OF_CONSCIOUSNESS": MyPower("STREAM_OF_CONSCIOUSNESS_POWER"); break;
			case "LONG_WINDED": MyPower("LONG_WINDED_POWER"); break;
			case "AMTRAK_JOE": MyPower("AMTRAK_JOE_POWER"); break;
			case "BIPARTISAN": MyPower("BIPARTISAN_POWER"); break;
			case "DARK_BRANDON_RISES": MyPower("DARK_BRANDON_RISES_POWER"); break;
			case "NO_MALARKEY": MyPower("NO_MALARKEY_POWER"); break;
			case "DOUBLE_VISION": MyPower("DOUBLE_VISION_POWER"); break;
			case "WELL_RESTED": MyPower("WELL_RESTED_POWER"); break;
			case "HEAVY_SLEEPER": MyPower("HEAVY_SLEEPER_POWER"); break;
			case "OFF_SCRIPT": MyPower("OFF_SCRIPT_POWER"); break;
			case "TALL_TALES": MyPower("TALL_TALES_POWER"); break;
			case "GAFFE_MACHINE": MyPower("GAFFE_MACHINE_POWER"); break;
			case "INFRASTRUCTURE_LAW": MyPower("INFRASTRUCTURE_LAW_POWER"); break;
			case "SOUL_OF_THE_NATION": MyPower("SOUL_OF_THE_NATION_POWER"); break;
		}
	}

	// ---------------------------------------------------------------- relics, potions, powers

	private static async Task BidenRelicChecks()
	{
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature joe = me.Creature;
		var ctx = new BlockingPlayerChoiceContext();

		RunConsole("relic add TRAVEL_PILLOW");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await DrowsyCmd.Doze(ctx, joe, 2m);
		Check(DrowsyCmd.GetDrowsy(joe) == 3, $"Travel Pillow: Doze 2 gave {DrowsyCmd.GetDrowsy(joe)} Drowsy (expected 3)");
		RunConsole("relic remove TRAVEL_PILLOW");
		await Task.Delay(300);

		RunConsole("relic add DARK_BRANDON_MUG");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 6);
		await DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(600);
		Check(_bidenLastLaser == 9, $"Dark Brandon Mug: Laser Eyes at 6 Drowsy hit for {_bidenLastLaser} (expected 9)");
		RunConsole("relic remove DARK_BRANDON_MUG");
		await Task.Delay(300);

		RunConsole("relic add ICE_CREAM_CONE");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 0);
		joe.SetCurrentHpInternal(joe.MaxHp - 10);
		await DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(500);
		Check(joe.CurrentHp == joe.MaxHp - 7, $"Ice Cream Cone: waking healed to {joe.CurrentHp}/{joe.MaxHp} (expected +3)");
		joe.SetCurrentHpInternal(joe.MaxHp);
		RunConsole("relic remove ICE_CREAM_CONE");
		await Task.Delay(300);

		RunConsole("relic add CORVETTE");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		int hand = PileType.Hand.GetPile(me).Cards.Count;
		await DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(800);
		Check(PileType.Hand.GetPile(me).Cards.Count == hand + 2, $"'67 Corvette: waking drew {PileType.Hand.GetPile(me).Cards.Count - hand} (expected 2)");
		RunConsole("relic remove CORVETTE");
		await Task.Delay(300);

		RunConsole("relic add TELEPROMPTER");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		CardModel speech = await AddToHand("LET_ME_FINISH");
		await DrowsyCmd.WakeUp(ctx, joe);
		RunConsole("energy 3");
		await Task.Delay(400);
		int energy = me.PlayerCombatState!.Energy;
		await CardCmd.AutoPlay(ctx, speech, null);
		await Task.Delay(800);
		Check(me.PlayerCombatState.Energy == energy, $"Teleprompter: a Tangent cost {energy - me.PlayerCombatState.Energy} after waking (expected 0)");
		RunConsole("relic remove TELEPROMPTER");
		await Task.Delay(300);

		RunConsole("relic add INDEX_CARDS");
		await Task.Delay(400);
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		var finish = (TangentCard)await AddToHand("LET_ME_FINISH");
		finish.SetLine(finish.LineCount);
		hand = PileType.Hand.GetPile(me).Cards.Count;
		RunConsole("energy 3");
		await CardCmd.AutoPlay(ctx, finish, null);
		await Task.Delay(900);
		Check(PileType.Hand.GetPile(me).Cards.Count == hand - 1 + 2 + 1, $"Index Cards: the last line drew 2, plus 1 ({hand} -> {PileType.Hand.GetPile(me).Cards.Count})");
		RunConsole("relic remove INDEX_CARDS");
		await Task.Delay(300);

		RunConsole("relic add AMTRAK_PASS");
		await Task.Delay(400);
		RunConsole("energy 3");
		await Task.Delay(300);
		energy = me.PlayerCombatState.Energy;
		for (int i = 0; i < 5; i++)
		{
			await BidenTrimHand();
			CardModel chip = await AddToHand("CHOCOLATE_CHIP");
			await CardCmd.AutoPlay(ctx, chip, null);
			await Task.Delay(500);
		}
		Check(me.PlayerCombatState.Energy == energy + 1, $"Amtrak Pass: 5 free cards gave {me.PlayerCombatState.Energy - energy} Energy (expected 1)");
		CheckRelicTexts();
		RunConsole("relic remove AMTRAK_PASS");
		await Task.Delay(300);
		await BidenResetState(ctx, awake: false, drowsy: 0);
	}

	private static async Task BidenPotionChecks()
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			await Fight(CardFightEncounter);
		}
		Player me = Me;
		Creature joe = me.Creature;
		var ctx = new BlockingPlayerChoiceContext();

		await BidenResetState(ctx, awake: false, drowsy: 0);
		PotionModel? potion = await AddPotion("WARM_MILK");
		if (potion != null)
		{
			await potion.OnUseWrapper(ctx, joe);
			await Task.Delay(500);
			Check(DrowsyCmd.GetDrowsy(joe) == 8, $"Warm Milk: Drowsy {DrowsyCmd.GetDrowsy(joe)} (expected 8)");
		}
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		potion = await AddPotion("ESPRESSO_SHOT");
		if (potion != null)
		{
			int hand = PileType.Hand.GetPile(me).Cards.Count;
			await potion.OnUseWrapper(ctx, joe);
			await Task.Delay(700);
			Check(DrowsyCmd.IsDarkBrandon(joe) && PileType.Hand.GetPile(me).Cards.Count == hand + 2,
				$"Espresso Shot: awake {DrowsyCmd.IsDarkBrandon(joe)}, hand {hand} -> {PileType.Hand.GetPile(me).Cards.Count}");
		}
		await BidenResetState(ctx, awake: false, drowsy: 0);
		potion = await AddPotion("DARK_ROAST");
		if (potion != null)
		{
			await potion.OnUseWrapper(ctx, joe);
			await Task.Delay(500);
			Check(DrowsyCmd.IsDarkBrandon(joe) && joe.HasPower<DoubleDamagePower>(),
				$"Dark Roast: awake {DrowsyCmd.IsDarkBrandon(joe)}, Double Damage {joe.HasPower<DoubleDamagePower>()}");
		}
		await BidenResetState(ctx, awake: false, drowsy: 0);
	}

	/// <summary>The powers whose triggers the cards test can't see: on waking, on Tangent line changes, on nodding off, at turn start.</summary>
	private static async Task BidenPowerChecks()
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			await Fight(CardFightEncounter);
		}
		Player me = Me;
		Creature joe = me.Creature;
		ICombatState combat = joe.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();

		// Waking: Laser Focus +4 and Double Vision (2 hits), Wide Awake (5 Block, 1 card), No Malarkey (1 Strength).
		// The laser hits every enemy: keep it under the Ruby Raiders' 18 HP, at full HP, so the fight goes on.
		await BidenResetState(ctx, awake: false, drowsy: 3);
		await BidenTrimHand();
		foreach (Creature enemy in combat.HittableEnemies)
		{
			enemy.SetCurrentHpInternal(enemy.MaxHp);
		}
		await PowerCmd.Apply<LaserFocusPower>(ctx, joe, 4m, joe, null);
		await PowerCmd.Apply<DoubleVisionPower>(ctx, joe, 1m, joe, null);
		await PowerCmd.Apply<WideAwakePower>(ctx, joe, 5m, joe, null);
		await PowerCmd.Apply<NoMalarkeyPower>(ctx, joe, 1m, joe, null);
		int lasers = _bidenLasers;
		int block = joe.Block;
		int hand = PileType.Hand.GetPile(me).Cards.Count;
		await DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(1500);
		Check(_bidenLasers - lasers == 2 && _bidenLastLaser == 7, $"Laser Focus + Double Vision: {_bidenLasers - lasers} hits of {_bidenLastLaser} (expected 2 of 7)");
		Check(joe.Block == block + 5 && PileType.Hand.GetPile(me).Cards.Count == hand + 1,
			$"Wide Awake: Block {block} -> {joe.Block}, hand {hand} -> {PileType.Hand.GetPile(me).Cards.Count}");
		Check(joe.GetPowerAmount<StrengthPower>() == 1, $"No Malarkey: {joe.GetPowerAmount<StrengthPower>()} Strength (expected 1)");

		// Costs and playability the free test plays skip: Mic Drop is free awake, Up All Night only plays awake,
		// Zero to Sixty gets cheaper with each card played this turn.
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		CardModel mic = await AddToHand("MIC_DROP");
		CardModel night = await AddToHand("UP_ALL_NIGHT");
		CardModel sixty = await AddToHand("ZERO_TO_SIXTY");
		RunConsole("energy 3");
		await Task.Delay(300);
		int played = CombatManager.Instance.History.CardPlaysFinished.Count(e => e.HappenedThisTurn(combat) && e.CardPlay.Card.Owner == me);
		Check(mic.EnergyCost.GetAmountToSpend() == 2 && !night.CanPlay(out _, out _), $"Asleep: Mic Drop costs {mic.EnergyCost.GetAmountToSpend()} (expected 2), Up All Night playable {night.CanPlay(out _, out _)} (expected no)");
		Check(sixty.EnergyCost.GetAmountToSpend() == Math.Max(0, 2 - played), $"Zero to Sixty costs {sixty.EnergyCost.GetAmountToSpend()} after {played} cards this turn (expected {Math.Max(0, 2 - played)})");
		await DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(300);
		Check(mic.EnergyCost.GetAmountToSpend() == 0 && night.CanPlay(out _, out _), $"Awake: Mic Drop costs {mic.EnergyCost.GetAmountToSpend()} (expected 0), Up All Night playable {night.CanPlay(out _, out _)} (expected yes)");

		// Heavy Sleeper: the line moves to 15.
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await PowerCmd.Apply<HeavySleeperPower>(ctx, joe, 5m, joe, null);
		await DrowsyCmd.Doze(ctx, joe, 12m);
		Check(DrowsyCmd.GetNodOffLine(joe) == 15 && !DrowsyCmd.HasNoddedOff(joe), $"Heavy Sleeper: line {DrowsyCmd.GetNodOffLine(joe)}, 12 Drowsy and awake (expected line 15)");

		// Tangent powers: a line change grows the card (Tall Tales) and gives Block (Stream of Consciousness).
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await BidenTrimHand();
		await PowerCmd.Apply<TallTalesPower>(ctx, joe, 1m, joe, null);
		await PowerCmd.Apply<StreamOfConsciousnessPower>(ctx, joe, 1m, joe, null);
		RunConsole("energy 5");
		var story = (TangentCard)await AddToHand("LONG_STORY_SHORT");
		decimal damage = story.DynamicVars.Damage.BaseValue;
		block = joe.Block;
		CardModel chipCard = await AddToHand("CHOCOLATE_CHIP");
		await CardCmd.AutoPlay(ctx, chipCard, null);
		await Task.Delay(700);
		Check(story.Line == 2 && story.DynamicVars.Damage.BaseValue == damage + 1, $"Tall Tales: line {story.Line}, damage {damage} -> {story.DynamicVars.Damage.BaseValue}");
		Check(joe.Block >= block + 3 + 1, $"Stream of Consciousness: Block {block} -> {joe.Block} (Chocolate Chip 3, plus 1 per Tangent that moved)");

		// Off Script: the first Tangent each turn does every line.
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await PowerCmd.Apply<OffScriptPower>(ctx, joe, 1m, joe, null);
		var first = (TangentCard)await AddToHand("LET_ME_FINISH");
		bool tangentPlayed = CombatManager.Instance.History.CardPlaysFinished.Any(e => e.HappenedThisTurn(combat) && e.CardPlay.Card is TangentCard && e.CardPlay.Card.Owner == me);
		Check(first.DoesAllLines == !tangentPlayed, $"Off Script: does all lines {first.DoesAllLines} (a Tangent already played this turn: {tangentPlayed})");

		// Nodding off: Power Nap (8 Block), Snoring (6 to ALL), Well Rested (1 Dexterity). The turn ends.
		await BidenResetState(ctx, awake: false, drowsy: 0);
		await PowerCmd.Apply<PowerNapPower>(ctx, joe, 8m, joe, null);
		await PowerCmd.Apply<SnoringPower>(ctx, joe, 6m, joe, null);
		await PowerCmd.Apply<WellRestedPower>(ctx, joe, 1m, joe, null);
		await PowerCmd.Apply<InfrastructureLawPower>(ctx, joe, 1m, joe, null);
		await PowerCmd.Apply<AmtrakJoePower>(ctx, joe, 1m, joe, null);
		foreach (Creature enemy in combat.HittableEnemies)
		{
			enemy.SetCurrentHpInternal(enemy.MaxHp);
		}
		int enemies = combat.HittableEnemies.Count;
		int enemyHp = combat.HittableEnemies.Sum(e => e.CurrentHp + e.Block);
		block = joe.Block;
		int round = combat.RoundNumber;
		await DrowsyCmd.NodOff(ctx, joe);
		await Task.Delay(800);
		Check(joe.Block >= block + 8 + 8, $"Power Nap + Aviator Shades: Block {block} -> {joe.Block} (expected +16)");
		Check(combat.HittableEnemies.Sum(e => e.CurrentHp + e.Block) <= enemyHp - 6 * enemies, $"Snoring: 6 damage to ALL {enemies} enemies");
		Check(joe.GetPowerAmount<DexterityPower>() == 1, $"Well Rested: {joe.GetPowerAmount<DexterityPower>()} Dexterity (expected 1)");
		// Next turn: awake, Infrastructure Law's Energy and Amtrak Joe's extra card.
		await WaitUntil(() => (combat.RoundNumber > round && me.PlayerCombatState!.Phase == PlayerTurnPhase.Play) || !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(30));
		await Task.Delay(1000);
		if (CombatManager.Instance.IsInProgress)
		{
			Check(DrowsyCmd.IsDarkBrandon(joe), "After nodding off: awake the next turn");
			Check(me.PlayerCombatState!.Energy == me.PlayerCombatState.MaxEnergy + 1, $"Infrastructure Law: {me.PlayerCombatState.Energy} Energy (expected {me.PlayerCombatState.MaxEnergy + 1})");
			Check(PileType.Hand.GetPile(me).Cards.Count >= 6, $"Amtrak Joe: {PileType.Hand.GetPile(me).Cards.Count} cards in hand (expected 6)");
		}
		await BidenResetState(ctx, awake: false, drowsy: 0);
	}
}
