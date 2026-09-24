using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using PresMod.Characters.Trump.Mechanics;
using PresMod.Framework;

namespace PresMod.Characters.Trump.Nodes;

/// <summary>
/// Combat overlay added to every NCombatRoom (CombatUiPatch). Keeps a Wall display next to each Donald player,
/// the Deport line + DENIED stamp on enemy health bars, and pops "DEPORTED!" when an enemy is Deported.
/// Step 8 VFX: dust when the Wall rises, rubble when it's spent or reaches a new stage, and a stamp slam on Deport,
/// built from the game's own effects. Does nothing in combats without a mod character.
/// </summary>
public partial class NTrumpCombatUi : Node
{
	private const string LineName = "TrumpDeportLine";

	private const string StampName = "TrumpDenied";

	private static readonly string StampArt = CharacterArt.Dir("TRUMP") + "ui/deport_stamp.png";

	private readonly Dictionary<Creature, NWallDisplay> _walls = new Dictionary<Creature, NWallDisplay>();

	private Font? _font;

	public override void _EnterTree()
	{
		_font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres") ?? ThemeDB.FallbackFont;
		WallCmd.StageReached += OnStageReached;
		WallCmd.HeightChanged += OnHeightChanged;
		DeportCmd.Deported += OnDeported;
	}

	public override void _ExitTree()
	{
		WallCmd.StageReached -= OnStageReached;
		WallCmd.HeightChanged -= OnHeightChanged;
		DeportCmd.Deported -= OnDeported;
	}

	public override void _Process(double delta)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}
		Player? me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
		bool meIsDonald = me?.Character is Trump;
		foreach (NCreature node in room.CreatureNodes.ToList())
		{
			if (!IsInstanceValid(node) || node.Entity == null)
			{
				continue;
			}
			Creature creature = node.Entity;
			// The Donald always shows his Wall; other players only once they have one (Coalition Wall in co-op).
			if (creature.IsPlayer && (creature.Player?.Character is Trump || _walls.ContainsKey(creature) || WallCmd.GetHeight(creature) > 0))
			{
				EnsureWall(node, creature);
			}
			else if (creature.Side == CombatSide.Enemy)
			{
				UpdateDeportMarker(node, creature, meIsDonald ? me : null);
			}
		}
	}

	private void EnsureWall(NCreature node, Creature creature)
	{
		if (!_walls.TryGetValue(creature, out NWallDisplay? wall) || !IsInstanceValid(wall))
		{
			wall = new NWallDisplay { Name = "TrumpWall", Creature = creature };
			node.AddChild(wall);
			_walls[creature] = wall;
		}
		Rect2 box = node.Hitbox.GetRect();
		wall.Position = new Vector2(box.End.X + 26f, box.End.Y);
	}

	private void UpdateDeportMarker(NCreature node, Creature enemy, Player? me)
	{
		NHealthBar? bar = FindHealthBar(node);
		if (bar == null)
		{
			return;
		}
		Control? container = Traverse.Create(bar).Property<Control>("HpBarContainer").Value;
		if (container == null)
		{
			return;
		}
		ColorRect line = container.GetNodeOrNull<ColorRect>(LineName) ?? CreateLine(container);
		Control stamp = bar.GetNodeOrNull<Control>(StampName) ?? CreateStamp(bar);
		bool show = me != null && enemy.IsAlive && !DeportCmd.IsImmune(enemy);
		line.Visible = show;
		stamp.Visible = show && DeportCmd.CanDeport(enemy, me!);
		if (!show)
		{
			return;
		}
		float fraction = (float)(DeportCmd.LineHp(enemy, me!) / enemy.MaxHp);
		line.Position = new Vector2(Mathf.Round(container.Size.X * fraction) - 1f, -4f);
		line.Size = new Vector2(3f, container.Size.Y + 8f);
		stamp.Position = new Vector2(container.Position.X + container.Size.X * 0.5f - stamp.Size.X * 0.5f, container.Position.Y - stamp.Size.Y - 2f);
	}

	private static ColorRect CreateLine(Control container)
	{
		var line = new ColorRect { Name = LineName, Color = new Color("ff5a3c"), MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 2 };
		container.AddChild(line);
		return line;
	}

	/// <summary>The DENIED stamp over an enemy's health bar: the generated stamp art, or the Step 4 text if it's missing.</summary>
	private Control CreateStamp(Control bar)
	{
		if (CharacterArt.Load(StampArt) is Texture2D art)
		{
			var image = new TextureRect
			{
				Name = StampName,
				Texture = art,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				Size = new Vector2(104f, 104f * art.GetHeight() / art.GetWidth()),
				RotationDegrees = -8f,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ZIndex = 3
			};
			image.PivotOffset = image.Size * 0.5f;
			bar.AddChild(image);
			return image;
		}
		var stamp = new Label
		{
			Name = StampName,
			Text = new LocString("static_hover_tips", "TRUMP_DEPORT.stamp").GetRawText(),
			RotationDegrees = -10f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 3
		};
		if (_font != null)
		{
			stamp.AddThemeFontOverride("font", _font);
		}
		stamp.AddThemeFontSizeOverride("font_size", 24);
		stamp.AddThemeColorOverride("font_color", new Color("e0332a"));
		stamp.AddThemeColorOverride("font_outline_color", new Color("1a0a08"));
		stamp.AddThemeConstantOverride("outline_size", 8);
		bar.AddChild(stamp);
		return stamp;
	}

	private static NHealthBar? FindHealthBar(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is NHealthBar bar)
			{
				return bar;
			}
			NHealthBar? found = FindHealthBar(child);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	private void OnStageReached(Creature creature, int stage)
	{
		if (_walls.TryGetValue(creature, out NWallDisplay? wall) && IsInstanceValid(wall))
		{
			wall.ShowStageBanner(stage);
			if (NCombatRoom.Instance is NCombatRoom room)
			{
				VfxCmd.PlayVfx(wall.TopGlobalPosition, "vfx/vfx_rock_shatter", room.CombatVfxContainer);
			}
		}
	}

	private void OnHeightChanged(Creature creature, int change)
	{
		if (change == 0 || !_walls.TryGetValue(creature, out NWallDisplay? wall) || !IsInstanceValid(wall) || NCombatRoom.Instance is not NCombatRoom room)
		{
			return;
		}
		// Dust off the top as bricks go on; rubble from the middle when height is spent (Demolition, Wrecking Ball).
		Vector2 at = change > 0 ? wall.TopGlobalPosition : wall.MiddleGlobalPosition;
		VfxCmd.PlayVfx(at, change > 0 ? "vfx/vfx_sandy_impact" : "vfx/vfx_rock_shatter", room.CombatVfxContainer);
	}

	private void OnDeported(Creature enemy)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		NCreature? node = room?.GetCreatureNode(enemy);
		if (room == null || node == null)
		{
			return;
		}
		SlamStamp(room, node);
		var label = new Label
		{
			Text = new LocString("static_hover_tips", "TRUMP_DEPORT.popup").GetRawText(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			Size = new Vector2(360f, 60f)
		};
		if (_font != null)
		{
			label.AddThemeFontOverride("font", _font);
		}
		label.AddThemeFontSizeOverride("font_size", 44);
		label.AddThemeColorOverride("font_color", new Color("f2b92e"));
		label.AddThemeColorOverride("font_outline_color", new Color("1a0a08"));
		label.AddThemeConstantOverride("outline_size", 12);
		room.CombatVfxContainer.AddChild(label);
		Rect2 box = node.Hitbox.GetGlobalRect();
		label.GlobalPosition = new Vector2(box.GetCenter().X - 180f, box.Position.Y - 20f);
		Tween tween = label.CreateTween();
		tween.TweenProperty(label, "position:y", label.Position.Y - 90f, 1.4).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.6).SetDelay(0.8);
		tween.TweenCallback(Callable.From(label.QueueFree));
	}

	/// <summary>Deport: the DENIED stamp slams down on the enemy (big and faint → full size), holds, then fades.</summary>
	private static void SlamStamp(NCombatRoom room, NCreature node)
	{
		Rect2 box = node.Hitbox.GetGlobalRect();
		VfxCmd.PlayVfx(new Vector2(box.GetCenter().X, box.End.Y), "vfx/vfx_sandy_impact", room.CombatVfxContainer);
		if (CharacterArt.Load(StampArt) is not Texture2D art)
		{
			return;
		}
		var stamp = new Sprite2D { Texture = art, RotationDegrees = -12f, ZIndex = 10 };
		float size = Mathf.Clamp(box.Size.X * 1.1f, 180f, 320f) / art.GetWidth();
		room.CombatVfxContainer.AddChild(stamp);
		stamp.GlobalPosition = box.GetCenter();
		stamp.Scale = Vector2.One * size * 2.6f;
		stamp.Modulate = new Color(1f, 1f, 1f, 0f);
		Tween tween = stamp.CreateTween();
		tween.TweenProperty(stamp, "scale", Vector2.One * size, 0.16).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		tween.Parallel().TweenProperty(stamp, "modulate:a", 1f, 0.12);
		tween.TweenInterval(0.55);
		tween.TweenProperty(stamp, "modulate:a", 0f, 0.4);
		tween.TweenCallback(Callable.From(stamp.QueueFree));
	}
}
