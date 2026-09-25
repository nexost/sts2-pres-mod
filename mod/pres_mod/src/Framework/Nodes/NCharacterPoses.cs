using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PresMod.Framework.Nodes;

/// <summary>
/// A character's combat body without a Spine rig: one generated painting per pose (images/&lt;id&gt;/combat_idle, _attack,
/// _cast, _hurt .png) on the scene's Visuals sprite, swapped on the game's animation triggers (ArtPatches) with a little
/// motion on top: a lunge for attacks, a hop for casts, a flinch and shake when hit, a slow sink on death, and a
/// breathing idle. Added by ArtPatches to any mod character whose combat scene has a Sprite2D "Visuals".
/// </summary>
public partial class NCharacterPoses : Node
{
	public const string NodeName = "CharacterPoses";

	/// <summary>Pose → (image, share of the character's CombatFigureHeight). Leaning poses are a little shorter.</summary>
	private static readonly (string Pose, string File, float Scale)[] Poses =
	{
		("idle", "combat_idle", 1f),
		("attack", "combat_attack", 0.965f),
		("cast", "combat_cast", 1f),
		("hurt", "combat_hurt", 0.965f),
	};

	/// <summary>res://images/&lt;id&gt;/ of the character (set by ArtPatches before the node enters the tree).</summary>
	public string ArtDir { get; set; } = "";

	/// <summary>The idle figure's height in pixels (IModCharacter.CombatFigureHeight).</summary>
	public float FigureHeight { get; set; } = 290f;

	private readonly Dictionary<string, (Texture2D Tex, float Height)> _poses = new Dictionary<string, (Texture2D, float)>();

	/// <summary>Paintings of the alternate pose set, when one is on (<see cref="SetVariant"/>).</summary>
	private readonly Dictionary<string, (Texture2D Tex, float Height)> _variantPoses = new Dictionary<string, (Texture2D, float)>();

	/// <summary>Colour on the figure while an alternate pose set without its own paintings is on.</summary>
	private Color _tint = Colors.White;

	private Sprite2D? _sprite;

	private Vector2 _home;

	private Vector2 _baseScale = Vector2.One;

	private string _action = "idle";

	private float _time;

	private float _duration;

	private double _clock;

	private bool _dead;

	public override void _Ready()
	{
		_sprite = GetParent().GetNodeOrNull<Sprite2D>("%Visuals") ?? GetParent().GetNodeOrNull<Sprite2D>("Visuals");
		LoadPoses("", _poses);
		if (_sprite == null)
		{
			return;
		}
		_home = _sprite.Position;
		_clock = GD.Randf() * 10f;
		SetPose("idle");
	}

	/// <summary>Called with the game's animation trigger names (CreatureAnimator): Idle, Attack, Cast, PowerUp, Hit, Dead, Revive.</summary>
	public void Play(string trigger)
	{
		switch (trigger)
		{
			case "Attack":
				Start("attack", 0.55f);
				break;
			case "Cast":
			case "PowerUp":
				Start("cast", 0.7f);
				break;
			case "Hit":
				if (!_dead)
				{
					Start("hurt", 0.5f);
				}
				break;
			case "Dead":
				_dead = true;
				Start("dead", 1.2f);
				break;
			case "Revive":
				_dead = false;
				Start("idle", 0f);
				break;
			case "Idle":
				if (!_dead)
				{
					Start("idle", 0f);
				}
				break;
		}
	}

	private void Start(string action, float duration)
	{
		_action = action;
		_time = 0f;
		_duration = duration;
		SetPose(action == "dead" ? "hurt" : action);
	}

	/// <summary>
	/// Switch to an alternate pose set for a form the character takes mid-fight (Biden's Dark Brandon): "dark_" loads
	/// dark_combat_idle.png and so on. A pose without its own painting keeps the normal one, shown with <paramref name="tint"/>.
	/// Null switches back.
	/// </summary>
	public void SetVariant(string? prefix, Color tint)
	{
		_variantPoses.Clear();
		if (!string.IsNullOrEmpty(prefix))
		{
			LoadPoses(prefix, _variantPoses);
		}
		_tint = string.IsNullOrEmpty(prefix) || _variantPoses.Count > 0 ? Colors.White : tint;
		SetPose(_action == "dead" ? "hurt" : _action);
	}

	/// <summary>
	/// Loads one pose set. Paintings made one by one are each fitted to their own share of the figure height. A set cut
	/// from one pose sheet (art_post.pose_sheet) has images of exactly the same height, drawn at one scale: those all get
	/// the full height, so the character keeps one size across poses.
	/// </summary>
	private void LoadPoses(string prefix, Dictionary<string, (Texture2D Tex, float Height)> into)
	{
		foreach ((string pose, string file, float scale) in Poses)
		{
			Texture2D? tex = CharacterArt.Load(ArtDir + prefix + file + ".png");
			if (tex != null)
			{
				into[pose] = (tex, FigureHeight * scale);
			}
		}
		if (into.Count > 1 && into.Values.Select(p => p.Tex.GetHeight()).Distinct().Count() == 1)
		{
			foreach (string pose in into.Keys.ToList())
			{
				into[pose] = (into[pose].Tex, FigureHeight);
			}
		}
	}

	/// <summary>The painting's rectangle on screen (global coordinates), e.g. to start an effect at the character's eyes.</summary>
	public Rect2? FigureRect => _sprite == null || _sprite.Texture == null ? null : _sprite.GetGlobalTransform() * _sprite.GetRect();

	private void SetPose(string pose)
	{
		if (_variantPoses.Count > 0 && (_variantPoses.TryGetValue(pose, out var v) || _variantPoses.TryGetValue("idle", out v)) && _sprite != null)
		{
			CharacterArt.FitByFeet(_sprite, v.Tex, v.Height);
			_baseScale = _sprite.Scale;
			return;
		}
		if (_sprite == null || (!_poses.TryGetValue(pose, out var p) && !_poses.TryGetValue("idle", out p)))
		{
			return;
		}
		CharacterArt.FitByFeet(_sprite, p.Tex, p.Height);
		_baseScale = _sprite.Scale;
	}

	public override void _Process(double delta)
	{
		if (_sprite == null)
		{
			return;
		}
		_clock += delta;
		Vector2 offset = Vector2.Zero;
		float alpha = 1f;
		if (_action != "idle")
		{
			_time += (float)delta;
			float k = _duration > 0f ? Mathf.Clamp(_time / _duration, 0f, 1f) : 1f;
			switch (_action)
			{
				case "attack":
					offset = new Vector2(70f, -10f) * Mathf.Sin(Mathf.Pi * k);
					break;
				case "cast":
					offset.Y = -14f * Mathf.Sin(Mathf.Pi * k);
					break;
				case "hurt":
					offset.X = -22f * (1f - k) + 5f * Mathf.Sin(_time * 55f) * (1f - k);
					break;
				case "dead":
					offset = new Vector2(-20f, 30f) * k;
					alpha = 1f - k;
					break;
			}
			if (k >= 1f && _action != "dead")
			{
				_action = "idle";
				SetPose("idle");
			}
		}
		// Breathing: a slow stretch from the feet up, off while dead.
		float breath = _dead ? 0f : 0.012f * Mathf.Sin((float)_clock * 2.4f);
		_sprite.Scale = new Vector2(_baseScale.X * (1f - breath * 0.5f), _baseScale.Y * (1f + breath));
		_sprite.Position = _home + offset;
		_sprite.SelfModulate = new Color(_tint.R, _tint.G, _tint.B, alpha);
	}
}
