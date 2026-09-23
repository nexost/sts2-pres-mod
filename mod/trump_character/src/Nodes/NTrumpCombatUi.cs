using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TrumpMod.Mechanics;

namespace TrumpMod.Nodes;

/// <summary>
/// Combat overlay added to every NCombatRoom (CombatUiPatch). Keeps a Wall display next to each Donald player,
/// the Deport line + DENIED stamp on enemy health bars, and pops "DEPORTED!" when an enemy is Deported.
/// Does nothing in combats without a mod character.
/// </summary>
public partial class NTrumpCombatUi : Node
{
	private const string LineName = "TrumpDeportLine";

	private const string StampName = "TrumpDenied";

	private readonly Dictionary<Creature, NWallDisplay> _walls = new Dictionary<Creature, NWallDisplay>();

	private Font? _font;

	public override void _EnterTree()
	{
		_font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres") ?? ThemeDB.FallbackFont;
		WallCmd.StageReached += OnStageReached;
		DeportCmd.Deported += OnDeported;
	}

	public override void _ExitTree()
	{
		WallCmd.StageReached -= OnStageReached;
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
		bool meIsDonald = me != null && ModContent.IsModCharacter(me.Character);
		foreach (NCreature node in room.CreatureNodes.ToList())
		{
			if (!IsInstanceValid(node) || node.Entity == null)
			{
				continue;
			}
			Creature creature = node.Entity;
			// The Donald always shows his Wall; other players only once they have one (Coalition Wall in co-op).
			if (creature.IsPlayer && (ModContent.IsModCharacter(creature.Player?.Character) || _walls.ContainsKey(creature) || WallCmd.GetHeight(creature) > 0))
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
		Label stamp = bar.GetNodeOrNull<Label>(StampName) ?? CreateStamp(bar);
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
		stamp.Position = new Vector2(container.Position.X + container.Size.X * 0.5f - 46f, container.Position.Y - 40f);
	}

	private static ColorRect CreateLine(Control container)
	{
		var line = new ColorRect { Name = LineName, Color = new Color("ff5a3c"), MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 2 };
		container.AddChild(line);
		return line;
	}

	private Label CreateStamp(Control bar)
	{
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
		}
	}

	private void OnDeported(Creature enemy)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		NCreature? node = room?.GetCreatureNode(enemy);
		if (room == null || node == null)
		{
			return;
		}
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
}
