using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Discard the hand, draw that many, and upgrade the new cards for the rest of combat.</summary>
public sealed class BuildBackBetter : CardModel
{
	public BuildBackBetter()
		: base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
		foreach (CardModel card in hand)
		{
			await CardCmd.Discard(choiceContext, card);
		}
		IEnumerable<CardModel> drawn = await CardPileCmd.Draw(choiceContext, hand.Count, Owner);
		foreach (CardModel card in drawn.Where(c => c.IsUpgradable).ToList())
		{
			CardCmd.Upgrade(card);
		}
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
