namespace TrumpMod.Models.Cards;

/// <summary>Block equal to half the Wall. Upgrade adds Retain.</summary>
public sealed class HoldTheLine : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
		new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => WallCmd.GetHeight(card.Owner.Creature) / 2)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Wall };

	public HoldTheLine()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), DynamicVars.CalculatedBlock.Props, cardPlay);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}
}
