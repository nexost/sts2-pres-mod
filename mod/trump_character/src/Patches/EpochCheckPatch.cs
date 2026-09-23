using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace TrumpMod.Patches;

/// <summary>
/// P2: the "15 elites / 15 bosses defeated" timeline checks throw ArgumentOutOfRangeException for any character
/// they don't know, which would crash the game after an elite or boss fight. Skip them for our characters.
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
public static class FifteenElitesEpochPatch
{
	public static bool Prefix(Player localPlayer)
	{
		return !ModContent.IsModCharacter(localPlayer.Character);
	}
}

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
public static class FifteenBossesEpochPatch
{
	public static bool Prefix(Player localPlayer)
	{
		return !ModContent.IsModCharacter(localPlayer.Character);
	}
}

/// <summary>
/// P6: beating an act boss unlocks "&lt;CHARACTER&gt;2_EPOCH" / 3 / 4, looked up by a string built from the character Id.
/// EpochModel.Get throws for TRUMP2_EPOCH, which aborted the post-boss rewards. Skip until we add our own timeline.
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
public static class ActBossEpochPatch
{
	public static bool Prefix(Player localPlayer)
	{
		return !ModContent.IsModCharacter(localPlayer.Character);
	}
}
