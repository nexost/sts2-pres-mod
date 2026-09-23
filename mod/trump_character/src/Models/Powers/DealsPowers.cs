namespace TrumpMod.Models.Powers;

/// <summary>Protectionism: at the start of your turn, apply Amount Tariff to ALL enemies.</summary>
public sealed class ProtectionismPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tariff };

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(Owner) || combatState.HittableEnemies.Count == 0)
		{
			return;
		}
		Flash();
		foreach (Creature enemy in combatState.HittableEnemies.ToList())
		{
			await PowerCmd.Apply<TariffPower>(new ThrowingPlayerChoiceContext(), enemy, Amount, Owner, null);
		}
	}
}

/// <summary>Merch Stand: at the end of combat, gain Amount Gold. Paid while the power still exists (before the fight's powers are cleared).</summary>
public sealed class MerchStandPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner.Player != null && Owner.IsAlive)
		{
			Flash();
			await PlayerCmd.GainGold(Amount, Owner.Player);
		}
	}
}

/// <summary>They'll Pay For It: whenever an enemy attacks you and none of it reaches your HP, gain Amount Gold.</summary>
public sealed class TheyllPayForItPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (command.Attacker == null || command.Attacker.Side == Owner.Side || Owner.Player == null || Owner.IsDead)
		{
			return;
		}
		List<DamageResult> onMe = command.Results.SelectMany(r => r).Where(r => r.Receiver == Owner).ToList();
		if (onMe.Count > 0 && onMe.All(r => r.UnblockedDamage == 0))
		{
			Flash();
			await PlayerCmd.GainGold(Amount, Owner.Player);
		}
	}
}

/// <summary>The Art of the Deal: whenever you Pay Gold, draw Amount cards.</summary>
public sealed class ArtOfTheDealPower : PowerModel, IAfterPayGold
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.PayGold };

	public async Task AfterPayGold(PlayerChoiceContext choiceContext, Player player, int amount)
	{
		if (player.Creature == Owner)
		{
			Flash();
			await CardPileCmd.Draw(choiceContext, Amount, player);
		}
	}
}

/// <summary>
/// Gold Tower: at the end of your turn, gain Amount Block for every 30 Gold you have. The card sets Amount 2 (1 per 15 Gold)
/// or 3 upgraded (1 per 10), so copies stack by simply adding up.
/// </summary>
public sealed class GoldTowerPower : PowerModel
{
	public const int GoldPerStep = 30;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner) || Owner.Player == null)
		{
			return;
		}
		int block = Owner.Player.Gold * Amount / GoldPerStep;
		if (block > 0)
		{
			Flash();
			await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
		}
	}
}
