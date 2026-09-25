using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) Block, (2) 1 Energy, (3) draw.</summary>
public sealed class FolksListen : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override TargetType[] LineTargets => new[] { TargetType.Self, TargetType.Self, TargetType.Self };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(6m, ValueProp.Move), new EnergyVar(1), new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block)).Append(HoverTipFactory.ForEnergy(this));

	public FolksListen()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (line)
		{
			case 1:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
			case 2:
				await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
				break;
			case 3:
				await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
