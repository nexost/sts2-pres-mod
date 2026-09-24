using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Block; as Dark Brandon, also draw 2.</summary>
public sealed class MorningPerson : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(7m, ValueProp.Move), new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public MorningPerson()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		if (this.IsAwake())
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
