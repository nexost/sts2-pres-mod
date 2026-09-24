using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>The basic way to choose when Dark Brandon comes out.</summary>
public sealed class CupOfJoe : CardModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.DarkBrandon, BidenHoverTips.LaserEyes };

	public CupOfJoe()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DrowsyCmd.WakeUp(choiceContext, Owner.Creature);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
