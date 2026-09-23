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
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TrumpMod.Mechanics;
using TrumpMod.Models;
using TrumpMod.Models.Cards;
using TrumpMod.Models.Powers;
using TrumpMod.Models.Relics;

namespace TrumpMod.Dev;

/// <summary>
/// Automated test driver, only active when the game is launched with --trump-test &lt;mode&gt;.
///   autoslay    : the game's own AutoSlay bot plays a full run as our character (god mode on), screenshots on a timer.
///   ui          : character select, run start, in-run card library, then the Step 4 mechanics checks in a real fight
///                 (Wall stages and perks, Deport line, Pay Gold, Tariff, Tweet, boss immunity, Orobas upgrade).
///   deportsweep : fights every encounter in the game and Deports every enemy (bosses killed) to catch scripted
///                 fights that break when an enemy escapes instead of dying.
///   balance     : one run by a heuristic bot for balance numbers, any character (DevHarness.Balance.cs).
///   cards       : plays every card of the character, base and upgraded, in real fights, logs what each one changed,
///                 checks the key effects and renders every card, power, relic and potion text (DevHarness.Cards.cs).
/// Options: --trump-out &lt;dir&gt; (screenshots + report), --trump-seed &lt;seed&gt;, --trump-encounters A,B (deportsweep only),
/// --trump-tile slot/count (window grid for parallel runs), --trump-mute (silence).
/// While active, saves go to modded_trumptest/ so real (modded) profiles are never touched.
/// </summary>
public static partial class DevHarness
{
	public const string ArgMode = "trump-test";

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
		_outDir = CommandLineHelper.GetValue("trump-out") ?? ProjectSettings.GlobalizePath("user://trump_test");
		Directory.CreateDirectory(Path.Combine(_outDir, "shots"));
		Note($"Harness active, mode={_mode}, out={_outDir}");
		((SceneTree)Engine.GetMainLoop()).ProcessFrame += Tick;
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Error("Unhandled: " + e.ExceptionObject);
		TaskScheduler.UnobservedTaskException += (_, e) => Error("Unobserved task: " + e.Exception);
		if (_mode == "balance")
		{
			DeportCmd.Deported += OnBalanceDeported;
		}
	}

	private static void Tick()
	{
		try
		{
			KeepWindowTiled();
			if (!_started && NGame.Instance != null)
			{
				_started = true;
				string seed = CommandLineHelper.GetValue("trump-seed") ?? "TRUMPTEST1";
				switch (_mode)
				{
					case "ui":
						TaskHelper.RunSafely(Guarded(RunUiTest));
						break;
					case "deportsweep":
						TaskHelper.RunSafely(Guarded(RunDeportSweep));
						break;
					case "cards":
						TaskHelper.RunSafely(Guarded(RunCardTest));
						break;
					default:
						TaskHelper.RunSafely(StartAutoSlayWhenMenuReady(seed));
						break;
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
		await StartRunAsDonald(screenshots: true);

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
		await MechanicsChecks();

		// Permanent Structure kept a quarter of the 40 Wall, in a property the save file carries.
		PermanentStructure? structure = Me.Relics.OfType<PermanentStructure>().FirstOrDefault();
		Check(structure?.StoredHeight == 10, $"Permanent Structure kept {structure?.StoredHeight} height (expected 10)");
		int saved = structure == null ? -1 : SavedProperties.From(structure)?.ints?.FirstOrDefault(p => p.name == nameof(PermanentStructure.StoredHeight)).value ?? -1;
		Check(saved == 10, $"Permanent Structure height is in the save data ({saved})");

		// Swap in the Ancient version for the boss fight: Diamond Shovel builds 4 per turn.
		RunConsole("relic remove GOLDEN_SHOVEL");
		RunConsole("relic add DIAMOND_SHOVEL");
		await Task.Delay(600);

		// Boss immunity: primary enemies in boss rooms can't be Deported, minions can.
		EncounterModel boss = ModelDb.Acts.First().AllEncounters.First(e => e.RoomType == RoomType.Boss);
		await Fight(boss.Id.Entry);
		Check(WallCmd.GetHeight(Me.Creature) == 14, $"Next combat's Wall starts at {WallCmd.GetHeight(Me.Creature)} (Diamond Shovel 4 + kept 10)");
		foreach (Creature enemy in Me.Creature.CombatState!.Enemies.Where(e => e.IsAlive))
		{
			Check(DeportCmd.IsImmune(enemy) == enemy.IsPrimaryEnemy, $"boss room {boss.Id.Entry}: {enemy.Monster?.Id.Entry} primary={enemy.IsPrimaryEnemy} immune={DeportCmd.IsImmune(enemy)}");
		}
		Screenshot("boss_no_deport_line");

		// Touch of Orobas maps our starter to its Ancient version.
		var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
		RelicModel upgraded = touch.GetUpgradedStarterRelic(ModelDb.Relic<GoldenShovel>());
		Check(upgraded is DiamondShovel, $"Touch of Orobas: Golden Shovel -> {upgraded.Id.Entry}");
		Note("UI + mechanics test finished");
	}

	private static async Task MechanicsChecks()
	{
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();

		Check(WallCmd.GetHeight(donald) == 2, $"Golden Shovel: Wall is {WallCmd.GetHeight(donald)} on turn 1 (expected 2)");
		Check(me.Character.StartingHp == 70 && donald.MaxHp == 70, $"Max HP {donald.MaxHp} (expected 70)");

		// Pay Gold: unplayable when broke, pays and works when not.
		CardModel loan = await AddToHand("SMALL_LOAN");
		await PlayerCmd.SetGold(5, me);
		await Task.Delay(400);
		Check(!loan.CanPlay(out _, out _), "Small Loan unplayable with 5 Gold");
		Screenshot("pay_gold_unaffordable");
		await PlayerCmd.SetGold(99, me);
		await Task.Delay(400);
		Check(loan.CanPlay(out _, out _), "Small Loan playable with 99 Gold");
		Screenshot("pay_gold_badge");
		int energyBefore = me.PlayerCombatState!.Energy;
		await CardCmd.AutoPlay(ctx, loan, null);
		await Task.Delay(800);
		Check(me.Gold == 89, $"Small Loan paid 10 Gold (gold now {me.Gold})");
		Check(me.PlayerCombatState.Energy == energyBefore + 1, $"Small Loan gave 1 Energy ({energyBefore} -> {me.PlayerCombatState.Energy})");

		// Tweets.
		Creature enemy = combat.HittableEnemies.First();
		RunConsole("energy 5");
		CardModel mean = await AddToHand("MEAN_TWEET");
		await CardCmd.AutoPlay(ctx, mean, enemy);
		await Task.Delay(800);
		CardModel? tweet = PileType.Hand.GetPile(me).Cards.FirstOrDefault(c => c is Tweet);
		Check(tweet != null, "Mean Tweet added a Tweet to the hand");
		Screenshot("tweet_in_hand");
		if (tweet != null)
		{
			int hpBefore = enemy.CurrentHp;
			await CardCmd.AutoPlay(ctx, tweet, null);
			await Task.Delay(800);
			Check(enemy.CurrentHp < hpBefore, $"Tweet hit ALL enemies ({hpBefore} -> {enemy.CurrentHp})");
		}

		// Gold frame next to the Regent's orange one (the closest base-game colour).
		await AddToHand("BEAT_INTO_SHAPE");
		Screenshot("frame_vs_regent");

		// Tariff.
		CardModel slap = await AddToHand("SLAP_A_TARIFF");
		await CardCmd.AutoPlay(ctx, slap, enemy);
		await Task.Delay(800);
		Check(enemy.GetPowerAmount<TariffPower>() == 2, $"Slap a Tariff applied Tariff {enemy.GetPowerAmount<TariffPower>()}");

		// Wall stages.
		await Task.Delay(1500);
		Screenshot("wall_start_6");
		foreach (int target in new[] { 11, 25, 45, 70 })
		{
			await WallCmd.Build(ctx, donald, target - WallCmd.GetHeight(donald));
			await Task.Delay(900);
			Screenshot($"wall_{target}_stage{WallCmd.GetStage(donald)}");
			await Task.Delay(1200);
		}
		Check(WallCmd.GetStage(donald) == 4 && WallCmd.GetSections(donald) == 7, $"Wall 70: stage {WallCmd.GetStage(donald)}, sections {WallCmd.GetSections(donald)}");

		// End turn: Section Block, stage-4 damage, Tariff gold; next turn: +1 draw, +1 energy.
		enemy.SetCurrentHpInternal(Math.Max(enemy.CurrentHp, 30));
		int enemyHp = enemy.CurrentHp;
		int expectedLoss = Math.Max(0, 7 - enemy.Block);
		int gold = me.Gold;
		int round = combat.RoundNumber;
		int heightBefore = WallCmd.GetHeight(donald);
		bool enemyAttacks = enemy.Monster?.NextMove?.Intents?.Any(i => i.GetType().Name.Contains("Attack")) ?? false;
		PlayerCmd.EndTurn(me, canBackOut: false);
		await WaitHelper.Until(() => donald.Block > 0 || combat.RoundNumber > round, _ct, TimeSpan.FromSeconds(15));
		Check(donald.Block >= 21, $"7 Sections gave {donald.Block} Block (expected 21)");
		Screenshot("end_turn_section_block");
		// The stage-4 damage lands right after the Section Block, before the enemy turn starts.
		await WaitHelper.Until(() => enemy.CurrentHp < enemyHp || !enemy.IsAlive || combat.RoundNumber > round, _ct, TimeSpan.FromSeconds(15));
		Check(enemy.CurrentHp <= enemyHp - expectedLoss || !enemy.IsAlive, $"Big Beautiful Wall stage hit the enemy for 7 ({enemyHp} -> {enemy.CurrentHp}, expected -{expectedLoss} after its Block)");
		await WaitHelper.Until(() => combat.RoundNumber > round && me.PlayerCombatState.Phase == PlayerTurnPhase.Play, _ct, TimeSpan.FromSeconds(30));
		await Task.Delay(1500);
		Check(WallCmd.GetHeight(donald) == heightBefore + 2, $"Golden Shovel built 2 at the start of the turn ({heightBefore} -> {WallCmd.GetHeight(donald)})");
		Check(me.PlayerCombatState.MaxEnergy == 4, $"Concrete stage: max energy {me.PlayerCombatState.MaxEnergy} (expected 4)");
		Check(PileType.Hand.GetPile(me).Cards.Count >= 6, $"Brick stage: drew {PileType.Hand.GetPile(me).Cards.Count} cards (expected 6)");
		if (enemyAttacks)
		{
			Check(me.Gold == gold + 2, $"Tariff paid 2 Gold when the enemy attacked ({gold} -> {me.Gold})");
		}
		else
		{
			Note($"Tariff: enemy didn't attack this turn (gold {gold} -> {me.Gold})");
		}
		Screenshot("next_turn_perks");

		// Demolition keeps the stages.
		await WallCmd.LoseHeight(ctx, donald, WallCmd.GetHeight(donald));
		await Task.Delay(800);
		Check(WallCmd.GetHeight(donald) == 0 && WallCmd.GetStage(donald) == 4, $"Wall torn down to {WallCmd.GetHeight(donald)}, stage still {WallCmd.GetStage(donald)}");
		Screenshot("wall_demolished_stage_kept");

		// Permanent Structure: rebuild to 40, a quarter (10) is kept when this combat ends (checked in RunUiTest).
		RunConsole("relic add PERMANENT_STRUCTURE");
		await Task.Delay(600);
		await WallCmd.Build(ctx, donald, 40 - WallCmd.GetHeight(donald));

		// Deport line: 25% of max HP.
		decimal line = DeportCmd.LineHp(enemy, me);
		enemy.SetCurrentHpInternal((int)Math.Floor(line) + 1);
		await Task.Delay(1200);
		Check(!DeportCmd.CanDeport(enemy, me), $"{enemy.CurrentHp}/{enemy.MaxHp} HP is above the line ({line:0.##})");
		Screenshot("deport_line_above");
		enemy.SetCurrentHpInternal((int)Math.Floor(line));
		await Task.Delay(1200);
		Check(DeportCmd.CanDeport(enemy, me), $"{enemy.CurrentHp}/{enemy.MaxHp} HP is under the line");
		Screenshot("deport_denied_stamp");
		CardModel deport = await AddToHand("DEPORT");
		enemy.SetCurrentHpInternal((int)Math.Floor(line) + 5);
		RunConsole("energy 2");
		await CardCmd.AutoPlay(ctx, deport, enemy);
		await Task.Delay(300);
		Screenshot("deported_popup");
		Check(combat.EscapedCreatures.Contains(enemy), $"Deport card ({deport.DynamicVars.Damage.IntValue} damage) took the enemy under the line and Deported it");
		await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, _ct, TimeSpan.FromSeconds(20));
		await Task.Delay(2500);
		Screenshot("rewards_after_deport");
	}

	// ---------------------------------------------------------------- deport sweep

	private static async Task RunDeportSweep()
	{
		await StartRunAsDonald(screenshots: false);
		RunConsole("godmode");
		// --trump-encounters A,B,C limits the sweep to those encounters.
		var only = (CommandLineHelper.GetValue("trump-encounters") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		var encounters = ModelDb.AllEncounters.Where(e => only.Count == 0 || only.Contains(e.Id.Entry)).OrderBy(e => e.Id.Entry).ToList();
		Note($"Deport sweep over {encounters.Count} encounters");
		foreach (EncounterModel encounter in encounters)
		{
			string id = encounter.Id.Entry;
			var result = new Dictionary<string, object> { ["encounter"] = id, ["room"] = encounter.RoomType.ToString() };
			Log.Info($"[{ModEntry.ModId}:sweep] BEGIN {id}");
			try
			{
				await Fight(id, TimeSpan.FromSeconds(25));
				ICombatState combat = Me.Creature.CombatState!;
				var ctx = new BlockingPlayerChoiceContext();
				result["enemies"] = combat.Enemies.Where(e => e.IsAlive).Select(Describe).ToList();
				// Deport one enemy at a time and pass a turn after each, so the ones left act without their partner
				// (bosses commanding minions, groups that buff or revive each other). Capped for encounters that keep summoning.
				var deported = new List<string>();
				int turns = 0;
				for (int i = 0; i < 8 && CombatManager.Instance.IsInProgress; i++)
				{
					Creature? target = combat.Enemies.FirstOrDefault(e => e.IsAlive && !DeportCmd.IsImmune(e));
					if (target == null)
					{
						break;
					}
					target.SetCurrentHpInternal(1);
					if (!await DeportCmd.TryDeport(ctx, target, Me))
					{
						Error($"sweep {id}: could not Deport {Describe(target)}");
						break;
					}
					deported.Add(Describe(target));
					if (!CombatManager.Instance.IsInProgress || !combat.Enemies.Any(e => e.IsAlive))
					{
						break;
					}
					if (!await PassTurn(combat))
					{
						Error($"sweep {id}: the turn after Deporting {Describe(target)} never came back");
						break;
					}
					turns++;
				}
				result["deported"] = deported;
				result["turnsAfterDeport"] = turns;
				// Immune bosses are finished with the game's `win` command: it strips powers first, so bosses that revive
				// (Test Subject's three forms) stay dead, then checks for victory.
				if (CombatManager.Instance.IsInProgress && combat.Enemies.Any(e => e.IsAlive))
				{
					RunConsole("win");
				}
				bool ended = await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
				result["ended"] = ended;
				if (!ended)
				{
					Error($"sweep {id}: combat did not end (alive: {string.Join(",", combat.Enemies.Where(e => e.IsAlive).Select(Describe))})");
				}
				await Task.Delay(1200);
			}
			catch (Exception e)
			{
				result["error"] = e.Message;
				Error($"sweep {id}: {e.Message}");
			}
			Log.Info($"[{ModEntry.ModId}:sweep] END {id}");
			_sweep.Add(result);
		}
		Note($"Deport sweep done: {_sweep.Count} encounters");
	}

	private static string Describe(Creature enemy)
	{
		return $"{enemy.Monster?.Id.Entry}{(enemy.IsPrimaryEnemy ? "" : "(minion)")}{(DeportCmd.IsImmune(enemy) ? "(immune)" : "")}";
	}

	/// <summary>End the player's turn and wait for the next one (or for the combat to end).</summary>
	private static async Task<bool> PassTurn(ICombatState combat)
	{
		int round = combat.RoundNumber;
		PlayerCmd.EndTurn(Me, canBackOut: false);
		return await WaitUntil(() => !CombatManager.Instance.IsInProgress || (combat.RoundNumber > round && Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play), TimeSpan.FromSeconds(40));
	}

	// ---------------------------------------------------------------- helpers

	private static async Task StartRunAsDonald(bool screenshots)
	{
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		Control mainMenu = await WaitHelper.ForNode<Control>(root, "/root/Game/RootSceneContainer/MainMenu", _ct, TimeSpan.FromSeconds(90));
		SaveManager.Instance.SetFtuesEnabled(enabled: false);
		await Task.Delay(2500);
		if (screenshots)
		{
			Screenshot("main_menu");
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
		List<NCharacterSelectButton> buttons = UiHelper.FindAll<NCharacterSelectButton>(charSelect.GetNode("CharSelectButtons/ButtonContainer"));
		buttons.First(b => b.Character is IModCharacter).Select();
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
		var report = new { mode = _mode, ok = _errors.Count == 0, errors = _errors, events = _events, sweep = _sweep };
		File.WriteAllText(Path.Combine(_outDir, "report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
	}
}
