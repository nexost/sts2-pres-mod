using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Dev;

/// <summary>
/// The Donald's trailer shots (DevHarness.Trailer.cs; the treatment's shot list, G04-G08 and the montage). Each is
/// recorded twice from a fresh fight: as played and clean. Scenery: Glory's castle for the Wall and the gold,
/// Underdocks and Hive for the Deports, Overgrowth for golf.
/// </summary>
public static partial class DevHarness
{
	private static Dictionary<string, Func<Task>> TrumpTrailerShots() => new Dictionary<string, Func<Task>>
	{
		["donald_wall"] = DonaldWall,
		["donald_rain"] = DonaldRain,
		["donald_fired"] = DonaldFired,
		["donald_wrecking"] = DonaldWrecking,
		["donald_chapter11"] = DonaldChapter11,
		["donald_deport"] = DonaldDeport,
		["donald_deport_eel"] = DonaldDeportEel,
		["donald_tweets"] = DonaldTweets,
		["donald_mass"] = DonaldMass,
		["donald_fury"] = DonaldFury,
		["donald_golf"] = DonaldGolf,
	};

	private static async Task DonaldReady(string act, string encounter, int energy = 5, int gold = 150, params string[] hand)
	{
		await TrailerFight(act, encounter);
		await TrailerTurn(energy, gold);
		int height = WallCmd.GetHeight(Me.Creature);
		if (height > 0)
		{
			await WallCmd.LoseHeight(new BlockingPlayerChoiceContext(), Me.Creature, height);
		}
		await SetHand(hand);
		await Wait(1.2);
	}

	/// <summary>A blue Tweet bubble over him with these words (his card effects pick their words at random).</summary>
	private static void DonaldSays(string text)
	{
		Creature donald = Me.Creature;
		if (NSpeechBubbleVfx.Create(text, donald, 1.6, VfxColor.Blue) is NSpeechBubbleVfx bubble)
		{
			donald.GetVfxContainer()?.AddChildSafely(bubble);
		}
	}

	/// <summary>G04: four plays, four stages: fence (14), brick (26), concrete (52), then the Big Beautiful Wall (74).</summary>
	private static Task DonaldWall() => Takes("donald_wall",
		() => DonaldReady("GLORY", "KNIGHTS_ELITE", energy: 6, hand: new[] { "HIRE_CONTRACTORS", "POUR_CONCRETE", "GREAT_WALL", "GOLDEN_ESCALATOR+" }),
		async () =>
		{
			PlayFromHand("HIRE_CONTRACTORS");
			await Wait(1.4);
			PlayFromHand("POUR_CONCRETE");
			await Wait(1.4);
			PlayFromHand("GREAT_WALL");
			await Wait(1.5);
			PlayFromHand("GOLDEN_ESCALATOR");
			await Wait(3.6);
		});

	/// <summary>G05: Make It Rain on three knights.</summary>
	private static Task DonaldRain() => Takes("donald_rain",
		() => DonaldReady("GLORY", "KNIGHTS_ELITE", hand: new[] { "MAKE_IT_RAIN" }),
		async () =>
		{
			PlayFromHand("MAKE_IT_RAIN");
			await Wait(3.2);
		});

	/// <summary>G08: You're Fired! on the biggest knight, then "SAD!".</summary>
	private static Task DonaldFired() => Takes("donald_fired",
		() => DonaldReady("GLORY", "KNIGHTS_ELITE", hand: new[] { "YOURE_FIRED" }),
		async () =>
		{
			PlayFromHand("YOURE_FIRED", BigEnemy);
			await Wait(2.0);
			DonaldSays("SAD!");
			await Wait(2.2);
		});

	/// <summary>Montage: the Wall at 74, then Wrecking Ball swings through all three knights.</summary>
	private static Task DonaldWrecking() => Takes("donald_wrecking",
		async () =>
		{
			await DonaldReady("GLORY", "KNIGHTS_ELITE", energy: 3, hand: new[] { "WRECKING_BALL" });
			await WallCmd.Build(new BlockingPlayerChoiceContext(), Me.Creature, 74);
			await Wait(3.5);
		},
		async () =>
		{
			PlayFromHand("WRECKING_BALL");
			await Wait(3.2);
		});

	/// <summary>Montage: Chapter 11 sprays a fortune of coins.</summary>
	private static Task DonaldChapter11() => Takes("donald_chapter11",
		() => DonaldReady("GLORY", "KNIGHTS_ELITE", gold: 450, hand: new[] { "CHAPTER11" }),
		async () =>
		{
			PlayFromHand("CHAPTER11");
			await Wait(3.0);
		});

	/// <summary>G06: a cultist at the Deport line (DENIED stamp), then Deport: swoosh, "DEPORTED!". The other stays.</summary>
	private static Task DonaldDeport() => Takes("donald_deport",
		async () =>
		{
			await DonaldReady("UNDERDOCKS", "CULTISTS_NORMAL", hand: new[] { "DEPORT" });
			// Right at the Deport line (25%): under it for the DENIED stamp, but above Deport's 7 damage, so it is
			// Deported rather than killed.
			Enemies.First().SetCurrentHpInternal((int)(Enemies.First().MaxHp * 0.25m));
			await Wait(0.8);
		},
		async () =>
		{
			await Wait(0.8);
			PlayFromHand("DEPORT", Enemies.First());
			await Wait(2.8);
		});

	/// <summary>G06, alternate: the Terror Eel elite Deported (ends the fight).</summary>
	private static Task DonaldDeportEel() => Takes("donald_deport_eel",
		async () =>
		{
			await DonaldReady("UNDERDOCKS", "TERROR_EEL_ELITE", hand: new[] { "DEPORT" });
			SetHpFraction(BigEnemy, 0.2);
			await Wait(0.8);
		},
		async () =>
		{
			await Wait(0.8);
			PlayFromHand("DEPORT", BigEnemy);
			await Wait(2.4);
		}, postRoll: 0.6);

	/// <summary>Montage: Tweetstorm, then the Tweets fired off one after another.</summary>
	private static Task DonaldTweets() => Takes("donald_tweets",
		() => DonaldReady("UNDERDOCKS", "CULTISTS_NORMAL", energy: 3, hand: new[] { "TWEETSTORM" }),
		async () =>
		{
			PlayFromHand("TWEETSTORM");
			await Wait(1.3);
			for (int i = 0; i < 4 && PileType.Hand.GetPile(Me).Cards.Any(c => c.Id.Entry == "TWEET"); i++)
			{
				PlayFromHand("TWEET");
				if (i == 2)
				{
					DonaldSays("WINNING!");
				}
				await Wait(0.55);
			}
			await Wait(1.6);
		});

	/// <summary>G07: four exoskeletons at 40%, Mass Deportation doubles the line and sends them all out at once.</summary>
	private static Task DonaldMass() => Takes("donald_mass",
		async () =>
		{
			await DonaldReady("HIVE", "EXOSKELETONS_NORMAL", energy: 3, hand: new[] { "MASS_DEPORTATION" });
			foreach (Creature enemy in Enemies)
			{
				SetHpFraction(enemy, 0.4);
			}
			await Wait(0.8);
		},
		async () =>
		{
			PlayFromHand("MASS_DEPORTATION");
			await Wait(3.0);
		}, postRoll: 0.6);

	/// <summary>Montage: Fire and Fury (X=3) on the exoskeletons.</summary>
	private static Task DonaldFury() => Takes("donald_fury",
		() => DonaldReady("HIVE", "EXOSKELETONS_NORMAL", energy: 3, hand: new[] { "FIRE_AND_FURY" }),
		async () =>
		{
			PlayFromHand("FIRE_AND_FURY");
			await Wait(3.2);
		});

	/// <summary>Montage: Hole in One onto the bird.</summary>
	private static Task DonaldGolf() => Takes("donald_golf",
		() => DonaldReady("OVERGROWTH", "BYRDONIS_ELITE", hand: new[] { "HOLE_IN_ONE" }),
		async () =>
		{
			PlayFromHand("HOLE_IN_ONE", BigEnemy);
			await Wait(3.0);
		});
}
