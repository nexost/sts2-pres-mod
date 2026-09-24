using PresMod.Characters.Trump.Cards;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Relics;

/// <summary>Common: whenever you Build, Build 1 additional.</summary>
public sealed class HardHat : RelicModel, IBuildModifier
{
	public override RelicRarity Rarity => RelicRarity.Common;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.Build, 1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build };

	public decimal ModifyBuild(Creature builder, decimal amount)
	{
		if (builder != Owner.Creature || amount <= 0m)
		{
			return amount;
		}
		Flash();
		return amount + DynamicVars[CardVars.Build].BaseValue;
	}
}

/// <summary>Uncommon: whenever you Deport an enemy, gain 1 Energy.</summary>
public sealed class LongRedTie : RelicModel, IAfterDeport
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport, HoverTipFactory.ForEnergy(this) };

	public async Task AfterDeport(PlayerChoiceContext choiceContext, Player deporter, Creature deported)
	{
		if (deporter == Owner && !CombatManager.Instance.IsOverOrEnding)
		{
			Flash();
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}
}

/// <summary>Uncommon: at the end of combat, gain 2 Gold for each Section (paid before the fight's Wall is cleared).</summary>
public sealed class GoldPlatedBricks : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new GoldVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		int gold = DynamicVars.Gold.IntValue * WallCmd.GetSections(Owner.Creature);
		if (gold > 0 && Owner.Creature.IsAlive)
		{
			Flash();
			await PlayerCmd.GainGold(gold, Owner);
		}
	}
}

/// <summary>Rare: Deported enemies still drop their Gold. The reward math is patched in RubberStampPatch.</summary>
public sealed class RubberStamp : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };
}

/// <summary>Rare: the first Tweet you play each turn is played twice (Throwing Axe's play-count hook, once per turn).</summary>
public sealed class LateNightPhone : RelicModel
{
	private bool _usedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	private bool UsedThisTurn
	{
		get
		{
			return _usedThisTurn;
		}
		set
		{
			AssertMutable();
			_usedThisTurn = value;
		}
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return !UsedThisTurn && card is Tweet && card.Owner == Owner ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		UsedThisTurn = true;
		Flash();
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner.Creature))
		{
			UsedThisTurn = false;
		}
		return Task.CompletedTask;
	}
}

/// <summary>Shop: at the start of your turn, if you have 250 or more Gold, gain 1 Energy.</summary>
public sealed class GoldPlatedToilet : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new GoldVar(250),
		new EnergyVar(1)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.ForEnergy(this) };

	public override async Task AfterEnergyReset(Player player)
	{
		if (player == Owner && Owner.Gold >= DynamicVars.Gold.IntValue)
		{
			Flash();
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}
}
