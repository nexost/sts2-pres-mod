namespace TrumpMod.Models.Cards;

/// <summary>Free cycling for Wall decks.</summary>
public sealed class Groundbreaking : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(CardVars.Build, 3m),
		new CardsVar(1)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public Groundbreaking()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await WallCmd.Build(choiceContext, Owner.Creature, DynamicVars[CardVars.Build].BaseValue, this);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.Build].UpgradeValueBy(2m);
	}
}
