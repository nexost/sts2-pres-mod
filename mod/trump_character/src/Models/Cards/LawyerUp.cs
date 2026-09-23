using MegaCrit.Sts2.Core.Models.Powers;

namespace TrumpMod.Models.Cards;

public sealed class LawyerUp : PayGoldCard
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(GoldCostVar, 40m),
		new PowerVar<IntangiblePower>(1m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.PayGold, HoverTipFactory.FromPower<IntangiblePower>() };

	public LawyerUp()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPaidPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, DynamicVars[nameof(IntangiblePower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[GoldCostVar].UpgradeValueBy(-10m);
	}
}
