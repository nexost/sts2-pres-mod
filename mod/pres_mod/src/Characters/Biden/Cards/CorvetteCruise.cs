using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Draw, and the next Attack costs 0 (the game's Free Attack).</summary>
public sealed class CorvetteCruise : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<FreeAttackPower>() };

	public CorvetteCruise()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		_ = BidenVfx.Vehicle(Owner.Creature, "corvette", size: 0.5f, seconds: 1.0f, behind: true);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		await PowerCmd.Apply<FreeAttackPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
