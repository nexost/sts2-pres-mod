using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Whenever you nod off, gain Block.</summary>
public sealed class PowerNap : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<PowerNapPower>(8m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff };

	public PowerNap()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<PowerNapPower>(choiceContext, Owner.Creature, DynamicVars[nameof(PowerNapPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(PowerNapPower)].UpgradeValueBy(3m);
	}
}
