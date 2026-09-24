using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Powers;

/// <summary>Wide Awake: whenever you wake up, gain Amount Block and draw 1 card.</summary>
public sealed class WideAwakePower : PowerModel, IAfterWakeUp
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp };

	public async Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner || Owner.Player == null)
		{
			return;
		}
		Flash();
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
		await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
	}
}

/// <summary>Laser Focus: Laser Eyes deal Amount more damage. Copies stack.</summary>
public sealed class LaserFocusPower : PowerModel, ILaserEyesModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.LaserEyes };

	public decimal ModifyLaserDamage(Creature owner, decimal damage) => owner == Owner ? damage + Amount : damage;
}

/// <summary>Early Riser: at the start of your turn, if you have 5 or more Drowsy, wake up.</summary>
public sealed class EarlyRiserPower : PowerModel
{
	public const int Threshold = 5;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Drowsy, BidenHoverTips.WakeUp };

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner.Player && DrowsyCmd.GetDrowsy(Owner) >= Threshold && !DrowsyCmd.IsDarkBrandon(Owner))
		{
			Flash();
			await DrowsyCmd.WakeUp(new ThrowingPlayerChoiceContext(), Owner);
		}
	}
}

/// <summary>Dark Brandon Rises: at the start of your turn, wake up. Every turn is a Dark Brandon turn; Drowsy never builds.</summary>
public sealed class DarkBrandonRisesPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, BidenHoverTips.DarkBrandon };

	public override async Task AfterEnergyResetLate(Player player)
	{
		if (player == Owner.Player && !DrowsyCmd.IsDarkBrandon(Owner))
		{
			Flash();
			await DrowsyCmd.WakeUp(new ThrowingPlayerChoiceContext(), Owner);
		}
	}
}

/// <summary>No Malarkey: whenever you wake up, gain Amount Strength.</summary>
public sealed class NoMalarkeyPower : PowerModel, IAfterWakeUp
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.WakeUp, HoverTipFactory.FromPower<StrengthPower>() };

	public async Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner)
		{
			return;
		}
		Flash();
		await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
	}
}

/// <summary>Double Vision: Laser Eyes hit Amount more times (twice with one copy).</summary>
public sealed class DoubleVisionPower : PowerModel, ILaserEyesModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.LaserEyes };

	public int ModifyLaserHits(Creature owner, int hits) => owner == Owner ? hits + Amount : hits;
}
