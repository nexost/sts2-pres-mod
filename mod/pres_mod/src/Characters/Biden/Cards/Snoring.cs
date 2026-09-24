using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Whenever you nod off, damage ALL enemies.</summary>
public sealed class Snoring : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<SnoringPower>(6m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff };

	public Snoring()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<SnoringPower>(choiceContext, Owner.Creature, DynamicVars[nameof(SnoringPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(SnoringPower)].UpgradeValueBy(3m);
	}
}
