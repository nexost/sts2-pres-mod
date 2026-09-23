namespace TrumpMod.Models.Cards;

/// <summary>HP loss ignores Block, so it reaches enemies that stack Block, then Deports.</summary>
public sealed class BackgroundCheck : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new HpLossVar(7m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	public BackgroundCheck()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner.Creature, this);
		await DeportCmd.TryDeport(choiceContext, cardPlay.Target, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.HpLoss.UpgradeValueBy(3m);
	}
}
