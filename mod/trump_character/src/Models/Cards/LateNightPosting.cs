namespace TrumpMod.Models.Cards;

/// <summary>Blade Dance with Tweets.</summary>
public sealed class LateNightPosting : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public LateNightPosting()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await Tweet.CreateInHand(Owner, DynamicVars.Cards.IntValue, CombatState!);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
