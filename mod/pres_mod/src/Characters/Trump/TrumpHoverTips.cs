using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using PresMod.Characters.Trump.Cards;
using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump;

/// <summary>
/// Keyword tooltips for our mechanics. Text lives in our static_hover_tips.json (merged into the game's table);
/// Wall and Tariff reuse their power tooltips.
/// </summary>
public static class TrumpHoverTips
{
	private static IHoverTip Static(string key) =>
		new HoverTip(new LocString("static_hover_tips", key + ".title"), new LocString("static_hover_tips", key + ".description"));

	public static IHoverTip Build => Static("TRUMP_BUILD");

	public static IHoverTip Section => Static("TRUMP_SECTION");

	public static IHoverTip Deport => Static("TRUMP_DEPORT");

	public static IHoverTip PayGold => Static("TRUMP_PAY_GOLD");

	public static IHoverTip Wall => HoverTipFactory.FromPower<WallPower>();

	public static IHoverTip Tariff => HoverTipFactory.FromPower<TariffPower>();

	public static IHoverTip Tweet => HoverTipFactory.FromCard<Tweet>();
}
