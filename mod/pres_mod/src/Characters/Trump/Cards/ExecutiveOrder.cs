using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

public sealed class ExecutiveOrder : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<ExecutiveOrderPower>(1m) };

	public ExecutiveOrder()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<ExecutiveOrderPower>(choiceContext, Owner.Creature, DynamicVars[nameof(ExecutiveOrderPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
