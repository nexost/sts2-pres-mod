using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Dev;

/// <summary>
/// test.py vfx -c biden: every one of Sleepy Joe's visual effects (design.md §11) played in a fight against three
/// enemies, with screenshots at their key moments. The effects are called directly: they're visuals only, so nothing
/// in the fight changes except what the showcase sets up (Drowsy for the vignette, Laser Focus for the thin beam).
/// </summary>
public static partial class DevHarness
{
	private static async Task BidenVfxShowcase()
	{
		await StartRunAsCharacter(screenshots: false);
		await Fight(CardFightEncounter);
		Player me = Me;
		Creature joe = me.Creature;
		ICombatState combat = joe.CombatState!;
		var ctx = new BlockingPlayerChoiceContext();
		List<Creature> enemies = combat.HittableEnemies.ToList();
		Creature target = enemies.First();
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

		// Drowsy: Z's on each Doze, and the dusk vignette closing in near the line.
		await DrowsyCmd.Doze(ctx, joe, 4m);
		await Task.Delay(300);
		Screenshot("biden_doze_z");
		await DrowsyCmd.Doze(ctx, joe, 4m);
		await Task.Delay(900);
		Screenshot("biden_doze_vignette_8_of_10");

		// Nodding off (the sleeping Z's), then waking as Dark Brandon (burst, then glow and embers).
		BidenVfx.NoddedOff(joe);
		await Task.Delay(1400);
		Screenshot("biden_nodded_off");
		await Shots(BidenVfx.WokeUp(joe), (120, "biden_wake_burst"));
		await Task.Delay(800);
		Screenshot("biden_dark_brandon_aura");

		// Laser Eyes: a normal hit, Double Vision's second hit, and Laser Focus's thinner, brighter beam.
		await Shots(BidenVfx.LaserEyes(joe, enemies, 0), (650, "biden_laser_hit1"));
		await Shots(BidenVfx.LaserEyes(joe, enemies, 1), (650, "biden_laser_double_vision_hit2"));
		await PowerCmd.Apply<LaserFocusPower>(ctx, joe, 2m, joe, null);
		await Shots(BidenVfx.LaserEyes(joe, enemies, 0), (650, "biden_laser_focus"));
		await PowerCmd.Remove<LaserFocusPower>(joe);
		await Shots(BidenVfx.LaserShow(joe, enemies), (120, "biden_laser_show_1"), (260, "biden_laser_show_2"));

		// Tangents: a line moving on (one of his lines), and all lines as Dark Brandon.
		BidenVfx.TangentMoved(joe);
		await Task.Delay(500);
		Screenshot("biden_tangent_bubble");
		await Task.Delay(1500);
		BidenVfx.AllLines(joe, 3);
		await Task.Delay(1400);
		Screenshot("biden_tangent_all_lines");
		await Task.Delay(1200);

		// Cards.
		await Shots(BidenVfx.Snore(joe), (250, "biden_snore"));
		await Shots(BidenVfx.MicDrop(target), (150, "biden_mic_drop_fall"), (330, "biden_mic_drop_impact"));
		await Shots(BidenVfx.AirForceOne(joe), (300, "biden_air_force_one_1"), (480, "biden_air_force_one_2"));
		await Shots(BidenVfx.Vehicle(joe, "train", target, size: 0.95f), (300, "biden_train_1"), (500, "biden_train_2"));
		await Task.Delay(600);
		await Shots(BidenVfx.Vehicle(joe, "limo", target, size: 0.62f), (380, "biden_the_beast"));
		await Task.Delay(600);
		await Shots(BidenVfx.Vehicle(joe, "limo", enemies.Last(), size: 0.55f, seconds: 1.1f, copies: 3), (420, "biden_motorcade_1"), (700, "biden_motorcade_2"));
		await Task.Delay(600);
		await Shots(BidenVfx.Vehicle(joe, "corvette", target, size: 0.5f, seconds: 0.55f, fromHim: true), (60, "biden_zero_to_sixty_1"), (220, "biden_zero_to_sixty_2"));
		await Task.Delay(600);
		_ = BidenVfx.Vehicle(joe, "train", size: 0.8f, seconds: 1.3f, behind: true);
		await Task.Delay(650);
		Screenshot("biden_all_aboard_train_behind");
		await Task.Delay(1200);
		await Shots(BidenVfx.Vehicle(joe, "ice_cream_truck", size: 0.7f, seconds: 1.2f, behind: true), (500, "biden_ice_cream_truck"));
		BidenVfx.Sprinkles(joe);
		await Task.Delay(350);
		Screenshot("biden_sprinkles");
		await Task.Delay(1500);

		// Reach Across the Aisle is co-op only: here the enemies stand in for the allies, to show the golden lines.
		BidenVfx.ReachAcross(joe, enemies);
		await Task.Delay(250);
		Screenshot("biden_reach_across_standin");
		await Task.Delay(1200);

		// Potions.
		BidenVfx.Steam(joe);
		await Task.Delay(700);
		Screenshot("biden_warm_milk_steam");
		await Task.Delay(1200);
		await Shots(BidenVfx.Jitter(joe), (200, "biden_espresso_jitter"));
		BidenVfx.RedFlash();
		await Task.Delay(60);
		Screenshot("biden_dark_roast_flash");
		await Task.Delay(1200);
		BidenVfx.FellAsleep(joe);
		await Task.Delay(1500);
		Note("vfx showcase done");
	}
}
