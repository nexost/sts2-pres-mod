namespace TrumpMod.Models.Cards;

public sealed class Bribe : PayGoldCard
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(GoldCostVar, 20m),
		new EnergyVar(2)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.PayGold, HoverTipFactory.ForEnergy(this) };

	public Bribe()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPaidPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[GoldCostVar].UpgradeValueBy(-5m);
	}
}
