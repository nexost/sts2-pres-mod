using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace PresMod.Dev;

/// <summary>
/// Parallel batch runs (--pres-tile slot/count, set by scripts/test.py): each game is a borderless window in a grid
/// on the primary monitor, so all of them can be watched at once. Set straight on the DisplayServer, never through the
/// game's settings, and re-applied for the first seconds because the game applies its own display settings at startup.
/// With --pres-mute the game is silenced the same way (FMOD master volume via TestMutePatch, plus Godot's bus).
/// --pres-window fullscreen keeps the window a borderless fullscreen one (it doesn't minimize when another window
/// takes focus, so a trailer capture keeps its full 4K frame); --pres-window screenN puts a small window on screen N
/// (the co-op trailer's second instance, out of the way on another monitor).
/// </summary>
public static partial class DevHarness
{
	private const ulong TileForMs = 20000;

	private static ulong _tileStartMs;

	private static ulong _lastTileMs;

	private static void KeepWindowTiled()
	{
		string? tile = CommandLineHelper.GetValue("pres-tile");
		bool mute = CommandLineHelper.HasArg("pres-mute");
		string? window = CommandLineHelper.GetValue("pres-window");
		if (tile == null && !mute && window == null)
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
		if (window == "fullscreen")
		{
			if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
			{
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			}
		}
		else if (window != null && window.StartsWith("screen", StringComparison.Ordinal) && int.TryParse(window.AsSpan(6), out int screenIndex)
			&& screenIndex < DisplayServer.GetScreenCount())
		{
			if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
			{
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			}
			Vector2I at = DisplayServer.ScreenGetPosition(screenIndex);
			DisplayServer.WindowSetSize(new Vector2I(1280, 720));
			DisplayServer.WindowSetPosition(at + new Vector2I(40, 40));
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
