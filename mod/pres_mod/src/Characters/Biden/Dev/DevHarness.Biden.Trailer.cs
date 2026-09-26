using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Dev;

/// <summary>
/// Sleepy Joe's trailer shots (DevHarness.Trailer.cs; the treatment's shot list, G09-G14 and the montage). Each is
/// recorded from a fresh fight, as played and (mostly) clean. Scenery: Overgrowth for Sleepy Joe, the dark Underdocks
/// and the Vantom's cave for the red lasers, Hive and Glory for Dark Brandon's cards.
/// </summary>
public static partial class DevHarness
{
	private static Dictionary<string, Func<Task>> BidenTrailerShots() => new Dictionary<string, Func<Task>>
	{
		["joe_doze"] = JoeDoze,
		["joe_tangent"] = JoeTangent,
		["joe_nodoff"] = JoeNodOff,
		["joe_zero"] = JoeZero,
		["joe_wake_cave"] = JoeWakeCave,
		["joe_wake"] = JoeWake,
		["joe_lasershow"] = JoeLaserShow,
		["joe_micdrop"] = JoeMicDrop,
		["joe_motorcade"] = JoeMotorcade,
		["joe_airforce"] = JoeAirForce,
		["joe_sotu"] = JoeSotu,
		// T2's test shot, kept for the capture comparison.
		["biden_wake"] = BidenShotWake,
	};

	/// <summary>A fresh fight as Sleepy Joe with this much Drowsy (or as Dark Brandon), Energy and hand.</summary>
	private static async Task JoeReady(string act, string encounter, int drowsy, bool awake = false, int energy = 5, params string[] hand)
	{
		await TrailerFight(act, encounter);
		await TrailerTurn(energy);
		await BidenResetState(new BlockingPlayerChoiceContext(), awake, drowsy);
		await SetHand(hand);
		await Wait(awake ? 3.0 : 1.2);
	}

	/// <summary>G09: Z's rise and the dusk closes in, 3 to 9 Drowsy.</summary>
	private static Task JoeDoze() => Takes("joe_doze",
		() => JoeReady("OVERGROWTH", "BYRDONIS_ELITE", drowsy: 3, hand: new[] { "CATNAP", "RESTING_MY_EYES", "STRIKE_BIDEN", "DEFEND_BIDEN" }),
		async () =>
		{
			PlayFromHand("CATNAP");
			await Wait(1.6);
			PlayFromHand("RESTING_MY_EYES");
			await Wait(2.6);
		});

	/// <summary>G10: Here's the Deal changes its lit line with every other card played; the speech bubbles. Hand shown.</summary>
	private static Task JoeTangent() => Takes("joe_tangent",
		() => JoeReady("OVERGROWTH", "RUBY_RAIDERS_NORMAL", drowsy: 0, hand: new[] { "HERES_THE_DEAL", "CHOCOLATE_CHIP", "CHOCOLATE_CHIP", "WHERE_WAS_I" }),
		async () =>
		{
			await Wait(0.6);
			PlayFromHand("CHOCOLATE_CHIP");
			await Wait(1.4);
			PlayFromHand("CHOCOLATE_CHIP");
			await Wait(1.4);
			PlayFromHand("WHERE_WAS_I");
			await Wait(1.8);
		}, clean: false);

	/// <summary>
	/// G11 into G12, the whole loop in one take: Lights Out nods him off mid-turn ("Nodded Off", "Enemy Turn"), the
	/// raiders attack him asleep, and his next turn starts with him waking as Dark Brandon, lasers at 11 Drowsy.
	/// </summary>
	private static Task JoeNodOff() => Takes("joe_nodoff",
		() => JoeReady("OVERGROWTH", "RUBY_RAIDERS_NORMAL", drowsy: 7, hand: new[] { "STRIKE_BIDEN", "LIGHTS_OUT", "DEFEND_BIDEN", "STRIKE_BIDEN" }),
		async () =>
		{
			PlayFromHand("LIGHTS_OUT", BigEnemy);
			await Wait(12.0);
		});

	/// <summary>Montage: Zero to Sixty, the '67 peels out into the bird.</summary>
	private static Task JoeZero() => Takes("joe_zero",
		() => JoeReady("OVERGROWTH", "BYRDONIS_ELITE", drowsy: 0, hand: new[] { "ZERO_TO_SIXTY" }),
		async () =>
		{
			PlayFromHand("ZERO_TO_SIXTY", BigEnemy);
			await Wait(2.8);
		});

	private static async Task JoeWakeSetup(string act, string encounter)
	{
		await JoeReady(act, encounter, drowsy: 9, hand: new[] { "STRIKE_BIDEN", "CUP_OF_JOE", "DEFEND_BIDEN" });
		await PowerCmd.Apply<DoubleVisionPower>(new BlockingPlayerChoiceContext(), Me.Creature, 1m, Me.Creature, null);
		await Wait(1.2);
	}

	/// <summary>G12 in the Vantom's dark cave: Cup of Joe, the red burst, Dark Brandon, two red beams (Double Vision).</summary>
	private static Task JoeWakeCave() => Takes("joe_wake_cave",
		() => JoeWakeSetup("OVERGROWTH", "VANTOM_BOSS"),
		async () =>
		{
			PlayFromHand("CUP_OF_JOE");
			await Wait(5.2);
		});

	/// <summary>G12 on the docks, two cultists in the beams.</summary>
	private static Task JoeWake() => Takes("joe_wake",
		() => JoeWakeSetup("UNDERDOCKS", "CULTISTS_NORMAL"),
		async () =>
		{
			PlayFromHand("CUP_OF_JOE");
			await Wait(5.2);
		});

	/// <summary>G13: Laser Show (X=3) sweeps four exoskeletons.</summary>
	private static Task JoeLaserShow() => Takes("joe_lasershow",
		() => JoeReady("HIVE", "EXOSKELETONS_NORMAL", drowsy: 6, energy: 3, hand: new[] { "LASER_SHOW" }),
		async () =>
		{
			PlayFromHand("LASER_SHOW");
			await Wait(4.8);
		});

	/// <summary>G14: Mic Drop as Dark Brandon, a golden meteor onto a chomper.</summary>
	private static Task JoeMicDrop() => Takes("joe_micdrop",
		() => JoeReady("HIVE", "CHOMPERS_NORMAL", drowsy: 0, awake: true, hand: new[] { "MIC_DROP" }),
		async () =>
		{
			PlayFromHand("MIC_DROP", BigEnemy);
			await Wait(2.8);
		});

	/// <summary>G14: the Motorcade plows through the Decimillipede.</summary>
	private static Task JoeMotorcade() => Takes("joe_motorcade",
		() => JoeReady("HIVE", "DECIMILLIPEDE_ELITE", drowsy: 0, energy: 3, hand: new[] { "MOTORCADE" }),
		async () =>
		{
			PlayFromHand("MOTORCADE");
			await Wait(3.2);
		});

	/// <summary>G14: Air Force One's shadow over the knights.</summary>
	private static Task JoeAirForce() => Takes("joe_airforce",
		() => JoeReady("GLORY", "KNIGHTS_ELITE", drowsy: 0, energy: 3, hand: new[] { "AIR_FORCE_ONE" }),
		async () =>
		{
			PlayFromHand("AIR_FORCE_ONE");
			await Wait(3.2);
		});

	/// <summary>The collection's hero card: State of the Union wakes him (9 Drowsy) and hits the Queen's court.</summary>
	private static Task JoeSotu() => Takes("joe_sotu",
		() => JoeReady("GLORY", "QUEEN_BOSS", drowsy: 9, hand: new[] { "STATE_OF_THE_UNION" }),
		async () =>
		{
			PlayFromHand("STATE_OF_THE_UNION");
			await Wait(5.0);
		});

	/// <summary>
	/// T2's test shot (the capture comparison): at 6 Drowsy he plays Catnap (9 of 10), then Cup of Joe: the red
	/// burst, Dark Brandon, Laser Eyes on three raiders.
	/// </summary>
	private static async Task BidenShotWake()
	{
		await Fight(CardFightEncounter);
		await BidenResetState(new BlockingPlayerChoiceContext(), awake: false, drowsy: 6);
		await SetHand("STRIKE_BIDEN", "CATNAP", "CUP_OF_JOE", "HERES_THE_DEAL", "DEFEND_BIDEN");
		await Wait(1.5);
		await Record("biden_wake", async () =>
		{
			PlayFromHand("CATNAP");
			await Wait(1.8);
			PlayFromHand("CUP_OF_JOE");
			await Wait(4.0);
		});
	}
}
