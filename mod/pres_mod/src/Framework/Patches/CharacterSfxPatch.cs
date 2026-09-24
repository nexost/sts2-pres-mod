using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using PresMod.Framework;

namespace PresMod.Framework.Patches;

/// <summary>
/// P4: AttackSfx / CastSfx / DeathSfx are built from the character Id and can't be overridden.
/// Point them at another character's FMOD events until we ship our own bank.
/// </summary>
[HarmonyPatch]
public static class CharacterSfxPatch
{
	[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
	[HarmonyPostfix]
	public static void AttackSfx(CharacterModel __instance, ref string __result) => Redirect(__instance, ref __result);

	[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
	[HarmonyPostfix]
	public static void CastSfx(CharacterModel __instance, ref string __result) => Redirect(__instance, ref __result);

	[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
	[HarmonyPostfix]
	public static void DeathSfx(CharacterModel __instance, ref string __result) => Redirect(__instance, ref __result);

	private static void Redirect(CharacterModel character, ref string sfx)
	{
		if (character is IModCharacter modCharacter)
		{
			sfx = sfx.Replace(character.Id.Entry.ToLowerInvariant(), modCharacter.SfxProxyEntry);
		}
	}
}
