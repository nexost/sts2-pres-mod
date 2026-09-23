using TrumpMod.Models.Cards;

namespace TrumpMod.Models.Powers;

/// <summary>Blocked!: this turn, whenever you play a Tweet, gain Block.</summary>
public sealed class BlockedPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card is Tweet && cardPlay.Card.Owner.Creature == Owner)
		{
			Flash();
			await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (Owner.Side == side)
		{
			await PowerCmd.Remove(this);
		}
	}
}
