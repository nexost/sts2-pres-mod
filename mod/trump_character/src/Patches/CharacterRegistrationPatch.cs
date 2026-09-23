using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace TrumpMod.Patches;

/// <summary>
/// P1: ModelDb.AllCharacters is a hardcoded list of the five base characters. Append ours.
/// The card/relic/potion pool lists are all derived from this, so our pools follow automatically.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
public static class CharacterRegistrationPatch
{
	public static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		IReadOnlyList<CharacterModel> ours = ModContent.Characters;
		if (ours.Count == 0)
		{
			return;
		}
		List<CharacterModel> list = __result.ToList();
		foreach (CharacterModel character in ours)
		{
			if (!list.Contains(character))
			{
				list.Add(character);
			}
		}
		__result = list;
	}
}
