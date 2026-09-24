using HarmonyLib;
using PresMod.Characters.Trump.Relics;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Patches;

/// <summary>
/// The combat's Gold reward shrinks by the share of enemies that escaped (EncounterModel.CalculateGoldProportion).
/// With Rubber Stamp, enemies that left through Deport count as defeated again; the game's own escapes still don't.
/// </summary>
[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
public static class RubberStampPatch
{
	public static void Postfix(CombatRoom __instance)
	{
		ICombatState? combat = __instance.CombatState;
		if (combat == null || !combat.Players.Any(p => p.Relics.OfType<RubberStamp>().Any()))
		{
			return;
		}
		int spawned = __instance.Encounter.SpawnedEnemies.Count;
		int deported = combat.EscapedCreatures.Count(DeportCmd.WasDeported);
		if (spawned <= 0 || deported <= 0)
		{
			return;
		}
		float proportion = Math.Min(1f, __instance.GoldProportion + (float)deported / spawned);
		Traverse.Create(__instance).Property<float>(nameof(CombatRoom.GoldProportion)).Value = proportion;
		foreach (RubberStamp stamp in combat.Players.SelectMany(p => p.Relics.OfType<RubberStamp>()))
		{
			stamp.Flash();
		}
	}
}
