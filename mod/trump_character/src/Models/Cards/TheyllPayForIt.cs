using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Section Block plus this: the Wall pays for itself.</summary>
public sealed class TheyllPayForIt : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<TheyllPayForItPower>(4m) };

	public TheyllPayForIt()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<TheyllPayForItPower>(choiceContext, Owner.Creature, DynamicVars[nameof(TheyllPayForItPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(TheyllPayForItPower)].UpgradeValueBy(2m);
	}
}
