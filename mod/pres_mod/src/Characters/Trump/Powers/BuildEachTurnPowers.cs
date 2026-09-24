using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Powers;

/// <summary>Rebar: at the start of your turn, Build Amount.</summary>
public sealed class RebarPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner))
		{
			Flash();
			await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner, Amount);
		}
	}
}

/// <summary>Infrastructure Week: the Wall deck's Demon Form. At the start of your turn, Build Amount.</summary>
public sealed class InfrastructureWeekPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(Owner))
		{
			Flash();
			await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner, Amount);
		}
	}
}
