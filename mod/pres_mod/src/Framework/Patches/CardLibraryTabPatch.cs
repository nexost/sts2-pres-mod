using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace PresMod.Framework.Patches;

/// <summary>
/// P3: NCardLibrary only knows the five base characters. Opening it mid-run as a mod character indexes
/// _cardPoolFilters[character] and throws. Add a real filter tab for each of our characters instead.
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class CardLibraryTabPatch
{
	private static readonly MethodInfo _updateCardPoolFilter = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");

	public static void Postfix(NCardLibrary __instance)
	{
		try
		{
			var cardPoolFilters = Traverse.Create(__instance).Field<Dictionary<CharacterModel, NCardPoolFilter>>("_cardPoolFilters").Value;
			var poolFilters = Traverse.Create(__instance).Field<Dictionary<NCardPoolFilter, Func<CardModel, bool>>>("_poolFilters").Value;
			NCardPoolFilter template = Traverse.Create(__instance).Field<NCardPoolFilter>("_necrobinderFilter").Value;
			foreach (CharacterModel character in ModContent.Characters)
			{
				if (cardPoolFilters.ContainsKey(character))
				{
					continue;
				}
				NCardPoolFilter tab = CreateTab(__instance, template, character);
				CardPoolModel pool = character.CardPool;
				poolFilters[tab] = c => c.Pool == pool;
				cardPoolFilters[character] = tab;
			}
		}
		catch (Exception e)
		{
			Log.Error($"[{ModEntry.ModId}] Failed to add card library tab: {e}");
		}
	}

	private static NCardPoolFilter CreateTab(NCardLibrary library, NCardPoolFilter template, CharacterModel character)
	{
		// No DuplicateFlags.Signals: the template's Toggled connection would fire the handler twice.
		var flags = Node.DuplicateFlags.Groups | Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation;
		NCardPoolFilter tab = (NCardPoolFilter)template.Duplicate((int)flags);
		tab.Name = character.Id.Entry.ToLowerInvariant().Capitalize() + "Pool";
		tab.UniqueNameInOwner = false;

		// Set up the icon before the tab enters the tree: NCardPoolFilter._Ready caches Image's material for its
		// selection tint, and the HSV material is shared with the template, so give the tab its own first.
		TextureRect image = tab.GetNode<TextureRect>("Image");
		image.Texture = character.IconTexture;
		if (image.Material != null)
		{
			image.Material = (Material)image.Material.Duplicate();
		}
		TextureRect? shadow = image.GetNodeOrNull<TextureRect>("Shadow");
		if (shadow != null)
		{
			shadow.Texture = character.IconTexture;
		}

		template.GetParent().AddChild(tab);
		template.GetParent().MoveChild(tab, template.GetIndex() + 1);

		tab.Loc = new LocString("card_library", "POOL_" + character.Id.Entry + "_TIP");
		tab.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(f => _updateCardPoolFilter.Invoke(library, new object[] { f })));
		tab.Visible = true;
		return tab;
	}
}
