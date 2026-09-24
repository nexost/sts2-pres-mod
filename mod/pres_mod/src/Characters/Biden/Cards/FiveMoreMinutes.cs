using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Block and Doze 4.</summary>
public sealed class FiveMoreMinutes : SleepyCard
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(9m, ValueProp.Move), new DynamicVar(CardVars.Doze, 4m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.Static(StaticHoverTip.Block), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public FiveMoreMinutes()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await Doze(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
