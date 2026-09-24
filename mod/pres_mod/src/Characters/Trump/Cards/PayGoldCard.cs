using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>
/// Base for "Pay X Gold" cards: unplayable without X Gold, pays before its effect.
/// The cost is the "GoldCost" dynamic var, so upgrades can lower it; NCard shows it in the star-cost badge (PayGoldBadgePatch).
/// </summary>
public abstract class PayGoldCard : CardModel
{
	public const string GoldCostVar = "GoldCost";

	protected PayGoldCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
		: base(energyCost, type, rarity, targetType)
	{
	}

	public int GoldCost => DynamicVars[GoldCostVar].IntValue;

	// Canonical cards (card library, compendium) have no owner and throw on Owner access.
	public bool CanAfford => IsCanonical || Owner == null || GoldCmd.CanPay(Owner, GoldCost);

	protected override bool IsPlayable => CanAfford;

	protected override bool ShouldGlowRedInternal => !CanAfford;

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await GoldCmd.Pay(choiceContext, Owner, GoldCost);
		await OnPaidPlay(choiceContext, cardPlay);
	}

	protected abstract Task OnPaidPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}
