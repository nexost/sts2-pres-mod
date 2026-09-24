using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Relics;

/// <summary>Common: whenever you Doze, Doze 1 additional.</summary>
public sealed class TravelPillow : RelicModel, IDozeModifier
{
	public override RelicRarity Rarity => RelicRarity.Common;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Doze", 1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public decimal ModifyDoze(Creature owner, decimal amount) => owner == Owner.Creature && amount > 0m ? amount + DynamicVars["Doze"].BaseValue : amount;
}

/// <summary>Uncommon: whenever you wake up, heal 3 HP.</summary>
public sealed class IceCreamCone : RelicModel, IAfterWakeUp
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new HealVar(3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp };

	public async Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner.Creature)
		{
			return;
		}
		Flash();
		await CreatureCmd.Heal(owner, DynamicVars.Heal.BaseValue);
	}
}

/// <summary>Uncommon: whenever you play a Tangent card on its last line, draw 1 card.</summary>
public sealed class IndexCards : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card is TangentCard tangent && tangent.Owner == Owner && !tangent.PlayedAllLines && tangent.PlayedLine == tangent.LineCount)
		{
			Flash();
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}
}

/// <summary>Rare ('67 Corvette): whenever you wake up, draw 2 cards.</summary>
public sealed class Corvette : RelicModel, IAfterWakeUp
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp };

	public async Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner.Creature)
		{
			return;
		}
		Flash();
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}
}

/// <summary>Rare: Laser Eyes deal 50% more damage.</summary>
public sealed class DarkBrandonMug : RelicModel, ILaserEyesModifier
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.LaserEyes };

	public decimal ModifyLaserDamage(Creature owner, decimal damage) => owner == Owner.Creature ? damage * 1.5m : damage;
}

/// <summary>Rare: whenever you wake up, your Tangent cards cost 0 for the rest of that turn.</summary>
public sealed class Teleprompter : RelicModel, IAfterWakeUp
{
	private bool _awakeThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.Tangent };

	public Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner == Owner.Creature)
		{
			_awakeThisTurn = true;
			Flash();
		}
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(Owner.Creature))
		{
			_awakeThisTurn = false;
		}
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_awakeThisTurn = false;
		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!_awakeThisTurn || card is not TangentCard || card.Owner != Owner)
		{
			return false;
		}
		modifiedCost = 0m;
		return true;
	}
}

/// <summary>Shop: the first time you play 5 cards in a turn, gain 1 Energy.</summary>
public sealed class AmtrakPass : RelicModel
{
	private const int Stops = 5;

	private int _playedThisTurn;

	private bool _paidThisTurn;

	public override RelicRarity Rarity => RelicRarity.Shop;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(Stops), new EnergyVar(1) };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner || _paidThisTurn)
		{
			return;
		}
		_playedThisTurn++;
		if (_playedThisTurn >= Stops)
		{
			_paidThisTurn = true;
			Flash();
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner.Creature))
		{
			_playedThisTurn = 0;
			_paidThisTurn = false;
		}
		return Task.CompletedTask;
	}
}
