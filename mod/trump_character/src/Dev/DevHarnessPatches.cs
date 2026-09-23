using System;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Screens;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
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
			// Balance runs can pick any character (--trump-character IRONCLAD); every other test plays ours.
			string? wanted = DevHarness.BalanceMode ? CommandLineHelper.GetValue("trump-character") ?? "TRUMP" : null;
			bool unlocked = wanted != null ? __instance.Character.Id.Entry == wanted : ModContent.IsModCharacter(__instance.Character);
			Traverse.Create(__instance).Field("_isLocked").SetValue(!unlocked);
		}
	}
}

/// <summary>Balance mode only: our heuristic bot plays the fights instead of AutoSlay's random, unkillable one.</summary>
[HarmonyPatch(typeof(CombatRoomHandler), nameof(CombatRoomHandler.HandleAsync))]
public static class BalanceCombatPatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix(Rng random, CancellationToken ct, ref Task __result)
	{
		if (!DevHarness.BalanceMode)
		{
			return true;
		}
		__result = DevHarness.BalanceCombat(random, ct);
		return false;
	}
}

/// <summary>Balance mode only: card rewards are picked by rarity and type instead of at random.</summary>
[HarmonyPatch(typeof(CardRewardScreenHandler), nameof(CardRewardScreenHandler.HandleAsync))]
public static class BalanceCardRewardPatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix(Rng random, CancellationToken ct, ref Task __result)
	{
		if (!DevHarness.BalanceMode)
		{
			return true;
		}
		__result = DevHarness.BalanceCardReward(random, ct);
		return false;
	}
}

/// <summary>Balance mode only: rest sites heal under half HP and upgrade otherwise.</summary>
[HarmonyPatch(typeof(RestSiteRoomHandler), nameof(RestSiteRoomHandler.HandleAsync))]
public static class BalanceRestSitePatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix(Rng random, CancellationToken ct, ref Task __result)
	{
		if (!DevHarness.BalanceMode)
		{
			return true;
		}
		__result = DevHarness.BalanceRestSite(random, ct);
		return false;
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

/// <summary>Balance mode only: the map path follows HP (rest when hurt, elites when healthy) instead of always going left.</summary>
[HarmonyPatch(typeof(MapScreenHandler), nameof(MapScreenHandler.HandleAsync))]
public static class BalanceMapPatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix(Rng random, CancellationToken ct, ref Task __result)
	{
		if (!DevHarness.BalanceMode)
		{
			return true;
		}
		__result = DevHarness.BalanceMap(random, ct);
		return false;
	}
}

/// <summary>
/// Test mode only: never write the global settings.save. It's the same file the normal game uses, and the game saves
/// it on quit (window size and position when windowed), so tiled or muted test windows must not leak into it.
/// </summary>
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveSettings))]
public static class TestNoSettingsSavePatch
{
	public static bool Prepare() => DevHarness.Enabled;

	public static bool Prefix() => false;
}

/// <summary>Test mode with --trump-mute: every master volume change becomes 0, including the un-mute on focus.</summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetMasterVol))]
public static class TestMutePatch
{
	public static bool Prepare() => DevHarness.Enabled && CommandLineHelper.HasArg("trump-mute");

	public static void Prefix(ref float volume)
	{
		volume = 0f;
	}
}
