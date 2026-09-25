using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Dev;

/// <summary>
/// Sleepy Joe in the co-op test (test.py coop -c biden [IRONCLAD]). Nodding off mid-turn must end only that player's turn,
/// through the game's own end-turn path, and block their plays while the others finish; Reach Across the Aisle must reach
/// every player; and at the start of the next turn, on both instances, every Sleepy Joe who nodded off must wake up as
/// Dark Brandon and fire Laser Eyes. test.py also compares both instances' full state every turn.
/// </summary>
public static partial class DevHarness
{
	private static int _bidenCoopLasersAtStart;

	/// <summary>
	/// Turn 1. The host waits for the other players to end their turn (a second Sleepy Joe by nodding off, anyone else with
	/// End Turn), so their Energy and hand only change through Reach Across the Aisle, then plays it. Every Sleepy Joe
	/// then nods off: Catnap (Doze 3, so the wake-up has a laser) and Sleep In.
	/// </summary>
	private static async Task BidenCoopTurn(bool host, ICombatState combat)
	{
		Player me = Me;
		if (host)
		{
			List<Player> others = RunManager.Instance.DebugOnlyGetState()!.Players.Where(p => p != me).ToList();
			bool ended = await WaitUntil(() => others.All(CombatManager.Instance.IsPlayerReadyToEndTurn), TimeSpan.FromSeconds(60));
			Check(ended, "The other players ended their turn before Reach Across the Aisle");
			await BidenCoopReachAcross(combat, others);
		}

		await PlayCoopCard("CATNAP", null, combat);
		Check(DrowsyCmd.GetDrowsy(me.Creature) == 3, $"Catnap: Drowsy {DrowsyCmd.GetDrowsy(me.Creature)} (expected 3)");
		int round = combat.RoundNumber;
		RunConsole("card SLEEP_IN hand");
		if (!await WaitUntil(() => PileType.Hand.GetPile(me).Cards.Any(c => c.Id.Entry == "SLEEP_IN"), TimeSpan.FromSeconds(10)))
		{
			Error("SLEEP_IN never reached the hand");
			return;
		}
		Check(PileType.Hand.GetPile(me).Cards.Last(c => c.Id.Entry == "SLEEP_IN").TryManualPlay(null), "SLEEP_IN played");
		bool napped = await WaitUntil(() => CombatManager.Instance.IsPlayerReadyToEndTurn(me), TimeSpan.FromSeconds(15));
		Check(napped && DrowsyCmd.HasNoddedOff(me.Creature), "Sleep In: nodded off, and this player's turn ended");
		Check(CombatManager.Instance.PlayerActionsDisabled, "After nodding off, this player can't play more cards");
		if (!host)
		{
			// The host is still playing: nodding off ends this player's turn, not the round.
			Check(combat.RoundNumber == round && CombatManager.Instance.IsInProgress, $"The round waits for the other player (round {round} -> {combat.RoundNumber})");
		}
	}

	/// <summary>Reach Across the Aisle: every other player gains 1 Energy and draws 1; the one who played it breaks even.</summary>
	private static async Task BidenCoopReachAcross(ICombatState combat, List<Player> others)
	{
		Player me = Me;
		RunConsole("card REACH_ACROSS_THE_AISLE hand");
		if (!await WaitUntil(() => PileType.Hand.GetPile(me).Cards.Any(c => c.Id.Entry == "REACH_ACROSS_THE_AISLE"), TimeSpan.FromSeconds(10)))
		{
			Error("REACH_ACROSS_THE_AISLE never reached the hand");
			return;
		}
		await Task.Delay(500);
		var energy = others.Append(me).ToDictionary(p => p.NetId, p => p.PlayerCombatState!.Energy);
		var hand = others.Append(me).ToDictionary(p => p.NetId, p => PileType.Hand.GetPile(p).Cards.Count);
		CardModel reach = PileType.Hand.GetPile(me).Cards.Last(c => c.Id.Entry == "REACH_ACROSS_THE_AISLE");
		Check(reach.TryManualPlay(null), "REACH_ACROSS_THE_AISLE played");
		await WaitForCardToSettle(combat);
		await Task.Delay(1500);
		foreach (Player p in others)
		{
			int e = p.PlayerCombatState!.Energy, h = PileType.Hand.GetPile(p).Cards.Count;
			Check(e == energy[p.NetId] + 1 && h == hand[p.NetId] + 1,
				$"Reach Across the Aisle: player {p.NetId} ({p.Character.Id.Entry}) Energy {energy[p.NetId]} -> {e}, hand {hand[p.NetId]} -> {h} (expected +1, +1)");
		}
		int myEnergy = me.PlayerCombatState!.Energy, myHand = PileType.Hand.GetPile(me).Cards.Count;
		Check(myEnergy == energy[me.NetId] && myHand == hand[me.NetId],
			$"Reach Across the Aisle: the player who played it Energy {energy[me.NetId]} -> {myEnergy}, hand {hand[me.NetId]} -> {myHand} (expected even: paid 1, gained 1; the card left, drew 1)");
	}

	/// <summary>Turn 2: every Sleepy Joe nodded off in turn 1, so each is Dark Brandon now and fired a 3-damage laser.</summary>
	private static Task BidenCoopTurnStart(bool host, ICombatState combat, int turn)
	{
		if (turn == 1)
		{
			BidenCountEvents();
			_bidenCoopLasersAtStart = _bidenLasers;
		}
		else if (turn == 2)
		{
			List<Player> joes = RunManager.Instance.DebugOnlyGetState()!.Players.Where(p => p.Character.Id.Entry == "BIDEN").ToList();
			foreach (Player p in joes)
			{
				Check(DrowsyCmd.IsDarkBrandon(p.Creature) && !DrowsyCmd.HasNoddedOff(p.Creature) && DrowsyCmd.GetDrowsy(p.Creature) == 0,
					$"Turn 2: player {p.NetId} woke up as Dark Brandon (Drowsy {DrowsyCmd.GetDrowsy(p.Creature)})");
			}
			int lasers = _bidenLasers - _bidenCoopLasersAtStart;
			Check(lasers == joes.Count && _bidenLastLaser == 3, $"Turn 2: {lasers} Laser Eyes for {_bidenLastLaser} (expected {joes.Count}, 3 each)");
		}
		return Task.CompletedTask;
	}
}
