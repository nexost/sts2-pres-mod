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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using TrumpMod.Models;
using TrumpMod.Models.Cards;

namespace TrumpMod.Dev;

/// <summary>
/// Automated test driver, only active when the game is launched with --trump-test &lt;mode&gt;.
///   autoslay : the game's own AutoSlay bot plays a full run as our character (god mode on), screenshots on a timer.
///   ui       : scripted walk through character select, run start, in-run card library and a combat, with screenshots.
/// Options: --trump-out &lt;dir&gt; (screenshots + report), --trump-seed &lt;seed&gt;.
/// While active, saves go to modded_trumptest/ so real (modded) profiles are never touched.
/// </summary>
public static class DevHarness
{
	public const string ArgMode = "trump-test";

	public static bool Enabled => CommandLineHelper.HasArg(ArgMode);

	private static string _mode = "autoslay";

	private static string _outDir = "";

	private static readonly List<string> _events = new List<string>();

	private static readonly List<string> _errors = new List<string>();

	private static int _shotIndex;

	private static ulong _lastShotMs;

	private static bool _started;

	private static bool _godModeRequested;

	private const ulong _autoShotIntervalMs = 5000;

	private const int _maxAutoShots = 400;

	public static void InitIfRequested()
	{
		if (!Enabled)
		{
			return;
		}
		_mode = CommandLineHelper.GetValue(ArgMode) ?? "autoslay";
		_outDir = CommandLineHelper.GetValue("trump-out") ?? ProjectSettings.GlobalizePath("user://trump_test");
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
			if (!_started && NGame.Instance != null)
			{
				_started = true;
				string seed = CommandLineHelper.GetValue("trump-seed") ?? "TRUMPTEST1";
				if (_mode == "ui")
				{
					TaskHelper.RunSafely(RunUiTest());
				}
				else
				{
					Note($"Starting AutoSlay with seed {seed}");
					new AutoSlayer().Start(seed, Path.Combine(_outDir, "autoslay.log"));
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

	private static async Task RunUiTest()
	{
		CancellationToken ct = CancellationToken.None;
		try
		{
			Node root = ((SceneTree)Engine.GetMainLoop()).Root;
			Control mainMenu = await WaitHelper.ForNode<Control>(root, "/root/Game/RootSceneContainer/MainMenu", ct, TimeSpan.FromSeconds(90));
			// Fresh test profile: skip the "enable tutorials?" popup and tutorials, like AutoSlay does.
			SaveManager.Instance.SetFtuesEnabled(enabled: false);
			await Task.Delay(2500);
			Screenshot("main_menu");

			NButton abandon = mainMenu.GetNode<NButton>("MainMenuTextButtons/AbandonRunButton");
			if (abandon.Visible)
			{
				Note("Abandoning leftover test run");
				await UiHelper.Click(abandon);
				await WaitHelper.Until(() => NModalContainer.Instance?.OpenModal != null, ct, TimeSpan.FromSeconds(10));
				await UiHelper.Click(((Node)NModalContainer.Instance!.OpenModal!).GetNode<NButton>("VerticalPopup/YesButton"));
				await WaitHelper.Until(() => NModalContainer.Instance.OpenModal == null, ct, TimeSpan.FromSeconds(10));
			}

			await UiHelper.Click(mainMenu.GetNode<NButton>("MainMenuTextButtons/SingleplayerButton"));
			await WaitHelper.Until(() => (mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false)
				|| (mainMenu.GetNodeOrNull<NButton>("Submenus/SingleplayerSubmenu/StandardButton")?.Visible ?? false), ct, TimeSpan.FromSeconds(10));
			NButton? standard = mainMenu.GetNodeOrNull<NButton>("Submenus/SingleplayerSubmenu/StandardButton");
			if (standard != null && standard.Visible && !(mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false))
			{
				await UiHelper.Click(standard);
				await WaitHelper.Until(() => mainMenu.GetNodeOrNull<Control>("Submenus/CharacterSelectScreen")?.Visible ?? false, ct, TimeSpan.FromSeconds(10));
			}
			Control charSelect = mainMenu.GetNode<Control>("Submenus/CharacterSelectScreen");
			List<NCharacterSelectButton> buttons = UiHelper.FindAll<NCharacterSelectButton>(charSelect.GetNode("CharSelectButtons/ButtonContainer"));
			Note("Character buttons: " + string.Join(", ", buttons.Select(b => $"{b.Character.Id.Entry}{(b.IsLocked ? "(locked)" : "")}")));
			NCharacterSelectButton ours = buttons.First(b => b.Character is IModCharacter);
			ours.Select();
			await Task.Delay(3000);
			Screenshot("char_select");

			await UiHelper.Click(await WaitHelper.ForNode<NButton>(mainMenu, "Submenus/CharacterSelectScreen/ConfirmButton", ct));
			await WaitHelper.Until(() => RunManager.Instance.DebugOnlyGetState()?.CurrentRoom != null, ct, TimeSpan.FromSeconds(60));
			await Task.Delay(5000);
			Screenshot("run_start");

			// P3: the card library crashed mid-run for unknown characters.
			NRunSubmenuStack? stack = UiHelper.FindFirst<NRunSubmenuStack>(root);
			if (stack == null)
			{
				Error("No NRunSubmenuStack found; card library check skipped");
			}
			else
			{
				// Same path as the in-run Compendium button (NCompendiumSubmenu.OpenCardLibrary).
				NCardLibrary library = stack.GetSubmenuType<NCardLibrary>();
				library.Initialize(RunManager.Instance.DebugOnlyGetState()!);
				stack.Push(library);
				await Task.Delay(2500);
				Screenshot("card_library_in_run");
				stack.Pop();
				await Task.Delay(1000);
				Note("Card library opened and closed mid-run");
			}

			string encounter = ModelDb.Acts.First().AllWeakEncounters.First().Id.Entry;
			RunConsole("fight " + encounter);
			await WaitHelper.Until(() => CombatManager.Instance.IsInProgress, ct, TimeSpan.FromSeconds(30));
			await Task.Delay(5000);
			Screenshot("combat");

			await DeportCheck(ct);
			Note("UI test finished");
		}
		catch (Exception e)
		{
			Error("UI test failed: " + e);
		}
		finally
		{
			Finish();
		}
	}

	/// <summary>
	/// Engine check for the Deport idea: weaken every enemy, play Deport on each, and make sure they leave through
	/// the escape path, combat ends on its own and the rewards screen still appears.
	/// </summary>
	private static async Task DeportCheck(CancellationToken ct)
	{
		Player me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState())!;
		ICombatState combat = me.Creature.CombatState!;
		List<Creature> enemies = combat.HittableEnemies.ToList();
		Note($"Deport check vs {enemies.Count} enemies: {string.Join(", ", enemies.Select(e => $"{e.Monster?.Id.Entry} {e.CurrentHp}hp"))}");
		foreach (Creature enemy in enemies)
		{
			if (!CombatManager.Instance.IsInProgress)
			{
				break;
			}
			await CreatureCmd.SetCurrentHp(enemy, 10m);
			RunConsole("card DEPORT_PROTOTYPE hand");
			await Task.Delay(800);
			CardModel? deport = PileType.Hand.GetPile(me).Cards.LastOrDefault(c => c is DeportPrototype);
			if (deport == null)
			{
				Error("Deport check: card not found in hand");
				return;
			}
			RunConsole("energy 3");
			await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), deport, enemy);
			await Task.Delay(1500);
			bool escaped = combat.EscapedCreatures.Contains(enemy);
			(escaped ? (Action<string>)Note : Error)($"Deport check: {enemy.Monster?.Id.Entry} escaped={escaped} alive={enemy.IsAlive}");
			Screenshot("deport_" + enemy.Monster?.Id.Entry.ToLowerInvariant());
		}
		await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, ct, TimeSpan.FromSeconds(20));
		Note($"Deport check: combat ended on its own; escaped {combat.EscapedCreatures.Count}/{enemies.Count}");
		await Task.Delay(3000);
		Screenshot("deport_rewards");
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
		var report = new { mode = _mode, ok = _errors.Count == 0, errors = _errors, events = _events };
		File.WriteAllText(Path.Combine(_outDir, "report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
	}
}
