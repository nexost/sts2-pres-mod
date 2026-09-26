"""
ElevenLabs for the trailer (docs/00_project_plan.md, Step 12): the narrator's lines and sound effects.
The API key is the "elevenlabs_api_key" setting (local_settings.json, git-ignored, or ELEVENLABS_API_KEY).

  python trailer/tools/eleven.py credits                       what's left this month
  python trailer/tools/eleven.py cast                          every casting voice reads the casting lines
  python trailer/tools/eleven.py vo VOICE_ID [--takes N]       every narration line (trailer/audio/narration.json)
  python trailer/tools/eleven.py sfx [NAME,...] [--takes N]    the sound-effect kit (trailer/audio/sfx.json)

Output goes to build/trailer/audio/ (git-ignored).
"""
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

API = "https://api.elevenlabs.io"
AUDIO_DIR = os.path.join(REPO, "trailer", "audio")
OUT = os.path.join(REPO, "build", "trailer", "audio")


def key():
    k = presmod.setting("elevenlabs_api_key", "")
    if not k:
        sys.exit("No ElevenLabs key: set elevenlabs_api_key in local_settings.json (git-ignored)")
    return k


def request(method, path, body=None, raw=False):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(API + path, data=data, method=method,
                                 headers={"xi-api-key": key(), "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=180) as r:
            payload = r.read()
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {path}: {e.code} {e.read().decode(errors='replace')[:400]}") from None
    return payload if raw else json.loads(payload)


def credits():
    s = request("GET", "/v1/user/subscription")
    return s["character_count"], s["character_limit"]


def my_voice_ids():
    return {v["voice_id"] for v in request("GET", "/v1/voices")["voices"]}


def ensure_voice(voice):
    """Library voices must be added to the account before the API can speak with them."""
    if voice["id"] in my_voice_ids():
        return voice["id"]
    added = request("POST", f"/v1/voices/add/{voice['owner']}/{voice['id']}", {"new_name": voice["name"]})
    return added["voice_id"]


def tts(voice_id, text, out, model="eleven_v3", settings=None, seed=None):
    body = {"text": text, "model_id": model}
    if settings:
        body["voice_settings"] = settings
    if seed is not None:
        body["seed"] = seed
    audio = request("POST", f"/v1/text-to-speech/{voice_id}?output_format=mp3_44100_128", body, raw=True)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "wb") as f:
        f.write(audio)


def sound(prompt, out, seconds=None, influence=0.5):
    body = {"text": prompt, "prompt_influence": influence}
    if seconds:
        body["duration_seconds"] = seconds
    audio = request("POST", "/v1/sound-generation?output_format=mp3_44100_128", body, raw=True)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "wb") as f:
        f.write(audio)


def load(name):
    with open(os.path.join(AUDIO_DIR, name), encoding="utf-8") as f:
        return json.load(f)


def option(argv, flag, default):
    if flag in argv:
        i = argv.index(flag)
        value = argv[i + 1]
        del argv[i:i + 2]
        return value
    return default


def main():
    argv = sys.argv[1:]
    if not argv:
        sys.exit(__doc__)
    cmd = argv.pop(0)
    takes = int(option(argv, "--takes", "1"))
    before = credits()[0]
    if cmd == "credits":
        used, limit = credits()
        print(f"{used} of {limit} credits used this month ({limit - used} left)")
        return
    if cmd == "cast":
        cast = load("casting.json")
        for voice in cast["voices"]:
            vid = ensure_voice(voice)
            for i, line in enumerate(cast["lines"]):
                out = os.path.join(OUT, "casting", f"{voice['slug']}_{i + 1}.mp3")
                tts(vid, line["text"], out, model=cast.get("model", "eleven_v3"), settings=cast.get("settings"))
                print("wrote", os.path.relpath(out, REPO))
    elif cmd == "vo":
        voice_id = argv[0]
        script = load("narration.json")
        for line in script["lines"]:
            for t in range(takes):
                out = os.path.join(OUT, "vo", f"{line['id']}_t{t + 1}.mp3")
                tts(voice_id, line["text"], out, model=script.get("model", "eleven_v3"),
                    settings=line.get("settings", script.get("settings")))
                print("wrote", os.path.relpath(out, REPO))
    elif cmd == "sfx":
        kit = load("sfx.json")
        only = set(argv[0].split(",")) if argv else None
        for fx in kit["sounds"]:
            if only and fx["name"] not in only:
                continue
            for t in range(takes):
                out = os.path.join(OUT, "sfx", f"{fx['name']}_t{t + 1}.mp3")
                sound(fx["prompt"], out, fx.get("seconds"), fx.get("influence", 0.5))
                print("wrote", os.path.relpath(out, REPO))
    else:
        sys.exit(__doc__)
    after = credits()[0]
    print(f"Credits used: {after - before}")


if __name__ == "__main__":
    main()
