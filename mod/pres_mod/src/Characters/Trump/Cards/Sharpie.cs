using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Dark Shackles plus a card draw.</summary>
public sealed class Sharpie : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(CardVars.StrengthLoss, 6m),
		new CardsVar(1)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<StrengthPower>() };

	public Sharpie()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<SharpiePower>(choiceContext, cardPlay.Target, DynamicVars[CardVars.StrengthLoss].BaseValue, Owner.Creature, this);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.StrengthLoss].UpgradeValueBy(3m);
	}
}
