using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Retain the hand this turn (the game's Retain Hand), and Doze 1 per card kept.</summary>
public sealed class SoundAsleep : CardModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<RetainHandPower>(), BidenHoverTips.Doze, BidenHoverTips.Drowsy };

	public SoundAsleep()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int cards = PileType.Hand.GetPile(Owner).Cards.Count(c => c != this);
		await PowerCmd.Apply<RetainHandPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
		if (cards > 0)
		{
			await DrowsyCmd.Doze(choiceContext, Owner.Creature, cards, this);
		}
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
