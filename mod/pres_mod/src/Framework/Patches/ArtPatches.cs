using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using PresMod.Framework.Nodes;

namespace PresMod.Framework.Patches;

/// <summary>
/// Characters without Spine rigs: their combat, shop and rest-site scenes hold plain sprites of generated paintings
/// (images/&lt;id&gt;/combat_idle.png, merchant_pose.png, rest_site_pose.png, ...). These patches place the paintings and
/// drive the combat poses. They only touch a mod character's own scenes, and only when those use sprites
/// (a combat scene whose Visuals is a Sprite2D; a shop or rest-site scene with a Sprite2D named CharacterSprite),
/// so a character that later gets a Spine rig simply stops matching.
/// </summary>
public static class ArtPatches
{
	/// <summary>The name of the Sprite2D that holds the painting in the shop and rest-site scenes.</summary>
	public const string SpriteName = "CharacterSprite";

	public static string CombatScene(CharacterModel character) => SceneHelper.GetScenePath("creature_visuals/" + character.Id.Entry.ToLowerInvariant());

	/// <summary>The mod character whose scene this is, if any.</summary>
	private static (CharacterModel Character, IModCharacter Mod)? Owner(Node node, System.Func<CharacterModel, string> scenePath)
	{
		string path = node.SceneFilePath;
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		foreach (CharacterModel character in ModContent.Characters)
		{
			if (character is IModCharacter mod && scenePath(character) == path)
			{
				return (character, mod);
			}
		}
		return null;
	}

	/// <summary>Combat: give a sprite-bodied mod character the pose controller.</summary>
	[HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals._Ready))]
	public static class CreatureVisualsReady
	{
		public static void Postfix(NCreatureVisuals __instance)
		{
			if (__instance.GetNodeOrNull(NCharacterPoses.NodeName) != null || __instance.GetNodeOrNull("%Visuals") is not Sprite2D)
			{
				return;
			}
			if (Owner(__instance, CombatScene) is { } owner)
			{
				__instance.AddChild(new NCharacterPoses
				{
					Name = NCharacterPoses.NodeName,
					ArtDir = CharacterArt.Dir(owner.Character.Id.Entry),
					FigureHeight = owner.Mod.CombatFigureHeight
				});
			}
		}
	}

	/// <summary>Combat: the game's animation triggers (a no-op for sprite bodies) switch the pose.</summary>
	[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
	public static class AnimationTrigger
	{
		public static void Postfix(NCreature __instance, string trigger)
		{
			__instance.Visuals?.GetNodeOrNull<NCharacterPoses>(NCharacterPoses.NodeName)?.Play(trigger);
		}
	}

	/// <summary>Shop: the game's script starts a Spine animation on its first child; ours is a painting, so place it instead.</summary>
	[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
	public static class MerchantReady
	{
		public static bool Prefix(NMerchantCharacter __instance)
		{
			if (__instance.GetNodeOrNull<Sprite2D>(SpriteName) == null || Owner(__instance, c => c.MerchantAnimPath) is not { } owner)
			{
				return true;
			}
			Place(__instance, owner.Character, "merchant_pose", owner.Mod.MerchantFigureHeight);
			return false;
		}
	}

	/// <summary>Shop: PlayAnimation would call Spine on the painting.</summary>
	[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
	public static class MerchantAnimation
	{
		public static bool Prefix(NMerchantCharacter __instance) => __instance.GetNodeOrNull<Sprite2D>(SpriteName) == null;
	}

	/// <summary>Rest site: the game's script only animates Spine children, so the painting just needs placing.</summary>
	[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
	public static class RestSiteReady
	{
		public static void Postfix(NRestSiteCharacter __instance)
		{
			if (__instance.GetNodeOrNull<Sprite2D>(SpriteName) != null && Owner(__instance, c => c.RestSiteAnimPath) is { } owner)
			{
				Place(__instance, owner.Character, "rest_site_pose", owner.Mod.RestSiteFigureHeight);
			}
		}
	}

	/// <summary>Rest site: co-op seats flip the character; the game only flips Spine children.</summary>
	[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
	public static class RestSiteFlip
	{
		public static void Postfix(NRestSiteCharacter __instance)
		{
			if (__instance.GetNodeOrNull<Sprite2D>(SpriteName) is Sprite2D sprite)
			{
				// FlipH, not a negative scale: the idle tween animates the scale.
				sprite.FlipH = !sprite.FlipH;
				sprite.Position = new Vector2(-sprite.Position.X, sprite.Position.Y);
			}
		}
	}

	private static void Place(Node host, CharacterModel character, string file, float height)
	{
		if (host.GetNodeOrNull<Sprite2D>(SpriteName) is not Sprite2D sprite)
		{
			return;
		}
		Texture2D? tex = CharacterArt.Load(CharacterArt.Dir(character.Id.Entry) + file + ".png");
		if (tex != null)
		{
			CharacterArt.FitByFeet(sprite, tex, height);
		}
		// A gentle idle, like the game's relaxed loops.
		Tween tween = sprite.CreateTween().SetLoops();
		Vector2 scale = sprite.Scale;
		tween.TweenProperty(sprite, "scale", new Vector2(scale.X * 0.995f, scale.Y * 1.012f), 1.3).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(sprite, "scale", scale, 1.3).SetTrans(Tween.TransitionType.Sine);
	}
}
