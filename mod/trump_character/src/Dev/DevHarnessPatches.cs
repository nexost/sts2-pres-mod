using System;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
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

/// <summary>
/// Test mode only: the shared assets finish preloading in the background after the main menu shows. A run started
/// before that stalls on its own preload, past AutoSlay's 10 s first-room timeout, so the harness waits for this flag.
/// </summary>
[HarmonyPatch(typeof(PreloadManager), nameof(PreloadManager.LoadCommonAndMainMenuAssets))]
public static class CommonPreloadDonePatch
{
	public static bool Done { get; private set; }

	public static bool Prepare() => DevHarness.Enabled;

	public static void Postfix(ref Task __result)
	{
		__result = MarkDone(__result);
	}

	private static async Task MarkDone(Task preload)
	{
		await preload;
		Done = true;
	}
}

/// <summary>
/// Test mode only: AutoSlay's time limits (10 s per wait, 30 s stuck-watchdog) assume a fast machine. On 2026-09-23 the
/// PC got into a state where the game ran at ~8 fps (30 s shared preload instead of 2 s, with or without this mod; a
/// reboot fixed it) and the bot gave up on healthy runs. Stretch both limits so a slow machine doesn't fail the regression.
/// </summary>
public static class SlowMachineAllowance
{
	public const int Factor = 3;
}

[HarmonyPatch(typeof(WaitHelper), nameof(WaitHelper.Until))]
public static class AutoSlayWaitAllowancePatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static void Prefix(ref TimeSpan? timeout)
	{
		timeout = (timeout ?? AutoSlayConfig.nodeWaitTimeout) * SlowMachineAllowance.Factor;
	}
}

[HarmonyPatch(typeof(Watchdog), nameof(Watchdog.Check))]
public static class AutoSlayWatchdogAllowancePatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix(Watchdog __instance)
	{
		TimeSpan idle = DateTime.UtcNow - Traverse.Create(__instance).Field<DateTime>("_lastProgressTime").Value;
		// Within the stretched limit, skip the original (it would throw at 30 s); past it, let it log and throw.
		return idle <= AutoSlayConfig.watchdogTimeout || idle > AutoSlayConfig.watchdogTimeout * SlowMachineAllowance.Factor;
	}
}
