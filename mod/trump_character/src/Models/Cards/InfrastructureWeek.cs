using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Demon Form for the Wall.</summary>
public sealed class InfrastructureWeek : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<InfrastructureWeekPower>(7m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public InfrastructureWeek()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<InfrastructureWeekPower>(choiceContext, Owner.Creature, DynamicVars[nameof(InfrastructureWeekPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(InfrastructureWeekPower)].UpgradeValueBy(3m);
	}
}
