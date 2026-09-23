namespace TrumpMod.Models.Cards;

/// <summary>Double your Wall (a Build of its current height, so Build modifiers apply once).</summary>
public sealed class GreatWall : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public GreatWall()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await WallCmd.Build(choiceContext, Owner.Creature, WallCmd.GetHeight(Owner.Creature), this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
