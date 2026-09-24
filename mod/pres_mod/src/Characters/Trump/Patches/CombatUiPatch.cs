using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using PresMod.Framework;
using PresMod.Framework.Nodes;
using PresMod.Characters.Trump.Nodes;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Patches;

/// <summary>Adds our combat overlay (Wall display, Deport markers, popups) to every combat room.</summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class CombatUiPatch
{
	public static void Postfix(NCombatRoom __instance)
	{
		__instance.AddChild(new NTrumpCombatUi { Name = "TrumpCombatUi" });
	}
}
