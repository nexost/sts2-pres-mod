using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TrumpMod.Models.Cards;

/// <summary>A random Colorless card that's free this turn. Nobody knows what it means.</summary>
public sealed class Covfefe : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	public Covfefe()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		IEnumerable<CardModel> options = ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);
		CardModel? card = CardFactory.GetDistinctForCombat(Owner, options, 1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
		if (card != null)
		{
			card.SetToFreeThisTurn();
			await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
	}
}
