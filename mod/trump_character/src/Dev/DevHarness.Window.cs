using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace TrumpMod.Dev;

/// <summary>
/// Parallel batch runs (--trump-tile slot/count, set by scripts/test.py): each game is a borderless window in a grid
/// on the primary monitor, so all of them can be watched at once. Set straight on the DisplayServer, never through the
/// game's settings, and re-applied for the first seconds because the game applies its own display settings at startup.
/// With --trump-mute the game is silenced the same way (FMOD master volume via TestMutePatch, plus Godot's bus).
/// </summary>
public static partial class DevHarness
{
	private const ulong TileForMs = 20000;

	private static ulong _tileStartMs;

	private static ulong _lastTileMs;

	private static void KeepWindowTiled()
	{
		string? tile = CommandLineHelper.GetValue("trump-tile");
		bool mute = CommandLineHelper.HasArg("trump-mute");
		if (tile == null && !mute)
		{
			return;
		}
		ulong now = Time.GetTicksMsec();
		if (_tileStartMs == 0)
		{
			_tileStartMs = now;
		}
		if (now - _tileStartMs > TileForMs || now - _lastTileMs < 500)
		{
			return;
		}
		_lastTileMs = now;
		if (mute)
		{
			AudioServer.SetBusMute(0, true);
			NGame.Instance?.AudioManager?.SetMasterVol(0f);
		}
		if (tile == null)
		{
			return;
		}
		string[] parts = tile.Split('/');
		if (parts.Length != 2 || !int.TryParse(parts[0], out int slot) || !int.TryParse(parts[1], out int count) || count <= 0)
		{
			return;
		}
		int columns = (int)Math.Ceiling(Math.Sqrt(count));
		int rows = (int)Math.Ceiling(count / (double)columns);
		int screen = DisplayServer.GetPrimaryScreen();
		Vector2I origin = DisplayServer.ScreenGetPosition(screen);
		Vector2I size = DisplayServer.ScreenGetSize(screen);
		var cell = new Vector2I(size.X / columns, size.Y / rows);
		if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
		DisplayServer.WindowSetSize(cell);
		DisplayServer.WindowSetPosition(origin + new Vector2I(slot % columns * cell.X, slot / columns * cell.Y));
	}
}
