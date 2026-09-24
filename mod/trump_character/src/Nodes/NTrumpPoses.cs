using System.Collections.Generic;
using Godot;

namespace TrumpMod.Nodes;

/// <summary>
/// The Donald's combat body until a Spine rig exists: one generated painting per pose (idle, attack, cast, hurt) on the
/// scene's Visuals sprite, swapped on the game's animation triggers (ArtPatches) with a little motion on top:
/// a lunge for attacks, a hop for casts, a flinch and shake when hit, a slow sink on death, and a breathing idle.
/// </summary>
public partial class NTrumpPoses : Node
{
	public const string NodeName = "TrumpPoses";

	/// <summary>Pose → (image, on-screen height). Heights match Ironclad's combat size (bounds ~280 px).</summary>
	private static readonly (string Pose, string File, float Height)[] Poses =
	{
		("idle", "combat_idle", 290f),
		("attack", "combat_attack", 280f),
		("cast", "combat_cast", 290f),
		("hurt", "combat_hurt", 280f),
	};

	private readonly Dictionary<string, (Texture2D Tex, float Height)> _poses = new Dictionary<string, (Texture2D, float)>();

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
		foreach ((string pose, string file, float height) in Poses)
		{
			Texture2D? tex = TrumpArt.Load(TrumpArt.Dir + file + ".png");
			if (tex != null)
			{
				_poses[pose] = (tex, height);
			}
		}
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

	private void SetPose(string pose)
	{
		if (_sprite == null || (!_poses.TryGetValue(pose, out var p) && !_poses.TryGetValue("idle", out p)))
		{
			return;
		}
		TrumpArt.FitByFeet(_sprite, p.Tex, p.Height);
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
		_sprite.SelfModulate = new Color(1f, 1f, 1f, alpha);
	}
}
