namespace TrumpMod.Models.Powers;

/// <summary>Barbed Wire: Flame Barrier whose damage also counts your Sections at the moment you're hit. Gone after the enemy turn.</summary>
public sealed class BarbedWirePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult _, ValueProp props, Creature? dealer, CardModel? __)
	{
		if (target == Owner && dealer != null && props.IsPoweredAttack())
		{
			await CreatureCmd.Damage(choiceContext, dealer, Amount + WallCmd.GetSections(Owner), ValueProp.Unpowered, Owner, null);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (Owner.Side != side)
		{
			await PowerCmd.Remove(this);
		}
	}
}
