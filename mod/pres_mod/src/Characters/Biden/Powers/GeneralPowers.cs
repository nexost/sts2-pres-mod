using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>Amtrak Joe: draw Amount additional cards at the start of your turn.</summary>
public sealed class AmtrakJoePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner.Player ? count + Amount : count;
}

/// <summary>Bipartisan: whenever you play an Attack right after a Skill, or a Skill right after an Attack, gain Amount Block.</summary>
public sealed class BipartisanPower : PowerModel
{
	private CardType? _lastType;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner?.Creature != Owner)
		{
			return;
		}
		CardType type = cardPlay.Card.Type;
		bool crossed = (_lastType == CardType.Skill && type == CardType.Attack) || (_lastType == CardType.Attack && type == CardType.Skill);
		_lastType = type;
		if (crossed)
		{
			Flash();
			await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
		}
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner))
		{
			_lastType = null;
		}
		return Task.CompletedTask;
	}
}

/// <summary>Gaffe Machine: at the start of your turn, Amount random cards in your hand cost 0 this turn.</summary>
public sealed class GaffeMachinePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner.Player)
		{
			return Task.CompletedTask;
		}
		List<CardModel> pricey = PileType.Hand.GetPile(player).Cards.Where(c => !c.EnergyCost.CostsX && c.EnergyCost.GetResolved() > 0).ToList();
		for (int i = 0; i < Amount && pricey.Count > 0; i++)
		{
			CardModel card = player.RunState.Rng.CombatCardSelection.NextItem(pricey)!;
			pricey.Remove(card);
			card.EnergyCost.SetThisTurn(0);
		}
		if (Amount > 0)
		{
			Flash();
		}
		return Task.CompletedTask;
	}
}

/// <summary>Infrastructure Law: at the start of your turn, gain Amount Energy.</summary>
public sealed class InfrastructureLawPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner.Player)
		{
			Flash();
			await PlayerCmd.GainEnergy(Amount, player);
		}
	}
}

/// <summary>Soul of the Nation (Ancient): at the start of your turn, gain 5 Block and draw 1 card; Laser Eyes deal 10 more. Per copy.</summary>
public sealed class SoulOfTheNationPower : PowerModel, ILaserEyesModifier
{
	public const int BlockPerStack = 5;

	public const int LaserPerStack = 10;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.LaserEyes };

	public decimal ModifyLaserDamage(Creature owner, decimal damage) => owner == Owner ? damage + LaserPerStack * Amount : damage;

	public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner.Player ? count + Amount : count;

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner.Player)
		{
			Flash();
			await CreatureCmd.GainBlock(Owner, BlockPerStack * Amount, ValueProp.Unpowered, null);
		}
	}
}
