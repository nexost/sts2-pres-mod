using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TrumpMod.Models.Relics;

namespace TrumpMod.Patches;

/// <summary>
/// Touch of Orobas (an Ancient's gift) swaps your starter relic for its Ancient version from a hardcoded table,
/// falling back to Circlet for anything it doesn't know. Golden Shovel → Diamond Shovel.
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasPatch
{
	public static void Postfix(RelicModel starterRelic, ref RelicModel __result)
	{
		if (starterRelic is GoldenShovel)
		{
			__result = ModelDb.Relic<DiamondShovel>();
		}
	}
}
