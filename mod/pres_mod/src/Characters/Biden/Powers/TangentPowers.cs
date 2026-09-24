using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>Stream of Consciousness: whenever a Tangent card changes line, gain Amount Block.</summary>
public sealed class StreamOfConsciousnessPower : PowerModel, IAfterTangentLineChanged
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public async Task AfterTangentLineChanged(PlayerChoiceContext choiceContext, CardModel card, int line)
	{
		if (card.Owner?.Creature != Owner)
		{
			return;
		}
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
	}
}

/// <summary>Long-Winded: whenever you play a Tangent card, deal Amount damage to a random enemy.</summary>
public sealed class LongWindedPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card is not TangentCard || cardPlay.Card.Owner?.Creature != Owner || Owner.Player == null)
		{
			return;
		}
		Creature? target = Owner.Player.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
		if (target == null)
		{
			return;
		}
		Flash();
		await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner, null);
	}
}

/// <summary>Off Script: the first Tangent card you play each turn does all its lines.</summary>
public sealed class OffScriptPower : PowerModel, ITangentAllLines
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public bool DoesAllLines(CardModel card)
	{
		if (card.Owner?.Creature != Owner || CombatState == null)
		{
			return false;
		}
		return !CombatManager.Instance.History.CardPlaysFinished.Any(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card is TangentCard);
	}
}

/// <summary>Tall Tales: whenever a Tangent card changes line, its damage and Block go up by Amount for the rest of combat.</summary>
public sealed class TallTalesPower : PowerModel, IAfterTangentLineChanged
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public Task AfterTangentLineChanged(PlayerChoiceContext choiceContext, CardModel card, int line)
	{
		if (card is TangentCard tangent && card.Owner?.Creature == Owner)
		{
			tangent.GrowNumbers(Amount);
		}
		return Task.CompletedTask;
	}
}

/// <summary>Moment of Clarity: this turn, your Tangent cards do all their lines.</summary>
public sealed class MomentOfClarityPower : PowerModel, ITangentAllLines
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public bool DoesAllLines(CardModel card) => card.Owner?.Creature == Owner;

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(Owner))
		{
			await PowerCmd.Remove(this);
		}
	}
}
