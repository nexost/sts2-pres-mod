"""
Record trailer shots (docs/00_project_plan.md, Step 12). The game runs the harness's "trailer" mode
(mod/pres_mod/src/Dev/DevHarness.Trailer.cs), which stages each shot and marks where its clips start and end.

  python scripts/trailer_capture.py SHOTS [-c biden] [--method frames|audio|moviemaker|realtime]
  python scripts/trailer_capture.py coop [full|clean]  the co-op shot: The Donald hosts and records, Sleepy Joe joins

SHOTS is a comma list of the character's trailer shots (CharacterTests.TrailerShots), e.g. biden_wake.

Methods:
  frames      (default) the engine runs at a fixed 60 fps and the harness pipes every frame of each clip to ffmpeg:
              full resolution, no dropped frames. Silent: the game's sound is FMOD, played in real time.
  moviemaker  the engine's own Movie Maker (--write-movie: an MJPEG AVI of the whole session), cut into the clips.
  realtime    the game runs normally while ffmpeg records the primary screen (ddagrab + NVENC), cut into the clips.
  audio       the game's sound: the same shots played in real time while the PC's sound output is recorded (WASAPI
              loopback, PyAudioWPatch), cut into clips/<clip>.wav by the clips' clock times. Shots wait in game time,
              so the sound lines up with the silent 4K clips of the same shots. The game's music is off
              (--pres-nomusic); anything else playing on the PC is recorded too.

Output: build/trailer/capture/<time>_<method>/clips/<clip>.mp4, report.json, godot.log. Saves go to the test profile
(modded_prestest), like test.py. ffmpeg's path is the "ffmpeg" setting (default tools/ffmpeg/bin/ffmpeg.exe).
"""
import datetime
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import threading
import time
import wave

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

# test.py's helpers (godot.log lookup and scan); loaded by path, because "test" is also a standard-library package.
_spec = importlib.util.spec_from_file_location("presmod_test", os.path.join(os.path.dirname(os.path.abspath(__file__)), "test.py"))
testpy = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(testpy)

TIMEOUT = 900
FFMPEG = presmod.FFMPEG
# The clips' codec for the edit: H.264 4:4:4 at a high constant quality, like the in-game recorder (TrailerRecorder.cs).
CLIP_CODEC = ["-c:v", "h264_nvenc", "-preset", "p7", "-tune", "hq", "-rc", "constqp", "-qp", "12",
              "-profile:v", "high444p", "-pix_fmt", "yuv444p", "-movflags", "+faststart"]


def coop(looks="full,clean"):
    """Two instances in one co-op run over localhost (the game's --fastmp, like test.py coop). The host (The Donald)
    runs full screen at a fixed 60 fps and records coop_both_full and coop_both_clean; the client (Sleepy Joe) plays
    muted in a small window on the second screen (DevHarness.Trailer.Coop.cs)."""
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")
    out = os.path.join(presmod.REPO, "build", "trailer", "capture", f"{time.strftime('%Y%m%d_%H%M%S')}_coop")
    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    exe = os.path.join(presmod.GAME_DIR, "SlayTheSpire2.exe")
    roles = [("host", ["--fixed-fps", "60", "--fastmp", "host_standard", "--pres-window", "fullscreen",
                       "--pres-capture", "frames", "--pres-ffmpeg", FFMPEG], "TRUMP"),
             ("client", ["--fastmp", "join", "--clientId", "1001", "--pres-window", "screen1", "--pres-mute",
                         "--pres-capture", "none"], "BIDEN")]
    procs = []
    print(f"Launching the co-op trailer shot: The Donald (host, recording) + Sleepy Joe\n  output: {out}", flush=True)
    for role, extra, character in roles:
        role_out = os.path.join(out, role)
        os.makedirs(os.path.join(role_out, "clips"))
        args = [exe, "--log-file", os.path.join(role_out, "godot.log")] + extra + [
            "--pres-test", "trailer_coop", "--pres-out", role_out, "--pres-character", character, "--pres-savedir", f"modded_coop_{role}",
            "--pres-looks", looks]
        procs.append(subprocess.Popen(args, cwd=presmod.GAME_DIR, env=env))
        time.sleep(15 if role == "host" else 0)
    for proc in procs:
        try:
            proc.wait(timeout=TIMEOUT)
        except subprocess.TimeoutExpired:
            proc.kill()
    ok = True
    for role, *_ in roles:
        path = os.path.join(out, role, "report.json")
        report = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else None
        print(f"\n[{role}] " + ("no report" if report is None else ("OK" if report["ok"] else "ERRORS")))
        if report is None:
            ok = False
            continue
        ok &= report["ok"]
        for e in report["events"]:
            print("  ", e)
        for e in report["errors"]:
            print("   ERROR", e)
    for clip in sorted(os.listdir(os.path.join(out, "host", "clips"))):
        print(f"Clip {clip}: {describe(os.path.join(out, 'host', 'clips', clip))}")
    print("\nRESULT:", "PASS" if ok else "FAIL")
    sys.exit(0 if ok else 1)


def main():
    argv = sys.argv[1:]
    if argv[:1] == ["coop"]:
        coop(argv[1] if len(argv) > 1 else "full,clean")
        return
    ch = testpy.take_character_option(argv)
    method = "frames"
    if "--method" in argv:
        i = argv.index("--method")
        method = argv[i + 1]
        del argv[i:i + 2]
    if not argv or method not in ("frames", "audio", "moviemaker", "realtime"):
        sys.exit(__doc__)
    shots = argv[0]
    if not os.path.exists(FFMPEG):
        sys.exit(f"ffmpeg not found at {FFMPEG} (docs/00_project_plan.md, Step 12)")
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")

    out = os.path.join(presmod.REPO, "build", "trailer", "capture", f"{time.strftime('%Y%m%d_%H%M%S')}_{method}")
    os.makedirs(os.path.join(out, "clips"))
    args = [os.path.join(presmod.GAME_DIR, "SlayTheSpire2.exe")]
    if method == "frames":
        args += ["--fixed-fps", "60"]
    elif method == "moviemaker":
        args += ["--write-movie", os.path.join(out, "movie.avi")]
    args += ["--pres-test", "trailer", "--pres-out", out, "--pres-character", ch.entry, "--pres-shots", shots,
             "--pres-capture", "frames" if method == "frames" else "none", "--pres-ffmpeg", FFMPEG]
    if method == "audio":
        args += ["--pres-nomusic"]

    screen = None
    screen_start = None
    if method == "realtime":
        # The primary screen at 60 fps, straight from the GPU into NVENC; no mouse pointer.
        screen = subprocess.Popen([FFMPEG, "-y", "-hide_banner", "-loglevel", "error",
                                   "-filter_complex", "ddagrab=output_idx=0:framerate=60:draw_mouse=0",
                                   "-c:v", "h264_nvenc", "-preset", "p5", "-tune", "hq", "-rc", "constqp", "-qp", "14",
                                   os.path.join(out, "screen.mp4")], stdin=subprocess.PIPE)
        time.sleep(1.0)
        screen_start = datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(seconds=1.0)

    loopback = LoopbackRecorder(os.path.join(out, "sound.wav")) if method == "audio" else None
    if loopback:
        loopback.start()

    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    started = time.time()
    print(f"Launching game: trailer shots={shots} character={ch.entry} method={method}\n  output: {out}", flush=True)
    proc = subprocess.Popen(args, cwd=presmod.GAME_DIR, env=env)
    try:
        code = proc.wait(timeout=TIMEOUT)
    except subprocess.TimeoutExpired:
        proc.kill()
        code = "TIMEOUT"
    elapsed = time.time() - started
    if screen:
        screen.communicate(b"q")
    if loopback:
        loopback.stop()

    log = testpy.newest_log(started)
    problems = testpy.scan_log(log) if log else []
    if log:
        shutil.copy2(log, os.path.join(out, "godot.log"))
    report_path = os.path.join(out, "report.json")
    report = json.load(open(report_path, encoding="utf-8")) if os.path.exists(report_path) else None
    print(f"\nExit code: {code}   ({elapsed:.0f}s)")
    if not report:
        print("Harness: no report written (game crashed or harness never started)")
        sys.exit(1)
    for e in report["events"]:
        print("  ", e)
    for e in report["errors"]:
        print("  ERROR", e)
    print(f"Log problems: {len(problems)}")
    for p in problems[:40]:
        print("  ", p)

    for clip in report.get("clips", []):
        path = os.path.join(out, "clips", clip["name"] + ".mp4")
        if method == "moviemaker":
            # Movie Maker writes one frame per engine frame from launch on, so the harness's frame numbers index the AVI.
            first, last = clip["startFrame"], clip["endFrame"]
            cut([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-i", os.path.join(out, "movie.avi"),
                 "-vf", f"select=between(n\\,{first}\\,{last - 1}),setpts=N/60/TB", "-an", "-r", "60"] + CLIP_CODEC + [path])
        elif method == "audio":
            begin = (datetime.datetime.fromisoformat(clip["startUtc"]) - loopback.start_utc).total_seconds()
            path = os.path.join(out, "clips", clip["name"] + ".wav")
            cut([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-ss", f"{max(0.0, begin):.3f}",
                 "-i", os.path.join(out, "sound.wav"), "-t", f"{clip['wallSeconds']:.3f}", "-c:a", "pcm_s16le", path])
            print(f"Clip {clip['name']}: {clip['wallSeconds']:.2f} s of sound, from {begin:.2f} s")
            continue
        elif method == "realtime":
            begin = (datetime.datetime.fromisoformat(clip["startUtc"]) - screen_start).total_seconds()
            cut([FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-ss", f"{max(0.0, begin):.3f}",
                 "-i", os.path.join(out, "screen.mp4"), "-t", f"{clip['wallSeconds']:.3f}", "-an"] + CLIP_CODEC + [path])
        print(f"Clip {clip['name']}: {describe(path)}")
    # The base game warns about missing FMOD parameters on some enemies' sounds; those lines aren't failures.
    ok = code == 0 and report["ok"] and not any(p.startswith("!") and "FMOD parameter" not in p for p in problems)
    print("\nRESULT:", "PASS" if ok else "FAIL")
    sys.exit(0 if ok else 1)


class LoopbackRecorder:
    """Records what the PC plays (the default output device, through WASAPI loopback) into a WAV. Windows delivers
    nothing while the output is silent, so gaps are filled with silence against the clock: sample N of the file is
    always N / rate seconds after start_utc."""

    def __init__(self, path):
        import pyaudiowpatch as pyaudio
        self.pyaudio = pyaudio
        self.path = path
        self.pa = pyaudio.PyAudio()
        wasapi = self.pa.get_host_api_info_by_type(pyaudio.paWASAPI)
        device = self.pa.get_device_info_by_index(wasapi["defaultOutputDevice"])
        if not device.get("isLoopbackDevice"):
            device = next((d for d in self.pa.get_loopback_device_info_generator() if device["name"] in d["name"]), None)
            if device is None:
                sys.exit("No loopback device for the default sound output")
        self.device = device
        self.channels = device["maxInputChannels"]
        self.rate = int(device["defaultSampleRate"])
        self.written = 0
        self.lock = threading.Lock()
        print(f"Recording the sound output: {device['name']} ({self.rate} Hz, {self.channels} ch)")

    def _callback(self, data, frames, time_info, status):
        with self.lock:
            expected = int((time.perf_counter() - self.t0) * self.rate) - frames
            gap = expected - self.written
            if gap > self.rate // 20:
                self.wav.writeframes(b"\x00" * (gap * self.channels * 2))
                self.written += gap
            self.wav.writeframes(data)
            self.written += frames
        return (None, self.pyaudio.paContinue)

    def start(self):
        self.wav = wave.open(self.path, "wb")
        self.wav.setnchannels(self.channels)
        self.wav.setsampwidth(2)
        self.wav.setframerate(self.rate)
        self.t0 = time.perf_counter()
        self.start_utc = datetime.datetime.now(datetime.timezone.utc)
        self.stream = self.pa.open(format=self.pyaudio.paInt16, channels=self.channels, rate=self.rate, input=True,
                                   input_device_index=self.device["index"], frames_per_buffer=1024, stream_callback=self._callback)

    def stop(self):
        self.stream.stop_stream()
        self.stream.close()
        with self.lock:
            self.wav.close()
        self.pa.terminate()


def cut(command):
    subprocess.run(command, check=True)


def describe(path):
    if not os.path.exists(path):
        return "missing"
    probe = subprocess.run([presmod.FFPROBE, "-v", "error", "-select_streams", "v:0", "-count_frames",
                            "-show_entries", "stream=width,height,avg_frame_rate,nb_read_frames", "-of", "json", path],
                           capture_output=True, text=True)
    s = json.loads(probe.stdout)["streams"][0]
    frames = int(s["nb_read_frames"])
    return f"{s['width']}x{s['height']}, {frames} frames ({frames / 60:.2f} s at 60 fps), {os.path.getsize(path) / 1e6:.0f} MB"


if __name__ == "__main__":
    main()
