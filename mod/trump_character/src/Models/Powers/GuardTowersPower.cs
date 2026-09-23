namespace TrumpMod.Models.Powers;

/// <summary>Guard Towers: at the end of your turn, one shot of Amount damage at a random enemy per Section. Copies stack.</summary>
public sealed class GuardTowersPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner) || Owner.IsDead || Owner.Player == null)
		{
			return;
		}
		int sections = WallCmd.GetSections(Owner);
		if (sections > 0 && CombatState.HittableEnemies.Count > 0)
		{
			Flash();
		}
		for (int i = 0; i < sections; i++)
		{
			Creature? target = Owner.Player.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
			if (target == null)
			{
				break;
			}
			await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner, null);
		}
	}
}
