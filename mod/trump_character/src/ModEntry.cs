using System;
using System.Linq;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using TrumpMod.Dev;

namespace TrumpMod;

/// <summary>
/// Entry point called by the game's ModManager right after our DLL and PCK are loaded,
/// which is before LocManager and ModelDb initialize.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	public const string ModId = "trump_character";

	public const string ResRoot = "res://" + ModId;

	internal static Harmony Harmony { get; private set; } = null!;

	public static void Initialize()
	{
		Log.Info($"[{ModId}] Initializing");

		// Godot only knows about Node scripts in the game's own assembly. Register ours so scenes in our PCK
		// can use them (same call GodotPlugins.Game.Main makes for sts2.dll).
		ScriptManagerBridge.LookupScriptsInAssembly(typeof(ModEntry).Assembly);

		CheckModelIdCollisions();

		Harmony = new Harmony("exeet." + ModId);
		Harmony.PatchAll(typeof(ModEntry).Assembly);
		Log.Info($"[{ModId}] Applied {Harmony.GetPatchedMethods().Count()} Harmony patches");

		DevHarness.InitIfRequested();
		SaveCleanup.InitIfRequested();
	}

	/// <summary>
	/// Model IDs come from class names alone, so a mod class named like a game class silently replaces the game's model.
	/// </summary>
	private static void CheckModelIdCollisions()
	{
		var gameIds = AbstractModelSubtypes.All.Select(ModelDb.GetId).ToHashSet();
		var clashes = typeof(ModEntry).Assembly.GetTypes()
			.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)))
			.Where(t => gameIds.Contains(ModelDb.GetId(t)))
			.Select(t => $"{t.FullName} -> {ModelDb.GetId(t)}")
			.ToList();
		foreach (string clash in clashes)
		{
			Log.Error($"[{ModId}] Model ID collision, this class would replace a base game model: {clash}");
		}
		if (clashes.Count > 0)
		{
			throw new InvalidOperationException($"[{ModId}] {clashes.Count} model ID collision(s), see log");
		}
	}
}
