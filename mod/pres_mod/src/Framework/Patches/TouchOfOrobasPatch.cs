using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace PresMod.Framework.Patches;

/// <summary>
/// A character's starter relic names its Ancient version (what Touch of Orobas turns it into), like Burning Blood →
/// Black Blood. Trump: Golden Shovel → Diamond Shovel.
/// </summary>
public interface IUpgradableStarterRelic
{
	RelicModel AncientUpgrade { get; }
}

/// <summary>
/// Touch of Orobas (an Ancient's gift) swaps your starter relic for its Ancient version from a hardcoded table,
/// falling back to Circlet for anything it doesn't know. Our starters say their own upgrade (IUpgradableStarterRelic).
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasPatch
{
	public static void Postfix(RelicModel starterRelic, ref RelicModel __result)
	{
		if (starterRelic is IUpgradableStarterRelic starter)
		{
			__result = starter.AncientUpgrade;
		}
	}
}
