using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>"End of quote. Repeat the line." Plays the last card played this turn again, if it's in the Discard Pile.</summary>
public sealed class RepeatTheLine : CardModel
{
	public RepeatTheLine()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override bool ShouldGlowGoldInternal => IsMutable && CombatState != null && LastRepeatable() != null;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardPlayFinishedEntry? last = LastRepeatable();
		if (last == null)
		{
			return;
		}
		Creature? target = last.CardPlay.Target is { IsAlive: true } alive ? alive : null;
		await CardCmd.AutoPlay(choiceContext, last.CardPlay.Card, target);
	}

	/// <summary>The owner's last card this turn, if it can be played again: in the Discard Pile, not X-cost, not another Repeat the Line.</summary>
	private CardPlayFinishedEntry? LastRepeatable()
	{
		CardPlayFinishedEntry? last = CombatManager.Instance.History.CardPlaysFinished
			.LastOrDefault(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner && e.CardPlay.Card != this);
		CardModel? card = last?.CardPlay.Card;
		if (card == null || card is RepeatTheLine || card.EnergyCost.CostsX || card.Pile?.Type != PileType.Discard)
		{
			return null;
		}
		return last;
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
