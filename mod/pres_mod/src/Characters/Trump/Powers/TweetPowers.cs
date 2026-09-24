using PresMod.Characters.Trump.Cards;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Powers;

/// <summary>Social Media Intern: at the start of your turn, add Amount Tweets into your Hand (Infinite Blades timing).</summary>
public sealed class SocialMediaInternPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == Owner.Player)
		{
			Flash();
			await Tweet.CreateInHand(player, Amount, combatState);
		}
	}
}

/// <summary>3 AM Posting: at the start of your turn, add Amount Tweets into your Hand.</summary>
public sealed class ThreeAmPostingPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == Owner.Player)
		{
			Flash();
			await Tweet.CreateInHand(player, Amount, combatState);
		}
	}
}

/// <summary>Ratings: whenever you play a Tweet, Build Amount.</summary>
public sealed class RatingsPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet, TrumpHoverTips.Build };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card is Tweet && cardPlay.Card.Owner.Creature == Owner)
		{
			Flash();
			await WallCmd.Build(choiceContext, Owner, Amount);
		}
	}
}

/// <summary>Verified Account: Tweets deal Amount additional damage (Accuracy for Tweets).</summary>
public sealed class VerifiedAccountPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
	{
		return dealer == Owner && props.IsPoweredAttack() && card is Tweet ? Amount : 0m;
	}
}
