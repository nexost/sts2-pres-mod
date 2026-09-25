using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>
/// A Tangent: Sleepy Joe's cards change their mind while he rambles (design.md §3).
/// - The card has 2 or 3 lines and only the lit one happens. Line 1 is lit when it comes into the hand.
/// - After its owner plays any other card, it moves to its next line (the last wraps back to the first).
/// - As Dark Brandon (or with an <see cref="ITangentAllLines"/> effect) every line happens, in order.
/// The text marks the lit line: its number is gold and the others are dimmed. Outside combat all lines show plainly.
/// It aims and counts as what it will do now (<see cref="LineTargets"/>): a line that hits one enemy needs a target; a line
/// of Block, cards or Energy needs none, and an Attack Tangent on such a line plays as a Skill (the frame changes too).
/// Text keys: {D1}{L1} line one{E1}, {D2}{L2} ... (the vars are filled in <see cref="AddExtraArgsToDescription"/>).
/// </summary>
public abstract class TangentCard : CardModel
{
	private const string _dimColor = "[color=#8b8778]";

	private static readonly MethodInfo? CardReload = typeof(NCard).GetMethod("Reload", BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>The type the card was last drawn as in the hand (see <see cref="RefreshHand"/>).</summary>
	private CardType? _shownType;

	protected TangentCard(int cost, CardType type, CardRarity rarity, TargetType target)
		: base(cost, type, rarity, target)
	{
	}

	/// <summary>How many lines the card has: 2 or 3.</summary>
	public abstract int LineCount { get; }

	/// <summary>The lit line, 1-based.</summary>
	public int Line { get; private set; } = 1;

	/// <summary>The line it was last played on (Index Cards), and whether that play did every line.</summary>
	public int PlayedLine { get; private set; }

	public bool PlayedAllLines { get; private set; }

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	/// <summary>Whether playing it now does every line: its owner is Dark Brandon, or something else says so.</summary>
	public bool DoesAllLines
	{
		get
		{
			if (!IsMutable || Owner?.Creature is not Creature owner || owner.CombatState == null)
			{
				return false;
			}
			return DrowsyCmd.IsDarkBrandon(owner) || BidenHooks.ListenersOf<ITangentAllLines>(owner).Any(l => l.DoesAllLines(this));
		}
	}

	/// <summary>
	/// What each line aims at, line 1 first: <see cref="TargetType.AnyEnemy"/> (one chosen enemy),
	/// <see cref="TargetType.AllEnemies"/>, or <see cref="TargetType.Self"/> (Block, cards, Energy, nothing).
	/// </summary>
	protected abstract TargetType[] LineTargets { get; }

	private bool InCombatNow => IsMutable && CombatState != null && Owner?.Creature != null;

	/// <summary>The lines playing it now would do: the lit one, or every line.</summary>
	private IEnumerable<int> LinesNow => DoesAllLines ? Enumerable.Range(1, LineCount) : new[] { Line };

	/// <summary>Aim it at one enemy only when a line it would do now needs one; all enemies or no target otherwise.</summary>
	public override TargetType TargetType
	{
		get
		{
			if (!InCombatNow)
			{
				return base.TargetType;
			}
			List<TargetType> targets = LinesNow.Select(line => LineTargets[line - 1]).ToList();
			return targets.Contains(TargetType.AnyEnemy) ? TargetType.AnyEnemy
				: targets.Contains(TargetType.AllEnemies) ? TargetType.AllEnemies
				: TargetType.Self;
		}
	}

	/// <summary>An Attack Tangent is an Attack while a line it would do now hits enemies, and a Skill on its other lines.</summary>
	public override CardType Type
	{
		get
		{
			CardType type = base.Type;
			if (type != CardType.Attack || !InCombatNow)
			{
				return type;
			}
			return LinesNow.Any(line => LineTargets[line - 1] != TargetType.Self) ? CardType.Attack : CardType.Skill;
		}
	}

	/// <summary>
	/// The enemy a one-enemy line hits: the chosen one, or the first enemy standing when the card was played without one
	/// (its lit line changed after it was aimed, while it waited in the queue).
	/// </summary>
	protected Creature? EnemyFor(CardPlay cardPlay) =>
		cardPlay.Target is Creature target && target.IsAlive && target.Side == CombatSide.Enemy ? target : CombatState?.HittableEnemies.FirstOrDefault();

	/// <summary>
	/// Redraw the Tangents in the local hand whose type changed (Attack ↔ Skill): the game draws a card's frame and type
	/// label only when the card is loaded. Called after a line moves, a card is drawn or played, and on waking or falling asleep.
	/// </summary>
	public static void RefreshHand()
	{
		if (NPlayerHand.Instance is not NPlayerHand hand || CardReload == null)
		{
			return;
		}
		foreach (NHandCardHolder holder in hand.ActiveHolders)
		{
			if (holder.CardNode is NCard node && node.Model is TangentCard tangent && tangent._shownType != tangent.Type)
			{
				tangent._shownType = tangent.Type;
				CardReload.Invoke(node, null);
			}
		}
	}

	/// <summary>Glows gold while it would do every line.</summary>
	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null && Pile?.Type == PileType.Hand && DoesAllLines;

	/// <summary>One line's effect.</summary>
	protected abstract Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay);

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PlayedLine = Line;
		PlayedAllLines = DoesAllLines;
		if (PlayedAllLines)
		{
			BidenVfx.AllLines(Owner.Creature, LineCount);
			for (int line = 1; line <= LineCount; line++)
			{
				await PlayLine(line, choiceContext, cardPlay);
			}
		}
		else
		{
			await PlayLine(Line, choiceContext, cardPlay);
		}
	}

	/// <summary>Light a line directly (Brain Freeze; tests).</summary>
	public void SetLine(int line)
	{
		Line = Math.Clamp(line, 1, LineCount);
		RefreshHand();
	}

	/// <summary>Move to the next line (after another card is played; Where Was I?).</summary>
	public async Task MoveToNextLine(PlayerChoiceContext choiceContext)
	{
		if (LineCount < 2)
		{
			return;
		}
		Line = Line % LineCount + 1;
		RefreshHand();
		BidenVfx.TangentMoved(Owner.Creature);
		foreach (IAfterTangentLineChanged listener in BidenHooks.ListenersOf<IAfterTangentLineChanged>(Owner.Creature))
		{
			await listener.AfterTangentLineChanged(choiceContext, this, Line);
		}
	}

	/// <summary>Tall Tales: every damage and Block number on the card goes up for the rest of combat.</summary>
	public void GrowNumbers(decimal by)
	{
		foreach (DynamicVar dynamicVar in DynamicVars.Values)
		{
			if (dynamicVar is DamageVar or BlockVar)
			{
				dynamicVar.BaseValue += by;
			}
		}
	}

	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card == this)
		{
			Line = 1;
		}
		RefreshHand();
		return Task.CompletedTask;
	}

	public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		if (card == this && oldPileType != PileType.Hand && Pile?.Type == PileType.Hand)
		{
			Line = 1;
			RefreshHand();
		}
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card == this || cardPlay.Card.Owner != Owner || Pile?.Type != PileType.Hand)
		{
			// Something else may have changed what the Tangents in hand do (Off Script's first Tangent was played).
			RefreshHand();
			return;
		}
		await MoveToNextLine(choiceContext);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		bool inPlay = IsMutable && (Pile?.Type == PileType.Hand || Pile?.Type == PileType.Play);
		bool all = inPlay && DoesAllLines;
		for (int line = 1; line <= 3; line++)
		{
			bool lit = !inPlay || all || line == Line;
			bool marked = inPlay && (all || line == Line);
			description.Add("L" + line, marked ? $"[gold]({line})[/gold]" : $"({line})");
			description.Add("D" + line, lit ? "" : _dimColor);
			description.Add("E" + line, lit ? "" : "[/color]");
		}
	}
}
