using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Heal. Exhaust.</summary>
public sealed class IceCreamTruck : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new HealVar(6m) };

	public IceCreamTruck()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Heal.UpgradeValueBy(3m);
	}
}
