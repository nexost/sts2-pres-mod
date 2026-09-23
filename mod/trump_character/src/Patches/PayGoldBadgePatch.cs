using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TrumpMod.Models.Cards;

namespace TrumpMod.Patches;

/// <summary>
/// Pay-Gold cards show their gold cost in the card's second cost badge (the one Regent uses for Stars):
/// coin icon, the amount, red when you can't afford it. Card nodes are reused, so other cards get the star back.
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateStarCostVisuals")]
public static class PayGoldBadgePatch
{
	private const string OriginalTextureMeta = "trump_star_icon_texture";

	private static Texture2D? _coin;

	private static Texture2D? Coin => _coin ??= ResourceLoader.Load<Texture2D>($"{ModEntry.ResRoot}/ui/gold_cost_icon.png");

	public static void Postfix(NCard __instance, PileType pileType)
	{
		TextureRect? starIcon = Traverse.Create(__instance).Field<TextureRect>("_starIcon").Value;
		Control? starLabel = Traverse.Create(__instance).Field<Control>("_starLabel").Value;
		if (starIcon == null || starLabel == null)
		{
			return;
		}
		if (__instance.Model is PayGoldCard card)
		{
			if (!starIcon.HasMeta(OriginalTextureMeta) && starIcon.Texture != null)
			{
				starIcon.SetMeta(OriginalTextureMeta, starIcon.Texture);
			}
			if (Coin != null)
			{
				starIcon.Texture = Coin;
			}
			starIcon.Visible = true;
			Traverse.Create(starLabel).Method("SetTextAutoSize", card.GoldCost.ToString()).GetValue();
			starLabel.AddThemeColorOverride("font_color", card.CanAfford ? StsColors.cream : StsColors.red);
		}
		else if (starIcon.HasMeta(OriginalTextureMeta))
		{
			starIcon.Texture = starIcon.GetMeta(OriginalTextureMeta).As<Texture2D>();
			starIcon.RemoveMeta(OriginalTextureMeta);
		}
	}
}
