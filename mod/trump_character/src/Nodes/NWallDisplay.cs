using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using TrumpMod.Mechanics;

namespace TrumpMod.Nodes;

/// <summary>
/// The Wall in front of The Donald: grows with height, changes material per stage (fence → brick → concrete → gold),
/// shows height, Sections and progress to the next stage, and a banner on stage-up.
/// Step 8: each stage is drawn from its generated painting (images/trump/wall/wall_stage_N.png): the top of the
/// painting caps the Wall and a strip from its lower half repeats below as the Wall grows. Before the art exists,
/// or before the first stage, it falls back to the Step 4 drawing.
/// </summary>
public partial class NWallDisplay : Node2D
{
	private const float Width = 150f;

	private const float MaxPixelHeight = 230f;

	private static readonly Color Ink = new Color("1a1410");

	private static readonly Color Cream = new Color("efe6d2");

	private static readonly Color Gold = new Color("f2b92e");

	public Creature? Creature { get; set; }

	private float _shownHeight;

	private int _height;

	private int _stage;

	private string? _banner;

	private float _bannerTime;

	private Font? _font;

	private static readonly Texture2D?[] StageArt = new Texture2D?[5];

	private static readonly Rect2[] StageArtUsed = new Rect2[5];

	private static bool _artLoaded;

	public override void _Ready()
	{
		_font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres") ?? ThemeDB.FallbackFont;
		ZIndex = 5;
		LoadStageArt();
	}

	/// <summary>On-screen height of the Wall art in pixels for a given (animated) Wall height.</summary>
	private static float PixelHeight(float height) => height <= 0f ? 0f : Mathf.Min(14f + height * 2.2f, MaxPixelHeight);

	/// <summary>Where the top of the Wall is on screen (the Build dust).</summary>
	public Vector2 TopGlobalPosition => ToGlobal(new Vector2(Width * 0.5f, -PixelHeight(WallTarget)));

	/// <summary>The middle of the Wall on screen (rubble when height is spent).</summary>
	public Vector2 MiddleGlobalPosition => ToGlobal(new Vector2(Width * 0.5f, -PixelHeight(Mathf.Max(_shownHeight, WallTarget)) * 0.5f));

	private float WallTarget => Creature == null ? _shownHeight : WallCmd.GetHeight(Creature);

	public void ShowStageBanner(int stage)
	{
		_banner = new LocString("powers", $"WALL_STAGE_POWER.name{stage}").GetRawText().ToUpperInvariant() + "!";
		_bannerTime = 2.4f;
	}

	public override void _Process(double delta)
	{
		if (Creature == null)
		{
			return;
		}
		_height = WallCmd.GetHeight(Creature);
		_stage = WallCmd.GetStage(Creature);
		_shownHeight = Mathf.MoveToward(_shownHeight, _height, (float)delta * Mathf.Max(40f, Mathf.Abs(_height - _shownHeight) * 4f));
		if (_bannerTime > 0f)
		{
			_bannerTime -= (float)delta;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_font == null)
		{
			return;
		}
		float h = PixelHeight(_shownHeight);
		int visualStage = WallRules.StageFor(Mathf.RoundToInt(_shownHeight));

		// Foundation line so the player can see where the wall will rise.
		DrawRect(new Rect2(-6f, -3f, Width + 12f, 6f), new Color(0.2f, 0.17f, 0.13f, 0.85f));
		if (h > 0f)
		{
			DrawWallBody(h, visualStage);
		}

		// Height number and Sections above the wall.
		float top = -h - 10f;
		string heightText = _height.ToString();
		DrawStringOutline(_font, new Vector2(-20f, top - 26f), heightText, HorizontalAlignment.Center, Width + 40f, 34, 8, Ink);
		DrawString(_font, new Vector2(-20f, top - 26f), heightText, HorizontalAlignment.Center, Width + 40f, 34, visualStage >= 4 ? Gold : Cream);
		int sections = WallRules.SectionsFor(_height);
		string sectionsKey = sections == 1 ? "WALL_POWER.sectionLabel" : "WALL_POWER.sectionsLabel";
		string sectionsText = new LocString("powers", sectionsKey).GetRawText().Replace("#", sections.ToString());
		DrawStringOutline(_font, new Vector2(-40f, top - 4f), sectionsText, HorizontalAlignment.Center, Width + 80f, 15, 5, Ink);
		DrawString(_font, new Vector2(-40f, top - 4f), sectionsText, HorizontalAlignment.Center, Width + 80f, 15, Cream);

		// Progress to the next stage under the foundation.
		int? next = WallRules.NextStageHeight(_stage);
		if (next.HasValue)
		{
			int previous = _stage == 0 ? 0 : WallRules.StageHeights[_stage - 1];
			float t = Mathf.Clamp((_height - previous) / (float)(next.Value - previous), 0f, 1f);
			DrawRect(new Rect2(0f, 10f, Width, 7f), new Color(0f, 0f, 0f, 0.6f));
			DrawRect(new Rect2(1f, 11f, (Width - 2f) * t, 5f), Gold);
			string nextText = new LocString("powers", $"WALL_STAGE_POWER.name{_stage + 1}").GetRawText() + " " + next.Value;
			DrawStringOutline(_font, new Vector2(-40f, 36f), nextText, HorizontalAlignment.Center, Width + 80f, 13, 4, Ink);
			DrawString(_font, new Vector2(-40f, 36f), nextText, HorizontalAlignment.Center, Width + 80f, 13, Cream);
		}

		if (_bannerTime > 0f && _banner != null)
		{
			float alpha = Mathf.Clamp(_bannerTime / 0.6f, 0f, 1f);
			float rise = (2.4f - _bannerTime) * 18f;
			Vector2 pos = new Vector2(-120f, top - 60f - rise);
			DrawStringOutline(_font, pos, _banner, HorizontalAlignment.Center, Width + 240f, 30, 10, new Color(Ink, alpha));
			DrawString(_font, pos, _banner, HorizontalAlignment.Center, Width + 240f, 30, new Color(Gold, alpha));
		}
	}

	private static void LoadStageArt()
	{
		if (_artLoaded)
		{
			return;
		}
		_artLoaded = true;
		for (int stage = 1; stage <= 4; stage++)
		{
			Texture2D? tex = TrumpArt.Load($"{TrumpArt.Dir}wall/wall_stage_{stage}.png");
			if (tex == null)
			{
				continue;
			}
			// The painting sits on a transparent canvas: use only its visible part.
			Rect2I used = tex.GetImage()?.GetUsedRect() ?? new Rect2I(0, 0, tex.GetWidth(), tex.GetHeight());
			if (used.Size.X < 8 || used.Size.Y < 8)
			{
				continue;
			}
			StageArt[stage] = tex;
			StageArtUsed[stage] = new Rect2(used.Position, used.Size);
		}
	}

	private bool DrawStageArt(float h, int stage)
	{
		Texture2D? tex = stage is >= 1 and <= 4 ? StageArt[stage] : null;
		if (tex == null)
		{
			return false;
		}
		Rect2 used = StageArtUsed[stage];
		float s = Width / used.Size.X;
		float capHeight = used.Size.Y * s;
		if (h <= capHeight)
		{
			// Still short: show the top of the painting, as if the rest is below ground.
			DrawTextureRectRegion(tex, new Rect2(0f, -h, Width, h), new Rect2(used.Position, new Vector2(used.Size.X, h / s)));
			return true;
		}
		if (h <= capHeight * 1.6f)
		{
			// A bit taller than the painting: stretch it, which reads better than a repeated band.
			DrawTextureRectRegion(tex, new Rect2(0f, -h, Width, h), used);
			return true;
		}
		// Much taller: the painting on top, then a strip from its lower half repeated down to the ground.
		DrawTextureRectRegion(tex, new Rect2(0f, -h, Width, capHeight), used);
		var strip = new Rect2(used.Position.X, used.Position.Y + used.Size.Y * 0.5f, used.Size.X, used.Size.Y * 0.45f);
		float stripHeight = strip.Size.Y * s;
		for (float y = -h + capHeight; y < 0f; y += stripHeight)
		{
			float part = Mathf.Min(stripHeight, -y);
			DrawTextureRectRegion(tex, new Rect2(0f, y, Width, part), new Rect2(strip.Position, new Vector2(strip.Size.X, part / s)));
		}
		return true;
	}

	private void DrawWallBody(float h, int stage)
	{
		if (DrawStageArt(h, stage))
		{
			return;
		}
		switch (stage)
		{
			case 0:
				// Survey stakes before the first Section.
				for (float x = 6f; x < Width; x += 20f)
				{
					DrawRect(new Rect2(x, -h, 6f, h), new Color("7a5a36"));
				}
				DrawLine(new Vector2(0f, -h * 0.6f), new Vector2(Width, -h * 0.6f), new Color("e0c040"), 2f);
				break;
			case 1:
				DrawRect(new Rect2(0f, -h, Width, h), new Color(0.55f, 0.58f, 0.6f, 0.25f));
				var wire = new Color(0.75f, 0.78f, 0.8f, 0.8f);
				for (float d = -h; d < Width; d += 12f)
				{
					DrawClippedLine(new Vector2(d, 0f), new Vector2(d + h, -h), wire);
					DrawClippedLine(new Vector2(d + h, 0f), new Vector2(d, -h), wire);
				}
				foreach (float x in new[] { 0f, Width / 2f - 2f, Width - 4f })
				{
					DrawRect(new Rect2(x, -h - 6f, 4f, h + 6f), new Color("5b6166"));
				}
				break;
			case 2:
				DrawBricks(h, new Color("8a3b2a"), new Color("c9b8a0"), 13f);
				break;
			case 3:
				DrawRect(new Rect2(0f, -h, Width, h), new Color("9a9a96"));
				for (float x = 0f; x < Width; x += 29f)
				{
					DrawLine(new Vector2(x, 0f), new Vector2(x, -h), new Color("6e6e6a"), 2f);
				}
				for (float y = 0f; y > -h; y -= 40f)
				{
					DrawLine(new Vector2(0f, y), new Vector2(Width, y), new Color("7d7d79"), 2f);
				}
				break;
			default:
				DrawBricks(h, new Color("d9a520"), new Color("7a5200"), 13f);
				DrawRect(new Rect2(-4f, -h - 8f, Width + 8f, 8f), new Color("ffe38a"));
				break;
		}
		DrawRect(new Rect2(0f, -h, Width, h), Ink, false, 2f);
	}

	/// <summary>Draws the part of a line that falls between x = 0 and x = Width (the fence mesh).</summary>
	private void DrawClippedLine(Vector2 from, Vector2 to, Color color)
	{
		float t0 = 0f;
		float t1 = 1f;
		float dx = to.X - from.X;
		if (Mathf.Abs(dx) > 0.001f)
		{
			float ta = (0f - from.X) / dx;
			float tb = (Width - from.X) / dx;
			t0 = Mathf.Max(t0, Mathf.Min(ta, tb));
			t1 = Mathf.Min(t1, Mathf.Max(ta, tb));
		}
		if (t1 > t0)
		{
			DrawLine(from.Lerp(to, t0), from.Lerp(to, t1), color, 1.5f);
		}
	}

	private void DrawBricks(float h, Color brick, Color mortar, float rowHeight)
	{
		DrawRect(new Rect2(0f, -h, Width, h), mortar);
		int row = 0;
		for (float y = 0f; y > -h; y -= rowHeight, row++)
		{
			float rowTop = Mathf.Max(y - rowHeight + 2f, -h);
			float offset = row % 2 == 0 ? 0f : -14f;
			for (float x = offset; x < Width; x += 28f)
			{
				float x0 = Mathf.Max(x + 1f, 0f);
				float x1 = Mathf.Min(x + 27f, Width);
				if (x1 > x0)
				{
					DrawRect(new Rect2(x0, rowTop, x1 - x0, y - rowTop - 1f), brick);
				}
			}
		}
	}
}
