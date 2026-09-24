using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden;

/// <summary>
/// Keyword tooltips for Sleepy Joe's mechanics. Text lives in our static_hover_tips.json (merged into the game's table);
/// Drowsy and Dark Brandon reuse their power tooltips.
/// </summary>
public static class BidenHoverTips
{
	private static IHoverTip Static(string key) =>
		new HoverTip(new LocString("static_hover_tips", key + ".title"), new LocString("static_hover_tips", key + ".description"));

	public static IHoverTip Doze => Static("BIDEN_DOZE");

	public static IHoverTip NodOff => Static("BIDEN_NOD_OFF");

	public static IHoverTip WakeUp => Static("BIDEN_WAKE_UP");

	public static IHoverTip LaserEyes => Static("BIDEN_LASER_EYES");

	public static IHoverTip Tangent => Static("BIDEN_TANGENT");

	public static IHoverTip Drowsy => HoverTipFactory.FromPower<DrowsyPower>();

	public static IHoverTip DarkBrandon => HoverTipFactory.FromPower<DarkBrandonPower>();
}
