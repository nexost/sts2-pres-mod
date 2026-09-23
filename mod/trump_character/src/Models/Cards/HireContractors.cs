namespace TrumpMod.Models.Cards;

/// <summary>Gold into Wall.</summary>
public sealed class HireContractors : PayGoldCard
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(GoldCostVar, 20m),
		new DynamicVar(CardVars.Build, 14m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.PayGold, TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public HireContractors()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPaidPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await WallCmd.Build(choiceContext, Owner.Creature, DynamicVars[CardVars.Build].BaseValue, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.Build].UpgradeValueBy(4m);
	}
}
