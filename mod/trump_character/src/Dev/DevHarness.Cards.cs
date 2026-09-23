using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using TrumpMod.Models.CardPools;
using TrumpMod.Models.Cards;
using TrumpMod.Models.Powers;
using TrumpMod.Models.Relics;

namespace TrumpMod.Dev;

/// <summary>
/// Step 5 test mode "cards": every card of the character, base and upgraded, is added to the hand and played in a real
/// fight (god mode, choice screens answered by the game's own AutoSlay selector). Each play logs what changed (energy,
/// Gold, Wall, Block, hand, enemy HP and powers); key effects are checked; every card, power, relic and potion text is
/// rendered to catch template mistakes. Relics and potions get their own checks first.
/// </summary>
public static partial class DevHarness
{
	/// <summary>Three sturdy enemies, so area cards and Deport have several targets.</summary>
	private const string CardFightEncounter = "RUBY_RAIDERS_NORMAL";

	private const int CardTestGold = 300;

	private const int CardTestWall = 30;

	private const int MaxHandSize = 10;

	private static async Task RunCardTest()
	{
		// No god mode here: it gives 999999999 Strength, so every attack would one-shot its target. The Donald is healed
		// before each card instead.
		await StartRunAsDonald(screenshots: false);
		using IDisposable selector = CardSelectCmd.UseSelector(new AutoSlayCardSelector(new Rng(20260923u)));
		AncientChecks();
		await RelicChecks();
		await PotionChecks();
		await PowerChecks();
		// --trump-only relics stops here; --trump-only A,B plays just those cards.
		string? only = CommandLineHelper.GetValue("trump-only");
		if (only == "relics")
		{
			Note("Card test finished (relics and potions only)");
			return;
		}
		var filter = (only ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		List<CardModel> cards = ModelDb.CardPool<TrumpCardPool>().AllCards.Append(ModelDb.Card<Tweet>())
			.Where(c => filter.Count == 0 || filter.Contains(c.Id.Entry)).ToList();
		Note($"Card test over {cards.Count} cards, base and upgraded");
		foreach (CardModel canonical in cards)
		{
			CheckText($"{canonical.Id.Entry} (library)", () => canonical.GetDescriptionForPile(PileType.None));
			foreach (bool upgraded in new[] { false, true })
			{
				await PlayOneCard(canonical, upgraded);
			}
		}
		Note("Card test finished");
	}

	// ---------------------------------------------------------------- cards

	private static async Task PlayOneCard(CardModel canonical, bool upgraded)
	{
		string label = canonical.Id.Entry + (upgraded ? "+" : "");
		Log.Info($"[{ModEntry.ModId}:sweep] BEGIN {label}");
		var result = new Dictionary<string, object> { ["encounter"] = label, ["room"] = "Card" };
		try
		{
			ICombatState combat = await EnsureCardFight();
			Player me = Me;
			var ctx = new BlockingPlayerChoiceContext();
			await PrepareCardTurn(ctx, combat);
			if (canonical is Tweet)
			{
				// The token isn't in any card pool, so the console can't add it.
				await Tweet.CreateInHand(me, 1, combat);
			}
			else
			{
				RunConsole($"card {canonical.Id.Entry} hand");
			}
			await Task.Delay(250);
			CardModel? card = PileType.Hand.GetPile(me).Cards.LastOrDefault(c => c.Id == canonical.Id);
			if (card == null)
			{
				Error($"card {label}: was not added to the hand");
				return;
			}
			if (upgraded)
			{
				if (!card.IsUpgradable)
				{
					Note($"card {label}: not upgradable");
					return;
				}
				CardCmd.Upgrade(card);
			}
			Creature? target = card.TargetType == TargetType.AnyEnemy ? combat.HittableEnemies.FirstOrDefault(e => card.IsValidTarget(e)) : null;
			if (card.TargetType == TargetType.AnyEnemy && target == null)
			{
				Note($"card {label}: no valid target");
				return;
			}
			CheckText(label, () => card.GetDescriptionForPile(PileType.Hand, target));
			CheckText($"{label} tips", () => string.Join(" ", card.HoverTips.OfType<HoverTip>().Select(t => t.Description)), allowEmpty: true);
			CardSnapshot before = CardSnapshot.Take(me, combat, target);
			bool playable = card.CanPlay(out _, out _);
			await CardCmd.AutoPlay(ctx, card, target);
			await WaitForCardToSettle(combat);
			CardSnapshot after = CardSnapshot.Take(me, combat, target);
			string diff = before.Diff(after);
			result["playable"] = playable;
			result["diff"] = diff;
			Note($"card {label}{(playable ? "" : " (was not playable)")}: {diff}");
			CheckCardEffect(canonical.Id.Entry, upgraded, before, after);
			foreach (PowerModel power in me.Creature.Powers.Concat(combat.Enemies.SelectMany(e => e.Powers)).ToList())
			{
				CheckText($"{label} -> power {power.Id.Entry}", () => string.Join(" ", power.HoverTips.OfType<HoverTip>().Select(t => t.Description)), allowEmpty: true);
			}
		}
		catch (Exception e)
		{
			result["error"] = e.Message;
			Error($"card {label}: {e}");
		}
		finally
		{
			Log.Info($"[{ModEntry.ModId}:sweep] END {label}");
			_sweep.Add(result);
		}
	}

	private static async Task<ICombatState> EnsureCardFight()
	{
		ICombatState? combat = Me.Creature.CombatState;
		if (!CombatManager.Instance.IsInProgress || combat == null || combat.HittableEnemies.Count < 2)
		{
			if (CombatManager.Instance.IsInProgress)
			{
				RunConsole("win");
				await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
				await Task.Delay(1000);
			}
			await Fight(CardFightEncounter);
			combat = Me.Creature.CombatState!;
		}
		return combat;
	}

	/// <summary>Same starting point for every card: its turn, 300 Gold, 3+ Energy, a 30 Wall, a small hand, healthy enemies.</summary>
	private static async Task PrepareCardTurn(PlayerChoiceContext ctx, ICombatState combat)
	{
		Player me = Me;
		await WaitUntil(() => me.PlayerCombatState?.Phase == PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(30));
		me.Creature.SetCurrentHpInternal(me.Creature.MaxHp);
		await PlayerCmd.SetGold(CardTestGold, me);
		if (me.PlayerCombatState!.Energy < 3)
		{
			me.PlayerCombatState.Energy = 3;
		}
		int height = WallCmd.GetHeight(me.Creature);
		if (height < CardTestWall)
		{
			await WallCmd.Build(ctx, me.Creature, CardTestWall - height);
		}
		foreach (CardModel extra in PileType.Hand.GetPile(me).Cards.Skip(3).ToList())
		{
			await CardPileCmd.Add(extra, PileType.Discard);
		}
		foreach (Creature enemy in combat.HittableEnemies)
		{
			enemy.SetCurrentHpInternal(enemy.MaxHp);
		}
	}

	private static async Task WaitForCardToSettle(ICombatState combat)
	{
		await Task.Delay(700);
		await WaitUntil(() => !CombatManager.Instance.IsInProgress || Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play, TimeSpan.FromSeconds(30));
		await Task.Delay(200);
	}

	/// <summary>Assertions for the effects most likely to go wrong. Everything else is covered by the logged diff.</summary>
	private static void CheckCardEffect(string id, bool up, CardSnapshot b, CardSnapshot a)
	{
		string label = id + (up ? "+" : "");
		void Expect(bool ok, string what) => Check(ok, $"{label}: {what}");
		switch (id)
		{
			case "BUILD_THE_WALL":
				Expect(a.Wall - b.Wall == (up ? 8 : 5), $"Wall {b.Wall} -> {a.Wall}");
				break;
			case "BRICK_TOSS":
				Expect(a.Wall - b.Wall == (up ? 5 : 3) && a.EnemyHp < b.EnemyHp, $"Wall {b.Wall} -> {a.Wall}, enemy HP {b.EnemyHp} -> {a.EnemyHp}");
				break;
			case "POUR_CONCRETE":
				Expect(a.Wall - b.Wall == (up ? 16 : 12), $"Wall {b.Wall} -> {a.Wall}");
				break;
			case "DEMOLITION":
			case "WRECKING_BALL":
				// If the hit ends the fight, the Wall is left as it was (the combat is already over).
				Expect((a.Wall == b.Wall - b.Wall / 2 || a.Enemies == 0) && a.EnemyHp < b.EnemyHp, $"Wall {b.Wall} -> {a.Wall}, enemy HP {b.EnemyHp} -> {a.EnemyHp}");
				break;
			case "GREAT_WALL":
				Expect(a.Wall == b.Wall * 2, $"Wall {b.Wall} -> {a.Wall}");
				break;
			case "HOLD_THE_LINE":
				Expect(a.Block - b.Block == b.Wall / 2, $"Block {b.Block} -> {a.Block} with Wall {b.Wall}");
				break;
			case "TEN_FEET_HIGHER":
				Expect(a.Wall - b.Wall == (up ? 14 : 10), $"Wall {b.Wall} -> {a.Wall} (over 25, so it builds twice)");
				break;
			case "WALL_STREET":
				Expect(a.Gold - b.Gold == (up ? 6 : 4) * (b.Wall / 10), $"Gold {b.Gold} -> {a.Gold} with {b.Wall / 10} Sections");
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
				Expect(b.Gold - a.Gold == 20 && a.Wall - b.Wall == (up ? 18 : 14), $"Gold {b.Gold} -> {a.Gold}, Wall {b.Wall} -> {a.Wall}");
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
				Expect(a.Tweets - b.Tweets == (up ? 3 : 2), $"Tweets in hand {b.Tweets} -> {a.Tweets}");
				break;
			case "BURNER_ACCOUNTS":
				Expect(a.UpgradedTweets - b.UpgradedTweets == (up ? 5 : 4), $"Upgraded Tweets in hand {b.UpgradedTweets} -> {a.UpgradedTweets}");
				break;
			case "TWEETSTORM":
				// X+1 Tweets, capped by the 10-card hand (the Tweetstorm itself leaves the hand first).
				Expect(a.Tweets - b.Tweets == Math.Min(b.Energy + 1, MaxHandSize - (b.Hand - 1)), $"Tweets in hand {b.Tweets} -> {a.Tweets} with {b.Energy} Energy and {b.Hand - 1} other cards");
				break;
			case "MEAN_TWEET":
			case "RALLY_SPEECH":
			case "BLOCKED":
			case "DOOMSCROLLING":
				Expect(a.Tweets - b.Tweets >= 1, $"Tweets in hand {b.Tweets} -> {a.Tweets}");
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
				Expect(a.DeportLine - b.DeportLine == (up ? 0.15m : 0.10m), $"Deport line {b.DeportLine:0.##} -> {a.DeportLine:0.##}");
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

	// ---------------------------------------------------------------- relics and potions

	private static async Task RelicChecks()
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
	private static async Task PowerChecks()
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

	/// <summary>Every Ancient (Neow, Darv, ...) has dialogues for The Donald, and every line renders. The Architect is its own event type, covered by full runs.</summary>
	private static void AncientChecks()
	{
		foreach (AncientEventModel ancient in ModelDb.AllAncients)
		{
			try
			{
				if (!ancient.DialogueSet.CharacterDialogues.TryGetValue(ModelDb.Character<Models.Characters.Trump>().Id.Entry, out IReadOnlyList<AncientDialogue>? dialogues))
				{
					Error($"Ancient {ancient.Id.Entry}: no dialogues for TRUMP");
					continue;
				}
				int lines = 0;
				foreach (AncientDialogueLine line in dialogues.SelectMany(d => d.Lines))
				{
					lines++;
					CheckText($"Ancient {ancient.Id.Entry} line {lines}", () => line.LineText?.GetFormattedText() ?? "");
				}
				Check(lines > 0, $"Ancient {ancient.Id.Entry}: {dialogues.Count} dialogues, {lines} lines for TRUMP");
			}
			catch (Exception e)
			{
				Error($"Ancient {ancient.Id.Entry}: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	private static async Task PotionChecks()
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

	private static async Task<PotionModel?> AddPotion(string id)
	{
		RunConsole($"potion {id}");
		await Task.Delay(400);
		PotionModel? potion = Me.Potions.LastOrDefault(p => p.Id.Entry == id);
		if (potion == null)
		{
			Error($"potion {id} was not added");
			return null;
		}
		CheckText($"potion {id}", () => string.Join(" ", potion.HoverTips.OfType<HoverTip>().Select(t => t.Description)));
		return potion;
	}

	private static void CheckRelicTexts()
	{
		foreach (RelicModel relic in Me.Relics)
		{
			CheckText($"relic {relic.Id.Entry}", () => string.Join(" ", relic.HoverTips.OfType<HoverTip>().Select(t => t.Description)));
		}
	}

	/// <summary>Renders a text and flags exceptions or leftover template braces (a missing or misnamed variable).</summary>
	private static void CheckText(string what, Func<string> render, bool allowEmpty = false)
	{
		try
		{
			string text = render();
			if ((!allowEmpty && string.IsNullOrWhiteSpace(text)) || text.Contains('{') || text.Contains('}'))
			{
				Error($"text {what}: \"{text}\"");
			}
		}
		catch (Exception e)
		{
			Error($"text {what}: {e.GetType().Name}: {e.Message}");
		}
	}

	private sealed class CardSnapshot
	{
		public int Energy;
		public int Gold;
		public int Wall;
		public int Block;
		public int Hand;
		public int Tweets;
		public int UpgradedTweets;
		public int Enemies;
		public int EnemyHp;
		public int Round;
		public decimal DeportLine;
		public string MyPowers = "";
		public string TargetPowers = "";
		public List<string> EnemyPowers = new List<string>();

		public int EnemiesWithPower(string id) => EnemyPowers.Count(p => p.Contains(id + ":"));

		public static CardSnapshot Take(Player me, ICombatState combat, Creature? target)
		{
			List<CardModel> hand = PileType.Hand.GetPile(me).Cards.ToList();
			return new CardSnapshot
			{
				Energy = me.PlayerCombatState?.Energy ?? 0,
				Gold = me.Gold,
				Wall = WallCmd.GetHeight(me.Creature),
				Block = me.Creature.Block,
				Hand = hand.Count,
				Tweets = hand.Count(c => c is Tweet),
				UpgradedTweets = hand.Count(c => c is Tweet && c.IsUpgraded),
				Enemies = combat.HittableEnemies.Count,
				EnemyHp = combat.HittableEnemies.Sum(e => e.CurrentHp),
				Round = combat.RoundNumber,
				DeportLine = DeportCmd.GetLine(me),
				MyPowers = Powers(me.Creature),
				TargetPowers = target == null ? "" : Powers(target),
				EnemyPowers = combat.HittableEnemies.Select(Powers).ToList()
			};
		}

		private static string Powers(Creature creature)
		{
			return string.Join(",", creature.Powers.Select(p => $"{p.Id.Entry}:{p.Amount}"));
		}

		public string Diff(CardSnapshot a)
		{
			var parts = new List<string>();
			void Add(string name, object before, object after)
			{
				if (!Equals(before, after))
				{
					parts.Add($"{name} {before}->{after}");
				}
			}
			Add("energy", Energy, a.Energy);
			Add("gold", Gold, a.Gold);
			Add("wall", Wall, a.Wall);
			Add("block", Block, a.Block);
			Add("hand", Hand, a.Hand);
			Add("tweets", Tweets, a.Tweets);
			Add("enemies", Enemies, a.Enemies);
			Add("enemyHP", EnemyHp, a.EnemyHp);
			Add("round", Round, a.Round);
			Add("line", DeportLine.ToString("0.##"), a.DeportLine.ToString("0.##"));
			Add("me", MyPowers, a.MyPowers);
			Add("target", TargetPowers, a.TargetPowers);
			Add("enemyPowers", string.Join(" | ", EnemyPowers), string.Join(" | ", a.EnemyPowers));
			return parts.Count == 0 ? "no visible change" : string.Join(", ", parts);
		}
	}
}
