using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace TrumpMod.Patches;

/// <summary>
/// P7: The Architect (the final event after the Act 3 boss) only has dialogue for the five base characters and,
/// unlike other Ancients, no character-agnostic fallback. For anyone else it picks nothing, and the Proceed button
/// then dereferences the null dialogue, so a mod character could never finish a winning run.
/// Add our own dialogues; the lines live in localization/eng/ancients.json (THE_ARCHITECT.talk.TRUMP.*).
/// </summary>
[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
public static class ArchitectDialoguePatch
{
	public static void Postfix(AncientDialogueSet __result)
	{
		foreach (CharacterModel character in ModContent.Characters)
		{
			if (__result.CharacterDialogues.ContainsKey(character.Id.Entry))
			{
				continue;
			}
			// One SFX slot per line; empty = no voice line. Same shape as Ironclad's.
			__result.CharacterDialogues[character.Id.Entry] = new List<AncientDialogue>
			{
				new AncientDialogue("", "", "") { VisitIndex = 0, EndAttackers = ArchitectAttackers.Both },
				new AncientDialogue("", "", "") { VisitIndex = 1, EndAttackers = ArchitectAttackers.Both },
				new AncientDialogue("", "", "") { VisitIndex = 2, EndAttackers = ArchitectAttackers.Both }
			};
		}
	}
}
