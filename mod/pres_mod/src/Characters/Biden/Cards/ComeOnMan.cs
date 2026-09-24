using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Vulnerable and a card.</summary>
public sealed class ComeOnMan : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VulnerablePower>(2m), new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<VulnerablePower>() };

	public ComeOnMan()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars[nameof(VulnerablePower)].BaseValue, Owner.Creature, this);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);
	}
}
