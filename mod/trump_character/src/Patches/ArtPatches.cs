using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TrumpMod.Nodes;

namespace TrumpMod.Patches;

/// <summary>
/// Step 8: The Donald's scenes use plain sprites of generated paintings instead of Spine rigs.
/// These patches place the paintings and drive the combat poses; every other character is left alone.
/// </summary>
public static class ArtPatches
{
	public const string CombatScene = "res://scenes/creature_visuals/trump.tscn";

	public const string MerchantScene = "res://scenes/merchant/characters/trump_merchant.tscn";

	public const string RestSiteScene = "res://scenes/rest_site/characters/trump_rest_site.tscn";

	/// <summary>The name of the Sprite2D that holds the painting in the shop and rest-site scenes.</summary>
	public const string SpriteName = "TrumpSprite";

	/// <summary>Combat: give The Donald's visuals the pose controller.</summary>
	[HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals._Ready))]
	public static class CreatureVisualsReady
	{
		public static void Postfix(NCreatureVisuals __instance)
		{
			if (__instance.SceneFilePath == CombatScene && __instance.GetNodeOrNull(NTrumpPoses.NodeName) == null)
			{
				__instance.AddChild(new NTrumpPoses { Name = NTrumpPoses.NodeName });
			}
		}
	}

	/// <summary>Combat: the game's animation triggers (a no-op for sprite bodies) switch the pose.</summary>
	[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
	public static class AnimationTrigger
	{
		public static void Postfix(NCreature __instance, string trigger)
		{
			__instance.Visuals?.GetNodeOrNull<NTrumpPoses>(NTrumpPoses.NodeName)?.Play(trigger);
		}
	}

	/// <summary>Shop: the game's script starts a Spine animation on its first child; ours is a painting, so place it instead.</summary>
	[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
	public static class MerchantReady
	{
		public static bool Prefix(NMerchantCharacter __instance)
		{
			if (__instance.SceneFilePath != MerchantScene)
			{
				return true;
			}
			Place(__instance, "merchant_pose", 330f);
			return false;
		}
	}

	/// <summary>Shop: PlayAnimation would call Spine on the painting.</summary>
	[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
	public static class MerchantAnimation
	{
		public static bool Prefix(NMerchantCharacter __instance) => __instance.SceneFilePath != MerchantScene;
	}

	/// <summary>Rest site: the game's script only animates Spine children, so the painting just needs placing.</summary>
	[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
	public static class RestSiteReady
	{
		public static void Postfix(NRestSiteCharacter __instance)
		{
			if (__instance.SceneFilePath == RestSiteScene)
			{
				Place(__instance, "rest_site_pose", 560f);
			}
		}
	}

	/// <summary>Rest site: co-op seats flip the character; the game only flips Spine children.</summary>
	[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
	public static class RestSiteFlip
	{
		public static void Postfix(NRestSiteCharacter __instance)
		{
			if (__instance.SceneFilePath == RestSiteScene && __instance.GetNodeOrNull<Sprite2D>(SpriteName) is Sprite2D sprite)
			{
				// FlipH, not a negative scale: the idle tween animates the scale.
				sprite.FlipH = !sprite.FlipH;
				sprite.Position = new Vector2(-sprite.Position.X, sprite.Position.Y);
			}
		}
	}

	private static void Place(Node host, string file, float height)
	{
		if (host.GetNodeOrNull<Sprite2D>(SpriteName) is not Sprite2D sprite)
		{
			return;
		}
		Texture2D? tex = TrumpArt.Load(TrumpArt.Dir + file + ".png");
		if (tex != null)
		{
			TrumpArt.FitByFeet(sprite, tex, height);
		}
		// A gentle idle, like the game's relaxed loops.
		Tween tween = sprite.CreateTween().SetLoops();
		Vector2 scale = sprite.Scale;
		tween.TweenProperty(sprite, "scale", new Vector2(scale.X * 0.995f, scale.Y * 1.012f), 1.3).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(sprite, "scale", scale, 1.3).SetTrans(Tween.TransitionType.Sine);
	}
}
