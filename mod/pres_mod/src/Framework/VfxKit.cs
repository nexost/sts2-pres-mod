using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace PresMod.Framework;

/// <summary>
/// Small combat effects any character can use, built from Godot nodes (no scenes of their own): a screen flash,
/// particle bursts (confetti, sprinkles, coins, steam, dust), a sprite driving across the fight (a train, a limo),
/// a shadow sweeping over the ground (a plane overhead), and a glowing line between two points.
/// Only visuals: nothing here touches game state or the game's random numbers, so co-op stays in sync. Every call
/// does nothing without a combat room (fights without a scene, test mode). Positions are global canvas coordinates.
/// </summary>
public static class VfxKit
{
	private static readonly Random VisualRandom = new Random();

	private static NCombatRoom? Room => NCombatRoom.Instance;

	/// <summary>The part of the fight on screen, in global coordinates.</summary>
	public static Rect2 Screen => Room?.GetViewport().GetVisibleRect() ?? new Rect2(0, 0, 1920, 1080);

	/// <summary>Where a creature is on screen: its painting (NCharacterPoses), or else its hitbox.</summary>
	public static Rect2 Figure(MegaCrit.Sts2.Core.Nodes.Combat.NCreature node) =>
		node.FindChild(Nodes.NCharacterPoses.NodeName, recursive: true, owned: false) is Nodes.NCharacterPoses poses && poses.FigureRect is Rect2 figure
			? figure
			: node.Hitbox.GetGlobalRect();

	/// <summary>A solid round ball, lighter in the middle with a darker rim (a wrecking ball, a golf ball).</summary>
	public static GradientTexture2D Ball(int size, Color color) => new GradientTexture2D
	{
		Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
		Gradient = new Gradient
		{
			Offsets = new[] { 0f, 0.6f, 0.9f, 0.93f },
			Colors = new[] { color.Lightened(0.45f), color, color.Darkened(0.5f), new Color(0f, 0f, 0f, 0f) }
		}
	};

	/// <summary>A soft round spot: the colour in the middle, fading to nothing at the edge.</summary>
	public static GradientTexture2D Dot(int size, Color color) => new GradientTexture2D
	{
		Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
		Gradient = new Gradient { Colors = new[] { color, new Color(color.R, color.G, color.B, 0f) } }
	};

	/// <summary>A small solid rectangle (a sprinkle, a confetti bit, a spark streak).</summary>
	public static GradientTexture2D Bar(int width, int height) => new GradientTexture2D
	{
		Width = width, Height = height, Gradient = new Gradient { Colors = new[] { Colors.White, Colors.White } }
	};

	/// <summary>A colour ramp picking one of these colours per particle (hard steps, no blending).</summary>
	public static Gradient Palette(params Color[] colors)
	{
		var gradient = new Gradient { InterpolationMode = Gradient.InterpolationModeEnum.Constant };
		var offsets = new float[colors.Length];
		for (int i = 0; i < colors.Length; i++)
		{
			offsets[i] = i / (float)colors.Length;
		}
		gradient.Offsets = offsets;
		gradient.Colors = colors;
		return gradient;
	}

	/// <summary>Add an effect node over the fight (or under it: behind the creatures, in front of the background).</summary>
	public static T? Add<T>(T node, bool behind = false) where T : Node
	{
		if (Room is not NCombatRoom room)
		{
			node.QueueFree();
			return null;
		}
		(behind ? room.BackCombatVfxContainer : room.CombatVfxContainer).AddChildSafely(node);
		return node;
	}

	/// <summary>Free a node after a while (effects that end on their own).</summary>
	public static void FreeAfter(Node node, float seconds) => TaskHelper.RunSafely(FreeLater(node, seconds));

	private static async Task FreeLater(Node node, float seconds)
	{
		await Cmd.Wait(seconds);
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFreeSafely();
		}
	}

	/// <summary>
	/// A control covering the whole screen in the effects layer: over the fight, under the cards and the rest of the UI.
	/// (Sized by hand: anchored to the room, it didn't draw.)
	/// </summary>
	public static T? Overlay<T>(T control) where T : Control
	{
		control.MouseFilter = Control.MouseFilterEnum.Ignore;
		if (Add(control) == null)
		{
			return null;
		}
		Rect2 screen = Screen;
		control.GlobalPosition = screen.Position;
		control.Size = screen.Size;
		return control;
	}

	/// <summary>The whole screen flashes a colour, then fades.</summary>
	public static void Flash(Color color, float alpha = 0.35f, float seconds = 0.45f)
	{
		if (Overlay(new ColorRect { Color = new Color(color.R, color.G, color.B, alpha) }) is not ColorRect rect)
		{
			return;
		}
		rect.CreateTween().TweenProperty(rect, "color:a", 0f, seconds).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		FreeAfter(rect, seconds + 0.1f);
	}

	/// <summary>
	/// A one-shot burst of particles from a point: count particles thrown up and out at speed, falling with gravity,
	/// each coloured from <paramref name="colors"/> (confetti, sprinkles, coins, sparks).
	/// </summary>
	public static void Burst(Vector2 at, Texture2D texture, Gradient colors, int count = 40, float speed = 520f,
		float gravity = 900f, float lifetime = 1.4f, float spread = 70f, Vector2? direction = null, bool spin = true, float scale = 1f,
		bool additive = false, bool behind = false)
	{
		var particles = new CpuParticles2D
		{
			Amount = count, Lifetime = lifetime, OneShot = true, Explosiveness = 0.95f, Texture = texture, LocalCoords = false,
			Direction = direction ?? Vector2.Up, Spread = spread, Gravity = new Vector2(0f, gravity),
			InitialVelocityMin = speed * 0.45f, InitialVelocityMax = speed, ColorInitialRamp = colors,
			ScaleAmountMin = scale * 0.6f, ScaleAmountMax = scale * 1.2f,
			AngleMin = 0f, AngleMax = 360f, AngularVelocityMin = spin ? -540f : 0f, AngularVelocityMax = spin ? 540f : 0f,
			DampingMin = 10f, DampingMax = 40f,
			ColorRamp = new Gradient { Offsets = new[] { 0f, 0.75f, 1f }, Colors = new[] { Colors.White, Colors.White, new Color(1f, 1f, 1f, 0f) } },
		};
		if (additive)
		{
			particles.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		}
		if (Add(particles, behind) == null)
		{
			return;
		}
		particles.GlobalPosition = at;
		particles.Emitting = true;
		FreeAfter(particles, lifetime + 0.5f);
	}

	/// <summary>Soft puffs drifting up from an area (steam, smoke, dust), spread over <paramref name="seconds"/>.</summary>
	public static void Puffs(Vector2 at, Vector2 extents, Color color, int count = 14, float seconds = 0.8f, float rise = 90f,
		float size = 1f, bool behind = false)
	{
		var particles = new CpuParticles2D
		{
			Amount = count, Lifetime = 1.4f, OneShot = true, Explosiveness = 1f - Mathf.Clamp(seconds / 1.4f, 0f, 0.95f),
			Texture = Dot(64, Colors.White), LocalCoords = false,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = extents,
			Direction = Vector2.Up, Spread = 25f, Gravity = new Vector2(0f, -rise * 0.4f), InitialVelocityMin = rise * 0.4f, InitialVelocityMax = rise,
			ScaleAmountMin = size * 0.8f, ScaleAmountMax = size * 1.8f,
			ScaleAmountCurve = new Curve { MinValue = 0f, MaxValue = 1f },
			ColorRamp = new Gradient { Offsets = new[] { 0f, 0.2f, 1f }, Colors = new[] { new Color(color, 0f), color, new Color(color, 0f) } },
		};
		particles.ScaleAmountCurve.AddPoint(new Vector2(0f, 0.5f));
		particles.ScaleAmountCurve.AddPoint(new Vector2(1f, 1f));
		if (Add(particles, behind) == null)
		{
			return;
		}
		particles.GlobalPosition = at;
		particles.Emitting = true;
		FreeAfter(particles, 1.4f + seconds + 0.5f);
	}

	/// <summary>
	/// A sprite (a vehicle facing right) drives across the fight from off the left edge to off the right edge, with its
	/// bottom on <paramref name="groundY"/>: speed lines behind it and dust at its wheels. Returns after
	/// <paramref name="untilX"/> is reached (when the hit lands), while it drives on.
	/// </summary>
	public static async Task DriveBy(Texture2D? texture, float height, float groundY, float untilX, float seconds = 0.9f,
		bool behind = false, float startX = float.NaN, int copies = 1, float gap = 40f, Color? dust = null)
	{
		if (texture == null || Room == null)
		{
			return;
		}
		Rect2 screen = Screen;
		float scale = height / texture.GetHeight();
		float width = texture.GetWidth() * scale;
		float convoy = copies * width + (copies - 1) * gap;
		float fromX = float.IsNaN(startX) ? screen.Position.X - convoy : startX;
		float toX = screen.End.X + width;
		var group = new Node2D { Name = "DriveBy" };
		for (int i = 0; i < copies; i++)
		{
			var sprite = new Sprite2D { Texture = texture, Scale = new Vector2(scale, scale), Centered = false,
				Position = new Vector2(-(width + gap) * i - width, -height) };
			group.AddChild(sprite);
			// Speed lines behind each vehicle.
			for (int k = 0; k < 3; k++)
			{
				float y = -height * (0.25f + 0.25f * k);
				var line = new Line2D
				{
					Points = new[] { new Vector2(sprite.Position.X - width * 0.1f, y), new Vector2(sprite.Position.X - width * (0.6f + 0.2f * k), y) },
					Width = height * 0.035f, DefaultColor = new Color(1f, 1f, 1f, 0.45f),
					Gradient = new Gradient { Colors = new[] { new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0f) } },
				};
				group.AddChild(line);
			}
			// Dust kicked up at the rear wheels.
			var wheels = new CpuParticles2D
			{
				Amount = 18, Lifetime = 0.6, Texture = Dot(48, Colors.White), LocalCoords = false,
				Position = new Vector2(sprite.Position.X + width * 0.15f, -height * 0.05f),
				Direction = new Vector2(-1f, -0.4f), Spread = 25f, InitialVelocityMin = 60f, InitialVelocityMax = 160f,
				Gravity = new Vector2(0f, -40f), ScaleAmountMin = height / 520f, ScaleAmountMax = height / 280f,
				ColorRamp = new Gradient { Colors = new[] { dust ?? new Color(0.72f, 0.66f, 0.56f, 0.45f), new Color(0.72f, 0.66f, 0.56f, 0f) } },
			};
			group.AddChild(wheels);
		}
		if (Add(group, behind) == null)
		{
			return;
		}
		group.GlobalPosition = new Vector2(fromX + (copies - 1) * (width + gap) + width, groundY);
		float endX = toX + convoy;
		float distance = endX - group.GlobalPosition.X;
		Tween tween = group.CreateTween();
		tween.TweenProperty(group, "global_position:x", endX, seconds).SetTrans(Tween.TransitionType.Linear);
		FreeAfter(group, seconds + 0.8f);
		float lead = group.GlobalPosition.X;
		float wait = distance > 0 ? Mathf.Clamp((untilX - lead) / distance * seconds, 0f, seconds) : 0f;
		await Cmd.Wait(wait);
	}

	/// <summary>
	/// A shadow sweeps across the ground from left to right: a plane passing overhead (<paramref name="shape"/>, pointing
	/// right, centred on 0,0), squashed onto the floor, with a weak shake as it passes the middle.
	/// </summary>
	public static async Task ShadowPass(Vector2[] shape, float length, float groundY, float seconds = 0.9f)
	{
		if (Room == null)
		{
			return;
		}
		Rect2 screen = Screen;
		// Over everything: a plane's shadow falls on the fighters too.
		var shadow = new Polygon2D { Polygon = shape, Color = new Color(0f, 0f, 0.03f, 0.6f), Scale = new Vector2(length / 400f, length / 400f * 0.45f) };
		if (Add(shadow) == null)
		{
			return;
		}
		shadow.GlobalPosition = new Vector2(screen.Position.X - length, groundY);
		// Wind streaks high up, rushing the same way.
		for (int i = 0; i < 6; i++)
		{
			float y = screen.Position.Y + screen.Size.Y * (0.12f + 0.05f * i);
			float len = screen.Size.X * (0.25f + 0.05f * (i % 3));
			var streak = new Line2D
			{
				Points = new[] { Vector2.Zero, new Vector2(-len, 0f) }, Width = 3f + i % 2 * 2f,
				Gradient = new Gradient { Colors = new[] { new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0f) } },
			};
			if (Add(streak) == null)
			{
				continue;
			}
			streak.GlobalPosition = new Vector2(screen.Position.X - 50f * i, y);
			streak.CreateTween().TweenProperty(streak, "global_position:x", screen.End.X + len, seconds * (0.8f + 0.06f * i)).SetTrans(Tween.TransitionType.Linear);
			FreeAfter(streak, seconds + 0.4f);
		}
		shadow.CreateTween().TweenProperty(shadow, "global_position:x", screen.End.X + length, seconds).SetTrans(Tween.TransitionType.Linear);
		FreeAfter(shadow, seconds + 0.2f);
		await Cmd.Wait(seconds * 0.5f);
		NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
	}

	/// <summary>A top-down plane silhouette pointing right, 400 wide, for <see cref="ShadowPass"/>.</summary>
	public static Vector2[] PlaneShape()
	{
		var upper = new[]
		{
			new Vector2(200, 0), new Vector2(188, -10), new Vector2(150, -17), new Vector2(40, -19), new Vector2(-30, -150),
			new Vector2(-72, -150), new Vector2(-40, -19), new Vector2(-150, -15), new Vector2(-180, -62), new Vector2(-202, -62),
			new Vector2(-194, -10), new Vector2(-206, 0),
		};
		var points = new List<Vector2>(upper);
		for (int i = upper.Length - 2; i >= 1; i--)
		{
			points.Add(new Vector2(upper[i].X, -upper[i].Y));
		}
		return points.ToArray();
	}

	/// <summary>A glowing line from one point to another that flares and fades (a bond between allies, a golden handshake).</summary>
	public static void Beam(Vector2 from, Vector2 to, Color color, float width = 22f, float seconds = 0.9f)
	{
		var line = new Line2D
		{
			Points = new[] { Vector2.Zero, to - from }, Width = width, DefaultColor = color,
			BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round,
			Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
		};
		var core = new Line2D { Points = line.Points, Width = width * 0.35f, DefaultColor = new Color(1f, 1f, 0.95f, 1f),
			BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round };
		line.AddChild(core);
		if (Add(line) == null)
		{
			return;
		}
		line.GlobalPosition = from;
		line.Modulate = new Color(1f, 1f, 1f, 0f);
		Tween tween = line.CreateTween();
		tween.TweenProperty(line, "modulate:a", 1f, seconds * 0.2f);
		tween.TweenProperty(line, "modulate:a", 0f, seconds * 0.8f).SetEase(Tween.EaseType.In);
		FreeAfter(line, seconds + 0.1f);
	}
}
