using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Starter: the sleepy Defend. Doze pushes toward nodding off, which brings Dark Brandon.</summary>
public sealed class Catnap : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new BlockVar(6m, ValueProp.Move),
		new DynamicVar(CardVars.Doze, 3m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public Catnap()
		: base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await DrowsyCmd.Doze(choiceContext, Owner.Creature, DynamicVars[CardVars.Doze].BaseValue, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
