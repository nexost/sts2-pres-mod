using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Block for the Drowsy he has plus a bit, then he nods off.</summary>
public sealed class SleepIn : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(5m),
		new CalculationExtraVar(1m),
		new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => DrowsyCmd.GetDrowsy(card.Owner.Creature))
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Drowsy, BidenHoverTips.NodOff };

	public SleepIn()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), DynamicVars.CalculatedBlock.Props, cardPlay);
		await DrowsyCmd.NodOff(choiceContext, Owner.Creature);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.CalculationBase.UpgradeValueBy(3m);
	}
}
