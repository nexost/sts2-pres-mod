using System.IO;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace PresMod.Dev;

/// <summary>
/// The trailer's director (docs/00_project_plan.md, Step 12): mode "trailer" stages shots and records them as clips.
/// Run it with scripts/trailer_capture.py, which picks the shots (--pres-shots a,b) and the capture method
/// (--pres-capture):
///   frames : the engine runs at --fixed-fps 60 and TrailerRecorder writes every frame of each clip (no drops, 4K).
///   none   : nothing is recorded in the game; the script records the screen or the sound in real time, or uses the
///            engine's own Movie Maker (--write-movie). Clip start and end are still written, as frame numbers and
///            clock times.
/// Shots before the run starts: menu_spire, select. Shared shots in a run: gallery (screenshots of candidate fights),
/// cards (every card of every mod character rendered to a PNG). The rest are character hooks
/// (CharacterTests.TrailerShots). Waits inside a shot use game time (Wait), never the wall clock, so their timing
/// survives a capture that draws slower than real time.
/// </summary>
public static partial class DevHarness
{
	private const int TrailerFps = 60;

	private static readonly List<object> _clips = new List<object>();

	private static TrailerRecorder? _recorder;

	private static string? _clipName;

	private static long _clipStartFrame;

	private static DateTime _clipStartUtc;

	private static readonly string[] PreRunShots = { "menu_spire", "select" };

	private static readonly Dictionary<string, Func<Task>> SharedTrailerShots = new Dictionary<string, Func<Task>>
	{
		["gallery"] = TrailerGallery,
		["cards"] = TrailerCardRenders,
	};

	private static async Task RunTrailer()
	{
		string[] shots = (CommandLineHelper.GetValue("pres-shots") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		Note($"Trailer: capture={TrailerCapture}, shots={string.Join(",", shots)}");
		if (CommandLineHelper.HasArg("pres-nomusic"))
		{
			// TestNoMusicPatch turns any value into 0; this applies it now, over the settings loaded at startup.
			NGame.Instance?.AudioManager?.SetBgmVol(0f);
		}
		Directory.CreateDirectory(Path.Combine(_outDir, "clips"));
		await StartRunAsCharacter(screenshots: false,
			atMenu: shots.Contains("menu_spire") ? ShotMenuSpire : null,
			atSelect: shots.Contains("select") ? ShotSelect : null);
		foreach (string shot in shots.Where(s => !PreRunShots.Contains(s)))
		{
			Func<Task>? stage = SharedTrailerShots.GetValueOrDefault(shot) ?? Kit.TrailerShots.GetValueOrDefault(shot);
			if (stage == null)
			{
				Error($"No trailer shot '{shot}' for {TestCharacterId} (known: {string.Join(", ", SharedTrailerShots.Keys.Concat(Kit.TrailerShots.Keys))})");
				continue;
			}
			Note($"Shot {shot}");
			try
			{
				await stage();
			}
			catch (Exception e)
			{
				Error($"Shot {shot} failed: {e}");
				await EndClip();
			}
		}
		Note("Trailer shots done");
	}

	private static string TrailerCapture => CommandLineHelper.GetValue("pres-capture") ?? "frames";

	private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

	/// <summary>Waits in game time: under --fixed-fps this is exactly seconds × 60 frames of the clip.</summary>
	private static async Task Wait(double seconds)
	{
		await Tree.ToSignal(Tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private static async Task Frames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
		}
	}

	/// <summary>Records the action as one clip, with a little still time before and after for the edit's cuts.</summary>
	private static async Task Record(string clip, Func<Task> action, double preRoll = 0.5, double postRoll = 1.0)
	{
		await BeginClip(clip);
		await Wait(preRoll);
		await action();
		await Wait(postRoll);
		await EndClip();
	}

	/// <summary>
	/// The same moment in several looks (the game as played, and clean with no interface), each from a fresh setup
	/// because the moment changes the fight (a Deport, a kill). Clips are named clip_full, clip_clean.
	/// </summary>
	private static async Task Takes(string clip, Func<Task> setup, Func<Task> action, double preRoll = 0.5, double postRoll = 1.2, bool clean = true)
	{
		var looks = new List<(string Suffix, TrailerUi.Look Look)> { ("full", TrailerUi.Look.Full) };
		if (clean)
		{
			looks.Add(("clean", TrailerUi.Look.Clean));
		}
		foreach ((string suffix, TrailerUi.Look look) in looks)
		{
			await setup();
			TrailerUi.Apply(look);
			await Wait(0.6);
			await Record($"{clip}_{suffix}", action, preRoll, postRoll);
		}
		TrailerUi.Apply(TrailerUi.Look.Full);
	}

	private static async Task BeginClip(string clip)
	{
		if (_clipName != null)
		{
			await EndClip();
		}
		_clipName = clip;
		_clipStartFrame = (long)Engine.GetFramesDrawn();
		_clipStartUtc = DateTime.UtcNow;
		TrailerUi.HideVersionLabels();
		TrailerUi.ParkMouse();
		if (TrailerCapture == "frames")
		{
			string? ffmpeg = CommandLineHelper.GetValue("pres-ffmpeg");
			if (ffmpeg == null)
			{
				Error("Capture 'frames' needs --pres-ffmpeg");
				return;
			}
			_recorder = TrailerRecorder.Start(ffmpeg, Path.Combine(_outDir, "clips", clip + ".mp4"), TrailerFps);
		}
		Note($"Clip {clip} started (frame {_clipStartFrame})");
	}

	private static async Task EndClip()
	{
		if (_clipName == null)
		{
			return;
		}
		long endFrame = (long)Engine.GetFramesDrawn();
		DateTime endUtc = DateTime.UtcNow;
		string? failure = null;
		int written = 0;
		string? size = null;
		if (_recorder != null)
		{
			failure = await _recorder.Stop();
			written = _recorder.Frames;
			size = $"{_recorder.Size.X}x{_recorder.Size.Y}";
			_recorder = null;
		}
		double wallSeconds = (endUtc - _clipStartUtc).TotalSeconds;
		_clips.Add(new
		{
			name = _clipName,
			startFrame = _clipStartFrame,
			endFrame,
			startUtc = _clipStartUtc.ToString("O"),
			endUtc = endUtc.ToString("O"),
			framesWritten = written,
			size,
			wallSeconds
		});
		if (failure != null)
		{
			Error($"Clip {_clipName}: {failure}");
		}
		else
		{
			Note($"Clip {_clipName} done: {endFrame - _clipStartFrame} frames drawn, {written} written, {wallSeconds:F1} s on the clock");
		}
		_clipName = null;
	}

	// ---------------------------------------------------------------- setting up a moment

	/// <summary>A fresh fight in an act's scenery (OVERGROWTH, UNDERDOCKS, HIVE, GLORY): ends the current one first.</summary>
	private static async Task TrailerFight(string act, string encounter)
	{
		if (CombatManager.Instance.IsInProgress)
		{
			RunConsole("win");
			await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(20));
			await Wait(0.5);
		}
		if (RunManager.Instance.DebugOnlyGetState()?.Act.Id.Entry != act)
		{
			RunConsole("act " + act);
			await Wait(3.0);
		}
		await Fight(encounter);
		await Wait(0.5);
	}

	/// <summary>The player's turn with this much Energy and Gold, full HP, every enemy healthy.</summary>
	private static async Task TrailerTurn(int energy = 5, int gold = 150)
	{
		Player me = Me;
		await WaitUntil(() => me.PlayerCombatState?.Phase == PlayerTurnPhase.Play || !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(30));
		me.Creature.SetCurrentHpInternal(me.Creature.MaxHp);
		await PlayerCmd.SetGold(gold, me);
		me.PlayerCombatState!.Energy = energy;
		foreach (Creature enemy in me.Creature.CombatState!.HittableEnemies)
		{
			enemy.SetCurrentHpInternal(enemy.MaxHp);
		}
	}

	private static IReadOnlyList<Creature> Enemies => Me.Creature.CombatState!.HittableEnemies.ToList();

	/// <summary>The enemy with the most max HP (the elite or the biggest one).</summary>
	private static Creature BigEnemy => Enemies.OrderByDescending(e => e.MaxHp).First();

	private static void SetHpFraction(Creature creature, double fraction)
	{
		creature.SetCurrentHpInternal(Math.Max(1, (int)Math.Round(creature.MaxHp * fraction)));
	}

	/// <summary>Plays a card from the hand the way a player does (through the action queue), aimed at target.</summary>
	private static void PlayFromHand(string cardId, Creature? target = null)
	{
		CardModel? card = PileType.Hand.GetPile(Me).Cards.FirstOrDefault(c => c.Id.Entry == cardId);
		if (card == null)
		{
			Error($"{cardId} is not in the hand");
			return;
		}
		if (!card.TryManualPlay(target))
		{
			Error($"{cardId} could not be played");
		}
	}

	/// <summary>Replaces the hand with these cards, in this order (left to right). "ID+" adds it upgraded.</summary>
	private static async Task SetHand(params string[] cardIds)
	{
		foreach (CardModel card in PileType.Hand.GetPile(Me).Cards.ToList())
		{
			await CardPileCmd.Add(card, PileType.Discard);
		}
		foreach (string id in cardIds)
		{
			CardModel card = await AddToHand(id.TrimEnd('+'));
			if (id.EndsWith('+') && card.IsUpgradable)
			{
				CardCmd.Upgrade(card);
			}
		}
	}

	// ---------------------------------------------------------------- before the run: the menu and character select

	/// <summary>G01: the Spire from the title screen, with the logo, the menu and every label hidden.</summary>
	private static async Task ShotMenuSpire(Control mainMenu)
	{
		var hidden = new List<CanvasItem>();
		foreach (string path in new[] { "MainMenuTextButtons", "%ChangeProfileButton", "%PatchNotesButton", "%ContinueRunInfo", "%ButtonReticleLeft", "%ButtonReticleRight", "%TimelineNotificationDot" })
		{
			if (mainMenu.GetNodeOrNull<CanvasItem>(path) is CanvasItem item)
			{
				item.Modulate = Colors.Transparent;
				hidden.Add(item);
			}
		}
		NMainMenuBg? bg = Traverse.Create(mainMenu).Field("_bg").GetValue() as NMainMenuBg;
		bg?.HideLogo();
		TrailerUi.HideVersionLabels();
		await Wait(2.0);
		await Record("menu_spire", () => Wait(8.0), preRoll: 0, postRoll: 0);
		bg?.ShowLogo();
		foreach (CanvasItem item in hidden)
		{
			item.Modulate = Colors.White;
		}
	}

	/// <summary>
	/// G03: the character select screen. The roster row (the game's random-character "?" hidden), switching between
	/// The Donald and Sleepy Joe, then each select painting on its own with the screen's chrome hidden.
	/// </summary>
	private static async Task ShotSelect(Control charSelect)
	{
		List<NCharacterSelectButton> buttons = UiHelper.FindAll<NCharacterSelectButton>(charSelect.GetNode("CharSelectButtons/ButtonContainer"));
		foreach (NCharacterSelectButton random in buttons.Where(b => b.Character is RandomCharacter))
		{
			random.Visible = false;
		}
		NCharacterSelectButton donald = buttons.First(b => b.Character.Id.Entry == "TRUMP");
		NCharacterSelectButton joe = buttons.First(b => b.Character.Id.Entry == "BIDEN");
		TrailerUi.HideVersionLabels();
		// The Ascension panel ("Poverty") sits over the roster and the paintings; selecting a character shows it again.
		void HideAscension()
		{
			foreach (NAscensionPanel panel in UiHelper.FindAll<NAscensionPanel>(charSelect))
			{
				panel.Modulate = Colors.Transparent;
			}
		}
		donald.Select();
		HideAscension();
		await Wait(2.5);
		await Record("select_row", async () =>
		{
			joe.Select();
			HideAscension();
			await Wait(2.8);
			donald.Select();
			HideAscension();
			await Wait(2.8);
		}, preRoll: 1.0, postRoll: 0.3);

		var hidden = new List<CanvasItem>();
		foreach (string path in new[] { "InfoPanel", "CharSelectButtons", "BackButton", "ConfirmButton", "UnreadyButton", "%AscensionPanel", "%ActDropdown", "ActLabel", "RemotePlayerContainer", "ReadyAndWaitingPanel" })
		{
			if (charSelect.GetNodeOrNull<CanvasItem>(path) is CanvasItem item)
			{
				item.Modulate = Colors.Transparent;
				hidden.Add(item);
			}
		}
		foreach ((NCharacterSelectButton who, string name) in new[] { (donald, "donald"), (joe, "joe") })
		{
			who.Select();
			HideAscension();
			await Wait(2.5);
			HideAscension();
			await Record($"select_{name}_clean", () => Wait(5.0), preRoll: 0, postRoll: 0);
		}
		foreach (CanvasItem item in hidden)
		{
			item.Modulate = Colors.White;
		}
		foreach (NAscensionPanel panel in UiHelper.FindAll<NAscensionPanel>(charSelect))
		{
			panel.Modulate = Colors.White;
		}
	}

	// ---------------------------------------------------------------- shared shots in a run

	/// <summary>(act, encounter) pairs the probe photographs, to choose each section's scenery and enemies.</summary>
	private static readonly (string Act, string Encounter)[] GalleryFights =
	{
		("OVERGROWTH", "RUBY_RAIDERS_NORMAL"), ("OVERGROWTH", "BYRDONIS_ELITE"), ("OVERGROWTH", "PHROG_PARASITE_ELITE"), ("OVERGROWTH", "VANTOM_BOSS"),
		("UNDERDOCKS", "CULTISTS_NORMAL"), ("UNDERDOCKS", "GREMLIN_MERC_NORMAL"), ("UNDERDOCKS", "TERROR_EEL_ELITE"), ("UNDERDOCKS", "SKULKING_COLONY_ELITE"),
		("HIVE", "EXOSKELETONS_NORMAL"), ("HIVE", "CHOMPERS_NORMAL"), ("HIVE", "ENTOMANCER_ELITE"), ("HIVE", "DECIMILLIPEDE_ELITE"),
		("GLORY", "AXEBOTS_NORMAL"), ("GLORY", "KNIGHTS_ELITE"), ("GLORY", "MECHA_KNIGHT_ELITE"), ("GLORY", "QUEEN_BOSS"),
	};

	/// <summary>The probe: a clean screenshot of each candidate fight (and a full one per act), no video.</summary>
	private static async Task TrailerGallery()
	{
		string? lastAct = null;
		foreach ((string act, string encounter) in GalleryFights)
		{
			try
			{
				await TrailerFight(act, encounter);
				await TrailerTurn();
				if (act != lastAct)
				{
					TrailerUi.Apply(TrailerUi.Look.Full);
					await Wait(1.0);
					Screenshot($"gallery_{act.ToLowerInvariant()}_full_{encounter.ToLowerInvariant()}");
					lastAct = act;
				}
				TrailerUi.Apply(TrailerUi.Look.Clean);
				await Wait(1.0);
				Screenshot($"gallery_{act.ToLowerInvariant()}_{encounter.ToLowerInvariant()}");
				Note($"Gallery {act} {encounter}: {Enemies.Count} enemies ({string.Join(", ", Enemies.Select(e => $"{e.Monster?.Id.Entry} {e.MaxHp} HP"))})");
			}
			catch (Exception e)
			{
				Error($"Gallery {act} {encounter}: {e.Message}");
			}
			TrailerUi.Apply(TrailerUi.Look.Full);
		}
	}

	/// <summary>
	/// G18: every card of every mod character, base and upgraded, rendered by the game at 3x on a transparent
	/// background (cards/&lt;character&gt;/&lt;card&gt;.png and _plus.png), for the card callouts and the collection beat.
	/// </summary>
	private static async Task TrailerCardRenders()
	{
		const float scale = 3f;
		var size = new Vector2I(1000, 1360);
		var viewport = new SubViewport { Size = size, TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
		Tree.Root.AddChild(viewport);
		int count = 0;
		foreach (CharacterModel character in ModContent.Characters)
		{
			string dir = Path.Combine(_outDir, "cards", character.Id.Entry.ToLowerInvariant());
			Directory.CreateDirectory(dir);
			IEnumerable<CardModel> cards = character.CardPool.AllCards.Concat(KitFor(character.Id.Entry).ExtraCards?.Invoke() ?? Enumerable.Empty<CardModel>());
			foreach (CardModel canonical in cards.DistinctBy(c => c.Id))
			{
				foreach (bool upgraded in new[] { false, true })
				{
					CardModel model = canonical.ToMutable();
					if (upgraded)
					{
						if (!model.IsUpgradable)
						{
							continue;
						}
						model.UpgradeInternal();
					}
					NCard? node = NCard.Create(model);
					if (node == null)
					{
						continue;
					}
					viewport.AddChild(node);
					node.Scale = Vector2.One * scale;
					node.Position = new Vector2(size.X / 2f, size.Y / 2f);
					await Frames(2);
					node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
					await Frames(3);
					viewport.GetTexture().GetImage().SavePng(Path.Combine(dir, $"{canonical.Id.Entry.ToLowerInvariant()}{(upgraded ? "_plus" : "")}.png"));
					viewport.RemoveChild(node);
					node.QueueFree();
					count++;
				}
			}
		}
		viewport.QueueFree();
		Note($"Rendered {count} card images");
	}
}
