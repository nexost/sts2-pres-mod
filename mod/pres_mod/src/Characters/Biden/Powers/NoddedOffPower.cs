using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>
/// He nodded off (DrowsyCmd.NodOff): no more Drowsy until he wakes, and at the start of his next turn he wakes up as
/// Dark Brandon. Same turn-start timing as the Golden Shovel and Bound Phylactery: right after the energy reset.
/// </summary>
public sealed class NoddedOffPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon, BidenHoverTips.LaserEyes };

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner.Player)
		{
			await DrowsyCmd.WakeUp(new ThrowingPlayerChoiceContext(), Owner);
		}
	}
}
