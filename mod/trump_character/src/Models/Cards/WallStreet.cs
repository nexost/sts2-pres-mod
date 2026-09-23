namespace TrumpMod.Models.Cards;

/// <summary>Bridges the Wall and Deals: Gold per Section.</summary>
public sealed class WallStreet : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new GoldVar(4) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null && WallCmd.GetSections(Owner.Creature) > 0;

	public WallStreet()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int gold = DynamicVars.Gold.IntValue * WallCmd.GetSections(Owner.Creature);
		if (gold > 0)
		{
			await PlayerCmd.GainGold(gold, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
		DynamicVars.Gold.UpgradeValueBy(2m);
	}
}
