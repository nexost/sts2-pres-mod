using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Block; as Dark Brandon, also 1 Energy.</summary>
public sealed class GameFace : CardModel
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new EnergyVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public GameFace()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		if (this.IsAwake())
		{
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
