using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using PresMod.Characters.Trump;
using PresMod.Characters.Trump.Cards;
using PresMod.Characters.Trump.Mechanics;
using PresMod.Characters.Trump.Powers;
using PresMod.Characters.Trump.Relics;

namespace PresMod.Dev;

/// <summary>
/// The Donald's test hooks (see Dev/CharacterTests.cs): his Wall, Deport, Pay Gold, Tariff and Tweet checks for the ui
/// and cards tests, the deport sweep mode, and his card values for the balance bot.
/// </summary>
public static partial class DevHarness
{
	/// <summary>Every card starts with this Wall, so Wall-scaling cards have something to scale.</summary>
	private const int TrumpCardTestWall = 30;

	private static int _trumpDeportsThisCombat;

	[CharacterTestKit("TRUMP")]
	private static CharacterTests TrumpTests() => new CharacterTests
	{
		UiChecks = async () =>
		{
			await TrumpMechanicsChecks();
			await TrumpAfterMechanics();
		},
		ExtraModes = { ["deportsweep"] = TrumpDeportSweep },
		RelicChecks = TrumpRelicChecks,
		PotionChecks = TrumpPotionChecks,
		PowerChecks = TrumpPowerChecks,
		CoopTurn = TrumpCoopTurn,
		// The Tweet token isn't in any card pool, so the console can't add it.
		ExtraCards = () => new[] { ModelDb.Card<Tweet>() },
		AddCardToHand = async (card, combat) =>
		{
			if (card is not Tweet)
			{
				return false;
			}
			await Tweet.CreateInHand(Me, 1, combat);
			return true;
		},
		PrepareCardTurn = async ctx =>
		{
			int height = WallCmd.GetHeight(Me.Creature);
			if (height < TrumpCardTestWall)
			{
				await WallCmd.Build(ctx, Me.Creature, TrumpCardTestWall - height);
			}
		},
		Snapshot = (me, extra) =>
		{
			List<CardModel> hand = PileType.Hand.GetPile(me).Cards.ToList();
			extra["wall"] = WallCmd.GetHeight(me.Creature);
			extra["tweets"] = hand.Count(c => c is Tweet);
			extra["upgradedTweets"] = hand.Count(c => c is Tweet && c.IsUpgraded);
			extra["line"] = DeportCmd.GetLine(me);
		},
		CheckCardEffect = TrumpCheckCardEffect,
		BalanceInit = () => DeportCmd.Deported += _ => _trumpDeportsThisCombat++,
		BalanceCombatStart = () => _trumpDeportsThisCombat = 0,
		BalanceCombatStats = (me, stats) =>
		{
			stats["maxWall"] = Math.Max(stats.TryGetValue("maxWall", out object? wall) ? (int)wall : 0, WallCmd.GetHeight(me.Creature));
			stats["deports"] = _trumpDeportsThisCombat;
		},
		// A Deport removes the enemy once the hit takes it under the line (bosses are immune).
		RemovesEnemy = (fight, enemy, damage, card) => card is IDeportingCard deporting && !DeportCmd.IsImmune(enemy)
			&& enemy.CurrentHp - Math.Max(0, damage - enemy.Block) <= (double)DeportCmd.LineHp(enemy, fight.Me, deporting.DeportLineMultiplier),
		CardValue = (card, fight, get) =>
		{
			int spread = card.TargetType == TargetType.AllEnemies ? Math.Max(1, fight.Enemies.Count) : 1;
			return TrumpBuildValue(fight, get(CardVars.Build)) + get("TariffPower") * 1.5 * spread - get(PayGoldCard.GoldCostVar) * 0.04;
		}
	};

	// ---------------------------------------------------------------- ui test: the mechanics fight

	private static async Task TrumpMechanicsChecks()
	{
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();

		Check(WallCmd.GetHeight(donald) == 2, $"Golden Shovel: Wall is {WallCmd.GetHeight(donald)} on turn 1 (expected 2)");
		Check(me.Character.StartingHp == 70 && donald.MaxHp == 70, $"Max HP {donald.MaxHp} (expected 70)");

		// Pay Gold: unplayable when broke, pays and works when not.
		CardModel loan = await AddToHand("SMALL_LOAN");
		await PlayerCmd.SetGold(5, me);
		await Task.Delay(400);
		Check(!loan.CanPlay(out _, out _), "Small Loan unplayable with 5 Gold");
		Screenshot("pay_gold_unaffordable");
		await PlayerCmd.SetGold(99, me);
		await Task.Delay(400);
		Check(loan.CanPlay(out _, out _), "Small Loan playable with 99 Gold");
		Screenshot("pay_gold_badge");
		int energyBefore = me.PlayerCombatState!.Energy;
		await CardCmd.AutoPlay(ctx, loan, null);
		await Task.Delay(800);
		Check(me.Gold == 89, $"Small Loan paid 10 Gold (gold now {me.Gold})");
		Check(me.PlayerCombatState.Energy == energyBefore + 1, $"Small Loan gave 1 Energy ({energyBefore} -> {me.PlayerCombatState.Energy})");

		// Tweets.
		Creature enemy = combat.HittableEnemies.First();
		RunConsole("energy 5");
		CardModel mean = await AddToHand("MEAN_TWEET");
		await CardCmd.AutoPlay(ctx, mean, enemy);
		await Task.Delay(800);
		CardModel? tweet = PileType.Hand.GetPile(me).Cards.FirstOrDefault(c => c is Tweet);
		Check(tweet != null, "Mean Tweet added a Tweet to the hand");
		Screenshot("tweet_in_hand");
		if (tweet != null)
		{
			int hpBefore = enemy.CurrentHp;
			await CardCmd.AutoPlay(ctx, tweet, null);
			await Task.Delay(800);
			Check(enemy.CurrentHp < hpBefore, $"Tweet hit ALL enemies ({hpBefore} -> {enemy.CurrentHp})");
		}

		// Gold frame next to the Regent's orange one (the closest base-game colour).
		await AddToHand("BEAT_INTO_SHAPE");
		Screenshot("frame_vs_regent");

		// Tariff.
		CardModel slap = await AddToHand("SLAP_A_TARIFF");
		await CardCmd.AutoPlay(ctx, slap, enemy);
		await Task.Delay(800);
		Check(enemy.GetPowerAmount<TariffPower>() == 2, $"Slap a Tariff applied Tariff {enemy.GetPowerAmount<TariffPower>()}");

		// Wall stages.
		await Task.Delay(1500);
		Screenshot("wall_start_6");
		foreach (int target in new[] { 11, 25, 45, 70 })
		{
			await WallCmd.Build(ctx, donald, target - WallCmd.GetHeight(donald));
			await Task.Delay(900);
			Screenshot($"wall_{target}_stage{WallCmd.GetStage(donald)}");
			await Task.Delay(1200);
		}
		Check(WallCmd.GetStage(donald) == 4 && WallCmd.GetSections(donald) == 7, $"Wall 70: stage {WallCmd.GetStage(donald)}, sections {WallCmd.GetSections(donald)}");

		// End turn: Section Block, stage-4 damage, Tariff gold; next turn: +1 draw, +1 energy.
		enemy.SetCurrentHpInternal(Math.Max(enemy.CurrentHp, 30));
		int enemyHp = enemy.CurrentHp;
		int expectedLoss = Math.Max(0, 7 - enemy.Block);
		int gold = me.Gold;
		int round = combat.RoundNumber;
		int heightBefore = WallCmd.GetHeight(donald);
		bool enemyAttacks = enemy.Monster?.NextMove?.Intents?.Any(i => i.GetType().Name.Contains("Attack")) ?? false;
		PlayerCmd.EndTurn(me, canBackOut: false);
		await WaitHelper.Until(() => donald.Block > 0 || combat.RoundNumber > round, _ct, TimeSpan.FromSeconds(15));
		Check(donald.Block >= 21, $"7 Sections gave {donald.Block} Block (expected 21)");
		Screenshot("end_turn_section_block");
		// The stage-4 damage lands right after the Section Block, before the enemy turn starts.
		await WaitHelper.Until(() => enemy.CurrentHp < enemyHp || !enemy.IsAlive || combat.RoundNumber > round, _ct, TimeSpan.FromSeconds(15));
		Check(enemy.CurrentHp <= enemyHp - expectedLoss || !enemy.IsAlive, $"Big Beautiful Wall stage hit the enemy for 7 ({enemyHp} -> {enemy.CurrentHp}, expected -{expectedLoss} after its Block)");
		await WaitHelper.Until(() => combat.RoundNumber > round && me.PlayerCombatState.Phase == PlayerTurnPhase.Play, _ct, TimeSpan.FromSeconds(30));
		await Task.Delay(1500);
		Check(WallCmd.GetHeight(donald) == heightBefore + 2, $"Golden Shovel built 2 at the start of the turn ({heightBefore} -> {WallCmd.GetHeight(donald)})");
		Check(me.PlayerCombatState.MaxEnergy == 4, $"Concrete stage: max energy {me.PlayerCombatState.MaxEnergy} (expected 4)");
		Check(PileType.Hand.GetPile(me).Cards.Count >= 6, $"Brick stage: drew {PileType.Hand.GetPile(me).Cards.Count} cards (expected 6)");
		if (enemyAttacks)
		{
			Check(me.Gold == gold + 2, $"Tariff paid 2 Gold when the enemy attacked ({gold} -> {me.Gold})");
		}
		else
		{
			Note($"Tariff: enemy didn't attack this turn (gold {gold} -> {me.Gold})");
		}
		Screenshot("next_turn_perks");

		// Demolition keeps the stages.
		await WallCmd.LoseHeight(ctx, donald, WallCmd.GetHeight(donald));
		await Task.Delay(800);
		Check(WallCmd.GetHeight(donald) == 0 && WallCmd.GetStage(donald) == 4, $"Wall torn down to {WallCmd.GetHeight(donald)}, stage still {WallCmd.GetStage(donald)}");
		Screenshot("wall_demolished_stage_kept");

		// Permanent Structure: rebuild to 40, a quarter (10) is kept when this combat ends (checked in RunUiTest).
		RunConsole("relic add PERMANENT_STRUCTURE");
		await Task.Delay(600);
		await WallCmd.Build(ctx, donald, 40 - WallCmd.GetHeight(donald));

		// Deport line: 25% of max HP.
		decimal line = DeportCmd.LineHp(enemy, me);
		enemy.SetCurrentHpInternal((int)Math.Floor(line) + 1);
		await Task.Delay(1200);
		Check(!DeportCmd.CanDeport(enemy, me), $"{enemy.CurrentHp}/{enemy.MaxHp} HP is above the line ({line:0.##})");
		Screenshot("deport_line_above");
		enemy.SetCurrentHpInternal((int)Math.Floor(line));
		await Task.Delay(1200);
		Check(DeportCmd.CanDeport(enemy, me), $"{enemy.CurrentHp}/{enemy.MaxHp} HP is under the line");
		Screenshot("deport_denied_stamp");
		CardModel deport = await AddToHand("DEPORT");
		enemy.SetCurrentHpInternal((int)Math.Floor(line) + 5);
		RunConsole("energy 2");
		await CardCmd.AutoPlay(ctx, deport, enemy);
		await Task.Delay(300);
		Screenshot("deported_popup");
		Check(combat.EscapedCreatures.Contains(enemy), $"Deport card ({deport.DynamicVars.Damage.IntValue} damage) took the enemy under the line and Deported it");
		await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, _ct, TimeSpan.FromSeconds(20));
		await Task.Delay(2500);
		Screenshot("rewards_after_deport");
	}

	// ---------------------------------------------------------------- deport sweep (test.py deportsweep)

	private static async Task TrumpDeportSweep()
	{
		await StartRunAsCharacter(screenshots: false);
		RunConsole("godmode");
		// --pres-encounters A,B,C limits the sweep to those encounters.
		var only = (CommandLineHelper.GetValue("pres-encounters") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		var encounters = ModelDb.AllEncounters.Where(e => only.Count == 0 || only.Contains(e.Id.Entry)).OrderBy(e => e.Id.Entry).ToList();
		Note($"Deport sweep over {encounters.Count} encounters");
		foreach (EncounterModel encounter in encounters)
		{
			string id = encounter.Id.Entry;
			var result = new Dictionary<string, object> { ["encounter"] = id, ["room"] = encounter.RoomType.ToString() };
			Log.Info($"[{ModEntry.ModId}:sweep] BEGIN {id}");
			try
			{
				await Fight(id, TimeSpan.FromSeconds(25));
				ICombatState combat = Me.Creature.CombatState!;
				var ctx = new BlockingPlayerChoiceContext();
				result["enemies"] = combat.Enemies.Where(e => e.IsAlive).Select(DescribeForDeport).ToList();
				// Deport one enemy at a time and pass a turn after each, so the ones left act without their partner
				// (bosses commanding minions, groups that buff or revive each other). Capped for encounters that keep summoning.
				var deported = new List<string>();
				int turns = 0;
				for (int i = 0; i < 8 && CombatManager.Instance.IsInProgress; i++)
				{
					Creature? target = combat.Enemies.FirstOrDefault(e => e.IsAlive && !DeportCmd.IsImmune(e));
					if (target == null)
					{
						break;
					}
					target.SetCurrentHpInternal(1);
					if (!await DeportCmd.TryDeport(ctx, target, Me))
					{
						Error($"sweep {id}: could not Deport {DescribeForDeport(target)}");
						break;
					}
					deported.Add(DescribeForDeport(target));
					if (!CombatManager.Instance.IsInProgress || !combat.Enemies.Any(e => e.IsAlive))
					{
						break;
					}
					if (!await PassTurn(combat))
					{
						Error($"sweep {id}: the turn after Deporting {DescribeForDeport(target)} never came back");
						break;
					}
					turns++;
				}
				result["deported"] = deported;
				result["turnsAfterDeport"] = turns;
				// Immune bosses are finished with the game's `win` command: it strips powers first, so bosses that revive
				// (Test Subject's three forms) stay dead, then checks for victory.
				if (CombatManager.Instance.IsInProgress && combat.Enemies.Any(e => e.IsAlive))
				{
					RunConsole("win");
				}
				bool ended = await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
				result["ended"] = ended;
				if (!ended)
				{
					Error($"sweep {id}: combat did not end (alive: {string.Join(",", combat.Enemies.Where(e => e.IsAlive).Select(DescribeForDeport))})");
				}
				await Task.Delay(1200);
			}
			catch (Exception e)
			{
				result["error"] = e.Message;
				Error($"sweep {id}: {e.Message}");
			}
			Log.Info($"[{ModEntry.ModId}:sweep] END {id}");
			_sweep.Add(result);
		}
		Note($"Deport sweep done: {_sweep.Count} encounters");
	}

	private static string DescribeForDeport(Creature enemy)
	{
		return $"{enemy.Monster?.Id.Entry}{(enemy.IsPrimaryEnemy ? "" : "(minion)")}{(DeportCmd.IsImmune(enemy) ? "(immune)" : "")}";
	}

	/// <summary>After the mechanics fight: Permanent Structure's carried Wall, boss immunity with the Diamond Shovel, Touch of Orobas.</summary>
	private static async Task TrumpAfterMechanics()
	{
		// Permanent Structure kept a quarter of the 40 Wall, in a property the save file carries.
		PermanentStructure? structure = Me.Relics.OfType<PermanentStructure>().FirstOrDefault();
		Check(structure?.StoredHeight == 10, $"Permanent Structure kept {structure?.StoredHeight} height (expected 10)");
		int saved = structure == null ? -1 : SavedProperties.From(structure)?.ints?.FirstOrDefault(p => p.name == nameof(PermanentStructure.StoredHeight)).value ?? -1;
		Check(saved == 10, $"Permanent Structure height is in the save data ({saved})");

		// Swap in the Ancient version for the boss fight: Diamond Shovel builds 4 per turn.
		RunConsole("relic remove GOLDEN_SHOVEL");
		RunConsole("relic add DIAMOND_SHOVEL");
		await Task.Delay(600);

		// Boss immunity: primary enemies in boss rooms can't be Deported, minions can.
		EncounterModel boss = ModelDb.Acts.First().AllEncounters.First(e => e.RoomType == RoomType.Boss);
		await Fight(boss.Id.Entry);
		Check(WallCmd.GetHeight(Me.Creature) == 14, $"Next combat's Wall starts at {WallCmd.GetHeight(Me.Creature)} (Diamond Shovel 4 + kept 10)");
		foreach (Creature enemy in Me.Creature.CombatState!.Enemies.Where(e => e.IsAlive))
		{
			Check(DeportCmd.IsImmune(enemy) == enemy.IsPrimaryEnemy, $"boss room {boss.Id.Entry}: {enemy.Monster?.Id.Entry} primary={enemy.IsPrimaryEnemy} immune={DeportCmd.IsImmune(enemy)}");
		}
		Screenshot("boss_no_deport_line");

		// Touch of Orobas maps our starter to its Ancient version.
		var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
		RelicModel upgraded = touch.GetUpgradedStarterRelic(ModelDb.Relic<GoldenShovel>());
		Check(upgraded is DiamondShovel, $"Touch of Orobas: Golden Shovel -> {upgraded.Id.Entry}");
	}

	// ---------------------------------------------------------------- cards test

	/// <summary>Assertions for the effects most likely to go wrong. Everything else is covered by the logged diff.</summary>
	private static void TrumpCheckCardEffect(string id, bool up, CardSnapshot b, CardSnapshot a, Action<bool, string> Expect)
	{
		switch (id)
		{
			case "BUILD_THE_WALL":
				Expect(a.I("wall") - b.I("wall") == (up ? 8 : 5), $"Wall {b.I("wall")} -> {a.I("wall")}");
				break;
			case "BRICK_TOSS":
				Expect(a.I("wall") - b.I("wall") == (up ? 5 : 3) && a.EnemyHp < b.EnemyHp, $"Wall {b.I("wall")} -> {a.I("wall")}, enemy HP {b.EnemyHp} -> {a.EnemyHp}");
				break;
			case "POUR_CONCRETE":
				Expect(a.I("wall") - b.I("wall") == (up ? 16 : 12), $"Wall {b.I("wall")} -> {a.I("wall")}");
				break;
			case "DEMOLITION":
			case "WRECKING_BALL":
				// If the hit ends the fight, the Wall is left as it was (the combat is already over).
				Expect((a.I("wall") == b.I("wall") - b.I("wall") / 2 || a.Enemies == 0) && a.EnemyHp < b.EnemyHp, $"Wall {b.I("wall")} -> {a.I("wall")}, enemy HP {b.EnemyHp} -> {a.EnemyHp}");
				break;
			case "GREAT_WALL":
				Expect(a.I("wall") == b.I("wall") * 2, $"Wall {b.I("wall")} -> {a.I("wall")}");
				break;
			case "HOLD_THE_LINE":
				Expect(a.Block - b.Block == b.I("wall") / 2, $"Block {b.Block} -> {a.Block} with Wall {b.I("wall")}");
				break;
			case "TEN_FEET_HIGHER":
				Expect(a.I("wall") - b.I("wall") == (up ? 14 : 10), $"Wall {b.I("wall")} -> {a.I("wall")} (over 25, so it builds twice)");
				break;
			case "WALL_STREET":
				Expect(a.Gold - b.Gold == (up ? 6 : 4) * (b.I("wall") / 10), $"Gold {b.Gold} -> {a.Gold} with {b.I("wall") / 10} Sections");
				break;
			case "CHAPTER11":
				Expect(a.Gold == 0 && a.Block - b.Block == b.Gold / (up ? 3 : 4), $"Gold {b.Gold} -> {a.Gold}, Block {b.Block} -> {a.Block}");
				break;
			case "BRIBE":
				Expect(b.Gold - a.Gold == (up ? 15 : 20) && a.Energy - b.Energy == 2, $"Gold {b.Gold} -> {a.Gold}, Energy {b.Energy} -> {a.Energy}");
				break;
			case "SMALL_LOAN":
				Expect(b.Gold - a.Gold == 10 && a.Energy - b.Energy == 1, $"Gold {b.Gold} -> {a.Gold}, Energy {b.Energy} -> {a.Energy}");
				break;
			case "CASINO":
				Expect(b.Gold - a.Gold == 10, $"Gold {b.Gold} -> {a.Gold}");
				break;
			case "HIRE_CONTRACTORS":
				Expect(b.Gold - a.Gold == 20 && a.I("wall") - b.I("wall") == (up ? 18 : 14), $"Gold {b.Gold} -> {a.Gold}, Wall {b.I("wall")} -> {a.I("wall")}");
				break;
			case "CAMPAIGN_DONATION":
				Expect(a.Gold - b.Gold == (up ? 12 : 8), $"Gold {b.Gold} -> {a.Gold}");
				break;
			case "YOURE_FIRED":
				Expect(b.Gold - a.Gold == (up ? 75 : 99) && a.Enemies == b.Enemies - 1, $"Gold {b.Gold} -> {a.Gold}, enemies {b.Enemies} -> {a.Enemies}");
				break;
			case "LAWYER_UP":
				Expect(b.Gold - a.Gold == (up ? 30 : 40) && a.MyPowers.Contains("INTANGIBLE_POWER"), $"Gold {b.Gold} -> {a.Gold}, powers {a.MyPowers}");
				break;
			case "TWO_SCOOPS":
				Expect(a.Energy - b.Energy == 2, $"Energy {b.Energy} -> {a.Energy}");
				break;
			case "LATE_NIGHT_POSTING":
				Expect(a.I("tweets") - b.I("tweets") == (up ? 3 : 2), $"Tweets in hand {b.I("tweets")} -> {a.I("tweets")}");
				break;
			case "BURNER_ACCOUNTS":
				Expect(a.I("upgradedTweets") - b.I("upgradedTweets") == (up ? 5 : 4), $"Upgraded Tweets in hand {b.I("upgradedTweets")} -> {a.I("upgradedTweets")}");
				break;
			case "TWEETSTORM":
				// X+1 Tweets, capped by the 10-card hand (the Tweetstorm itself leaves the hand first).
				Expect(a.I("tweets") - b.I("tweets") == Math.Min(b.Energy + 1, MaxHandSize - (b.Hand - 1)), $"Tweets in hand {b.I("tweets")} -> {a.I("tweets")} with {b.Energy} Energy and {b.Hand - 1} other cards");
				break;
			case "MEAN_TWEET":
			case "RALLY_SPEECH":
			case "BLOCKED":
			case "DOOMSCROLLING":
				Expect(a.I("tweets") - b.I("tweets") >= 1, $"Tweets in hand {b.I("tweets")} -> {a.I("tweets")}");
				break;
			case "SAD":
				Expect(a.TargetPowers.Contains("WEAK_POWER"), $"target powers {a.TargetPowers}");
				break;
			case "POWER_HANDSHAKE":
				Expect(a.TargetPowers.Contains("VULNERABLE_POWER"), $"target powers {a.TargetPowers}");
				break;
			case "FAKE_NEWS":
				Expect(a.TargetPowers.Contains("WEAK_POWER") && a.TargetPowers.Contains("VULNERABLE_POWER"), $"target powers {a.TargetPowers}");
				break;
			case "SLAP_A_TARIFF":
				Expect(a.TargetPowers.Contains("TARIFF_POWER"), $"target powers {a.TargetPowers}");
				break;
			case "TRADE_WAR":
				Expect(a.EnemiesWithPower("TARIFF_POWER") == a.Enemies, $"Tariff on {a.EnemiesWithPower("TARIFF_POWER")} of {a.Enemies} enemies");
				break;
			case "LOW_ENERGY":
				Expect(a.EnemiesWithPower("LOW_ENERGY_POWER") == a.Enemies, $"Low Energy on {a.EnemiesWithPower("LOW_ENERGY_POWER")} of {a.Enemies} enemies");
				break;
			case "BORDER_CONTROL":
				Expect(a.D("line") - b.D("line") == (up ? 0.15m : 0.10m), $"Deport line {b.D("line"):0.##} -> {a.D("line"):0.##}");
				break;
			case "COVFEFE":
				Expect(a.Hand >= b.Hand, $"hand {b.Hand} -> {a.Hand}");
				break;
			case "GOLF_WEEKEND":
				Expect(a.Round > b.Round, $"round {b.Round} -> {a.Round} (the turn ended)");
				break;
		}
		if (id is "REBAR" or "GUARD_TOWERS" or "BORDER_WALL" or "PROTECTIONISM" or "MERCH_STAND" or "SOCIAL_MEDIA_INTERN" or "RATINGS"
			or "EXECUTIVE_ORDER" or "INFRASTRUCTURE_WEEK" or "REINFORCED_CONCRETE" or "LAW_AND_ORDER" or "THEYLL_PAY_FOR_IT"
			or "ART_OF_THE_DEAL" or "GOLD_TOWER" or "SO_MUCH_WINNING" or "VERIFIED_ACCOUNT" or "THREE_AM_POSTING" or "MAKE_THE_SPIRE_GREAT_AGAIN")
		{
			Expect(a.MyPowers.Contains(id + "_POWER"), $"power applied ({a.MyPowers})");
		}
	}

	/// <summary>
	/// Co-op: the host plays Coalition Wall (ALL players Build 12), the client Trickle Down (ALL players gain 12 Gold)
	/// and Slap a Tariff. Each checks every player, so both sides must see the other's Wall and Gold change.
	/// </summary>
	private static async Task TrumpCoopTurn(bool host, ICombatState combat)
	{
		List<Player> players = RunManager.Instance.DebugOnlyGetState()!.Players.ToList();
		if (host)
		{
			Dictionary<ulong, int> walls = players.ToDictionary(p => p.NetId, p => WallCmd.GetHeight(p.Creature));
			await PlayCoopCard("COALITION_WALL", null, combat);
			foreach (Player p in players)
			{
				int now = WallCmd.GetHeight(p.Creature);
				Check(now == walls[p.NetId] + 12, $"Coalition Wall: player {p.NetId} Wall {walls[p.NetId]} -> {now} (expected +12)");
			}
		}
		else
		{
			Dictionary<ulong, int> gold = players.ToDictionary(p => p.NetId, p => p.Gold);
			await PlayCoopCard("TRICKLE_DOWN", null, combat);
			foreach (Player p in players)
			{
				Check(p.Gold == gold[p.NetId] + 12, $"Trickle Down: player {p.NetId} Gold {gold[p.NetId]} -> {p.Gold} (expected +12)");
			}
			Creature enemy = combat.HittableEnemies.First();
			await PlayCoopCard("SLAP_A_TARIFF", enemy, combat);
			Check(enemy.GetPowerAmount<TariffPower>() == 2, $"Slap a Tariff: Tariff {enemy.GetPowerAmount<TariffPower>()} (expected 2)");
		}
	}

	private static async Task TrumpRelicChecks()
	{
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();

		RunConsole("relic add HARD_HAT");
		await Task.Delay(400);
		int height = WallCmd.GetHeight(donald);
		await WallCmd.Build(ctx, donald, 5);
		Check(WallCmd.GetHeight(donald) - height == 6, $"Hard Hat: Build 5 built {WallCmd.GetHeight(donald) - height} (expected 6)");
		CheckRelicTexts();
		RunConsole("relic remove HARD_HAT");

		RunConsole("relic add LONG_RED_TIE");
		await Task.Delay(400);
		int energy = me.PlayerCombatState!.Energy;
		Creature first = combat.HittableEnemies.First();
		first.SetCurrentHpInternal(1);
		await DeportCmd.TryDeport(ctx, first, me);
		await Task.Delay(500);
		Check(me.PlayerCombatState.Energy == energy + 1, $"Long Red Tie: Deport gave {me.PlayerCombatState.Energy - energy} Energy (expected 1)");
		RunConsole("relic remove LONG_RED_TIE");

		RunConsole("relic add LATE_NIGHT_PHONE");
		await Task.Delay(400);
		int tweets = Tweet.PlayedThisCombat(me);
		CardModel tweet = (await Tweet.CreateInHand(me, 1, combat)).First();
		await CardCmd.AutoPlay(ctx, tweet, null);
		await Task.Delay(1200);
		Check(Tweet.PlayedThisCombat(me) - tweets == 2, $"Late-Night Phone: the first Tweet was played {Tweet.PlayedThisCombat(me) - tweets} times (expected 2)");
		RunConsole("relic remove LATE_NIGHT_PHONE");

		RunConsole("relic add GOLD_PLATED_TOILET");
		await Task.Delay(400);
		await PlayerCmd.SetGold(300, me);
		int round = combat.RoundNumber;
		PlayerCmd.EndTurn(me, canBackOut: false);
		await WaitUntil(() => combat.RoundNumber > round && me.PlayerCombatState.Phase == PlayerTurnPhase.Play, TimeSpan.FromSeconds(30));
		await Task.Delay(500);
		Check(me.PlayerCombatState.Energy == me.PlayerCombatState.MaxEnergy + 1, $"Gold-Plated Toilet: {me.PlayerCombatState.Energy} Energy with 300 Gold (expected {me.PlayerCombatState.MaxEnergy + 1})");
		RunConsole("relic remove GOLD_PLATED_TOILET");

		// Gold-Plated Bricks and Rubber Stamp: Deport everyone with a 40 Wall, then read the reward.
		RunConsole("relic add GOLD_PLATED_BRICKS");
		RunConsole("relic add RUBBER_STAMP");
		await Task.Delay(400);
		CheckRelicTexts();
		await WallCmd.Build(ctx, donald, Math.Max(0, 40 - WallCmd.GetHeight(donald)));
		int sections = WallCmd.GetSections(donald);
		int gold = me.Gold;
		CombatRoom? room = RunManager.Instance.DebugOnlyGetState()?.CurrentRoom as CombatRoom;
		Note($"Before the final Deports: {combat.Enemies.Count} enemies ({combat.HittableEnemies.Count} hittable), {combat.EscapedCreatures.Count} escaped, room {room?.Encounter.Id.Entry}");
		foreach (Creature enemy in combat.HittableEnemies.ToList())
		{
			enemy.SetCurrentHpInternal(1);
			bool deported = await DeportCmd.TryDeport(ctx, enemy, me);
			Note($"Deport {enemy.Monster?.Id.Entry}: {deported}, in progress: {CombatManager.Instance.IsInProgress}");
		}
		bool ended = await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
		await WaitUntil(() => me.Gold != gold, TimeSpan.FromSeconds(5));
		await Task.Delay(1000);
		Note($"After: combat ended {ended}, escaped {combat.EscapedCreatures.Count} (deported {combat.EscapedCreatures.Count(DeportCmd.WasDeported)}), spawned {room?.Encounter.SpawnedEnemies.Count}, gold {gold} -> {me.Gold}");
		Check(me.Gold - gold == 2 * sections, $"Gold-Plated Bricks: +{me.Gold - gold} Gold for {sections} Sections (expected {2 * sections})");
		float proportion = room?.GoldProportion ?? -1f;
		Check(Math.Abs(proportion - 1f) < 0.001f, $"Rubber Stamp: Gold reward share {proportion:0.##} after Deporting everyone (expected 1)");
		RunConsole("relic remove GOLD_PLATED_BRICKS");
		RunConsole("relic remove RUBBER_STAMP");
		await Task.Delay(400);
	}

	/// <summary>
	/// The turn-based powers, applied directly, then one full turn: end-of-turn Section Block (with Reinforced Concrete)
	/// and Guard Towers, start-of-turn Builds, Tweets and Tariffs, then Ratings and Verified Account on a Tweet, and
	/// Merch Stand when the fight ends.
	/// </summary>
	private static async Task TrumpPowerChecks()
	{
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();
		await WallCmd.Build(ctx, donald, 30 - WallCmd.GetHeight(donald));
		await PlayerCmd.SetGold(300, me);
		async Task Give<T>(decimal amount) where T : PowerModel => await PowerCmd.Apply<T>(ctx, donald, amount, donald, null);
		await Give<RebarPower>(3m);
		await Give<InfrastructureWeekPower>(7m);
		await Give<MakeTheSpireGreatAgainPower>(1m);
		await Give<GuardTowersPower>(2m);
		await Give<ReinforcedConcretePower>(2m);
		await Give<ProtectionismPower>(1m);
		await Give<SocialMediaInternPower>(1m);
		await Give<ThreeAmPostingPower>(2m);
		await Give<RatingsPower>(2m);
		await Give<VerifiedAccountPower>(3m);
		await Give<MerchStandPower>(15m);
		foreach (PowerModel power in donald.Powers.ToList())
		{
			CheckText($"power {power.Id.Entry}", () => string.Join(" ", power.HoverTips.OfType<HoverTip>().Select(t => t.Description)), allowEmpty: true);
		}
		await Task.Delay(500);

		int sections = WallCmd.GetSections(donald);
		int height = WallCmd.GetHeight(donald);
		int enemyHp = combat.HittableEnemies.Sum(e => e.CurrentHp);
		int enemyBlock = combat.HittableEnemies.Sum(e => e.Block);
		int round = combat.RoundNumber;
		donald.SetCurrentHpInternal(donald.MaxHp);
		PlayerCmd.EndTurn(me, canBackOut: false);
		await WaitUntil(() => donald.Block > 0, TimeSpan.FromSeconds(15));
		Check(donald.Block == sections * 5, $"Reinforced Concrete: {sections} Sections gave {donald.Block} Block (expected {sections * 5})");
		await WaitUntil(() => combat.HittableEnemies.Sum(e => e.CurrentHp) <= enemyHp - (sections * 2 - enemyBlock), TimeSpan.FromSeconds(10));
		Check(combat.HittableEnemies.Sum(e => e.CurrentHp) <= enemyHp - (sections * 2 - enemyBlock), $"Guard Towers: enemy HP {enemyHp} -> {combat.HittableEnemies.Sum(e => e.CurrentHp)} (expected -{sections * 2}, less {enemyBlock} Block)");
		await WaitUntil(() => combat.RoundNumber > round && me.PlayerCombatState?.Phase == PlayerTurnPhase.Play, TimeSpan.FromSeconds(30));
		await Task.Delay(800);
		int built = WallCmd.GetHeight(donald) - height;
		Check(built == 3 + 7 + 4 + 2, $"Start of turn: Rebar 3 + Infrastructure Week 7 + Make the Spire Great Again 4 + Golden Shovel 2 built {built} (expected 16)");
		int tweets = PileType.Hand.GetPile(me).Cards.Count(c => c is Tweet);
		Check(tweets == 1 + 2 + 1, $"Start of turn: Social Media Intern 1 + 3 AM Posting 2 + Make the Spire Great Again 1 = {tweets} Tweets (expected 4)");
		int tariffed = combat.HittableEnemies.Count(e => e.GetPowerAmount<TariffPower>() > 0);
		Check(tariffed == combat.HittableEnemies.Count, $"Protectionism: Tariff on {tariffed} of {combat.HittableEnemies.Count} enemies");
		Check(me.Gold >= 304, $"Make the Spire Great Again: Gold {me.Gold} (expected at least 304)");

		CardModel tweet = PileType.Hand.GetPile(me).Cards.First(c => c is Tweet);
		height = WallCmd.GetHeight(donald);
		foreach (Creature enemy in combat.HittableEnemies)
		{
			enemy.SetCurrentHpInternal(enemy.MaxHp);
		}
		enemyHp = combat.HittableEnemies.Sum(e => e.CurrentHp);
		int enemies = combat.HittableEnemies.Count;
		enemyBlock = combat.HittableEnemies.Sum(e => e.Block);
		await CardCmd.AutoPlay(ctx, tweet, null);
		await Task.Delay(1200);
		Check(WallCmd.GetHeight(donald) - height == 2, $"Ratings: a Tweet built {WallCmd.GetHeight(donald) - height} (expected 2)");
		int dealt = enemyHp - combat.HittableEnemies.Sum(e => e.CurrentHp);
		Check(dealt >= enemies * 6 - enemyBlock, $"Verified Account: a Tweet dealt {dealt} to {enemies} enemies (expected {enemies * 6}, less {enemyBlock} Block)");

		int gold = me.Gold;
		RunConsole("win");
		await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
		await WaitUntil(() => me.Gold != gold, TimeSpan.FromSeconds(5));
		Check(me.Gold - gold == 15, $"Merch Stand: +{me.Gold - gold} Gold at the end of combat (expected 15)");
		await Task.Delay(1000);
	}

	private static async Task TrumpPotionChecks()
	{
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();

		PotionModel? potion = await AddPotion("QUICK_DRY_CONCRETE");
		if (potion != null)
		{
			int height = WallCmd.GetHeight(donald);
			await potion.OnUseWrapper(ctx, donald);
			await Task.Delay(500);
			Check(WallCmd.GetHeight(donald) - height == 12, $"Quick-Dry Concrete: Wall {height} -> {WallCmd.GetHeight(donald)} (expected +12)");
		}
		potion = await AddPotion("COVFEFE_POTION");
		if (potion != null)
		{
			int tweets = PileType.Hand.GetPile(me).Cards.Count(c => c is Tweet && c.IsUpgraded);
			await potion.OnUseWrapper(ctx, donald);
			await Task.Delay(500);
			int now = PileType.Hand.GetPile(me).Cards.Count(c => c is Tweet && c.IsUpgraded);
			Check(now - tweets == 3, $"Covfefe potion: {now - tweets} Upgraded Tweets added (expected 3)");
		}
		potion = await AddPotion("DEPORTATION_DRAUGHT");
		if (potion != null)
		{
			Creature target = combat.HittableEnemies.First();
			Check(potion.IsValidTarget(target), "Deportation Draught can target a normal enemy");
			int enemies = combat.HittableEnemies.Count;
			await potion.OnUseWrapper(ctx, target);
			await Task.Delay(500);
			Check(combat.HittableEnemies.Count == enemies - 1, $"Deportation Draught: enemies {enemies} -> {combat.HittableEnemies.Count} (full HP target)");
		}
	}

	// ---------------------------------------------------------------- balance bot

	/// <summary>
	/// What a Build is worth in HP saved: each new Section blocks 3 per turn for the rest of the fight, and crossing a
	/// stage (25 draw, 45 Energy, 70 end-of-turn damage) pays every remaining turn.
	/// </summary>
	private static double TrumpBuildValue(FightView fight, double build)
	{
		if (build <= 0)
		{
			return 0;
		}
		int before = WallCmd.GetHeight(fight.Me.Creature);
		int after = before + (int)build;
		double value = build * 0.15;
		value += (WallRules.SectionsFor(after) - WallRules.SectionsFor(before)) * (double)WallRules.BlockPerSection * fight.TurnsLeft * 0.8;
		if (before < 25 && after >= 25)
		{
			value += 2 * fight.TurnsLeft;
		}
		if (before < 45 && after >= 45)
		{
			value += 4 * fight.TurnsLeft;
		}
		if (before < 70 && after >= 70)
		{
			value += WallRules.SectionsFor(after) * fight.TurnsLeft;
		}
		return value;
	}
}
