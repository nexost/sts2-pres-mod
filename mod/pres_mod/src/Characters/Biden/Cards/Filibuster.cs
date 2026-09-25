using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent, 2 cost: (1) big Block, (2) draw 3, (3) 2 Energy.</summary>
public sealed class Filibuster : TangentCard
{
	public override bool GainsBlock => true;

	public override int LineCount => 3;

	protected override TargetType[] LineTargets => new[] { TargetType.Self, TargetType.Self, TargetType.Self };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(12m, ValueProp.Move), new CardsVar(3), new EnergyVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.Block)).Append(HoverTipFactory.ForEnergy(this));

	public Filibuster()
		: base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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
				await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
				break;
			case 3:
				await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(4m);
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
