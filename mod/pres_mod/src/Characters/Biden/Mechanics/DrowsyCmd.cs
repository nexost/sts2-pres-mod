using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using PresMod.Characters.Biden.Powers;
using PresMod.Framework;
using PresMod.Framework.Nodes;

namespace PresMod.Characters.Biden.Mechanics;

/// <summary>
/// Sleepy Joe's cycle, and the only code that changes it (design.md §3):
///   Doze fills Drowsy; at the nod-off line (10) he nods off and his turn ends;
///   at the start of his next turn he wakes up as Dark Brandon: Laser Eyes hit ALL enemies for his Drowsy, which resets;
///   Dark Brandon lasts that turn (DarkBrandonPower), then he's Sleepy Joe again.
/// Wake Up cards skip the nap: Dark Brandon right away, with whatever Drowsy he has.
/// </summary>
public static class DrowsyCmd
{
	public const int BaseNodOffLine = 10;

	/// <summary>Raised after someone nods off. Used by the tests and the balance stats.</summary>
	public static event Action<Creature>? NoddedOff;

	/// <summary>Raised after Laser Eyes fire: (owner, damage per hit, hits). Used by the tests and the balance stats.</summary>
	public static event Action<Creature, int, int>? LaserFired;

	public static int GetDrowsy(Creature creature) => creature.GetPowerAmount<DrowsyPower>();

	public static bool IsDarkBrandon(Creature creature) => creature.HasPower<DarkBrandonPower>();

	public static bool HasNoddedOff(Creature creature) => creature.HasPower<NoddedOffPower>();

	/// <summary>Drowsy at which he nods off: 10, raised by Heavy Sleeper.</summary>
	public static int GetNodOffLine(Creature creature)
	{
		decimal line = BaseNodOffLine;
		foreach (INodOffLineModifier modifier in BidenHooks.ListenersOf<INodOffLineModifier>(creature))
		{
			line = modifier.ModifyNodOffLine(creature, line);
		}
		return Math.Max(1, (int)line);
	}

	/// <summary>Whether a Doze of this much now would nod him off (sleepy cards glow red as a warning).</summary>
	public static bool WouldNodOff(Creature owner, decimal amount)
	{
		if (owner.CombatState == null || IsDarkBrandon(owner) || HasNoddedOff(owner))
		{
			return false;
		}
		foreach (IDozeModifier modifier in BidenHooks.ListenersOf<IDozeModifier>(owner))
		{
			amount = modifier.ModifyDoze(owner, amount);
		}
		return amount > 0m && GetDrowsy(owner) + amount >= GetNodOffLine(owner);
	}

	/// <summary>
	/// Gain Drowsy. Does nothing while he's Dark Brandon (wide awake) or once he has nodded off (asleep until next turn).
	/// Reaching the line nods him off.
	/// </summary>
	public static async Task Doze(PlayerChoiceContext choiceContext, Creature owner, decimal amount, CardModel? cardSource = null)
	{
		if (owner.CombatState == null || owner.IsDead || IsDarkBrandon(owner) || HasNoddedOff(owner))
		{
			return;
		}
		foreach (IDozeModifier modifier in BidenHooks.ListenersOf<IDozeModifier>(owner))
		{
			amount = modifier.ModifyDoze(owner, amount);
		}
		if (amount <= 0m)
		{
			return;
		}
		await PowerCmd.Apply<DrowsyPower>(choiceContext, owner, amount, owner, cardSource);
		if (GetDrowsy(owner) >= GetNodOffLine(owner))
		{
			await NodOff(choiceContext, owner);
		}
		else
		{
			BidenVfx.Dozed(owner);
		}
	}

	/// <summary>
	/// Nod off: his turn ends once the current card finishes, and he wakes up as Dark Brandon at the start of the next one
	/// (NoddedOffPower). At the end of a turn it just marks the nap.
	/// </summary>
	public static async Task NodOff(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner.CombatState == null || owner.IsDead || HasNoddedOff(owner))
		{
			return;
		}
		await PowerCmd.Apply<NoddedOffPower>(choiceContext, owner, 1m, owner, null);
		BidenVfx.NoddedOff(owner);
		NoddedOff?.Invoke(owner);
		foreach (IAfterNodOff listener in BidenHooks.ListenersOf<IAfterNodOff>(owner))
		{
			await listener.AfterNodOff(choiceContext, owner);
		}
		// Same call as the End Turn button and Golf Weekend. In co-op it locks this player's turn; the others play on.
		if (owner.Player is Player player && owner.CombatState.CurrentSide == CombatSide.Player
			&& player.PlayerCombatState?.Phase == PlayerTurnPhase.Play)
		{
			PlayerCmd.EndTurn(player, canBackOut: false);
		}
	}

	/// <summary>Wake up as Dark Brandon now: Laser Eyes for his Drowsy, then Drowsy resets. Nothing happens if he's already awake.</summary>
	public static async Task WakeUp(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner.CombatState == null || owner.IsDead)
		{
			return;
		}
		await PowerCmd.Remove<NoddedOffPower>(owner);
		if (IsDarkBrandon(owner))
		{
			return;
		}
		int drowsy = GetDrowsy(owner);
		await PowerCmd.Apply<DarkBrandonPower>(choiceContext, owner, 1m, owner, null);
		await BidenVfx.WokeUp(owner);
		await LaserEyes(choiceContext, owner, drowsy);
		await PowerCmd.Remove<DrowsyPower>(owner);
		foreach (IAfterWakeUp listener in BidenHooks.ListenersOf<IAfterWakeUp>(owner))
		{
			await listener.AfterWakeUp(choiceContext, owner);
		}
	}

	/// <summary>The end of a Dark Brandon turn: back to Sleepy Joe.</summary>
	public static async Task FallBackAsleep(Creature owner)
	{
		await PowerCmd.Remove<DarkBrandonPower>(owner);
		BidenVfx.FellAsleep(owner);
	}

	/// <summary>Laser Eyes damage per hit and number of hits for this much Drowsy, after modifiers.</summary>
	public static (int Damage, int Hits) LaserFor(Creature owner, int drowsy)
	{
		decimal damage = drowsy;
		int hits = 1;
		foreach (ILaserEyesModifier modifier in BidenHooks.ListenersOf<ILaserEyesModifier>(owner))
		{
			damage = modifier.ModifyLaserDamage(owner, damage);
			hits = modifier.ModifyLaserHits(owner, hits);
		}
		return ((int)Math.Floor(damage), Math.Max(0, hits));
	}

	private static async Task LaserEyes(PlayerChoiceContext choiceContext, Creature owner, int drowsy)
	{
		(int damage, int hits) = LaserFor(owner, drowsy);
		if (damage <= 0 || hits <= 0)
		{
			return;
		}
		for (int i = 0; i < hits; i++)
		{
			List<Creature> enemies = owner.CombatState!.HittableEnemies.ToList();
			if (enemies.Count == 0)
			{
				break;
			}
			await BidenVfx.LaserEyes(owner, enemies, i);
			await CreatureCmd.Damage(choiceContext, enemies, damage, ValueProp.Unpowered, owner, null);
		}
		LaserFired?.Invoke(owner, damage, hits);
	}
}
