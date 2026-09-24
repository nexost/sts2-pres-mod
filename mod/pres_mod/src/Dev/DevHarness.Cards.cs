using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace PresMod.Dev;

/// <summary>
/// Test mode "cards": every card of the character under test, base and upgraded, is added to the hand and played in a real
/// fight (choice screens answered by the game's own AutoSlay selector). Each play logs what changed (energy, Gold, Block,
/// hand, enemy HP and powers, plus the character's own counters); the character's kit checks the key effects; every card,
/// power, relic and potion text is rendered to catch template mistakes. The kit's relic, potion and power checks run first.
/// </summary>
public static partial class DevHarness
{
	/// <summary>Three sturdy enemies, so area cards have several targets.</summary>
	internal const string CardFightEncounter = "RUBY_RAIDERS_NORMAL";

	private const int CardTestGold = 300;

	internal const int MaxHandSize = 10;

	private static async Task RunCardTest()
	{
		// No god mode here: it gives 999999999 Strength, so every attack would one-shot its target. The character is
		// healed before each card instead.
		await StartRunAsCharacter(screenshots: false);
		using IDisposable selector = CardSelectCmd.UseSelector(new AutoSlayCardSelector(new Rng(20260923u)));
		CharacterTests kit = Kit;
		AncientChecks();
		foreach (Func<Task>? checks in new[] { kit.RelicChecks, kit.PotionChecks, kit.PowerChecks })
		{
			if (checks != null)
			{
				await checks();
			}
		}
		await RelicAndPotionTexts();
		// --pres-only relics stops here; --pres-only A,B plays just those cards.
		string? only = CommandLineHelper.GetValue("pres-only");
		if (only == "relics")
		{
			Note("Card test finished (relics and potions only)");
			return;
		}
		var filter = (only ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		List<CardModel> cards = TestCharacter.CardPool.AllCards.Concat(kit.ExtraCards?.Invoke() ?? Enumerable.Empty<CardModel>())
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

	/// <summary>Every relic and potion of the character renders its text (the character's own checks test what they do).</summary>
	private static async Task RelicAndPotionTexts()
	{
		foreach (RelicModel relic in TestCharacter.RelicPool.AllRelics)
		{
			CheckText($"relic {relic.Id.Entry}", () => string.Join(" ", relic.HoverTips.OfType<HoverTip>().Select(t => t.Description)));
		}
		foreach (PotionModel potion in TestCharacter.PotionPool.AllPotions)
		{
			CheckText($"potion {potion.Id.Entry}", () => string.Join(" ", potion.HoverTips.OfType<HoverTip>().Select(t => t.Description)));
		}
		await Task.CompletedTask;
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
			await PrepareCardTurn(ctx, combat, canonical, upgraded);
			// Tokens outside the card pool can't be added by the console; the character's kit adds those.
			if (Kit.AddCardToHand == null || !await Kit.AddCardToHand(canonical, combat))
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
			CardSnapshot before = CardSnapshot.Take(me, combat, target, Kit.Snapshot);
			bool playable = card.CanPlay(out _, out _);
			await CardCmd.AutoPlay(ctx, card, target);
			await WaitForCardToSettle(combat);
			CardSnapshot after = CardSnapshot.Take(me, combat, target, Kit.Snapshot);
			string diff = before.Diff(after);
			result["playable"] = playable;
			result["diff"] = diff;
			Note($"card {label}{(playable ? "" : " (was not playable)")}: {diff}");
			Kit.CheckCardEffect?.Invoke(canonical.Id.Entry, upgraded, before, after, (ok, what) => Check(ok, $"{label}: {what}"));
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

	/// <summary>Same starting point for every card: its turn, 300 Gold, 3+ Energy, a small hand, healthy enemies, then the character's own setup.</summary>
	private static async Task PrepareCardTurn(PlayerChoiceContext ctx, ICombatState combat, CardModel canonical, bool upgraded)
	{
		Player me = Me;
		await WaitUntil(() => me.PlayerCombatState?.Phase == PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(30));
		me.Creature.SetCurrentHpInternal(me.Creature.MaxHp);
		await PlayerCmd.SetGold(CardTestGold, me);
		if (me.PlayerCombatState!.Energy < 3)
		{
			me.PlayerCombatState.Energy = 3;
		}
		if (Kit.PrepareCardTurn != null)
		{
			await Kit.PrepareCardTurn(ctx);
		}
		if (Kit.PrepareCardTurnFor != null)
		{
			await Kit.PrepareCardTurnFor(ctx, canonical, upgraded);
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

	// ---------------------------------------------------------------- relics and potions

	/// <summary>Every Ancient (Neow, Darv, ...) has dialogues for the character, and every line renders. The Architect is its own event type, covered by full runs.</summary>
	private static void AncientChecks()
	{
		foreach (AncientEventModel ancient in ModelDb.AllAncients)
		{
			try
			{
				if (!ancient.DialogueSet.CharacterDialogues.TryGetValue(TestCharacterId, out IReadOnlyList<AncientDialogue>? dialogues))
				{
					Error($"Ancient {ancient.Id.Entry}: no dialogues for {TestCharacterId}");
					continue;
				}
				int lines = 0;
				foreach (AncientDialogueLine line in dialogues.SelectMany(d => d.Lines))
				{
					lines++;
					CheckText($"Ancient {ancient.Id.Entry} line {lines}", () => line.LineText?.GetFormattedText() ?? "");
				}
				Check(lines > 0, $"Ancient {ancient.Id.Entry}: {dialogues.Count} dialogues, {lines} lines for {TestCharacterId}");
			}
			catch (Exception e)
			{
				Error($"Ancient {ancient.Id.Entry}: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	internal static async Task<PotionModel?> AddPotion(string id)
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

	internal static void CheckRelicTexts()
	{
		foreach (RelicModel relic in Me.Relics)
		{
			CheckText($"relic {relic.Id.Entry}", () => string.Join(" ", relic.HoverTips.OfType<HoverTip>().Select(t => t.Description)));
		}
	}

	/// <summary>Renders a text and flags exceptions or leftover template braces (a missing or misnamed variable).</summary>
	internal static void CheckText(string what, Func<string> render, bool allowEmpty = false)
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

	/// <summary>The visible state before and after a card: shared counters, plus the character's own (CharacterTests.Snapshot).</summary>
	internal sealed class CardSnapshot
	{
		public int Energy;
		public int Gold;
		public int Block;
		public int Hand;
		public int Enemies;
		public int EnemyHp;
		public int Round;
		public string MyPowers = "";
		public string TargetPowers = "";
		public List<string> EnemyPowers = new List<string>();

		/// <summary>The character's own counters, e.g. Trump's "wall", "tweets", "line".</summary>
		public Dictionary<string, decimal> Extra = new Dictionary<string, decimal>();

		public int I(string key) => (int)Extra.GetValueOrDefault(key);

		public decimal D(string key) => Extra.GetValueOrDefault(key);

		public int EnemiesWithPower(string id) => EnemyPowers.Count(p => p.Contains(id + ":"));

		public static CardSnapshot Take(Player me, ICombatState combat, Creature? target, Action<Player, Dictionary<string, decimal>>? extra)
		{
			List<CardModel> hand = PileType.Hand.GetPile(me).Cards.ToList();
			var snapshot = new CardSnapshot
			{
				Energy = me.PlayerCombatState?.Energy ?? 0,
				Gold = me.Gold,
				Block = me.Creature.Block,
				Hand = hand.Count,
				Enemies = combat.HittableEnemies.Count,
				EnemyHp = combat.HittableEnemies.Sum(e => e.CurrentHp),
				Round = combat.RoundNumber,
				MyPowers = Powers(me.Creature),
				TargetPowers = target == null ? "" : Powers(target),
				EnemyPowers = combat.HittableEnemies.Select(Powers).ToList()
			};
			extra?.Invoke(me, snapshot.Extra);
			return snapshot;
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
			Add("block", Block, a.Block);
			Add("hand", Hand, a.Hand);
			foreach (string key in Extra.Keys.Union(a.Extra.Keys))
			{
				Add(key, D(key).ToString("0.##"), a.D(key).ToString("0.##"));
			}
			Add("enemies", Enemies, a.Enemies);
			Add("enemyHP", EnemyHp, a.EnemyHp);
			Add("round", Round, a.Round);
			Add("me", MyPowers, a.MyPowers);
			Add("target", TargetPowers, a.TargetPowers);
			Add("enemyPowers", string.Join(" | ", EnemyPowers), string.Join(" | ", a.EnemyPowers));
			return parts.Count == 0 ? "no visible change" : string.Join(", ", parts);
		}
	}
}
