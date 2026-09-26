using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace PresMod.Dev;

/// <summary>
/// The co-op trailer shot (G15, "two presidents, one Spire"), mode "trailer_coop": two instances in one co-op run
/// (scripts/trailer_capture.py coop_both). The host is The Donald and records (--fixed-fps 60, full screen); the
/// client is Sleepy Joe, in a small window on another screen. In the Queen's throne room, twice (as played, then
/// clean): both stand ready, Donald's Coalition Wall raises a Wall in front of each of them, Joe reaches across the
/// aisle (golden lines to Donald), then drinks his Cup of Joe and fires Dark Brandon's lasers past the Walls.
/// Everything goes through networked commands and real card plays, so both instances stay in sync. The client
/// reacts to what it sees in the shared state (its Wall, then its own card resolving), not to a clock: the host
/// draws slower than real time while it records.
/// </summary>
public static partial class DevHarness
{
	/// <summary>The takes of this launch (--pres-looks full,clean). One per launch is the most reliable: between two
	/// fights the client can see the old fight's state for a moment.</summary>
	private static string[] CoopTrailerLooks => (CommandLineHelper.GetValue("pres-looks") ?? "full,clean").Split(',', StringSplitOptions.RemoveEmptyEntries);

	private static async Task RunTrailerCoop()
	{
		bool host = IsCoopHost;
		DisableTutorials();
		Note($"trailer co-op {(host ? "host" : "client")}, character {TestCharacterId}");
		System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_outDir, "clips"));
		RunState run = await CoopEnterRun(host);
		for (int take = 0; take < CoopTrailerLooks.Length; take++)
		{
			if (host)
			{
				await Task.Delay(2000);
				if (run.Act.Id.Entry != "GLORY")
				{
					RunConsole("act GLORY");
					await Task.Delay(4000);
				}
				RunConsole("fight QUEEN_BOSS");
			}
			await WaitUntil(() => CombatManager.Instance.IsInProgress && Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play, TimeSpan.FromSeconds(90));
			await Task.Delay(3000);
			if (host)
			{
				await CoopTrailerHostTake(run, CoopTrailerLooks[take]);
			}
			else
			{
				await CoopTrailerClientTake(run);
			}
		}
		if (host)
		{
			// The client leaves first, so the host doesn't see a dropped connection.
			await Task.Delay(8000);
		}
	}

	private static Player OtherPlayer(RunState run) => run.Players.First(p => p != Me);

	private static bool HasPower(Creature creature, string powerId) => creature.Powers.Any(p => p.Id.Entry == powerId);

	private static async Task CoopTrailerHostTake(RunState run, string look)
	{
		Creature joe = OtherPlayer(run).Creature;
		await PlayCoopCardLater("COALITION_WALL");
		// Joe is ready once his two cards are in his hand and he is 9 Drowsy with Double Vision.
		bool ready = await WaitUntil(() => PileType.Hand.GetPile(OtherPlayer(run)).Cards.Any(c => c.Id.Entry == "CUP_OF_JOE")
			&& HasPower(joe, "DOUBLE_VISION_POWER") && (joe.Powers.FirstOrDefault(p => p.Id.Entry == "DROWSY_POWER")?.Amount ?? 0) >= 9, TimeSpan.FromSeconds(60));
		Check(ready, "Sleepy Joe is set up for the co-op shot");
		TrailerUi.Apply(look == "clean" ? TrailerUi.Look.Clean : TrailerUi.Look.Full);
		await Wait(1.0);
		await Record($"coop_both_{look}", async () =>
		{
			await Wait(2.2);
			PlayFromHand("COALITION_WALL");
			// Joe reacts to his Wall: Reach Across the Aisle, then Cup of Joe. Film until the lasers are done.
			await WaitUntil(() => HasPower(joe, "DARK_BRANDON_POWER"), TimeSpan.FromSeconds(60));
			await Wait(4.5);
		}, preRoll: 0.2, postRoll: 0.4);
		TrailerUi.Apply(TrailerUi.Look.Full);
		RunConsole("win");
		await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(30));
	}

	private static async Task CoopTrailerClientTake(RunState run)
	{
		Creature me = Me.Creature;
		if (me.CombatState == null || PileType.Hand.GetPile(Me) == null)
		{
			Error("co-op client: no fight to set up");
			return;
		}
		int index = me.CombatState.Creatures.ToList().IndexOf(me);
		RunConsole($"power DROWSY_POWER 9 {index}");
		RunConsole($"power DOUBLE_VISION_POWER 1 {index}");
		await PlayCoopCardLater("REACH_ACROSS_THE_AISLE");
		await PlayCoopCardLater("CUP_OF_JOE");
		// Coalition Wall reached me: my own Wall.
		await WaitUntil(() => HasPower(me, "WALL_POWER"), TimeSpan.FromSeconds(120));
		await Task.Delay(1600);
		PlayFromHand("REACH_ACROSS_THE_AISLE");
		await WaitUntil(() => !PileType.Hand.GetPile(Me).Cards.Any(c => c.Id.Entry == "REACH_ACROSS_THE_AISLE"), TimeSpan.FromSeconds(20));
		await Task.Delay(2400);
		PlayFromHand("CUP_OF_JOE");
		await WaitUntil(() => !CombatManager.Instance.IsInProgress, TimeSpan.FromSeconds(180));
	}

	/// <summary>Puts a card in this player's hand with the networked console command, to be played later.</summary>
	private static async Task PlayCoopCardLater(string cardId)
	{
		int before = PileType.Hand.GetPile(Me).Cards.Count(c => c.Id.Entry == cardId);
		RunConsole($"card {cardId} hand");
		if (!await WaitUntil(() => PileType.Hand.GetPile(Me).Cards.Count(c => c.Id.Entry == cardId) > before, TimeSpan.FromSeconds(10)))
		{
			Error($"{cardId} never reached the hand");
		}
	}
}
