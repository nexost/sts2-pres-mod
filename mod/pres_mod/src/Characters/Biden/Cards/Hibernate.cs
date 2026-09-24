using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Huge Block and Doze 6.</summary>
public sealed class Hibernate : SleepyCard
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(18m, ValueProp.Move), new DynamicVar(CardVars.Doze, 6m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public Hibernate()
		: base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(6m);
	}
}
