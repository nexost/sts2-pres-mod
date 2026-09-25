using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using PresMod.Characters.Biden.Powers;
using PresMod.Framework;
using PresMod.Framework.Nodes;

namespace PresMod.Characters.Biden.Mechanics;

/// <summary>
/// Sleepy Joe's visuals, mostly the game's own effects recoloured (Framework/VfxRecolor) or built with Framework/VfxKit
/// (design.md §11):
///   Laser Eyes: the Defect's hyperbeam in red from his eyes; Double Vision's extra hit comes from a second, offset pair
///     in a deeper red; with Laser Focus the beam is thinner and brighter;
///   Doze: a few Z's rise from his head; near the nod-off line, the screen edges darken (only for his own player);
///   nodding off: the sleeping Z's sleeping monsters use, until he wakes;
///   waking as Dark Brandon: a red power-up burst and a shake, then a red glow and embers behind him for the whole turn;
///   Tangents: a speech bubble when one moves to its next line; three quick ones when Dark Brandon does every line;
///   cards: Snore and Snoring (the game's scream shockwave in snore blue), Laser Show (the sweeping beam in red), Mic
///     Drop (a golden meteor), Air Force One (a jet's shadow over the fight), the train, limo, motorcade, sports car and
///     ice cream truck driving across (their own art, images/biden/vfx/), Reach Across the Aisle (a golden line to
///     every ally);
///   the Ice Cream Cone's heal: a burst of sprinkles; potions: Warm Milk steam, an Espresso Shot jitter, a red flash
///     for Dark Roast.
/// Only visuals: nothing here changes the game, so co-op stays in sync, and visual randomness never touches the game's
/// RNG. Every effect is skipped without a combat room (fights without a scene, test mode).
/// </summary>
public static class BidenVfx
{
	private const float Red = 0f;
	private const float DeepRed = 0.025f;
	private const float SnoreBlue = 0.58f;
	private static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
	private const int TangentLines = 8;

	/// <summary>Where his eyes and the top of his head are on the painting, as a share of its width and height (he faces right).</summary>
	private static readonly Vector2 EyesOnFigure = new Vector2(0.6f, 0.15f);
	private static readonly Vector2 HeadOnFigure = new Vector2(0.55f, 0.04f);

	private static readonly Dictionary<Creature, NSleepingVfx> Sleeping = new Dictionary<Creature, NSleepingVfx>();
	private static readonly Dictionary<Creature, Node2D> Auras = new Dictionary<Creature, Node2D>();
	private static readonly Dictionary<Creature, ulong> LastBubble = new Dictionary<Creature, ulong>();
	private static readonly Random VisualRandom = new Random();
	private static TextureRect? _vignette;

	private static NCreature? NodeOf(Creature creature) => NCombatRoom.Instance?.GetCreatureNode(creature);

	private static Vector2 OnFigure(NCreature node, Vector2 share, float hitboxY)
	{
		if (node.FindChild(NCharacterPoses.NodeName, recursive: true, owned: false) is NCharacterPoses poses && poses.FigureRect is Rect2 figure)
		{
			return figure.Position + figure.Size * share;
		}
		Rect2 box = node.Hitbox.GetGlobalRect();
		return new Vector2(box.Position.X + box.Size.X * 0.62f, box.Position.Y + box.Size.Y * hitboxY);
	}

	/// <summary>His eyes on screen: on the painting shown now, else near the top of his hitbox.</summary>
	public static Vector2 Eyes(NCreature node) => OnFigure(node, EyesOnFigure, 0.14f);

	private static Vector2 Head(NCreature node) => OnFigure(node, HeadOnFigure, 0.02f);

	private static T? Place<T>(T? vfx, Vector2 global) where T : Node2D
	{
		if (vfx == null || NCombatRoom.Instance == null)
		{
			return null;
		}
		NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
		vfx.GlobalPosition = global;
		return vfx;
	}

	// ---------------------------------------------------------------- Drowsy, nodding off, waking up

	/// <summary>Drowsy went up: a few Z's rise from his head, and the screen edges follow how close he is to the line.</summary>
	public static void Dozed(Creature owner)
	{
		if (NodeOf(owner) is NCreature node && Place(NSleepingVfx.Create(Head(node)), Head(node)) is NSleepingVfx z)
		{
			TaskHelper.RunSafely(StopAfter(z, 0.6f));
		}
		UpdateVignette(owner);
	}

	private static async Task StopAfter(NSleepingVfx z, float seconds)
	{
		await Cmd.Wait(seconds);
		if (GodotObject.IsInstanceValid(z))
		{
			z.Stop();
		}
	}

	/// <summary>He nodded off: the sleeping Z's stay over his head until he wakes.</summary>
	public static void NoddedOff(Creature owner)
	{
		StopSleeping(owner);
		if (NodeOf(owner) is NCreature node && NSleepingVfx.Create(Head(node)) is NSleepingVfx z)
		{
			node.AddChildSafely(z);
			z.GlobalPosition = Head(node);
			Sleeping[owner] = z;
		}
		UpdateVignette(owner);
	}

	private static void StopSleeping(Creature owner)
	{
		if (Sleeping.Remove(owner, out NSleepingVfx? z) && GodotObject.IsInstanceValid(z))
		{
			z.Stop();
		}
	}

	/// <summary>He woke up as Dark Brandon: a red power-up burst and a jolt, then red embers around him for the turn.</summary>
	public static async Task WokeUp(Creature owner)
	{
		StopSleeping(owner);
		UpdateVignette(owner);
		if (NodeOf(owner) is not NCreature node)
		{
			return;
		}
		if (NPowerUpVfx.CreateNormal(owner) is NPowerUpVfx burst)
		{
			VfxRecolor.Apply(burst, Red);
		}
		NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
		StartAura(owner, node);
		await Cmd.Wait(0.25f);
	}

	/// <summary>The Dark Brandon turn ended: the aura fades and its embers die out.</summary>
	public static void FellAsleep(Creature owner)
	{
		if (Auras.Remove(owner, out Node2D? aura) && GodotObject.IsInstanceValid(aura))
		{
			foreach (CpuParticles2D embers in aura.GetChildren().OfType<CpuParticles2D>())
			{
				embers.Emitting = false;
			}
			aura.CreateTween().TweenProperty(aura, "modulate:a", 0f, 1.2);
			TaskHelper.RunSafely(FreeAfter(aura, 2.2f));
		}
	}

	private static async Task FreeAfter(Node node, float seconds)
	{
		await Cmd.Wait(seconds);
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFreeSafely();
		}
	}

	/// <summary>
	/// Dark Brandon's aura, drawn behind his painting: a soft red glow that breathes, and embers drifting up around him.
	/// </summary>
	private static void StartAura(Creature owner, NCreature node)
	{
		FellAsleep(owner);
		Rect2 figure = node.FindChild(NCharacterPoses.NodeName, recursive: true, owned: false) is NCharacterPoses poses && poses.FigureRect is Rect2 r
			? r
			: node.Hitbox.GetGlobalRect();
		var aura = new Node2D { Name = "DarkBrandonAura" };
		var add = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		var glow = new Sprite2D
		{
			Texture = RadialDot(128, new Color(1f, 0.12f, 0.06f, 0.55f)), Material = add,
			Scale = new Vector2(figure.Size.X * 1.5f / 128f, figure.Size.Y * 1.05f / 128f),
		};
		var embers = new CpuParticles2D
		{
			Amount = 40, Lifetime = 1.8, Preprocess = 1.8, Texture = RadialDot(32, Colors.White), LocalCoords = false,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(figure.Size.X * 0.55f, figure.Size.Y * 0.42f),
			Direction = Vector2.Up, Spread = 18f, Gravity = new Vector2(0f, -45f),
			InitialVelocityMin = 20f, InitialVelocityMax = 70f, ScaleAmountMin = 0.35f, ScaleAmountMax = 0.8f,
			ColorRamp = new Gradient
			{
				Offsets = new[] { 0f, 0.2f, 1f },
				Colors = new[] { new Color(1f, 0.9f, 0.6f, 0f), new Color(1.6f, 0.35f, 0.12f, 1f), new Color(0.7f, 0.03f, 0.02f, 0f) }
			},
			Material = add,
		};
		aura.AddChild(glow);
		aura.AddChild(embers);
		node.AddChildSafely(aura);
		// First child of the creature: drawn before (behind) his painting, after the background.
		node.MoveChild(aura, 0);
		aura.GlobalPosition = figure.GetCenter();
		Tween breathe = aura.CreateTween().SetLoops();
		breathe.TweenProperty(glow, "modulate:a", 0.55f, 0.9).SetTrans(Tween.TransitionType.Sine);
		breathe.TweenProperty(glow, "modulate:a", 1f, 0.9).SetTrans(Tween.TransitionType.Sine);
		Auras[owner] = aura;
	}

	/// <summary>A soft round spot: the colour in the middle, fading to nothing at the edge.</summary>
	private static GradientTexture2D RadialDot(int size, Color color) => new GradientTexture2D
	{
		Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
		Gradient = new Gradient { Colors = new[] { color, new Color(color.R, color.G, color.B, 0f) } }
	};

	/// <summary>
	/// The screen edges darken as his Drowsy nears the nod-off line: from 60% of the way, up to a soft dusk at the line.
	/// Only for his own player, and gone while he's asleep or Dark Brandon.
	/// </summary>
	private static void UpdateVignette(Creature owner)
	{
		if (owner.Player == null || !LocalContext.IsMe(owner.Player) || NCombatRoom.Instance is not NCombatRoom room)
		{
			return;
		}
		float share = DrowsyCmd.GetDrowsy(owner) / (float)DrowsyCmd.GetNodOffLine(owner);
		float target = DrowsyCmd.IsDarkBrandon(owner) || DrowsyCmd.HasNoddedOff(owner) || owner.IsDead
			? 0f
			: Mathf.Clamp((share - 0.5f) / 0.5f, 0f, 1f) * 0.85f;
		if (_vignette == null || !GodotObject.IsInstanceValid(_vignette) || !_vignette.IsInsideTree())
		{
			if (target <= 0f)
			{
				return;
			}
			_vignette = VfxKit.Overlay(new TextureRect
			{
				Texture = new GradientTexture2D
				{
					Width = 256, Height = 256, Fill = GradientTexture2D.FillEnum.Radial,
					FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1.05f, 1.05f),
					Gradient = new Gradient
					{
						Offsets = new[] { 0f, 0.38f, 1f },
						Colors = new[] { new Color(0.14f, 0.1f, 0.42f, 0f), new Color(0.14f, 0.1f, 0.42f, 0f), new Color(0.14f, 0.1f, 0.42f, 0.95f) }
					}
				},
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
				Modulate = new Color(1f, 1f, 1f, 0f),
			});
			if (_vignette == null)
			{
				return;
			}
		}
		_vignette.CreateTween().TweenProperty(_vignette, "modulate:a", target, 0.5);
	}

	// ---------------------------------------------------------------- Tangents

	/// <summary>A Tangent moved to its next line: now and then (at most every few seconds) he says one of his lines.</summary>
	public static void TangentMoved(Creature owner)
	{
		ulong now = Time.GetTicksMsec();
		if (NodeOf(owner) == null || (LastBubble.TryGetValue(owner, out ulong last) && now - last < 4000))
		{
			return;
		}
		LastBubble[owner] = now;
		TalkCmd.Play(new LocString("static_hover_tips", $"BIDEN_VFX.tangent{VisualRandom.Next(1, TangentLines + 1)}"), owner, VfxColor.White, VfxDuration.Short);
	}

	/// <summary>As Dark Brandon a Tangent does every line: "Point one!", "Point two!", "And three!" in quick bubbles.</summary>
	public static void AllLines(Creature owner, int lines)
	{
		if (NodeOf(owner) == null)
		{
			return;
		}
		LastBubble[owner] = Time.GetTicksMsec();
		TaskHelper.RunSafely(PointByPoint(owner, Math.Min(lines, 3)));
	}

	private static async Task PointByPoint(Creature owner, int lines)
	{
		for (int i = 1; i <= lines; i++)
		{
			string text = new LocString("static_hover_tips", $"BIDEN_VFX.point{i}").GetFormattedText();
			if (NSpeechBubbleVfx.Create(text, owner, 0.55) is NSpeechBubbleVfx bubble)
			{
				owner.GetVfxContainer()?.AddChildSafely(bubble);
			}
			await Cmd.Wait(0.6f);
		}
	}

	// ---------------------------------------------------------------- Cards

	/// <summary>Snore and Snoring: a pale blue snore wave rolls out from him (the game's scream), with a few Z's.</summary>
	public static async Task Snore(Creature owner)
	{
		if (NodeOf(owner) is not NCreature node)
		{
			return;
		}
		if (Place(NScreamVfx.Create(Head(node)), Head(node)) is NScreamVfx wave)
		{
			VfxRecolor.Apply(wave, SnoreBlue, 0.8f);
			// Its streaks are white, which a recolour leaves alone: a pale blue tint on top.
			wave.Modulate = new Color(0.72f, 0.86f, 1f);
		}
		if (Place(NSleepingVfx.Create(Head(node)), Head(node)) is NSleepingVfx z)
		{
			_ = TaskHelper.RunSafely(StopAfter(z, 0.5f));
		}
		await Cmd.Wait(0.25f);
	}

	/// <summary>Laser Show: the Defect's sweeping beam in red, raked across every enemy from his eyes.</summary>
	public static async Task LaserShow(Creature owner, IReadOnlyList<Creature> enemies)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null || NodeOf(owner) is not NCreature node)
		{
			return;
		}
		var targets = new Godot.Collections.Array<Vector2>();
		foreach (Creature enemy in enemies)
		{
			if (room.GetCreatureNode(enemy) is NCreature target)
			{
				targets.Add(target.VfxSpawnPosition);
			}
		}
		if (targets.Count == 0 || NSweepingBeamVfx.Create(Eyes(node), targets) is not NSweepingBeamVfx beam)
		{
			return;
		}
		VfxRecolor.Apply(beam, Red);
		// Halfway through, the beam spawns its own impacts at the scene root (NSweepingBeamImpactVfx): recolour those
		// as they enter the tree, before they start playing.
		Window root = room.GetTree().Root;
		Node.ChildEnteredTreeEventHandler recolor = child =>
		{
			if (child is NSweepingBeamImpactVfx impact)
			{
				VfxRecolor.Apply(impact, Red);
			}
		};
		root.ChildEnteredTree += recolor;
		room.CombatVfxContainer.AddChildSafely(beam);
		await Cmd.Wait(0.5f);
		_ = TaskHelper.RunSafely(Unhook(root, recolor));
	}

	/// <summary>Stop listening once the beam's impacts are out (the same handler instance, or it stays subscribed).</summary>
	private static async Task Unhook(Window root, Node.ChildEnteredTreeEventHandler handler)
	{
		await Cmd.Wait(1f);
		if (GodotObject.IsInstanceValid(root))
		{
			root.ChildEnteredTree -= handler;
		}
	}

	// ---------------------------------------------------------------- Laser Eyes

	/// <summary>
	/// One Laser Eyes hit: the Defect's hyperbeam (charge-up, beam, shake, sound) recoloured red, from his eyes at the
	/// farthest enemy, with its impact on every enemy. Double Vision's extra hits come from a second pair, offset and a
	/// deeper red; Laser Focus makes the beam thinner and brighter.
	/// </summary>
	public static async Task LaserEyes(Creature owner, IReadOnlyList<Creature> enemies, int hit)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null || enemies.Count == 0 || NodeOf(owner) is not NCreature joe || room.GetCreatureNode(enemies[^1]) is not NCreature far)
		{
			return;
		}
		Vector2 eyes = Eyes(joe) + new Vector2(-6f, -18f) * (hit % 2);
		float hue = hit % 2 == 0 ? Red : DeepRed;
		if (NHyperbeamVfx.Create(eyes, far.VfxSpawnPosition) is not NHyperbeamVfx beam)
		{
			return;
		}
		VfxRecolor.Apply(beam, hue);
		if (owner.HasPower<LaserFocusPower>())
		{
			beam.Scale = new Vector2(1f, 0.55f);
			beam.Modulate = new Color(1.5f, 1.5f, 1.5f);
		}
		room.CombatVfxContainer.AddChildSafely(beam);
		// The beam comes out after the hyperbeam's charge-up; the hits land with it.
		await Cmd.Wait(NHyperbeamVfx.hyperbeamAnticipationDuration + 0.03f);
		foreach (Creature enemy in enemies)
		{
			NCreature? node = room.GetCreatureNode(enemy);
			if (node != null && NHyperbeamImpactVfx.Create(eyes, node.VfxSpawnPosition) is NHyperbeamImpactVfx impact)
			{
				VfxRecolor.Apply(impact, hue);
				room.CombatVfxContainer.AddChildSafely(impact);
			}
		}
	}

	// ---------------------------------------------------------------- More cards

	/// <summary>Mic Drop: a golden meteor slams down on the target, the screen shakes hard, and sparks fly.</summary>
	public static async Task MicDrop(Creature target)
	{
		if (NodeOf(target) is not NCreature node || NLargeMagicMissileVfx.Create(node.GetBottomOfHitbox(), Gold) is not NLargeMagicMissileVfx meteor)
		{
			return;
		}
		NCombatRoom.Instance!.CombatVfxContainer.AddChildSafely(meteor);
		await Cmd.Wait(meteor.WaitTime);
		NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
		VfxKit.Burst(node.GetBottomOfHitbox(), VfxKit.Dot(24, Colors.White), VfxKit.Palette(Gold, new Color(1f, 0.95f, 0.7f)),
			count: 30, speed: 650f, additive: true);
	}

	/// <summary>Air Force One: the jet's shadow sweeps over the fight.</summary>
	public static Task AirForceOne(Creature owner) =>
		NodeOf(owner) is NCreature node ? VfxKit.ShadowPass(VfxKit.PlaneShape(), FigureHeight(node) * 3.2f, Ground(node), 0.8f) : Task.CompletedTask;

	/// <summary>The floor under a creature (the bottom of his painting or hitbox).</summary>
	private static float Ground(NCreature node) =>
		node.FindChild(NCharacterPoses.NodeName, recursive: true, owned: false) is NCharacterPoses poses && poses.FigureRect is Rect2 figure
			? figure.End.Y
			: node.Hitbox.GetGlobalRect().End.Y;

	private static float FigureHeight(NCreature node) =>
		node.FindChild(NCharacterPoses.NodeName, recursive: true, owned: false) is NCharacterPoses poses && poses.FigureRect is Rect2 figure
			? figure.Size.Y
			: node.Hitbox.GetGlobalRect().Size.Y;

	/// <summary>
	/// A vehicle drives across the fight (images/biden/vfx/NAME.png): returns when it reaches the target (the hit lands
	/// then) while it drives on. Without a target it just passes by. The size is a share of his figure's height;
	/// fromHim: it peels out from where he stands, in a puff of smoke.
	/// </summary>
	public static async Task Vehicle(Creature owner, string name, Creature? target = null, float size = 0.6f, float seconds = 0.8f,
		int copies = 1, bool behind = false, bool fromHim = false)
	{
		if (NodeOf(owner) is not NCreature joe)
		{
			return;
		}
		Texture2D? texture = CharacterArt.Load(CharacterArt.Dir("BIDEN") + "vfx/" + name + ".png");
		float height = FigureHeight(joe);
		float ground = Ground(joe) + height * (behind ? -0.12f : 0.04f);
		float untilX = target != null && NodeOf(target) is NCreature t ? t.VfxSpawnPosition.X : VfxKit.Screen.Position.X;
		if (fromHim)
		{
			VfxKit.Puffs(new Vector2(joe.VfxSpawnPosition.X, ground), new Vector2(50f, 8f), new Color(0.85f, 0.85f, 0.85f, 0.55f),
				count: 16, seconds: 0.4f, rise: 60f, size: 1.1f);
		}
		await VfxKit.DriveBy(texture, height * size, ground, untilX, seconds, behind, fromHim ? joe.VfxSpawnPosition.X : float.NaN, copies);
		if (target != null)
		{
			NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
		}
	}

	/// <summary>Reach Across the Aisle: a golden line from him to every ally, with a sparkle on each.</summary>
	public static void ReachAcross(Creature owner, IEnumerable<Creature> allies)
	{
		if (NodeOf(owner) is not NCreature joe)
		{
			return;
		}
		foreach (Creature ally in allies)
		{
			if (ally == owner || NodeOf(ally) is not NCreature node)
			{
				continue;
			}
			VfxKit.Beam(joe.VfxSpawnPosition, node.VfxSpawnPosition, Gold);
			VfxKit.Burst(node.VfxSpawnPosition, VfxKit.Dot(20, Colors.White), VfxKit.Palette(Gold, Colors.White),
				count: 18, speed: 300f, gravity: 150f, additive: true);
		}
	}

	// ---------------------------------------------------------------- Relics and potions

	/// <summary>Ice Cream Cone (and the Ice Cream Truck): a burst of rainbow sprinkles over him.</summary>
	public static void Sprinkles(Creature owner)
	{
		if (NodeOf(owner) is NCreature node)
		{
			VfxKit.Burst(Head(node), VfxKit.Bar(14, 5), VfxKit.Palette(new Color("ff5fa2"), new Color("ffd23f"), new Color("3ec1ff"),
				new Color("7ee06d"), new Color("ffffff"), new Color("b58cff")), count: 46, speed: 560f, gravity: 1000f);
		}
	}

	/// <summary>Warm Milk: soft steam rises around him.</summary>
	public static void Steam(Creature owner)
	{
		if (NodeOf(owner) is NCreature node)
		{
			float h = FigureHeight(node);
			VfxKit.Puffs(node.VfxSpawnPosition + new Vector2(0f, h * 0.1f), new Vector2(h * 0.25f, h * 0.15f), new Color(1f, 0.97f, 0.9f, 0.4f),
				count: 18, seconds: 0.9f, rise: 90f, size: 1.2f);
		}
	}

	/// <summary>Espresso Shot: he jitters with caffeine, throwing off little sparks.</summary>
	public static async Task Jitter(Creature owner)
	{
		if (NodeOf(owner) is not NCreature node)
		{
			return;
		}
		if (node.FindChild(NCharacterPoses.NodeName, recursive: true, owned: false) is NCharacterPoses poses)
		{
			poses.Shake(0.6f, 7f);
		}
		VfxKit.Burst(node.VfxSpawnPosition, VfxKit.Bar(18, 4), VfxKit.Palette(new Color("ffe066"), new Color("c8894a"), Colors.White),
			count: 20, speed: 420f, gravity: 0f, spread: 180f, additive: true);
		await Cmd.Wait(0.45f);
	}

	/// <summary>Dark Roast: the screen flashes red.</summary>
	public static void RedFlash() => VfxKit.Flash(new Color(0.9f, 0.05f, 0.05f), 0.33f, 0.5f);
}
