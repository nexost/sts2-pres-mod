using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using PresMod.Framework.Patches;

namespace PresMod.Dev;

/// <summary>
/// coop mode: two game instances on this PC play one co-op run (test.py coop). The game's own developer option
/// --fastmp connects them over localhost without Steam: "host_standard" opens a lobby, "join" (--clientId N) joins it.
/// Both pick the character under test, the host starts a fight with a networked console command, and each side plays
/// its own cards (the character's CoopTurn hook, e.g. Trump's Coalition Wall and Trickle Down, then Strikes and
/// Defends). At the start of every turn both record the whole game state: every player (HP, Block, Gold, cards,
/// powers, the character's own counters) and every enemy. test.py compares the two records; any difference is a
/// desync. Then the rest site and the shop show both characters.
/// </summary>
public static partial class DevHarness
{
	private static readonly List<object> _coopStates = new List<object>();

	private const int CoopTurns = 3;

	private static bool IsCoopHost => (CommandLineHelper.GetValue("fastmp") ?? "").StartsWith("host", StringComparison.Ordinal);

	private static async Task RunCoopTest()
	{
		bool host = IsCoopHost;
		Node root = ((SceneTree)Engine.GetMainLoop()).Root;
		DisableTutorials();
		Note($"co-op {(host ? "host" : "client")}, character {TestCharacterId}");

		// fastmp opens the lobby by itself: the host's, or the client's once it has connected.
		NCharacterSelectScreen? select = null;
		await WaitHelper.Until(() => (select = UiHelper.FindFirst<NCharacterSelectScreen>(root)) != null && select.Visible && select.Lobby != null,
			_ct, TimeSpan.FromSeconds(120));
		await Task.Delay(2000);
		if (host)
		{
			// Readying alone would start a one-player run: wait for the client.
			await WaitHelper.Until(() => select!.Lobby.Players.Count >= 2, _ct, TimeSpan.FromSeconds(120));
			Note("client joined the lobby");
		}
		List<NCharacterSelectButton> buttons = UiHelper.FindAll<NCharacterSelectButton>(select!);
		buttons.First(b => b.Character.Id.Entry == TestCharacterId).Select();
		await Task.Delay(2500);
		Screenshot("lobby");
		await UiHelper.Click(select!.GetNode<NButton>("ConfirmButton"));
		await WaitHelper.Until(() => RunManager.Instance.DebugOnlyGetState()?.CurrentRoom != null, _ct, TimeSpan.FromSeconds(90));
		await Task.Delay(5000);
		RunState run = RunManager.Instance.DebugOnlyGetState()!;
		Check(run.Players.Count == 2, $"co-op run started with {run.Players.Count} players ({string.Join(", ", run.Players.Select(p => p.Character.Id.Entry))})");
		Screenshot("run_start");

		// One fight: the host starts it (console commands are networked in co-op).
		if (host)
		{
			await Task.Delay(2000);
			RunConsole("fight " + ModelDb.Acts.First().AllWeakEncounters.First().Id.Entry);
		}
		await WaitHelper.Until(() => CombatManager.Instance.IsInProgress && Me.PlayerCombatState?.Phase == PlayerTurnPhase.Play, _ct, TimeSpan.FromSeconds(60));
		ICombatState combat = Me.Creature.CombatState!;
		await Task.Delay(3000);
		Screenshot("combat");
		for (int turn = 1; turn <= CoopTurns && CombatManager.Instance.IsInProgress; turn++)
		{
			// Both sides record the state before either plays a card this turn.
			RecordCoopState($"turn{turn}", combat);
			await Task.Delay(4000);
			if (turn == 1 && Kit.CoopTurn != null)
			{
				await Kit.CoopTurn(host, combat);
			}
			await PlayBasicCards(combat);
			if (turn == 1)
			{
				Screenshot("combat_turn1");
			}
			if (!await PassTurn(combat))
			{
				Error($"turn {turn}: the next turn never came (the other player may be stuck)");
				break;
			}
			await Task.Delay(1500);
		}

		// The rest site and the shop, with both characters in them. The host moves the party.
		if (host)
		{
			RunConsole("win");
		}
		await WaitHelper.Until(() => !CombatManager.Instance.IsInProgress, _ct, TimeSpan.FromSeconds(30));
		await Task.Delay(3000);
		foreach ((string room, Type roomType, string label) in new[] { ("RestSite", typeof(RestSiteRoom), "rest_site"), ("Shop", typeof(MerchantRoom), "shop") })
		{
			if (host)
			{
				RunConsole("room " + room);
			}
			await WaitHelper.Until(() => RunManager.Instance.DebugOnlyGetState()?.CurrentRoom?.GetType() == roomType, _ct, TimeSpan.FromSeconds(30));
			await Task.Delay(4500);
			int figures = UiHelper.FindAll<Sprite2D>(root).Count(s => s.Name == ArtPatches.SpriteName && s.Texture != null && s.IsVisibleInTree());
			Check(figures >= run.Players.Count(p => ModContent.IsModCharacter(p.Character)),
				$"{room}: {figures} character painting(s) shown for {run.Players.Count} players");
			Screenshot(label);
		}
		// The client leaves first, so the host doesn't see a dropped connection mid-test.
		if (host)
		{
			await Task.Delay(8000);
		}
	}

	/// <summary>
	/// Adds a card to this player's hand (a networked console command) and plays it the way a player does
	/// (TryManualPlay goes through the synced action queue; CardCmd.AutoPlay would only run locally).
	/// </summary>
	private static async Task PlayCoopCard(string cardId, Creature? target, ICombatState combat)
	{
		int before = PileType.Hand.GetPile(Me).Cards.Count(c => c.Id.Entry == cardId);
		RunConsole($"card {cardId} hand");
		if (!await WaitUntil(() => PileType.Hand.GetPile(Me).Cards.Count(c => c.Id.Entry == cardId) > before, TimeSpan.FromSeconds(10)))
		{
			Error($"{cardId} never reached the hand");
			return;
		}
		CardModel card = PileType.Hand.GetPile(Me).Cards.Last(c => c.Id.Entry == cardId);
		Check(card.TryManualPlay(target), $"{cardId} played");
		await WaitForCardToSettle(combat);
		await Task.Delay(1500);
	}

	/// <summary>Plays the Strikes (at the first enemy) and Defends in hand while there is energy, like a player would.</summary>
	private static async Task PlayBasicCards(ICombatState combat)
	{
		for (int i = 0; i < 5 && CombatManager.Instance.IsInProgress; i++)
		{
			CardModel? card = PileType.Hand.GetPile(Me).Cards
				.FirstOrDefault(c => (c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend)) && c.CanPlay(out _, out _));
			if (card == null)
			{
				return;
			}
			Creature? target = card.TargetType == TargetType.AnyEnemy ? combat.HittableEnemies.FirstOrDefault(card.IsValidTarget) : null;
			if (!card.TryManualPlay(target))
			{
				return;
			}
			await WaitForCardToSettle(combat);
		}
	}

	/// <summary>Every player and enemy as both instances should see them. Card lists are sorted: only contents matter.</summary>
	private static void RecordCoopState(string label, ICombatState combat)
	{
		RunState run = RunManager.Instance.DebugOnlyGetState()!;
		var players = new Dictionary<string, object>();
		foreach (Player player in run.Players.OrderBy(p => p.NetId))
		{
			var state = new Dictionary<string, object>
			{
				["character"] = player.Character.Id.Entry,
				["hp"] = player.Creature.CurrentHp,
				["block"] = player.Creature.Block,
				["gold"] = player.Gold,
				["hand"] = string.Join(",", PileType.Hand.GetPile(player).Cards.Select(c => c.Id.Entry + (c.IsUpgraded ? "+" : "")).OrderBy(s => s, StringComparer.Ordinal)),
				["draw"] = PileType.Draw.GetPile(player).Cards.Count,
				["discard"] = PileType.Discard.GetPile(player).Cards.Count,
				["powers"] = string.Join(",", player.Creature.Powers.Select(p => $"{p.Id.Entry}:{p.Amount}").OrderBy(s => s, StringComparer.Ordinal)),
			};
			var extra = new Dictionary<string, decimal>();
			KitFor(player.Character.Id.Entry).Snapshot?.Invoke(player, extra);
			foreach ((string key, decimal value) in extra)
			{
				state[key] = value;
			}
			players[player.NetId.ToString()] = state;
		}
		var enemies = combat.Enemies.Select(e => (object)new Dictionary<string, object>
		{
			["id"] = e.Monster?.Id.Entry ?? e.Name,
			["hp"] = e.CurrentHp,
			["block"] = e.Block,
			["alive"] = e.IsAlive,
			["powers"] = string.Join(",", e.Powers.Select(p => $"{p.Id.Entry}:{p.Amount}").OrderBy(s => s, StringComparer.Ordinal)),
		}).ToList();
		_coopStates.Add(new Dictionary<string, object> { ["label"] = label, ["players"] = players, ["enemies"] = enemies });
		Note($"state {label} recorded");
	}
}
