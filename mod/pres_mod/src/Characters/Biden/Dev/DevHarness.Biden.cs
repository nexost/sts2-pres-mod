using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Models.Relics;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using PresMod.Characters.Biden.Cards;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Relics;

namespace PresMod.Dev;

/// <summary>
/// Sleepy Joe's test hooks (see Dev/CharacterTests.cs): Drowsy, nodding off, Dark Brandon and Laser Eyes, Tangent cards
/// and the Aviator Shades in the ui test. Characters/Trump/Dev/DevHarness.Trump.cs is the full example.
/// </summary>
public static partial class DevHarness
{
	[CharacterTestKit("BIDEN")]
	private static CharacterTests BidenTests()
	{
		BidenCountEvents();
		return new CharacterTests
		{
			UiChecks = BidenMechanicsChecks,
			RelicChecks = BidenRelicChecks,
			PotionChecks = BidenPotionChecks,
			PowerChecks = BidenPowerChecks,
			// Base versions play as Sleepy Joe with 3 Drowsy, upgraded versions as Dark Brandon, on a clean slate of powers.
			PrepareCardTurnFor = async (ctx, card, upgraded) => await BidenResetState(ctx, awake: upgraded, drowsy: 3),
			Snapshot = BidenSnapshot,
			CheckCardEffect = BidenCheckCardEffect
		};
	}

	// ---------------------------------------------------------------- ui test: the mechanics fight

	private static async Task BidenMechanicsChecks()
	{
		Player me = Me;
		Creature joe = me.Creature;
		ICombatState combat = joe.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();
		Creature enemy = combat.HittableEnemies.First();
		int lasers = 0;
		int laserDamage = 0;
		DrowsyCmd.LaserFired += (owner, damage, hits) =>
		{
			if (owner == joe)
			{
				lasers += hits;
				laserDamage = damage;
			}
		};

		Check(me.Character.StartingHp == 70 && joe.MaxHp == 70, $"Max HP {joe.MaxHp} (expected 70)");
		Check(me.Relics.Any(r => r is AviatorShades), "Starter relic: Aviator Shades");
		Check(DrowsyCmd.GetDrowsy(joe) == 0 && !DrowsyCmd.IsDarkBrandon(joe), $"Turn 1: {DrowsyCmd.GetDrowsy(joe)} Drowsy, Sleepy Joe");

		// The navy frame next to the Defect's (the closest base-game colour).
		RunConsole("energy 9");
		await AddToHand("STRIKE_DEFECT");
		Screenshot("frame_vs_defect");

		// Tangent: Here's the Deal starts on line 1, moves on after another card, and only the lit line happens.
		var deal = (TangentCard)await AddToHand("HERES_THE_DEAL");
		Check(deal.Line == 1, $"Here's the Deal enters the hand on line {deal.Line} (expected 1)");
		Screenshot("tangent_line1");
		CardModel strike = await AddToHand("STRIKE_BIDEN");
		await CardCmd.AutoPlay(ctx, strike, enemy);
		await Task.Delay(800);
		Check(deal.Line == 2, $"After a Strike, Here's the Deal is on line {deal.Line} (expected 2)");
		string text = deal.GetDescriptionForPile(PileType.Hand);
		Check(text.Contains("[gold](2)[/gold]") && text.Contains("[color=#8b8778](1)") && text.Contains("[color=#8b8778](3)"),
			"Line 2 is lit in the card text, lines 1 and 3 are dimmed");
		Screenshot("tangent_line2");
		await BidenInspect(deal, "tangent_line2_card");
		int block = joe.Block;
		int enemyHp = enemy.CurrentHp;
		await CardCmd.AutoPlay(ctx, deal, enemy);
		await Task.Delay(800);
		Check(joe.Block == block + 7 && enemy.CurrentHp == enemyHp, $"Line 2 only: Block {block} -> {joe.Block} (expected +7), enemy HP {enemyHp} -> {enemy.CurrentHp} (expected unchanged)");

		// Doze, then nodding off mid-turn: 7 Drowsy + Catnap's Doze 3 = 10. The turn ends; the Aviator Shades add 8 Block.
		await DrowsyCmd.Doze(ctx, joe, 7m);
		await Task.Delay(600);
		Check(DrowsyCmd.GetDrowsy(joe) == 7 && !DrowsyCmd.HasNoddedOff(joe), $"Doze 7: Drowsy {DrowsyCmd.GetDrowsy(joe)}, still awake");
		Screenshot("drowsy_7");
		int round = combat.RoundNumber;
		block = joe.Block;
		CardModel nap = await AddToHand("CATNAP");
		await CardCmd.AutoPlay(ctx, nap, null);
		await Task.Delay(300);
		Check(DrowsyCmd.HasNoddedOff(joe), $"Catnap took Drowsy to {DrowsyCmd.GetDrowsy(joe)}: nodded off");
		Check(joe.Block == block + 6 + 8, $"Catnap 6 + Aviator Shades 8 Block ({block} -> {joe.Block})");
		Screenshot("nodded_off");
		bool ended = await WaitUntil(() => combat.RoundNumber > round || me.PlayerCombatState!.Phase != PlayerTurnPhase.Play, TimeSpan.FromSeconds(10));
		Check(ended, "Nodding off ended the turn");

		// Next turn: he wakes up as Dark Brandon, Laser Eyes hit for his 10 Drowsy, and Drowsy resets.
		await WaitUntil(() => combat.RoundNumber > round && me.PlayerCombatState!.Phase == PlayerTurnPhase.Play, TimeSpan.FromSeconds(30));
		await Task.Delay(1500);
		Check(DrowsyCmd.IsDarkBrandon(joe) && !DrowsyCmd.HasNoddedOff(joe), "Woke up as Dark Brandon at the start of the turn");
		Check(lasers == 1 && laserDamage == 10, $"Laser Eyes fired {lasers} time(s) for {laserDamage} (expected once, 10)");
		Check(DrowsyCmd.GetDrowsy(joe) == 0, $"Drowsy reset after waking ({DrowsyCmd.GetDrowsy(joe)})");
		Screenshot("dark_brandon_turn");

		// As Dark Brandon a Tangent does all its lines: damage, Block and draw 2.
		if (CombatManager.Instance.IsInProgress && enemy.IsAlive)
		{
			RunConsole("energy 9");
			enemy.SetCurrentHpInternal(Math.Min(enemy.MaxHp, Math.Max(enemy.CurrentHp, 30)));
			var deal2 = (TangentCard)await AddToHand("HERES_THE_DEAL");
			Screenshot("tangent_all_lines");
			await BidenInspect(deal2, "tangent_all_lines_card");
			int hand = PileType.Hand.GetPile(me).Cards.Count;
			int hp = enemy.CurrentHp + enemy.Block;
			block = joe.Block;
			await CardCmd.AutoPlay(ctx, deal2, enemy);
			await Task.Delay(1200);
			Check(enemy.CurrentHp + enemy.Block <= hp - 8, $"All lines: dealt 8 ({hp} -> {enemy.CurrentHp + enemy.Block} HP + Block)");
			Check(joe.Block == block + 7, $"All lines: gained 7 Block ({block} -> {joe.Block})");
			Check(PileType.Hand.GetPile(me).Cards.Count == hand - 1 + 2, $"All lines: drew 2 ({hand} -> {PileType.Hand.GetPile(me).Cards.Count}, the card itself left)");

			// Drowsy can't build while he's awake.
			await DrowsyCmd.Doze(ctx, joe, 5m);
			Check(DrowsyCmd.GetDrowsy(joe) == 0, $"Doze does nothing as Dark Brandon (Drowsy {DrowsyCmd.GetDrowsy(joe)})");
		}

		// End of the Dark Brandon turn: back to Sleepy Joe, with no end-of-turn Doze on an awake turn.
		if (!await BidenPassTurn(combat))
		{
			return;
		}
		Check(!DrowsyCmd.IsDarkBrandon(joe), "Back to Sleepy Joe after the Dark Brandon turn");
		Check(DrowsyCmd.GetDrowsy(joe) == 0, $"No end-of-turn Doze after a Dark Brandon turn (Drowsy {DrowsyCmd.GetDrowsy(joe)})");

		// A Sleepy Joe turn ends with the Aviator Shades' Doze 2.
		if (!await BidenPassTurn(combat))
		{
			return;
		}
		Check(DrowsyCmd.GetDrowsy(joe) == 2, $"Aviator Shades: Doze 2 at the end of a Sleepy Joe turn (Drowsy {DrowsyCmd.GetDrowsy(joe)})");

		// Wake Up right away (the Wake Up cards come in C4): Laser Eyes for the 2 Drowsy he has.
		lasers = 0;
		Task wake = DrowsyCmd.WakeUp(ctx, joe);
		await Task.Delay(550);
		Screenshot("laser_eyes_beam");
		await wake;
		await Task.Delay(1200);
		Check(DrowsyCmd.IsDarkBrandon(joe) && lasers == 1 && laserDamage == 2, $"Wake Up: Dark Brandon, Laser Eyes for {laserDamage} (expected 2)");
		Screenshot("wake_up_laser");

		// Touch of Orobas maps the starter to its Ancient version.
		var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
		RelicModel upgraded = touch.GetUpgradedStarterRelic(ModelDb.Relic<AviatorShades>());
		Check(upgraded is DarkAviators, $"Touch of Orobas: Aviator Shades -> {upgraded.Id.Entry}");
	}

	/// <summary>
	/// Raise a card in the hand as the mouse would, for a screenshot of its text, then lower it. (The inspect screen draws
	/// a copy outside the hand, so it can't show a Tangent's lit line.)
	/// </summary>
	private static async Task BidenInspect(CardModel card, string label)
	{
		NHandCardHolder? holder = NPlayerHand.Instance?.ActiveHolders.FirstOrDefault(h => h.CardNode?.Model == card);
		MethodInfo? focus = typeof(NCardHolder).GetMethod("OnFocus", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo? unfocus = typeof(NCardHolder).GetMethod("OnUnfocus", BindingFlags.Instance | BindingFlags.NonPublic);
		if (holder == null || focus == null || unfocus == null)
		{
			Note($"Couldn't raise {card.Id.Entry} in the hand for a screenshot");
			return;
		}
		focus.Invoke(holder, null);
		await Task.Delay(700);
		Screenshot(label);
		unfocus.Invoke(holder, null);
		await Task.Delay(400);
	}

	/// <summary>End the turn and wait for the next one; false (with an error) if the fight ended or it never came back.</summary>
	private static async Task<bool> BidenPassTurn(ICombatState combat)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			Note("The fight ended before the rest of the checks");
			return false;
		}
		BidenKeepEnemyAlive(combat);
		if (!await PassTurn(combat))
		{
			Error("The next turn never came");
			return false;
		}
		await Task.Delay(1200);
		return true;
	}

	/// <summary>The checks need a few turns: keep the first enemy from dying to chip damage.</summary>
	private static void BidenKeepEnemyAlive(ICombatState combat)
	{
		Creature? enemy = combat.HittableEnemies.FirstOrDefault();
		enemy?.SetCurrentHpInternal(Math.Min(enemy.MaxHp, Math.Max(enemy.CurrentHp, 30)));
	}
}
