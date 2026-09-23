using TrumpMod.Models.Cards;

namespace TrumpMod.Models.Powers;

/// <summary>Executive Order: the first Amount Skills you play each turn cost 0 (Free Skill that refills every turn).</summary>
public sealed class ExecutiveOrderPower : PowerModel
{
	private int _usedThisTurn;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!Applies(card))
		{
			return false;
		}
		modifiedCost = 0m;
		return true;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (Applies(cardPlay.Card))
		{
			_usedThisTurn++;
			Flash();
		}
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner))
		{
			_usedThisTurn = 0;
		}
		return Task.CompletedTask;
	}

	private bool Applies(CardModel card)
	{
		return _usedThisTurn < Amount && card.Owner.Creature == Owner && card.Type == CardType.Skill
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}

/// <summary>
/// Make the Spire Great Again (Ancient): at the start of your turn, Build 4, gain 4 Gold and add a Tweet into your Hand,
/// each multiplied by Amount so copies stack.
/// </summary>
public sealed class MakeTheSpireGreatAgainPower : PowerModel
{
	public const int BuildPerStack = 4;

	public const int GoldPerStack = 4;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Tweet };

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner) && Owner.Player != null)
		{
			Flash();
			await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner, BuildPerStack * Amount);
			await PlayerCmd.GainGold(GoldPerStack * Amount, Owner.Player);
		}
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == Owner.Player)
		{
			await Tweet.CreateInHand(player, Amount, combatState);
		}
	}
}
