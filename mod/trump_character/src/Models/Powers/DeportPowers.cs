namespace TrumpMod.Models.Powers;

/// <summary>Border Control: your Deport line is Amount% higher. Copies stack.</summary>
public sealed class BorderControlPower : PowerModel, IDeportLineModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	public decimal ModifyDeportLine(Player player, decimal line)
	{
		return player.Creature == Owner ? line + Amount / 100m : line;
	}
}

/// <summary>Border Wall: your Deport line is Amount% higher for each Section. Bridges the Wall and Deport.</summary>
public sealed class BorderWallPower : PowerModel, IDeportLineModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport, TrumpHoverTips.Section };

	public decimal ModifyDeportLine(Player player, decimal line)
	{
		return player.Creature == Owner ? line + Amount / 100m * WallCmd.GetSections(Owner) : line;
	}
}

/// <summary>Law and Order: at the end of your turn, Deport ALL enemies (each still has to be under the line).</summary>
public sealed class LawAndOrderPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	// Runs after the Early end-of-turn damage (Big Beautiful Wall, Guard Towers), so those hits can take enemies under the line first.
	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		Player? player = Owner.Player;
		if (participants.Contains(Owner) && player != null && CombatState.HittableEnemies.Any(e => DeportCmd.CanDeport(e, player)))
		{
			Flash();
			await DeportCmd.TryDeportAll(choiceContext, player);
		}
	}
}

/// <summary>So Much Winning: whenever an enemy dies or is Deported, gain Amount Energy and draw Amount cards.</summary>
public sealed class SoMuchWinningPower : PowerModel, IAfterDeport
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport };

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && creature.Side != Owner.Side)
		{
			await Pay(choiceContext);
		}
	}

	public async Task AfterDeport(PlayerChoiceContext choiceContext, Player deporter, Creature deported)
	{
		await Pay(choiceContext);
	}

	private async Task Pay(PlayerChoiceContext choiceContext)
	{
		Player? player = Owner.Player;
		if (player == null || Owner.IsDead || CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		Flash();
		await PlayerCmd.GainEnergy(Amount, player);
		await CardPileCmd.Draw(choiceContext, Amount, player);
	}
}
