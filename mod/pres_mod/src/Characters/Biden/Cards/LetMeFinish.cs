using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) Block, (2) draw.</summary>
public sealed class LetMeFinish : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 2;

	protected override TargetType[] LineTargets => new[] { TargetType.Self, TargetType.Self };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block));

	public LetMeFinish()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (line == 1)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
			return;
		}
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
