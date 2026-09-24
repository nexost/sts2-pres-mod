using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>
/// Sleepy Joe's sleepiness (DrowsyCmd). Filled by Doze; at the nod-off line (10, or higher with Heavy Sleeper) he nods off.
/// Waking up fires Laser Eyes for this amount and removes it.
/// </summary>
public sealed class DrowsyPower : PowerModel
{
	private const string _lineVar = "Line";

	/// <summary>A buff on purpose: Artifact and debuff cleanses must not eat his sleepiness.</summary>
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(_lineVar, DrowsyCmd.BaseNodOffLine) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff, BidenHoverTips.DarkBrandon, BidenHoverTips.LaserEyes };

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		RefreshLine();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		RefreshLine();
		return Task.CompletedTask;
	}

	private void RefreshLine()
	{
		DynamicVars[_lineVar].BaseValue = DrowsyCmd.GetNodOffLine(Owner);
	}
}
