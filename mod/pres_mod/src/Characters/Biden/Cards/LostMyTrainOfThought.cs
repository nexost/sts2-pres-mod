using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Shuffle the hand into the Draw Pile, then draw that many. (Tangents come back on line 1.)</summary>
public sealed class LostMyTrainOfThought : CardModel
{
	public LostMyTrainOfThought()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
		if (hand.Count == 0)
		{
			return;
		}
		await CardPileCmd.Add(hand, PileType.Draw, CardPilePosition.Random);
		await CardPileCmd.Draw(choiceContext, hand.Count, Owner);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
