using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Draw and a little Block.</summary>
public sealed class AllAboard : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2), new BlockVar(4m, ValueProp.Move) };

	public AllAboard()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		_ = BidenVfx.Vehicle(Owner.Creature, "train", size: 0.8f, seconds: 1.3f, behind: true);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
