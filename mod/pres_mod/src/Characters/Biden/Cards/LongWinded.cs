using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Damage a random enemy whenever you play a Tangent.</summary>
public sealed class LongWinded : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<LongWindedPower>(4m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public LongWinded()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<LongWindedPower>(choiceContext, Owner.Creature, DynamicVars[nameof(LongWindedPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(LongWindedPower)].UpgradeValueBy(1m);
	}
}
