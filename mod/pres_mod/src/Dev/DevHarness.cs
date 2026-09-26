using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using PresMod.Framework.Patches;
using PresMod.Framework;
using PresMod.Framework.Nodes;

namespace PresMod.Dev;

/// <summary>
/// Automated test driver for every character of the mod, only active when the game is launched with --pres-test &lt;mode&gt;.
/// The character under test is --pres-character ID (default: the first mod character).
///   autoslay : the game's own AutoSlay bot plays a full run as the character (god mode on), screenshots on a timer.
///   ui       : character select, run start, in-run card library, a fight with the pose checks and the character's own
///              mechanics (CharacterTests.UiChecks), then the shop and rest-site scenes.
///   cards    : plays every card of the character, base and upgraded, in real fights, logs what each one changed, checks
///              the key effects and renders every card, power, relic and potion text (DevHarness.Cards.cs).
///   balance  : one run by a heuristic bot for balance numbers, any character (DevHarness.Balance.cs).
///   coop     : two instances play a co-op fight and record the state each turn to find desyncs (DevHarness.Coop.cs).
///   trailer  : stages and records trailer shots (DevHarness.Trailer.cs, scripts/trailer_capture.py).
///   other    : modes a character adds itself (CharacterTests.ExtraModes, e.g. Trump's deportsweep).
/// Character-specific checks live in Characters/&lt;Name&gt;/Dev/DevHarness.&lt;Name&gt;.cs (CharacterTests.cs explains the hooks).
/// Options: --pres-out &lt;dir&gt; (screenshots + report), --pres-seed &lt;seed&gt;, --pres-tile slot/count (window grid for
/// parallel runs), --pres-mute (silence), plus mode options (--pres-only, --pres-encounters, --pres-fullheal, --pres-favor).
/// While active, saves go to modded_prestest/ so real (modded) profiles are never touched.
/// </summary>
public static partial class DevHarness
{
	public const string ArgMode = "pres-test";

	public static bool Enabled => CommandLineHelper.HasArg(ArgMode);

	private static string _mode = "autoslay";

	private static string _outDir = "";

	private static readonly List<string> _events = new List<string>();

	private static readonly List<string> _errors = new List<string>();

	private static readonly List<object> _sweep = new List<object>();

	private static int _shotIndex;

	private static ulong _lastShotMs;

	private static bool _started;

	private static bool _godModeRequested;

	private const ulong _autoShotIntervalMs = 5000;

	private const int _maxAutoShots = 400;

	private static readonly CancellationToken _ct = CancellationToken.None;

	private static Player Me => LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState())!;

	public static void InitIfRequested()
	{
		if (!Enabled)
		{
			return;
		}
		_mode = CommandLineHelper.GetValue(ArgMode) ?? "autoslay";
		_outDir = CommandLineHelper.GetValue("pres-out") ?? ProjectSettings.GlobalizePath("user://pres_test");
		Directory.CreateDirectory(Path.Combine(_outDir, "shots"));
		Note($"Harness active, mode={_mode}, out={_outDir}");
		((SceneTree)Engine.GetMainLoop()).ProcessFrame += Tick;
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Error("Unhandled: " + e.ExceptionObject);
		TaskScheduler.UnobservedTaskException += (_, e) => Error("Unobserved task: " + e.Exception);
	}

	private static void Tick()
	{
		try
		{
			KeepWindowTiled();
			if (!_started && NGame.Instance != null)
			{
				_started = true;
				string seed = CommandLineHelper.GetValue("pres-seed") ?? "PRESTEST1";
				if (_mode == "ui")
				{
					TaskHelper.RunSafely(Guarded(RunUiTest));
				}
				else if (_mode == "cards")
				{
					TaskHelper.RunSafely(Guarded(RunCardTest));
				}
				else if (_mode == "coop")
				{
					TaskHelper.RunSafely(Guarded(RunCoopTest));
				}
				else if (_mode == "trailer")
				{
					TaskHelper.RunSafely(Guarded(RunTrailer));
				}
				else if (_mode == "trailer_coop")
				{
					TaskHelper.RunSafely(Guarded(RunTrailerCoop));
				}
				else if (Kit.ExtraModes.TryGetValue(_mode, out Func<Task>? extra))
				{
					TaskHelper.RunSafely(Guarded(extra));
				}
				else
				{
					TaskHelper.RunSafely(StartAutoSlayWhenMenuReady(seed));
				}
			}
			if (_mode == "autoslay")
			{
				AutoslayTick();
			}
		}
		catch (Exception e)
		{
			Error("Tick: " + e);
		}
	}

	private static async Task Guarded(Func<Task> test)
	{
		try
		{
			await test();
		}
		catch (Exception e)
		{
			Error($"{_mode} test failed: {e}");
		}
		finally
		{
			Finish();
		}
	}

	/// <summary>
	/// AutoSlay's timers start with the bot: a 30 s stuck-watchdog, and 10 s for the first room once a run starts.
	/// A slow boot can use up the first before the main menu appears, and a run started while the shared assets are
	/// still preloading misses the second. Start the bot once the menu is up and the preload is done.
	/// </summary>
	private static async Task StartAutoSlayWhenMenuReady(string seed)
	{
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		await WaitHelper.ForNode<Control>(root, "/root/Game/RootSceneContainer/MainMenu", _ct, TimeSpan.FromSeconds(120));
		if (!await WaitUntil(() => CommonPreloadDonePatch.Done, TimeSpan.FromSeconds(120)))
		{
			Note("Shared asset preload not reported done after 120 s; starting AutoSlay anyway");
		}
		await Task.Delay(2000);
		if (BalanceMode)
		{
			UseAscensionZero();
		}
		Note($"Starting AutoSlay with seed {seed}");
		new AutoSlayer().Start(seed, Path.Combine(_outDir, "autoslay.log"));
	}

	private static void AutoslayTick()
	{
		if (!_godModeRequested && RunManager.Instance.IsInProgress && RunManager.Instance.DebugOnlyGetState()?.CurrentRoom != null)
		{
			_godModeRequested = true;
			RunConsole("godmode");
		}
		ulong now = Time.GetTicksMsec();
		if (now - _lastShotMs >= _autoShotIntervalMs && _shotIndex < _maxAutoShots)
		{
			_lastShotMs = now;
			RunState? state = RunManager.Instance.DebugOnlyGetState();
			string label = state?.CurrentRoom == null ? "menu" : $"a{state.CurrentActIndex + 1}_f{state.ActFloor}_{state.CurrentRoom.RoomType}";
			Screenshot(label);
		}
	}

	// ---------------------------------------------------------------- ui + mechanics

	private static async Task RunUiTest()
	{
		await StartRunAsCharacter(screenshots: true);

		// In-run card library opens on our tab (P3).
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		NRunSubmenuStack? stack = UiHelper.FindFirst<NRunSubmenuStack>(root);
		if (stack != null)
		{
			NCardLibrary library = stack.GetSubmenuType<NCardLibrary>();
			library.Initialize(RunManager.Instance.DebugOnlyGetState()!);
			stack.Push(library);
			await Task.Delay(2500);
			Screenshot("card_library_in_run");
			stack.Pop();
			await Task.Delay(1000);
		}

		string weak = ModelDb.Acts.First().AllWeakEncounters.First().Id.Entry;
		await Fight(weak);
		Screenshot("combat_start");
		await PoseChecks();
		// The character's own mechanics (Characters/<Name>/Dev/DevHarness.<Name>.cs); they may fight more encounters.
		if (Kit.UiChecks != null)
		{
			await Kit.UiChecks();
		}
		else
		{
			Note($"No UiChecks registered for {TestCharacterId}");
		}

		// The shop and rest-site scenes.
		if (CombatManager.Instance.IsInProgress)
		{
			RunConsole("win");
			await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, _ct, TimeSpan.FromSeconds(20));
		}
		await Task.Delay(1500);
		CharacterModel character = TestCharacter;
		foreach ((string room, string scene, string label) in new[] { ("Shop", character.MerchantAnimPath, "shop"), ("RestSite", character.RestSiteAnimPath, "rest_site") })
		{
			RunConsole("room " + room);
			await Task.Delay(4000);
			Node? host = UiHelper.FindAll<Node>(((SceneTree)Engine.GetMainLoop()).Root).FirstOrDefault(n => n.SceneFilePath == scene);
			Check(host != null, $"{room}: the character's scene {scene} is shown");
			if (host?.GetNodeOrNull<Sprite2D>(ArtPatches.SpriteName) is Sprite2D sprite)
			{
				Check(sprite.Texture != null, $"{room}: the character's painting is shown ({sprite.Texture?.ResourcePath ?? "none"})");
			}
			Screenshot(label);
		}
		Note("UI + mechanics test finished");
	}

	/// <summary>A sprite-bodied character (no Spine rig) swaps paintings on the game's animation triggers (NCharacterPoses).</summary>
	private static async Task PoseChecks()
	{
		NCreature? node = NCombatRoom.Instance?.GetCreatureNode(Me.Creature);
		if (node?.Visuals?.GetNodeOrNull("%Visuals") is not Sprite2D body)
		{
			Note("Combat body is not a sprite (Spine rig?): no pose checks");
			return;
		}
		NCharacterPoses? poses = node.Visuals.GetNodeOrNull<NCharacterPoses>(NCharacterPoses.NodeName);
		Check(poses != null, "Combat body has the pose controller");
		if (poses == null)
		{
			return;
		}
		Texture2D? idle = body.Texture;
		foreach ((string trigger, string label) in new[] { ("Attack", "pose_attack"), ("Cast", "pose_cast"), ("Hit", "pose_hurt") })
		{
			node.SetAnimationTrigger(trigger);
			await Task.Delay(220);
			Check(body.Texture != null && body.Texture != idle, $"{trigger}: pose changed to {body.Texture?.ResourcePath}");
			Screenshot(label);
			await Task.Delay(900);
		}
		Check(body.Texture == idle, "Back to the idle pose");
	}

	/// <summary>End the player's turn and wait for the next one (or for the combat to end).</summary>
	private static async Task<bool> PassTurn(ICombatState combat)
	{
		int round = combat.RoundNumber;
		if (RunManager.Instance.DebugOnlyGetState()!.Players.Count > 1)
		{
			// Co-op: like the End Turn button, through the synced queue, so the other players learn about it.
			RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new EndPlayerTurnAction(Me, Me.PlayerCombatState!.TurnNumber));
		}
		else
		{
			PlayerCmd.EndTurn(Me, canBackOut: false);
		}
		return await WaitUntil(() => !CombatManager.Instance.IsInProgress || (combat.RoundNumber > round && Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play), TimeSpan.FromSeconds(40));
	}

	// ---------------------------------------------------------------- helpers

	/// <summary>From the main menu to the first room as the test character. atMenu and atSelect let the trailer film the menu
	/// and the character select screen on the way (DevHarness.Trailer.cs).</summary>
	private static async Task StartRunAsCharacter(bool screenshots, Func<Control, Task>? atMenu = null, Func<Control, Task>? atSelect = null)
	{
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		Control mainMenu = await WaitHelper.ForNode<Control>(root, "/root/Game/RootSceneContainer/MainMenu", _ct, TimeSpan.FromSeconds(90));
		DisableTutorials();
		await Task.Delay(2500);
		if (screenshots)
		{
			Screenshot("main_menu");
		}
		if (atMenu != null)
		{
			await atMenu(mainMenu);
		}
		NButton abandon = mainMenu.GetNode<NButton>("MainMenuTextButtons/AbandonRunButton");
		if (abandon.Visible)
		{
			await UiHelper.Click(abandon);
			await WaitHelper.Until(() => NModalContainer.Instance?.OpenModal != null, _ct, TimeSpan.FromSeconds(10));
			await UiHelper.Click(((Node)NModalContainer.Instance!.OpenModal!).GetNode<NButton>("VerticalPopup/YesButton"));
			await WaitHelper.Until(() => NModalContainer.Instance.OpenModal == null, _ct, TimeSpan.FromSeconds(10));
		}
		await UiHelper.Click(mainMenu.GetNode<NButton>("MainMenuTextButtons/SingleplayerButton"));
		await WaitHelper.Until(() => (mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false)
			|| (mainMenu.GetNodeOrNull<NButton>("Submenus/SingleplayerSubmenu/StandardButton")?.Visible ?? false), _ct, TimeSpan.FromSeconds(10));
		NButton? standard = mainMenu.GetNodeOrNull<NButton>("Submenus/SingleplayerSubmenu/StandardButton");
		if (standard != null && standard.Visible && !(mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false))
		{
			await UiHelper.Click(standard);
			await WaitHelper.Until(() => mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false, _ct, TimeSpan.FromSeconds(10));
		}
		Control charSelect = mainMenu.GetNode<Control>("Submenus/CharacterSelectScreen");
		if (atSelect != null)
		{
			await atSelect(charSelect);
		}
		List<NCharacterSelectButton> buttons = UiHelper.FindAll<NCharacterSelectButton>(charSelect.GetNode("CharSelectButtons/ButtonContainer"));
		buttons.First(b => b.Character.Id.Entry == TestCharacterId).Select();
		await Task.Delay(3000);
		if (screenshots)
		{
			Screenshot("char_select");
		}
		await UiHelper.Click(await WaitHelper.ForNode<NButton>(mainMenu, "Submenus/CharacterSelectScreen/ConfirmButton", _ct));
		await WaitHelper.Until(() => RunManager.Instance.DebugOnlyGetState()?.CurrentRoom != null, _ct, TimeSpan.FromSeconds(60));
		await Task.Delay(5000);
		if (screenshots)
		{
			Screenshot("run_start");
		}
	}

	/// <summary>
	/// No tutorials or first-time popups in the test profile. The Ascension popup isn't a tutorial to the game, so turning
	/// tutorials off doesn't stop it: once a test run has met the Architect it would cover every later screenshot.
	/// </summary>
	private static void DisableTutorials()
	{
		SaveManager.Instance.SetFtuesEnabled(enabled: false);
		SaveManager.Instance.MarkFtueAsComplete(NAscensionSingleplayerFtue.id);
	}

	private static async Task Fight(string encounter, TimeSpan? timeout = null)
	{
		RunConsole("fight " + encounter);
		await WaitHelper.Until(() => CombatManager.Instance.IsInProgress && Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play, _ct, timeout ?? TimeSpan.FromSeconds(30));
		await Task.Delay(2500);
	}

	private static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan timeout)
	{
		try
		{
			await WaitHelper.Until(condition, _ct, timeout);
			return true;
		}
		catch (AutoSlayTimeoutException)
		{
			return false;
		}
	}

	private static async Task<CardModel> AddToHand(string cardId)
	{
		RunConsole($"card {cardId} hand");
		await Task.Delay(600);
		return PileType.Hand.GetPile(Me).Cards.Last(c => c.Id.Entry == cardId);
	}

	private static void Check(bool ok, string message)
	{
		if (ok)
		{
			Note("PASS " + message);
		}
		else
		{
			Error("FAIL " + message);
		}
	}

	private static void RunConsole(string command)
	{
		try
		{
			var console = Traverse.Create(NDevConsole.Instance).Field("_devConsole").GetValue<MegaCrit.Sts2.Core.DevConsole.DevConsole>();
			CmdResult result = console.ProcessCommand(command);
			if (result.success)
			{
				Note($"console `{command}` -> {result.msg}");
			}
			else
			{
				Error($"console `{command}` -> {result.msg}");
			}
		}
		catch (Exception e)
		{
			Error($"console `{command}` failed: {e.Message}");
		}
	}

	private static void Screenshot(string label)
	{
		try
		{
			Image image = ((SceneTree)Engine.GetMainLoop()).Root.GetViewport().GetTexture().GetImage();
			string file = Path.Combine(_outDir, "shots", $"{_shotIndex++:D3}_{label}.png");
			image.SavePng(file);
		}
		catch (Exception e)
		{
			Error($"screenshot {label} failed: {e.Message}");
		}
	}

	private static void Note(string message)
	{
		_events.Add($"{DateTime.Now:HH:mm:ss} {message}");
		Log.Info($"[{ModEntry.ModId}:test] {message}");
	}

	private static void Error(string message)
	{
		_errors.Add($"{DateTime.Now:HH:mm:ss} {message}");
		Log.Error($"[{ModEntry.ModId}:test] {message}");
	}

	/// <summary>Called (via patch) right before AutoSlay quits the game at the end of its run.</summary>
	internal static void OnAutoSlayQuit(int exitCode)
	{
		if (BalanceMode)
		{
			EndBalanceRun(exitCode == 0 ? "victory" : "error");
		}
		if (exitCode != 0)
		{
			Error($"AutoSlay ended with exit code {exitCode} (see autoslay.log)");
		}
		Screenshot("autoslay_end");
		WriteReport();
	}

	private static void Finish()
	{
		WriteReport();
		((SceneTree)Engine.GetMainLoop()).Quit(_errors.Count == 0 ? 0 : 1);
	}

	private static void WriteReport()
	{
		var report = new { mode = _mode, ok = _errors.Count == 0, errors = _errors, events = _events, sweep = _sweep, coop = _coopStates, clips = _clips };
		File.WriteAllText(Path.Combine(_outDir, "report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
	}
}
