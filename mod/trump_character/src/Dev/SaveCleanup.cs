using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace TrumpMod.Dev;

/// <summary>
/// Launched by the uninstaller with --trump-cleanup: removes run saves and run history that reference this mod's
/// content, so the game can't choke on them once the mod is gone. Deleting goes through the game's own save store,
/// which also deletes the Steam Cloud copy (otherwise cloud sync would bring the files straight back).
/// Every removed file is first copied to --trump-out/backup/. Then the game quits.
/// </summary>
public static class SaveCleanup
{
	public const string ArgMode = "trump-cleanup";

	private static readonly string[] _runSaveFiles = { "current_run.save", "current_run.save.backup", "current_run_mp.save", "current_run_mp.save.backup" };

	private static bool _done;

	public static bool Enabled => CommandLineHelper.HasArg(ArgMode);

	public static void InitIfRequested()
	{
		if (Enabled)
		{
			((SceneTree)Engine.GetMainLoop()).ProcessFrame += Tick;
		}
	}

	private static void Tick()
	{
		// Wait until startup (including cloud sync) is finished and the main menu exists.
		if (_done || NGame.Instance == null || NGame.Instance.GetNodeOrNull("/root/Game/RootSceneContainer/MainMenu") == null)
		{
			return;
		}
		_done = true;
		string outDir = CommandLineHelper.GetValue("trump-out") ?? ProjectSettings.GlobalizePath("user://trump_cleanup");
		var removed = new List<string>();
		var errors = new List<string>();
		try
		{
			ISaveStore store = Traverse.Create(SaveManager.Instance).Field("_saveStore").GetValue<ISaveStore>();
			string[] ids = typeof(ModEntry).Assembly.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)))
				.Select(t => "\"" + ModelDb.GetId(t) + "\"")
				.ToArray();
			for (int profile = 1; profile <= 3; profile++)
			{
				var candidates = _runSaveFiles.Select(f => RunSaveManager.GetRunSavePath(profile, f)).ToList();
				string historyDir = RunHistorySaveManager.GetHistoryPath(profile);
				if (store.DirectoryExists(historyDir))
				{
					candidates.AddRange(store.GetFilesInDirectory(historyDir).Where(f => f.EndsWith(".run")).Select(f => Path.Combine(historyDir, Path.GetFileName(f))));
				}
				foreach (string path in candidates)
				{
					try
					{
						if (!store.FileExists(path))
						{
							continue;
						}
						string? content = store.ReadFile(path);
						if (content == null || !ids.Any(content.Contains))
						{
							continue;
						}
						string backup = Path.Combine(outDir, "backup", path.Replace('/', Path.DirectorySeparatorChar));
						Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
						File.WriteAllText(backup, content);
						store.DeleteFile(path);
						removed.Add(path);
						Log.Info($"[{ModEntry.ModId}:cleanup] Removed {path} (backup at {backup})");
					}
					catch (Exception e)
					{
						errors.Add($"{path}: {e.Message}");
					}
				}
			}
		}
		catch (Exception e)
		{
			errors.Add(e.ToString());
		}
		Directory.CreateDirectory(outDir);
		File.WriteAllText(Path.Combine(outDir, "cleanup_result.json"), JsonSerializer.Serialize(new { ok = errors.Count == 0, removed, errors }, new JsonSerializerOptions { WriteIndented = true }));
		((SceneTree)Engine.GetMainLoop()).Quit(errors.Count == 0 ? 0 : 1);
	}
}
