using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;

namespace TrumpMod.Patches;

/// <summary>
/// Sprites inside res://images/atlases/&lt;atlas&gt;.sprites/ are served from prebuilt atlases. The game already
/// falls back to loose PNGs for cards, relics, powers and potions, but not for ui_atlas (e.g. the card energy icon).
/// Add one more fallback: res://trump_character/atlas_fallback/&lt;atlas&gt;/&lt;sprite&gt;.png
/// </summary>
[HarmonyPatch(typeof(AtlasResourceLoader), "GetFallbackPath")]
public static class AtlasFallbackPatch
{
	public static void Postfix(string atlasName, string spriteName, ref string? __result)
	{
		if (__result != null)
		{
			return;
		}
		string path = $"{ModEntry.ResRoot}/atlas_fallback/{atlasName}/{spriteName}.png";
		if (ResourceLoader.Exists(path))
		{
			__result = path;
		}
	}
}
