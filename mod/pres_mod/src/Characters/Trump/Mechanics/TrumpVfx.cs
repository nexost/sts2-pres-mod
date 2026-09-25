using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using PresMod.Framework;

namespace PresMod.Characters.Trump.Mechanics;

/// <summary>
/// The Donald's visuals (design.md "Visual effects"), besides the Wall and Deport ones in Nodes/NTrumpCombatUi. Built from
/// the game's own effects and Framework/VfxKit:
///   Gold: paying it throws coins out of him (the game's coin explosions, bigger for bigger sums); gaining it rains coins
///     onto him; Tariffs pop a gold coin over the enemy (NTrumpCombatUi watches Gold and Tariff and calls these);
///   Tweets: a blue speech bubble with one of his words ("SAD!", "HUGE!", ...);
///   cards: You're Fired! (the words slam across the screen, with a red flash), Fire and Fury (fire bursts on every enemy),
///     Make It Rain (gold coins pour down on the enemies), Wrecking Ball (a ball on a chain swings in), Hole in One and
///     Golf Weekend (a golf ball), Great Wall (a gold burst at the Wall);
///   potions and relics: the Covfefe splash, the Quick-Dry Concrete puff, the Golden Shovel's gold sparkles.
/// Only visuals: nothing here changes the game, so co-op stays in sync. Skipped without a combat room.
/// </summary>
public static class TrumpVfx
{
	private const int TweetLines = 8;

	public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);

	private static readonly Dictionary<Creature, ulong> LastBubble = new Dictionary<Creature, ulong>();
	private static readonly Random VisualRandom = new Random();

	private static NCreature? NodeOf(Creature creature) => NCombatRoom.Instance?.GetCreatureNode(creature);

	private static Texture2D? Coin() => CharacterArt.Load(CharacterArt.Dir("TRUMP") + "ui/gold_cost_icon.png");

	private static Gradient Golds() => VfxKit.Palette(Colors.White, new Color(1f, 0.93f, 0.7f), Colors.White);

	// ---------------------------------------------------------------- Gold and Tariffs

	/// <summary>His Gold changed: coins burst out of him when he pays, coins rain onto him when he gains.</summary>
	public static void GoldChanged(Creature donald, int change)
	{
		if (change == 0 || NodeOf(donald) is not NCreature node || NCombatRoom.Instance is not NCombatRoom room)
		{
			return;
		}
		Rect2 figure = VfxKit.Figure(node);
		if (change < 0)
		{
			VfxCmd.PlayVfx(figure.GetCenter(), -change >= 20 ? "vfx/vfx_coin_explosion_regular" : "vfx/vfx_coin_explosion_small", room.CombatVfxContainer);
			// A fortune going out (Chapter 11, You're Fired!): his own coins spray everywhere too.
			if (-change >= 60 && Coin() is Texture2D spent)
			{
				VfxKit.Burst(figure.GetCenter(), spent, Golds(), count: Math.Clamp(-change / 4, 15, 60), speed: 900f, gravity: 1400f,
					lifetime: 1.3f, spread: 80f, scale: figure.Size.Y / 800f);
			}
			return;
		}
		if (Coin() is Texture2D coin)
		{
			VfxKit.Burst(new Vector2(figure.GetCenter().X, figure.Position.Y - figure.Size.Y * 0.35f), coin, Golds(),
				count: Math.Clamp(6 + change / 2, 6, 40), speed: 220f, gravity: 1100f, lifetime: 1.1f, spread: 50f,
				direction: Vector2.Up, scale: figure.Size.Y / 900f);
		}
	}

	/// <summary>A Tariff went up on an enemy: a gold coin pops up over it and spins away.</summary>
	public static void Tariffed(Creature enemy)
	{
		if (NodeOf(enemy) is not NCreature node || Coin() is not Texture2D coin)
		{
			return;
		}
		Rect2 box = node.Hitbox.GetGlobalRect();
		var sprite = new Sprite2D { Texture = coin, Scale = Vector2.One * 0.2f };
		if (VfxKit.Add(sprite) == null)
		{
			return;
		}
		sprite.GlobalPosition = new Vector2(box.GetCenter().X, box.Position.Y);
		Tween tween = sprite.CreateTween();
		tween.TweenProperty(sprite, "scale", Vector2.One * 1.2f, 0.18).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(sprite, "global_position:y", box.Position.Y - 70f, 0.5).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(sprite, "scale:x", -1.2f, 0.2);
		tween.TweenProperty(sprite, "scale:x", 1.2f, 0.2);
		tween.TweenProperty(sprite, "modulate:a", 0f, 0.3);
		VfxKit.FreeAfter(sprite, 1.3f);
	}

	// ---------------------------------------------------------------- Tweets

	/// <summary>A Tweet was played: now and then (at most every couple of seconds), a blue bubble with one of his words.</summary>
	public static void Tweeted(Creature donald)
	{
		ulong now = Time.GetTicksMsec();
		if (NodeOf(donald) is not NCreature node || (LastBubble.TryGetValue(donald, out ulong last) && now - last < 2500))
		{
			return;
		}
		LastBubble[donald] = now;
		string text = new LocString("static_hover_tips", $"TRUMP_VFX.tweet{VisualRandom.Next(1, TweetLines + 1)}").GetFormattedText();
		if (NSpeechBubbleVfx.Create(text, donald, 1.1, VfxColor.Blue) is NSpeechBubbleVfx bubble)
		{
			donald.GetVfxContainer()?.AddChildSafely(bubble);
		}
		Rect2 figure = VfxKit.Figure(node);
		VfxKit.Burst(new Vector2(figure.GetCenter().X, figure.Position.Y + figure.Size.Y * 0.1f), VfxKit.Dot(20, Colors.White),
			VfxKit.Palette(new Color("5ab4ff"), new Color("a9dcff")), count: 14, speed: 260f, gravity: 120f, lifetime: 0.8f, spread: 90f, additive: true);
	}

	// ---------------------------------------------------------------- Cards

	/// <summary>You're Fired!: the words slam across the screen, the screen flashes red and shakes.</summary>
	public static async Task YoureFired()
	{
		if (NCombatRoom.Instance == null)
		{
			return;
		}
		Rect2 screen = VfxKit.Screen;
		var label = new Label
		{
			Text = new LocString("static_hover_tips", "TRUMP_VFX.fired").GetFormattedText(),
			HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
			Size = new Vector2(screen.Size.X, 240f), MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		if (ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres") is Font font)
		{
			label.AddThemeFontOverride("font", font);
		}
		label.AddThemeFontSizeOverride("font_size", 150);
		label.AddThemeColorOverride("font_color", new Color("f2b92e"));
		label.AddThemeColorOverride("font_outline_color", new Color("3a0806"));
		label.AddThemeConstantOverride("outline_size", 34);
		if (VfxKit.Add(label) == null)
		{
			return;
		}
		label.GlobalPosition = new Vector2(screen.Position.X, screen.Position.Y + screen.Size.Y * 0.3f);
		label.PivotOffset = label.Size * 0.5f;
		label.Scale = Vector2.One * 2.6f;
		label.Modulate = new Color(1f, 1f, 1f, 0f);
		label.RotationDegrees = -6f;
		Tween tween = label.CreateTween();
		tween.TweenProperty(label, "scale", Vector2.One, 0.18).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		tween.Parallel().TweenProperty(label, "modulate:a", 1f, 0.12);
		tween.TweenInterval(0.7);
		tween.TweenProperty(label, "modulate:a", 0f, 0.35);
		VfxKit.FreeAfter(label, 1.5f);
		await Cmd.Wait(0.18f);
		VfxKit.Flash(new Color(0.85f, 0.08f, 0.05f), 0.3f, 0.45f);
		NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
		await Cmd.Wait(0.35f);
	}

	/// <summary>Fire and Fury: fire bursts on every enemy, an orange flash and a big shake.</summary>
	public static async Task FireAndFury(IEnumerable<Creature> enemies)
	{
		if (NCombatRoom.Instance is not NCombatRoom room)
		{
			return;
		}
		foreach (Creature enemy in enemies)
		{
			if (NodeOf(enemy) is NCreature node)
			{
				VfxCmd.PlayVfx(node.VfxSpawnPosition, "vfx/vfx_fire_burst", room.CombatVfxContainer);
			}
		}
		VfxKit.Flash(new Color(1f, 0.45f, 0.05f), 0.25f, 0.5f);
		NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal);
		await Cmd.Wait(0.3f);
	}

	/// <summary>Make It Rain: gold coins pour down from the top of the screen onto every enemy.</summary>
	public static async Task MakeItRain(IEnumerable<Creature> enemies)
	{
		if (Coin() is not Texture2D coin)
		{
			return;
		}
		Rect2 screen = VfxKit.Screen;
		foreach (Creature enemy in enemies)
		{
			if (NodeOf(enemy) is not NCreature node)
			{
				continue;
			}
			Rect2 box = node.Hitbox.GetGlobalRect();
			var rain = new CpuParticles2D
			{
				Amount = 36, Lifetime = 1.0, OneShot = true, Explosiveness = 0.25f, Texture = coin, LocalCoords = false,
				EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(box.Size.X * 0.55f, 10f),
				Direction = Vector2.Down, Spread = 8f, Gravity = new Vector2(0f, 1600f), InitialVelocityMin = 300f, InitialVelocityMax = 500f,
				ScaleAmountMin = 0.35f, ScaleAmountMax = 0.6f, AngleMin = 0f, AngleMax = 360f, AngularVelocityMin = -360f, AngularVelocityMax = 360f,
			};
			if (VfxKit.Add(rain) == null)
			{
				return;
			}
			rain.GlobalPosition = new Vector2(box.GetCenter().X, screen.Position.Y - 30f);
			rain.Emitting = true;
			VfxKit.FreeAfter(rain, 2.2f);
		}
		await Cmd.Wait(0.45f);
	}

	/// <summary>Wrecking Ball: a ball on a chain swings down from above into the enemies, with rubble and a big shake.</summary>
	public static async Task WreckingBall(IReadOnlyList<Creature> enemies)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		List<NCreature> nodes = enemies.Select(NodeOf).OfType<NCreature>().ToList();
		if (room == null || nodes.Count == 0)
		{
			return;
		}
		Rect2 screen = VfxKit.Screen;
		Vector2 hit = new Vector2(nodes.Average(n => n.VfxSpawnPosition.X), nodes.Average(n => n.VfxSpawnPosition.Y));
		var pivot = new Node2D { Name = "WreckingBall" };
		Vector2 top = new Vector2(hit.X - screen.Size.X * 0.12f, screen.Position.Y - 60f);
		float arm = top.DistanceTo(hit);
		float ballSize = Math.Min(260f, screen.Size.Y * 0.2f);
		var chain = new Line2D { Points = new[] { Vector2.Zero, new Vector2(0f, arm) }, Width = 10f, DefaultColor = new Color(0.25f, 0.25f, 0.28f) };
		var ball = new Sprite2D { Texture = VfxKit.Ball(128, new Color(0.3f, 0.3f, 0.33f)), Scale = Vector2.One * (ballSize / 128f), Position = new Vector2(0f, arm) };
		pivot.AddChild(chain);
		pivot.AddChild(ball);
		if (VfxKit.Add(pivot) == null)
		{
			return;
		}
		pivot.GlobalPosition = top;
		float rest = Mathf.RadToDeg((hit - top).Angle()) - 90f;
		pivot.RotationDegrees = rest + 75f;
		Tween tween = pivot.CreateTween();
		tween.TweenProperty(pivot, "rotation_degrees", rest, 0.32).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(pivot, "rotation_degrees", rest - 18f, 0.25).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(pivot, "modulate:a", 0f, 0.3);
		VfxKit.FreeAfter(pivot, 1.2f);
		await Cmd.Wait(0.32f);
		foreach (NCreature node in nodes)
		{
			VfxCmd.PlayVfx(node.VfxSpawnPosition, "vfx/vfx_rock_shatter", room.CombatVfxContainer);
		}
		NGame.Instance?.ScreenShake(ShakeStrength.TooMuch, ShakeDuration.Short);
	}

	/// <summary>A golf ball arcs from him: onto a target (Hole in One: a gold burst where it lands) or off the screen (Golf Weekend: "FORE!").</summary>
	public static async Task Golf(Creature donald, Creature? target)
	{
		if (NodeOf(donald) is not NCreature node)
		{
			return;
		}
		Rect2 figure = VfxKit.Figure(node);
		Vector2 from = new Vector2(figure.End.X, figure.End.Y - figure.Size.Y * 0.08f);
		Vector2 to = target != null && NodeOf(target) is NCreature t ? t.VfxSpawnPosition : new Vector2(VfxKit.Screen.End.X + 80f, VfxKit.Screen.Position.Y + 120f);
		if (target == null && NSpeechBubbleVfx.Create(new LocString("static_hover_tips", "TRUMP_VFX.fore").GetFormattedText(), donald, 1.0) is NSpeechBubbleVfx bubble)
		{
			donald.GetVfxContainer()?.AddChildSafely(bubble);
		}
		var ball = new Sprite2D { Texture = VfxKit.Ball(48, new Color(0.97f, 0.97f, 0.97f)), Scale = Vector2.One * 0.55f };
		var trail = new CpuParticles2D
		{
			Amount = 30, Lifetime = 0.35, Texture = VfxKit.Dot(24, Colors.White), LocalCoords = false, Gravity = Vector2.Zero,
			InitialVelocityMin = 0f, InitialVelocityMax = 0f, ScaleAmountMin = 0.5f, ScaleAmountMax = 0.7f,
			ColorRamp = new Gradient { Colors = new[] { new Color(1f, 1f, 1f, 0.6f), new Color(1f, 1f, 1f, 0f) } },
		};
		ball.AddChild(trail);
		if (VfxKit.Add(ball) == null)
		{
			return;
		}
		ball.GlobalPosition = from;
		float seconds = target != null ? 0.45f : 0.8f;
		float apex = Math.Min(from.Y, to.Y) - VfxKit.Screen.Size.Y * 0.28f;
		Tween tween = ball.CreateTween();
		tween.TweenMethod(Callable.From<float>(k =>
		{
			if (GodotObject.IsInstanceValid(ball))
			{
				float x = Mathf.Lerp(from.X, to.X, k);
				float y = (1 - k) * (1 - k) * from.Y + 2 * (1 - k) * k * apex + k * k * to.Y;
				ball.GlobalPosition = new Vector2(x, y);
			}
		}), 0f, 1f, seconds);
		VfxKit.FreeAfter(ball, seconds + 0.4f);
		await Cmd.Wait(seconds);
		if (target != null)
		{
			VfxKit.Burst(to, VfxKit.Dot(22, Colors.White), VfxKit.Palette(Gold, Colors.White), count: 24, speed: 480f, additive: true);
		}
	}

	/// <summary>Great Wall: a burst of gold off the top of his Wall.</summary>
	public static void GoldBurst(Vector2 at, int count = 34) =>
		VfxKit.Burst(at, VfxKit.Dot(22, Colors.White), VfxKit.Palette(Gold, new Color(1f, 0.95f, 0.7f), Colors.White), count: count, speed: 600f, additive: true);

	// ---------------------------------------------------------------- Potions and relics

	/// <summary>Covfefe: a coffee-brown splash and a bubble.</summary>
	public static void Covfefe(Creature donald)
	{
		if (NodeOf(donald) is not NCreature node)
		{
			return;
		}
		Rect2 figure = VfxKit.Figure(node);
		VfxKit.Burst(new Vector2(figure.GetCenter().X, figure.Position.Y + figure.Size.Y * 0.2f), VfxKit.Dot(26, Colors.White),
			VfxKit.Palette(new Color("5a3217"), new Color("8a5a2b"), new Color("c8894a")), count: 30, speed: 420f, gravity: 1000f);
		if (NSpeechBubbleVfx.Create(new LocString("static_hover_tips", "TRUMP_VFX.covfefe").GetFormattedText(), donald, 1.0) is NSpeechBubbleVfx bubble)
		{
			donald.GetVfxContainer()?.AddChildSafely(bubble);
		}
	}

	/// <summary>Quick-Dry Concrete: a grey puff of concrete dust at the target's feet.</summary>
	public static void Concrete(Creature target)
	{
		if (NodeOf(target) is NCreature node)
		{
			Rect2 figure = VfxKit.Figure(node);
			VfxKit.Puffs(new Vector2(figure.End.X + figure.Size.X * 0.3f, figure.End.Y), new Vector2(figure.Size.X * 0.35f, 8f),
				new Color(0.72f, 0.72f, 0.7f, 0.7f), count: 20, seconds: 0.4f, rise: 70f, size: 1.4f);
		}
	}

	/// <summary>The Golden Shovel digs: gold sparkles at his feet.</summary>
	public static void Shovel(Creature donald)
	{
		if (NodeOf(donald) is NCreature node)
		{
			Rect2 figure = VfxKit.Figure(node);
			GoldBurst(new Vector2(figure.End.X, figure.End.Y), 16);
		}
	}
}
