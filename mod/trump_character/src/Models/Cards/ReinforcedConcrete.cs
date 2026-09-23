using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Copies stack: the Wall's defense has no ceiling.</summary>
public sealed class ReinforcedConcrete : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<ReinforcedConcretePower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public ReinforcedConcrete()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<ReinforcedConcretePower>(choiceContext, Owner.Creature, DynamicVars[nameof(ReinforcedConcretePower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(ReinforcedConcretePower)].UpgradeValueBy(1m);
	}
}
