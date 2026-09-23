using HarmonyLib;

namespace TrumpMod.Patches;

/// <summary>A card or potion that refuses some targets (You're Fired! and the Deportation Draught skip immune bosses).</summary>
public interface ITargetFilter
{
	bool CanTarget(Creature target);
}

/// <summary>
/// CardModel.IsValidTarget and PotionModel.IsValidTarget aren't virtual. Both drive the targeting arrow and the play check,
/// so filtering here keeps a Pay-99-Gold card from being wasted on a boss.
/// </summary>
[HarmonyPatch]
public static class TargetFilterPatch
{
	[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
	[HarmonyPostfix]
	public static void Card(CardModel __instance, Creature? target, ref bool __result)
	{
		if (__result && target != null && __instance is ITargetFilter filter && !filter.CanTarget(target))
		{
			__result = false;
		}
	}

	[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.IsValidTarget))]
	[HarmonyPostfix]
	public static void Potion(PotionModel __instance, Creature? target, ref bool __result)
	{
		if (__result && target != null && __instance is ITargetFilter filter && !filter.CanTarget(target))
		{
			__result = false;
		}
	}
}
