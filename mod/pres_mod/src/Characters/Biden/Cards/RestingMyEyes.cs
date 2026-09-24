using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Free Block and Doze that waits in the hand for the right moment.</summary>
public sealed class RestingMyEyes : SleepyCard
{
	public override bool GainsBlock => true;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(4m, ValueProp.Move), new DynamicVar(CardVars.Doze, 3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public RestingMyEyes()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
