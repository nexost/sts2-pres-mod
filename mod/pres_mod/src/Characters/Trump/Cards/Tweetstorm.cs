using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>X cost: add X+1 Tweets (Upgraded Tweets once upgraded).</summary>
public sealed class Tweetstorm : CardModel
{
	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public Tweetstorm()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await Tweet.CreateInHand(Owner, ResolveEnergyXValue() + 1, CombatState!, upgraded: IsUpgraded);
	}
}
