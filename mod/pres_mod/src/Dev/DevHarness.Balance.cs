using System.IO;
using System.Text.Json;
using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace PresMod.Dev;

/// <summary>
/// Test mode "balance": one run played by a heuristic bot instead of AutoSlay's random, unkillable one, for any
/// character (--pres-character, default the first mod character). AutoSlay still walks the map, events, shops and screens; these
/// decisions are ours (DevHarnessPatches routes them here):
///   combat      : no cheat buffs; cards are played for real (Energy is spent), scored by damage (kill bonus), Block
///                 against incoming attacks, Build, draw, Energy and the other characters' resources; potions in elite
///                 and boss fights or at low HP.
///   card reward : rarer first, Powers slightly preferred; with --pres-favor ID,ID,... those cards come first
///                 (test.py fills it with one play style's cards to check that each style works on its own).
///   rest site   : heal under 50% HP, otherwise upgrade.
///   map         : treasure and rest sites when hurt, elites only when healthy (AutoSlay always takes the leftmost path).
/// Runs are at Ascension 0. With --pres-fullheal the character heals to full after each fight (same for every
/// character), so runs reach Acts 2 and 3 and HP lost per fight can be compared act by act.
/// The result (victory, death, floor), each combat and each pick are written to balance.json for scripts/test.py.
/// </summary>
public static partial class DevHarness
{
	internal static bool BalanceMode => Enabled && _mode == "balance";

	private static readonly List<Dictionary<string, object>> _balanceCombats = new List<Dictionary<string, object>>();

	private static readonly List<Dictionary<string, object>> _balancePicks = new List<Dictionary<string, object>>();

	private static readonly Dictionary<string, int> _balancePlays = new Dictionary<string, int>();

	private static bool _balanceEnded;

	private static bool _balanceKitReady;

	/// <summary>Where the run last was; after a victory the run state is gone (back at the main menu).</summary>
	private static int _lastAct;

	private static int _lastFloor;

	private static string CharacterArg => TestCharacterId;

	// ---------------------------------------------------------------- combat

	internal static async Task BalanceCombat(Rng random, CancellationToken ct)
	{
		SaveManager.Instance.PrefsSave.FastMode = FastModeType.Instant;
		await WaitHelper.Until(() => CombatManager.Instance.IsInProgress, ct, AutoSlayConfig.nodeWaitTimeout, "Combat not started");
		RunState run = RunManager.Instance.DebugOnlyGetState()!;
		Player me = LocalContext.GetMe(run)!;
		ICombatState combat = me.Creature.CombatState!;
		CombatRoom? room = run.CurrentRoom as CombatRoom;
		int hpBefore = me.Creature.CurrentHp;
		int goldBefore = me.Gold;
		_lastAct = run.CurrentActIndex + 1;
		_lastFloor = run.TotalFloor;
		int turn = 0;
		// The character's own per-combat numbers (Trump: maxWall, deports), sampled after every card and turn.
		CharacterTests kit = Kit;
		if (!_balanceKitReady)
		{
			_balanceKitReady = true;
			kit.BalanceInit?.Invoke();
		}
		kit.BalanceCombatStart?.Invoke();
		var stats = new Dictionary<string, object>();
		while (CombatManager.Instance.IsInProgress && turn < 60)
		{
			turn++;
			await WaitHelper.Until(() => me.PlayerCombatState?.Phase == PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress, ct, TimeSpan.FromSeconds(30), "Play phase not started");
			if (!CombatManager.Instance.IsInProgress)
			{
				break;
			}
			AutoSlayer.CurrentWatchdog?.Reset($"Balance combat turn {turn}");
			await UsePotionsIfWorthIt(me, combat, room, turn, random, ct);
			for (int plays = 0; plays < 40; plays++)
			{
				if (me.PlayerCombatState?.Phase != PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress)
				{
					break;
				}
				(CardModel? card, Creature? target) = ChooseCard(me, combat, turn);
				if (card == null || !card.TryManualPlay(target))
				{
					break;
				}
				_balancePlays[card.Id.Entry] = _balancePlays.GetValueOrDefault(card.Id.Entry) + 1;
				await WaitHelper.Until(() => RunManager.Instance.ActionQueueSet.IsEmpty && !RunManager.Instance.ActionExecutor.IsRunning, ct, TimeSpan.FromSeconds(20), "Card play did not resolve");
				kit.BalanceCombatStats?.Invoke(me, stats);
			}
			if (me.PlayerCombatState?.Phase == PlayerTurnPhase.Play && CombatManager.Instance.IsInProgress)
			{
				PlayerCmd.EndTurn(me, canBackOut: false);
			}
			kit.BalanceCombatStats?.Invoke(me, stats);
		}
		await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, ct, TimeSpan.FromSeconds(30), "Combat did not end");
		var combatResult = new Dictionary<string, object>
		{
			["encounter"] = room?.Encounter.Id.Entry ?? "?",
			["type"] = room?.RoomType.ToString() ?? "?",
			["act"] = run.CurrentActIndex + 1,
			["floor"] = run.TotalFloor,
			["turns"] = turn,
			["hpBefore"] = hpBefore,
			["hpAfter"] = Math.Max(0, me.Creature.CurrentHp),
			["maxHp"] = me.Creature.MaxHp,
			["goldGained"] = me.Gold - goldBefore,
			["died"] = me.Creature.IsDead
		};
		foreach ((string key, object value) in stats)
		{
			combatResult[key] = value;
		}
		_balanceCombats.Add(combatResult);
		if (me.Creature.IsDead)
		{
			EndBalanceRun("death");
		}
		else if (CommandLineHelper.HasArg("pres-fullheal"))
		{
			// Measuring mode: every fight starts at full HP, so runs reach the later acts and each fight is judged alone.
			me.Creature.SetCurrentHpInternal(me.Creature.MaxHp);
		}
	}

	/// <summary>
	/// Best card to play now (value per Energy), with its target, or null to end the turn. Values are in "HP saved":
	/// Block counts up to the incoming damage, a kill is worth the attack it cancels, and plain damage is weighted by
	/// how much the enemies hit compared to the HP they have left.
	/// </summary>
	private static (CardModel?, Creature?) ChooseCard(Player me, ICombatState combat, int turn)
	{
		var fight = new FightView(me, combat);
		CardModel? best = null;
		Creature? bestTarget = null;
		double bestScore = 0;
		foreach (CardModel card in PileType.Hand.GetPile(me).Cards.ToList())
		{
			Creature? target = card.TargetType == TargetType.AnyEnemy ? PickTarget(card, fight) : null;
			if ((card.TargetType == TargetType.AnyEnemy && target == null) || !card.CanPlayTargeting(target))
			{
				continue;
			}
			double value = CardValue(card, fight, target, turn);
			double score = value / (card.EnergyCost.GetAmountToSpend() + 0.6);
			if (value > 0 && score > bestScore)
			{
				best = card;
				bestTarget = target;
				bestScore = score;
			}
		}
		return (best, bestTarget);
	}

	/// <summary>What the bot knows about the fight this instant: enemies, what each intends to hit for, and the danger level.</summary>
	internal sealed class FightView
	{
		public readonly List<Creature> Enemies;

		public readonly Dictionary<Creature, int> Intent;

		public readonly int Incoming;

		public readonly double DamageWeight;

		public FightView(Player me, ICombatState combat)
		{
			Me = me;
			Enemies = combat.HittableEnemies.ToList();
			var targets = new[] { me.Creature };
			Intent = Enemies.ToDictionary(e => e, e => e.Monster?.NextMove?.Intents?.OfType<MegaCrit.Sts2.Core.MonsterMoves.Intents.AttackIntent>().Sum(i => i.GetTotalDamage(targets, e)) ?? 0);
			Incoming = Math.Max(0, Intent.Values.Sum() - me.Creature.Block);
			double enemyHp = Math.Max(1, Enemies.Sum(e => e.CurrentHp + e.Block));
			DamageWeight = Math.Clamp(Math.Max(Intent.Values.Sum(), 6) / enemyHp * 4, 0.35, 1.2);
			TurnsLeft = Math.Clamp(enemyHp / 12, 1, 6);
		}

		public readonly Player Me;

		public readonly double TurnsLeft;

		/// <summary>Whether this much damage from this card removes the enemy: a kill, or another way the character has (Deport).</summary>
		public bool Kills(Creature enemy, double damage, CardModel? card = null)
		{
			if (damage > 0 && damage >= enemy.CurrentHp + enemy.Block)
			{
				return true;
			}
			return KitFor(Me.Character.Id.Entry).RemovesEnemy?.Invoke(this, enemy, damage, card) ?? false;
		}
	}

	/// <summary>Kill the most dangerous enemy this card can kill, else hit the one with the most damage per HP left.</summary>
	private static Creature? PickTarget(CardModel card, FightView fight)
	{
		List<Creature> valid = fight.Enemies.Where(card.IsValidTarget).ToList();
		if (valid.Count == 0)
		{
			return null;
		}
		double damage = Damage(card, null);
		Creature? killable = valid.Where(e => fight.Kills(e, damage, card)).OrderByDescending(e => fight.Intent[e]).ThenByDescending(e => e.CurrentHp).FirstOrDefault();
		return killable ?? valid.OrderByDescending(e => (fight.Intent[e] + 1.0) / (e.CurrentHp + e.Block)).First();
	}

	private static double Damage(CardModel card, Creature? target)
	{
		DynamicVarSet v = card.DynamicVars;
		double damage = v.ContainsKey("CalculatedDamage") ? (double)v.CalculatedDamage.Calculate(target)
			: v.ContainsKey("Damage") ? (double)v.Damage.BaseValue
			: v.ContainsKey("OstyDamage") ? (double)v.OstyDamage.BaseValue
			: 0;
		int hits = v.ContainsKey("Repeat") ? v.Repeat.IntValue : 1;
		if (card.EnergyCost.CostsX)
		{
			hits = card.Owner.PlayerCombatState?.Energy ?? 0;
		}
		return damage * hits;
	}

	/// <summary>A rough, character-agnostic value from the card's own numbers.</summary>
	private static double CardValue(CardModel card, FightView fight, Creature? target, int turn)
	{
		if (card.Type is CardType.Status or CardType.Curse)
		{
			return 0;
		}
		DynamicVarSet v = card.DynamicVars;
		double Get(string key) => v.TryGetValue(key, out DynamicVar? var) ? (double)var.BaseValue : 0;
		double hit = Damage(card, target);
		double value = 0;
		if (hit > 0)
		{
			IEnumerable<Creature> hitEnemies = card.TargetType == TargetType.AllEnemies ? fight.Enemies
				: target != null ? new[] { target }
				: fight.Enemies.Take(1);
			foreach (Creature enemy in hitEnemies)
			{
				value += Math.Min(hit, enemy.CurrentHp + enemy.Block) * fight.DamageWeight;
				if (fight.Kills(enemy, hit, card))
				{
					value += 6 + fight.Intent[enemy];
				}
			}
		}
		double block = v.ContainsKey("CalculatedBlock") ? (double)v.CalculatedBlock.Calculate(null) : Get("Block");
		value += Math.Min(block, fight.Incoming) + Math.Max(0, block - fight.Incoming) * 0.1;
		value += Get("Summon") * 0.8;
		value += Get("Cards") * 2 + Get("Energy") * 5 + Get("Stars") * 3 + Get("Forge") * 1.2;
		int spread = card.TargetType == TargetType.AllEnemies ? Math.Max(1, fight.Enemies.Count) : 1;
		value += (Get("PoisonPower") * 1.2 + Get("DoomPower")) * spread;
		value += Get("StrengthPower") * 6 + Get("DexterityPower") * 5 + (Get("WeakPower") + Get("VulnerablePower")) * 2.5 * spread;
		value += Get("Gold") * 0.15;
		// The character's own effects (Trump: Build, Tariff, Pay Gold).
		value += KitFor(card.Owner.Character.Id.Entry).CardValue?.Invoke(card, fight, Get) ?? 0;
		value += card.TargetType == TargetType.AnyEnemy ? Get("HpLoss") * fight.DamageWeight : -Get("HpLoss") * 0.5;
		if (card.Type == CardType.Power)
		{
			value += turn <= 2 ? 20 : 10;
		}
		return value > 0 ? value : 3;
	}

	private static async Task UsePotionsIfWorthIt(Player me, ICombatState combat, CombatRoom? room, int turn, Rng random, CancellationToken ct)
	{
		bool bigFight = room?.RoomType is RoomType.Elite or RoomType.Boss;
		bool lowHp = me.Creature.CurrentHp < me.Creature.MaxHp * 0.4;
		if (!(bigFight && turn == 1) && !lowHp)
		{
			return;
		}
		foreach (PotionModel potion in me.Potions.ToList())
		{
			if (me.PlayerCombatState?.Phase != PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress)
			{
				return;
			}
			Creature? target = potion.TargetType switch
			{
				TargetType.AnyEnemy => combat.HittableEnemies.Where(potion.IsValidTarget).OrderByDescending(e => e.CurrentHp).FirstOrDefault(),
				TargetType.AnyPlayer or TargetType.Self or TargetType.AnyAlly => me.Creature,
				_ => null
			};
			if (target == null && potion.TargetType.IsSingleTarget())
			{
				continue;
			}
			potion.EnqueueManualUse(target);
			await Task.Delay(300, ct);
			await WaitHelper.Until(() => RunManager.Instance.ActionQueueSet.IsEmpty, ct, TimeSpan.FromSeconds(20), "Potion did not resolve");
		}
	}

	// ---------------------------------------------------------------- rewards and rest sites

	internal static async Task BalanceCardReward(Rng random, CancellationToken ct)
	{
		NCardRewardSelectionScreen screen = AutoSlayer.GetCurrentScreen<NCardRewardSelectionScreen>();
		await Task.Delay(300, ct);
		List<NCardHolder> holders = UiHelper.FindAll<NCardHolder>(screen).Where(h => h.CardModel != null).ToList();
		if (holders.Count == 0)
		{
			return;
		}
		NCardHolder pick = holders.OrderByDescending(h => RewardScore(h.CardModel!, random)).First();
		_balancePicks.Add(new Dictionary<string, object>
		{
			["floor"] = RunManager.Instance.DebugOnlyGetState()?.TotalFloor ?? 0,
			["offered"] = holders.Select(h => h.CardModel!.Id.Entry).ToList(),
			["picked"] = pick.CardModel!.Id.Entry
		});
		pick.EmitSignal(NCardHolder.SignalName.Pressed, pick);
		await WaitHelper.Until(() => !GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree(), ct, TimeSpan.FromSeconds(10), "Card reward screen did not close after selection");
	}

	private static HashSet<string>? _favored;

	private static double RewardScore(CardModel card, Rng random)
	{
		_favored ??= (CommandLineHelper.GetValue("pres-favor") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		double favor = _favored.Contains(card.Id.Entry) ? 5 : 0;
		double rarity = card.Rarity switch
		{
			CardRarity.Rare => 3,
			CardRarity.Uncommon => 2,
			CardRarity.Ancient => 3.5,
			_ => 1
		};
		return favor + rarity + (card.Type == CardType.Power ? 0.5 : 0) + random.NextDouble();
	}

	internal static async Task BalanceRestSite(Rng random, CancellationToken ct)
	{
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		NRestSiteRoom room = await WaitHelper.ForNode<NRestSiteRoom>(root, "/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom", ct);
		List<NRestSiteButton> buttons = UiHelper.FindAll<NRestSiteButton>(room).Where(b => b.Option.IsEnabled).ToList();
		if (buttons.Count == 0)
		{
			return;
		}
		Player me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState())!;
		bool hurt = me.Creature.CurrentHp < me.Creature.MaxHp * 0.5;
		NRestSiteButton choice = buttons.FirstOrDefault(b => hurt ? b.Option is HealRestSiteOption : b.Option is SmithRestSiteOption)
			?? buttons.FirstOrDefault(b => b.Option is HealRestSiteOption or SmithRestSiteOption)
			?? random.NextItem(buttons)!;
		await UiHelper.Click(choice);
		NProceedButton proceed = room.ProceedButton;
		await WaitHelper.Until(() => proceed.IsEnabled || (NOverlayStack.Instance?.ScreenCount ?? 0) > 0, ct, TimeSpan.FromSeconds(10), "Rest site option did not respond");
		if ((NOverlayStack.Instance?.ScreenCount ?? 0) > 0)
		{
			return;
		}
		await UiHelper.Click(proceed);
	}

	internal static async Task BalanceMap(Rng random, CancellationToken ct)
	{
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		NRun runNode = root.GetNode<NRun>("/root/Game/RootSceneContainer/Run");
		await WaitHelper.Until(() => runNode.GlobalUi.MapScreen.IsVisibleInTree(), ct, AutoSlayConfig.mapScreenTimeout, "Map screen not visible");
		List<NMapPoint> points = UiHelper.FindAll<NMapPoint>(runNode.GlobalUi.MapScreen);
		RunState run = RunManager.Instance.DebugOnlyGetState()!;
		Player me = LocalContext.GetMe(run)!;
		IEnumerable<MapPoint> options = run.VisitedMapCoords.Count == 0
			? points.Where(p => p.Point.coord.row == 0).Select(p => p.Point)
			: points.First(p => p.Point.coord.Equals(run.VisitedMapCoords[^1])).Point.Children;
		MapPoint choice = options.OrderByDescending(p => RoomPreference(p.PointType, me) + random.NextDouble() * 0.2).First();
		NMapPoint next = points.First(p => p.Point.coord.Equals(choice.coord));
		await WaitHelper.Until(() => next.IsEnabled, ct, TimeSpan.FromSeconds(10), "Map point not enabled");
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnEntered() => entered.TrySetResult();
		RunManager.Instance.RoomEntered += OnEntered;
		try
		{
			await UiHelper.Click(next);
			await WaitHelper.ForTask(entered.Task, ct, AutoSlayConfig.mapScreenTimeout, "Room not entered after map click");
		}
		finally
		{
			RunManager.Instance.RoomEntered -= OnEntered;
		}
	}

	private static double RoomPreference(MapPointType type, Player me)
	{
		double hp = (double)me.Creature.CurrentHp / me.Creature.MaxHp;
		return type switch
		{
			MapPointType.RestSite => hp < 0.5 ? 6 : 2,
			MapPointType.Treasure => 4,
			MapPointType.Shop => me.Gold >= 150 ? 3.5 : 2,
			MapPointType.Unknown => 2.5,
			MapPointType.Monster => hp < 0.35 ? 1 : 3,
			MapPointType.Elite => hp > 0.75 ? 3.2 : hp > 0.5 ? 1.5 : 0,
			_ => 5
		};
	}

	/// <summary>Balance runs start at Ascension 0 for whichever character is being measured.</summary>
	private static void UseAscensionZero()
	{
		CharacterModel? character = ModelDb.AllCharacters.FirstOrDefault(c => c.Id.Entry == CharacterArg);
		if (character != null)
		{
			SaveManager.Instance.Progress.GetOrCreateCharacterStats(character.Id).PreferredAscension = 0;
		}
	}

	// ---------------------------------------------------------------- results

	/// <summary>Writes balance.json once per run: "death" from the combat handler, "victory" or "error" when AutoSlay quits.</summary>
	private static void EndBalanceRun(string result)
	{
		if (_balanceEnded)
		{
			return;
		}
		_balanceEnded = true;
		RunState? run = RunManager.Instance.DebugOnlyGetState();
		Player? me = run == null ? null : LocalContext.GetMe(run);
		var summary = new Dictionary<string, object?>
		{
			["character"] = CharacterArg,
			["seed"] = CommandLineHelper.GetValue("pres-seed"),
			["result"] = result,
			["act"] = run != null ? run.CurrentActIndex + 1 : _lastAct,
			["floor"] = run != null ? run.TotalFloor : _lastFloor,
			["maxHp"] = me?.Creature.MaxHp,
			["gold"] = me?.Gold,
			["deck"] = me?.Deck.Cards.Select(c => c.Id.Entry + (c.IsUpgraded ? "+" : "")).ToList(),
			["relics"] = me?.Relics.Select(r => r.Id.Entry).ToList(),
			["combats"] = _balanceCombats,
			["picks"] = _balancePicks,
			["plays"] = _balancePlays
		};
		File.WriteAllText(Path.Combine(_outDir, "balance.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
		Note($"Balance run ended: {result} at act {summary["act"]}, floor {summary["floor"]}");
		if (result == "death")
		{
			WriteReport();
			((SceneTree)Engine.GetMainLoop()).Quit(0);
		}
	}
}
