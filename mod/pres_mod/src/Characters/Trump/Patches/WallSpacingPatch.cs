using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Patches;

/// <summary>
/// Co-op: the game lines the players up from the center outward, 70 px apart, and a Wall stands in front of its owner
/// (NWallDisplay, 150 px wide). The front player's Wall faces the enemies, but anyone else's would stand on the player
/// in front of them. Make room, the way the game itself shifts the local Necrobinder to fit Osty.
/// A player shows a Wall when they're The Donald or have Wall height (Coalition Wall gives it to anyone: then
/// NTrumpCombatUi calls Relayout).
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class WallSpacingPatch
{
	/// <summary>Room a Wall needs beyond the game's 70 px gap: it starts 26 px past its owner, is 150 px wide, plus a margin.</summary>
	private const float ExtraGap = 26f + 150f + 20f - 70f;

	public static bool ShowsWall(Creature creature) => creature.Player?.Character is Trump || WallCmd.GetHeight(creature) > 0;

	public static void Postfix(List<NCreature> creatureNodes)
	{
		List<NCreature> players = creatureNodes.Where(n => n.Entity.IsPlayer).ToList();
		// Each row runs from the front (nearest the enemies, largest X) to the back.
		foreach (IGrouping<int, NCreature> row in players.GroupBy(n => Mathf.RoundToInt(n.Position.Y)))
		{
			float shift = 0f;
			bool front = true;
			foreach (NCreature player in row.OrderByDescending(n => n.Position.X))
			{
				if (!front && ShowsWall(player.Entity))
				{
					shift += ExtraGap;
				}
				front = false;
				if (shift <= 0f)
				{
					continue;
				}
				player.Position -= new Vector2(shift, 0f);
				foreach (NCreature pet in creatureNodes.Where(n => n.Entity.PetOwner != null && n.Entity.PetOwner == player.Entity.Player))
				{
					pet.Position -= new Vector2(shift, 0f);
				}
			}
		}
	}

	/// <summary>Lays the party out again (a player just got a Wall mid-fight), gliding everyone to their new places.</summary>
	public static void Relayout(NCombatRoom room)
	{
		ICombatRoomVisuals? visuals = Traverse.Create(room).Field("_visuals").GetValue<ICombatRoomVisuals>();
		if (visuals == null)
		{
			return;
		}
		List<NCreature> allies = room.CreatureNodes.Where(n => n.Entity.IsPlayer || n.Entity.PetOwner != null).ToList();
		Dictionary<NCreature, Vector2> before = allies.ToDictionary(n => n, n => n.Position);
		NCombatRoom.PositionPlayersAndPets(allies, visuals.Encounter.GetCameraScaling(), visuals.Encounter.FullyCenterPlayers);
		foreach (NCreature node in allies)
		{
			Vector2 target = node.Position;
			if (target == before[node])
			{
				continue;
			}
			node.Position = before[node];
			node.CreateTween().TweenProperty(node, "position", target, 0.5).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		}
	}
}
