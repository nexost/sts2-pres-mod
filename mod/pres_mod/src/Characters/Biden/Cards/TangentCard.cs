using MegaCrit.Sts2.Core.Localization;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>
/// A Tangent: Sleepy Joe's cards change their mind while he rambles (design.md §3).
/// - The card has 2 or 3 lines and only the lit one happens. Line 1 is lit when it comes into the hand.
/// - After its owner plays any other card, it moves to its next line (the last wraps back to the first).
/// - As Dark Brandon (or with an <see cref="ITangentAllLines"/> effect) every line happens, in order.
/// The text marks the lit line: its number is gold and the others are dimmed. Outside combat all lines show plainly.
/// Text keys: {D1}{L1} line one{E1}, {D2}{L2} ... (the vars are filled in <see cref="AddExtraArgsToDescription"/>).
/// </summary>
public abstract class TangentCard : CardModel
{
	private const string _dimColor = "[color=#8b8778]";

	protected TangentCard(int cost, CardType type, CardRarity rarity, TargetType target)
		: base(cost, type, rarity, target)
	{
	}

	/// <summary>How many lines the card has: 2 or 3.</summary>
	public abstract int LineCount { get; }

	/// <summary>The lit line, 1-based.</summary>
	public int Line { get; private set; } = 1;

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

	/// <summary>One line's effect.</summary>
	protected abstract Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay);

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (DoesAllLines)
		{
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

	/// <summary>Test and tool hook: light a line directly.</summary>
	public void SetLine(int line)
	{
		Line = Math.Clamp(line, 1, LineCount);
	}

	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card == this)
		{
			Line = 1;
		}
		return Task.CompletedTask;
	}

	public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		if (card == this && oldPileType != PileType.Hand && Pile?.Type == PileType.Hand)
		{
			Line = 1;
		}
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card == this || cardPlay.Card.Owner != Owner || Pile?.Type != PileType.Hand || LineCount < 2)
		{
			return;
		}
		Line = Line % LineCount + 1;
		foreach (IAfterTangentLineChanged listener in BidenHooks.ListenersOf<IAfterTangentLineChanged>(Owner.Creature))
		{
			await listener.AfterTangentLineChanged(choiceContext, this, Line);
		}
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
