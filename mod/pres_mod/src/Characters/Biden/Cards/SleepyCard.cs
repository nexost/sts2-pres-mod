using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>
/// A card that Dozes (its "Doze" var). It glows red when playing it now would nod him off mid-turn, since the turn ends
/// right after it: the warning from design.md §9. Nap decks that want the nap can play it anyway.
/// </summary>
public abstract class SleepyCard : CardModel
{
	protected SleepyCard(int cost, CardType type, CardRarity rarity, TargetType target)
		: base(cost, type, rarity, target)
	{
	}

	protected decimal DozeAmount => DynamicVars[CardVars.Doze].BaseValue;

	protected override bool ShouldGlowRedInternal => IsMutable && CombatState != null && Owner?.Creature is Creature owner
		&& Pile?.Type == PileType.Hand && DrowsyCmd.WouldNodOff(owner, DozeAmount);

	protected Task Doze(PlayerChoiceContext choiceContext) => DrowsyCmd.Doze(choiceContext, Owner.Creature, DozeAmount, this);
}

/// <summary>Shared checks for Sleepy Joe's cards.</summary>
public static class BidenCardExtensions
{
	/// <summary>The card's owner is Dark Brandon right now.</summary>
	public static bool IsAwake(this CardModel card) =>
		card.IsMutable && card.CombatState != null && card.Owner?.Creature is Creature owner && DrowsyCmd.IsDarkBrandon(owner);

	/// <summary>How many cards the owner has played this turn, not counting the one being played.</summary>
	public static int CardsPlayedThisTurn(this CardModel card) =>
		card.CombatState == null ? 0 : CombatManager.Instance.History.CardPlaysFinished.Count(e => e.HappenedThisTurn(card.CombatState) && e.CardPlay.Card.Owner == card.Owner);
}
