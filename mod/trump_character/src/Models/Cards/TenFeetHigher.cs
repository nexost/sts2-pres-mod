namespace TrumpMod.Models.Cards;

/// <summary>Build, and Build again once the Wall is a Brick Wall (25+). The check happens after the first Build.</summary>
public sealed class TenFeetHigher : CardModel
{
	public const int Threshold = 25;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.Build, 5m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null
		&& WallCmd.GetHeight(Owner.Creature) + DynamicVars[CardVars.Build].BaseValue >= Threshold;

	public TenFeetHigher()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal build = DynamicVars[CardVars.Build].BaseValue;
		await WallCmd.Build(choiceContext, Owner.Creature, build, this);
		if (WallCmd.GetHeight(Owner.Creature) >= Threshold)
		{
			await WallCmd.Build(choiceContext, Owner.Creature, build, this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.Build].UpgradeValueBy(2m);
	}
}
