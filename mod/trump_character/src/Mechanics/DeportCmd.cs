using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace TrumpMod.Mechanics;

/// <summary>
/// Deport (design v2): an enemy at or below the player's Deport line (25% of its max HP, raisable) leaves combat
/// through the game's own escape path. Escaped enemies drop no Gold. Primary enemies in boss rooms are immune;
/// minions (secondary enemies) are not.
/// </summary>
public static class DeportCmd
{
	public const decimal BaseLine = 0.25m;

	/// <summary>Upper bound for the line, whatever the modifiers (watch-list item in the design doc).</summary>
	public const decimal MaxLine = 1.0m;

	/// <summary>Raised after an enemy is Deported. Used for the "DEPORTED!" popup.</summary>
	public static event Action<Creature>? Deported;

	/// <summary>Enemies that left through Deport, as opposed to the game's own escapes (Rubber Stamp pays for these).</summary>
	private static readonly ConditionalWeakTable<Creature, object> _deported = new ConditionalWeakTable<Creature, object>();

	private static readonly object _marker = new object();

	public static bool WasDeported(Creature creature) => _deported.TryGetValue(creature, out _);

	public static decimal GetLine(Player player)
	{
		decimal line = BaseLine;
		foreach (IDeportLineModifier modifier in ModHooks.ListenersOf<IDeportLineModifier>(player.Creature))
		{
			line = modifier.ModifyDeportLine(player, line);
		}
		return Math.Clamp(line, 0m, MaxLine);
	}

	public static bool IsImmune(Creature target)
	{
		return target.CombatState?.Encounter?.RoomType == RoomType.Boss && target.IsPrimaryEnemy;
	}

	/// <summary>HP at or below which this enemy can be Deported by this player (fractional).</summary>
	public static decimal LineHp(Creature target, Player player, decimal multiplier = 1m)
	{
		return target.MaxHp * Math.Min(GetLine(player) * multiplier, MaxLine);
	}

	public static bool CanDeport(Creature target, Player player, decimal multiplier = 1m)
	{
		return target.IsAlive && target.Side == CombatSide.Enemy && !IsImmune(target) && target.CurrentHp <= LineHp(target, player, multiplier);
	}

	/// <summary>Deports the target if it's at or below the line. Returns true if it left combat.</summary>
	public static async Task<bool> TryDeport(PlayerChoiceContext choiceContext, Creature target, Player deporter, decimal multiplier = 1m)
	{
		if (!CanDeport(target, deporter, multiplier))
		{
			return false;
		}
		return await ForceDeport(choiceContext, target, deporter);
	}

	/// <summary>Deport ignoring the line (You're Fired!, Deportation Draught). Bosses stay immune.</summary>
	public static async Task<bool> ForceDeport(PlayerChoiceContext choiceContext, Creature target, Player deporter)
	{
		if (!target.IsAlive || target.Side != CombatSide.Enemy || IsImmune(target) || target.CombatState == null)
		{
			return false;
		}
		Deported?.Invoke(target);
		_deported.AddOrUpdate(target, _marker);
		await CreatureCmd.Escape(target);
		foreach (IAfterDeport listener in ModHooks.ListenersOf<IAfterDeport>(deporter.Creature))
		{
			await listener.AfterDeport(choiceContext, deporter, target);
		}
		await CombatManager.Instance.CheckWinCondition();
		return true;
	}

	public static async Task<int> TryDeportAll(PlayerChoiceContext choiceContext, Player deporter, decimal multiplier = 1m)
	{
		ICombatState? combat = deporter.Creature.CombatState;
		if (combat == null)
		{
			return 0;
		}
		int count = 0;
		foreach (Creature enemy in combat.Enemies.Where(e => e != null && e.IsAlive).ToList())
		{
			if (await TryDeport(choiceContext, enemy, deporter, multiplier))
			{
				count++;
			}
		}
		return count;
	}
}
