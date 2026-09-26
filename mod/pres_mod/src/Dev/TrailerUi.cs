using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace PresMod.Dev;

/// <summary>
/// Hides parts of the game's interface for trailer shots. The game has its own trailer mode (the "trailer" console
/// command, then keys 0-9), but each key pops a "Hide Combat UI" message on screen, so the same states are set here
/// directly: NCombatUi's debug flags (its own DebugHideCombatUi applies them), the top bar and relic bar, and the
/// version labels. Visual only; nothing about the fight changes.
/// </summary>
internal static class TrailerUi
{
	/// <summary>What a shot shows. Full: the game as played. Clean: the fight and its effects, no interface.</summary>
	internal sealed record Look(bool TopBar = true, bool CombatUi = true, bool Hand = true, bool PlayedCard = true, bool TextVfx = true)
	{
		public static readonly Look Full = new Look();

		public static readonly Look Clean = new Look(TopBar: false, CombatUi: false, Hand: false, PlayedCard: false);
	}

	private static Node Root => ((SceneTree)Engine.GetMainLoop()).Root;

	public static void Apply(Look look)
	{
		HideVersionLabels();
		ParkMouse();
		// NCombatUi keeps these as statics and re-applies them when a new combat UI is made.
		SetStatic(typeof(NCombatUi), "_isDebugHidden", !look.CombatUi);
		SetStatic(typeof(NCombatUi), "_isDebugHidingHand", !look.Hand);
		SetStaticProperty(typeof(NCombatUi), "IsDebugHidingPlayContainer", !look.PlayedCard);
		SetStaticProperty(typeof(NCombatUi), "IsDebugHideTextVfx", !look.TextVfx);
		if (NCombatRoom.Instance?.Ui is NCombatUi ui)
		{
			Traverse.Create(ui).Method("DebugHideCombatUi").GetValue();
		}
		foreach (NTopBar bar in UiHelper.FindAll<NTopBar>(Root))
		{
			bar.Modulate = look.TopBar ? Colors.White : Colors.Transparent;
		}
		foreach (NRelicInventory relics in UiHelper.FindAll<NRelicInventory>(Root))
		{
			relics.Modulate = look.TopBar ? Colors.White : Colors.Transparent;
		}
	}

	/// <summary>Moves the pointer to an empty strip on the left edge, so no hover tooltip (an enemy's intent) opens.</summary>
	public static void ParkMouse()
	{
		Viewport viewport = Root.GetViewport();
		Vector2 size = viewport.GetVisibleRect().Size;
		viewport.WarpMouse(new Vector2(4f, size.Y * 0.42f));
	}

	/// <summary>The build number, seed and "Modded" labels in the corners (the game's key 0 in trailer mode).</summary>
	public static void HideVersionLabels()
	{
		foreach (NDebugInfoLabelManager labels in UiHelper.FindAll<NDebugInfoLabelManager>(Root))
		{
			foreach (string field in new[] { "_releaseInfo", "_moddedWarning", "_seed", "_modWarningContainer" })
			{
				if (Traverse.Create(labels).Field(field).GetValue() is CanvasItem item)
				{
					item.Visible = false;
				}
			}
		}
	}

	private static void SetStatic(Type type, string field, bool value)
	{
		AccessTools.Field(type, field)?.SetValue(null, value);
	}

	private static void SetStaticProperty(Type type, string property, bool value)
	{
		AccessTools.PropertySetter(type, property)?.Invoke(null, new object[] { value });
	}
}
