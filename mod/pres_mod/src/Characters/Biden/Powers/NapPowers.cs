using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>Power Nap: whenever you nod off, gain Amount Block.</summary>
public sealed class PowerNapPower : PowerModel, IAfterNodOff
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff };

	public async Task AfterNodOff(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner)
		{
			return;
		}
		Flash();
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
	}
}

/// <summary>Snoring: whenever you nod off, deal Amount damage to ALL enemies.</summary>
public sealed class SnoringPower : PowerModel, IAfterNodOff
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff };

	public async Task AfterNodOff(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner || CombatState.HittableEnemies.Count == 0)
		{
			return;
		}
		Flash();
		await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner, null);
	}
}

/// <summary>Well Rested: whenever you nod off, gain Amount Dexterity.</summary>
public sealed class WellRestedPower : PowerModel, IAfterNodOff
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.NodOff, HoverTipFactory.FromPower<DexterityPower>() };

	public async Task AfterNodOff(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner)
		{
			return;
		}
		Flash();
		await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, Amount, Owner, null);
	}
}

/// <summary>Heavy Sleeper: the nod-off line is Amount higher (bigger naps, bigger Laser Eyes). Copies stack.</summary>
public sealed class HeavySleeperPower : PowerModel, INodOffLineModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Drowsy, BidenHoverTips.NodOff };

	public decimal ModifyNodOffLine(Creature owner, decimal line) => owner == Owner ? line + Amount : line;

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		Owner.GetPower<DrowsyPower>()?.RefreshLine();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power == this)
		{
			Owner.GetPower<DrowsyPower>()?.RefreshLine();
		}
		return Task.CompletedTask;
	}
}

/// <summary>Deep Sleep: the next Laser Eyes deal double damage, then it's gone.</summary>
public sealed class DeepSleepPower : PowerModel, ILaserEyesModifier, IAfterWakeUp
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.LaserEyes };

	public decimal ModifyLaserDamage(Creature owner, decimal damage) => owner == Owner ? damage * 2m : damage;

	public async Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner == Owner)
		{
			await PowerCmd.Remove(this);
		}
	}
}
