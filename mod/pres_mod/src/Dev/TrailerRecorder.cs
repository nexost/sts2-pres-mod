using System.Diagnostics;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace PresMod.Dev;

/// <summary>
/// Records one trailer clip (mode "trailer", scripts/trailer_capture.py). The game runs with the engine's --fixed-fps 60,
/// so every frame is exactly 1/60 s of game time however long it takes to draw. Each frame drawn between Start and Stop
/// is read back from the screen and piped to ffmpeg, so a clip holds its shot at the window's full resolution with
/// no dropped frames, and nothing else. The game's sound goes through FMOD in real time, so clips are silent.
/// </summary>
internal sealed class TrailerRecorder
{
	private readonly Process _ffmpeg;

	private readonly Stream _pipe;

	private readonly Viewport _viewport;

	private readonly Vector2I _size;

	private readonly Image.Format _format;

	private string? _failure;

	private TrailerRecorder(Process ffmpeg, Viewport viewport, Vector2I size, Image.Format format, string file)
	{
		_ffmpeg = ffmpeg;
		_pipe = ffmpeg.StandardInput.BaseStream;
		_viewport = viewport;
		_size = size;
		_format = format;
		File = file;
	}

	public string File { get; }

	public int Frames { get; private set; }

	public Vector2I Size => _size;

	/// <summary>Starts ffmpeg and hooks the end of every frame. The clip is H.264 4:4:4 at a high constant quality (NVENC).</summary>
	public static TrailerRecorder Start(string ffmpegPath, string file, int fps)
	{
		Viewport viewport = ((SceneTree)Engine.GetMainLoop()).Root.GetViewport();
		Image probe = viewport.GetTexture().GetImage();
		Vector2I size = probe.GetSize();
		Image.Format format = probe.GetFormat() == Image.Format.Rgb8 ? Image.Format.Rgb8 : Image.Format.Rgba8;
		string pixelFormat = format == Image.Format.Rgb8 ? "rgb24" : "rgba";
		var info = new ProcessStartInfo(ffmpegPath)
		{
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		foreach (string arg in new[]
		{
			"-y", "-hide_banner", "-loglevel", "error",
			"-f", "rawvideo", "-pix_fmt", pixelFormat, "-s", $"{size.X}x{size.Y}", "-framerate", fps.ToString(), "-i", "-",
			"-c:v", "h264_nvenc", "-preset", "p7", "-tune", "hq", "-rc", "constqp", "-qp", "12",
			"-profile:v", "high444p", "-pix_fmt", "yuv444p", "-movflags", "+faststart", file
		})
		{
			info.ArgumentList.Add(arg);
		}
		Process ffmpeg = Process.Start(info) ?? throw new InvalidOperationException("ffmpeg did not start: " + ffmpegPath);
		var recorder = new TrailerRecorder(ffmpeg, viewport, size, format, file);
		ffmpeg.ErrorDataReceived += (_, e) =>
		{
			if (!string.IsNullOrWhiteSpace(e.Data))
			{
				Log.Error($"[{ModEntry.ModId}:trailer] ffmpeg: {e.Data}");
			}
		};
		ffmpeg.BeginErrorReadLine();
		RenderingServer.FramePostDraw += recorder.OnFrame;
		return recorder;
	}

	private void OnFrame()
	{
		if (_failure != null)
		{
			return;
		}
		try
		{
			Image image = _viewport.GetTexture().GetImage();
			if (image.GetSize() != _size)
			{
				// The window changed size mid-clip: scale back so the stream keeps one frame size.
				image.Resize(_size.X, _size.Y, Image.Interpolation.Bilinear);
			}
			if (image.GetFormat() != _format)
			{
				image.Convert(_format);
			}
			byte[] data = image.GetData();
			_pipe.Write(data, 0, data.Length);
			Frames++;
		}
		catch (Exception e)
		{
			_failure = e.Message;
			Log.Error($"[{ModEntry.ModId}:trailer] frame capture failed: {e}");
		}
	}

	/// <summary>Unhooks, lets ffmpeg finish the file and returns the failure, if any.</summary>
	public async Task<string?> Stop()
	{
		RenderingServer.FramePostDraw -= OnFrame;
		try
		{
			_pipe.Close();
		}
		catch (IOException e)
		{
			_failure ??= e.Message;
		}
		await _ffmpeg.WaitForExitAsync();
		if (_ffmpeg.ExitCode != 0)
		{
			_failure ??= $"ffmpeg exited with code {_ffmpeg.ExitCode}";
		}
		return _failure;
	}
}
