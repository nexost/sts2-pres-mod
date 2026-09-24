using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Big Block and Doze 3.</summary>
public sealed class PillowFort : SleepyCard
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(14m, ValueProp.Move), new DynamicVar(CardVars.Doze, 3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public PillowFort()
		: base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(4m);
	}
}
