using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>The nod-off line is higher.</summary>
public sealed class HeavySleeper : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<HeavySleeperPower>(5m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Drowsy, BidenHoverTips.NodOff };

	public HeavySleeper()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<HeavySleeperPower>(choiceContext, Owner.Creature, DynamicVars[nameof(HeavySleeperPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(HeavySleeperPower)].UpgradeValueBy(3m);
	}
}
