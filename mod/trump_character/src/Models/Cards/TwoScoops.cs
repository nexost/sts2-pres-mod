namespace TrumpMod.Models.Cards;

/// <summary>Two for him, one for everyone else. Upgrade adds a draw.</summary>
public sealed class TwoScoops : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new EnergyVar(2),
		new CardsVar(1)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.ForEnergy(this) };

	public TwoScoops()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		if (IsUpgraded)
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}
}
