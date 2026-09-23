using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace TrumpMod.Dev;

/// <summary>
/// Test mode only: keep test saves in modded_trumptest/ instead of modded/, so automated runs
/// (which abandon whatever run is in progress) can never touch real saves.
/// Also usable on its own with --trump-savedir &lt;name&gt; (the uninstaller's self-test points cleanup at test saves).
/// Runs before any profile data is read: mods initialize before progress/prefs are loaded.
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), nameof(UserDataPathProvider.GetProfileDir))]
public static class TestSaveDirPatch
{
	private static string SaveDirName => CommandLineHelper.GetValue("trump-savedir") ?? "modded_trumptest";

	public static bool Prepare() => DevHarness.Enabled || CommandLineHelper.HasArg("trump-savedir");

	public static void Postfix(ref string __result)
	{
		__result = __result.Replace("modded/", SaveDirName + "/").Replace("modded\\", SaveDirName + "\\");
	}
}

/// <summary>
/// Test mode only: AutoSlay calls UnlockIfPossible() on every character button, then picks a random unlocked one.
/// Right at that point, mark every non-mod character as locked so the bot always plays ours.
/// (Locking them earlier breaks the screen: it auto-selects Ironclad on open and Ironclad has no unlock text.)
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.UnlockIfPossible))]
public static class TestOnlyModCharactersPatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static void Postfix(NCharacterSelectButton __instance)
	{
		if (AutoSlayer.IsActive)
		{
			Traverse.Create(__instance).Field("_isLocked").SetValue(!ModContent.IsModCharacter(__instance.Character));
		}
	}
}

/// <summary>Test mode only: write the harness report before AutoSlay quits the game.</summary>
[HarmonyPatch(typeof(AutoSlayer), "QuitGame")]
public static class TestAutoSlayQuitPatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static void Prefix(int exitCode)
	{
		DevHarness.OnAutoSlayQuit(exitCode);
	}
}
