using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Stacks: two copies take the line from 25% to 45%.</summary>
public sealed class BorderControl : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<BorderControlPower>(10m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	public BorderControl()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<BorderControlPower>(choiceContext, Owner.Creature, DynamicVars[nameof(BorderControlPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BorderControlPower)].UpgradeValueBy(5m);
	}
}
