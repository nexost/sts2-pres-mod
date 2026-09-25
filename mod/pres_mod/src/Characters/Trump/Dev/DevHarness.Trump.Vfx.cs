using PresMod.Characters.Trump.Mechanics;
using PresMod.Characters.Trump.Powers;

namespace PresMod.Dev;

/// <summary>
/// test.py vfx -c trump: The Donald's visual effects played in a fight against three enemies, with screenshots at their
/// key moments. Gold, Tariffs, the Wall and Deport go through the real commands (their effects come from what changes);
/// the card effects are called directly.
/// </summary>
public static partial class DevHarness
{
	private static async Task TrumpVfxShowcase()
	{
		await StartRunAsCharacter(screenshots: false);
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature donald = me.Creature;
		ICombatState combat = donald.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();
		await Task.Delay(1500);

		async Task Shots(Task effect, params (int At, string Label)[] shots)
		{
			int elapsed = 0;
			foreach ((int at, string label) in shots)
			{
				await Task.Delay(Math.Max(0, at - elapsed));
				elapsed = at;
				Screenshot(label);
			}
			await effect;
			await Task.Delay(900);
		}

		List<Creature> enemies() => combat.HittableEnemies.ToList();

		// Gold: gaining rains coins onto him, paying throws them out (bigger sums, bigger bursts).
		await PlayerCmd.GainGold(40, me);
		await Task.Delay(350);
		Screenshot("trump_gold_gain");
		await Task.Delay(1200);
		await PlayerCmd.LoseGold(25, me);
		await Task.Delay(250);
		Screenshot("trump_gold_pay");
		await Task.Delay(1200);

		// Tariffs pop a coin over the enemy.
		await PowerCmd.Apply<TariffPower>(ctx, enemies().First(), 2m, donald, null);
		await Task.Delay(300);
		Screenshot("trump_tariff");
		await Task.Delay(1000);

		// Tweets: a blue bubble and blue sparks.
		TrumpVfx.Tweeted(donald);
		await Task.Delay(350);
		Screenshot("trump_tweet");
		await Task.Delay(1400);

		// The Wall: rising to a new stage shakes harder each time; a huge jump (Great Wall) bursts gold.
		await WallCmd.Build(ctx, donald, 26m);
		await Task.Delay(500);
		Screenshot("trump_wall_stage2");
		await Task.Delay(1400);
		await WallCmd.Build(ctx, donald, 46m);
		await Task.Delay(350);
		Screenshot("trump_wall_great_wall_gold");
		await Task.Delay(1600);

		// Cards.
		await Shots(TrumpVfx.FireAndFury(enemies()), (150, "trump_fire_and_fury"));
		await Shots(TrumpVfx.MakeItRain(enemies()), (250, "trump_make_it_rain_1"), (550, "trump_make_it_rain_2"));
		await Shots(TrumpVfx.WreckingBall(enemies()), (180, "trump_wrecking_ball_swing"), (360, "trump_wrecking_ball_hit"));
		await Shots(TrumpVfx.Golf(donald, enemies().Last()), (220, "trump_hole_in_one_flight"), (500, "trump_hole_in_one_land"));
		await Shots(TrumpVfx.Golf(donald, null), (300, "trump_golf_weekend_fore"));
		TrumpVfx.Covfefe(donald);
		await Task.Delay(300);
		Screenshot("trump_covfefe");
		await Task.Delay(1300);
		TrumpVfx.Concrete(donald);
		await Task.Delay(400);
		Screenshot("trump_quick_dry_concrete");
		await Task.Delay(1000);
		TrumpVfx.Shovel(donald);
		await Task.Delay(200);
		Screenshot("trump_golden_shovel");
		await Task.Delay(1000);

		// You're Fired!: the words slam in, then the enemy is Deported (stamp, swoosh, shake).
		Creature fired = enemies().Last();
		await Shots(TrumpVfx.YoureFired(), (120, "trump_youre_fired_slam"), (360, "trump_youre_fired_flash"));
		Task deport = DeportCmd.ForceDeport(ctx, fired, me);
		await Task.Delay(180);
		Screenshot("trump_deport_stamp_swoosh");
		await deport;
		await Task.Delay(600);
		Screenshot("trump_deported_popup");
		await Task.Delay(1200);

		// Chapter 11: losing everything is the jumbo burst.
		int gold = me.Gold;
		await PlayerCmd.LoseGold(gold, me);
		await Task.Delay(300);
		Note($"Chapter 11: Gold {gold} -> {me.Gold}");
		Screenshot("trump_chapter_11_big_loss");
		await Task.Delay(1500);
		Note("vfx showcase done");
	}
}
