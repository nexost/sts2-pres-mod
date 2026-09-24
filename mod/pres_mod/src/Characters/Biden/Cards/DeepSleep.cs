using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Nod off; the next Laser Eyes deal double damage.</summary>
public sealed class DeepSleep : CardModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff, BidenHoverTips.LaserEyes };

	public DeepSleep()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<DeepSleepPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
		await DrowsyCmd.NodOff(choiceContext, Owner.Creature);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
