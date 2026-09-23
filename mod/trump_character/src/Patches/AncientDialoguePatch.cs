using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;

namespace TrumpMod.Patches;

/// <summary>
/// Gives our character their own lines with the Ancients (Neow, Darv, Orobas, Pael, Tanx, Tezcatara, Nonupeipe, Vakuu).
/// Without this they fall back to the "ANY" lines, which works but is bland. The Architect has its own patch
/// (ArchitectDialoguePatch) because its dialogues also set who attacks at the end.
///
/// Each dialogue is a line pattern: A = the Ancient speaks (with its own voice), C = the character speaks (no voice).
/// Dialogue 0 is the first visit; dialogue 1 repeats on every later visit (its text keys carry the "r" suffix).
/// The text lives in localization/eng/ancients.json as ANCIENT.talk.TRUMP.&lt;dialogue&gt;-&lt;line&gt;[r].ancient|char.
/// </summary>
[HarmonyPatch]
public static class AncientDialoguePatch
{
	private static readonly Dictionary<string, string[]> _patterns = new Dictionary<string, string[]>
	{
		["NEOW"] = new[] { "ACA", "A" },
		["DARV"] = new[] { "ACA", "A" },
		["OROBAS"] = new[] { "ACA", "A" },
		["PAEL"] = new[] { "CA", "C" },
		["TANX"] = new[] { "ACA", "A" },
		["TEZCATARA"] = new[] { "ACA", "A" },
		["NONUPEIPE"] = new[] { "CA", "A" },
		["VAKUU"] = new[] { "ACA", "A" }
	};

	public static IEnumerable<MethodBase> TargetMethods()
	{
		return typeof(AncientEventModel).Assembly.GetTypes()
			.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AncientEventModel)) && t != typeof(TheArchitect))
			.Select(t => AccessTools.DeclaredMethod(t, "DefineDialogues"))
			.Where(m => m != null)
			.Cast<MethodBase>();
	}

	public static void Postfix(AncientEventModel __instance, AncientDialogueSet __result)
	{
		if (!_patterns.TryGetValue(__instance.Id.Entry, out string[]? patterns))
		{
			return;
		}
		string voice = AncientVoice(__result);
		foreach (CharacterModel character in ModContent.Characters)
		{
			if (__result.CharacterDialogues.ContainsKey(character.Id.Entry))
			{
				continue;
			}
			__result.CharacterDialogues[character.Id.Entry] = patterns
				.Select((pattern, index) => new AncientDialogue(pattern.Select(speaker => speaker == 'A' ? voice : "").ToArray()) { VisitIndex = index })
				.ToList();
		}
	}

	/// <summary>The Ancient's own voice line, borrowed from the first line of its Ironclad dialogue.</summary>
	private static string AncientVoice(AncientDialogueSet set)
	{
		return set.CharacterDialogues.TryGetValue("IRONCLAD", out IReadOnlyList<AncientDialogue>? dialogues) && dialogues.Count > 0
			? dialogues[0].Lines[0].GetSfxOrFallbackPath()
			: AncientDialogueLine.sfxFallbackPath;
	}
}
