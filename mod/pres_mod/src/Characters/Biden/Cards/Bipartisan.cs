using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Block when you alternate Attacks and Skills.</summary>
public sealed class Bipartisan : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<BipartisanPower>(2m) };

	public Bipartisan()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<BipartisanPower>(choiceContext, Owner.Creature, DynamicVars[nameof(BipartisanPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BipartisanPower)].UpgradeValueBy(1m);
	}
}
