using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Free Block and a card.</summary>
public sealed class ChocolateChip : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(3m, ValueProp.Move), new CardsVar(1) };

	public ChocolateChip()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
