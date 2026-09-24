using System.Collections.Generic;
using Godot;

namespace PresMod.Framework;

/// <summary>
/// Art helpers shared by every character. A character's own generated art (poses, Wall stages, stamps, ...) lives in
/// res://images/&lt;id&gt;/, where &lt;id&gt; is the lowercase model ID (trump). The figures are trimmed cut-outs of any size,
/// so they're placed at runtime: feet on the node's origin, scaled to a height in pixels.
/// </summary>
public static class CharacterArt
{
	/// <summary>res://images/&lt;id&gt;/ for a character ID entry such as TRUMP.</summary>
	public static string Dir(string characterEntry) => $"res://images/{characterEntry.ToLowerInvariant()}/";

	private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D?>();

	/// <summary>The texture at a res:// path, or null when that art hasn't been made yet.</summary>
	public static Texture2D? Load(string path)
	{
		if (!Cache.TryGetValue(path, out Texture2D? tex))
		{
			tex = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
			Cache[path] = tex;
		}
		return tex;
	}

	/// <summary>Shows tex on the sprite with its bottom-center on the sprite's position, height pixels tall. Keeps a horizontal flip.</summary>
	public static void FitByFeet(Sprite2D sprite, Texture2D tex, float height)
	{
		sprite.Texture = tex;
		sprite.Centered = false;
		sprite.Offset = new Vector2(-tex.GetWidth() / 2f, -tex.GetHeight());
		float s = height / tex.GetHeight();
		float flip = sprite.Scale.X < 0f ? -1f : 1f;
		sprite.Scale = new Vector2(s * flip, s);
	}
}
