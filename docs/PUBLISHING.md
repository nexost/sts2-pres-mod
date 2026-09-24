# Publishing sts2-pres-mod

Published so far: **GitHub**, public repo https://github.com/nexost/sts2-pres-mod with release `v1.0.0` (the zip). Not on the Steam Workshop. Publishing anywhere is the user's call; this page lists the options, their rules, and the steps.

**Before any push:** commits must use the `nexost` identity (`git config --local user.name` / `user.email`, GitHub noreply email), never the global git identity.

## Where it can go

| Venue | Allowed? | Notes |
|---|---|---|
| **Nexus Mods** | **No** | Since 28 Sep 2020 Nexus has a blanket ban on "mods relating to sociopolitical issues in the United States", extended indefinitely. It is enforced on exactly this kind of mod: in January 2025 Nexus removed Marvel Rivals mods featuring Trump and Biden. Real-politician caricatures would be removed however lighthearted. |
| **Steam Workshop** | **Yes**, under Steam's content rules | The official channel since v0.107.1 (Major Update 2): subscribers get automatic updates. Mega Crit publishes an uploader (below). Steam's rules forbid hateful, harassing or discriminatory content. This mod aims jokes at the persona only, which fits, but a Workshop item can still be reported and reviewed. Visibility can be public, friends-only or private. |
| **GitHub release / direct zip** | Yes | `python scripts/build.py --zip` makes `build/release/sts2-pres-mod-v<version>.zip` with the installer. Good for sharing with friends. |

Sources: [Nexus file submission guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines); [Nexus ban news post](https://www.nexusmods.com/news/14373); [coverage of the 2025 Trump/Biden mod removals](https://www.pcgamesn.com/marvel-rivals/trump-biden-mod); [Mega Crit's uploader](https://github.com/megacrit/sts2-mod-uploader).

## Versioning: when to release

**Pushes to `master` are not releases.**
- `master` holds the development history: docs, tools and work in progress.
- Keep it building and passing its tests, but don't ship every commit.
- A release is a tested snapshot players install: a tag (`vX.Y.Z`), a zip and release notes.

**Release when players get something:** a bug fix, balance change, new content, or compatibility with a game update.
- **A crash fix** goes out on its own, quickly.
- **Smaller changes** are batched into one release.
- **Commits that only change docs or developer tools** (scripts, the art review tool, tests) don't need a release. The installed mod is the same.

**Version numbers** follow semantic versioning, MAJOR.MINOR.PATCH, as it applies to a mod:

| Bump | When | Examples |
|---|---|---|
| PATCH `1.0.1` | Fixes and tuning; nothing new, nothing breaks | Bug and crash fixes, balance numbers, text fixes, compatibility with a game patch |
| MINOR `1.1.0` | New content, old saves and runs still load | A new character (Joe Biden), new cards, relics or potions, new art |
| MAJOR `2.0.0` | Something breaks for players | Runs or saves from the previous version no longer load; content removed or renamed (model IDs changed) |
| Pre-release `1.1.0-beta.1` | A test build for friends before the real release | GitHub's "pre-release" flag (`gh release create ... --prerelease`) |

**Rules for this mod:**
- **Every release gets a new version in `mod.json`**, even for a one-line fix. Two different zips with the same version cause trouble:
  - co-op needs every player on the same version;
  - the game's mod list shows the version, so players can't tell the two apart.
- **One version everywhere:** the git tag, the zip and any Steam Workshop upload all use the same version, built from the tagged commit.
- **A version is never reused.** If a release was wrong, fix it and release the next number.
- **Game updates:** a compatibility release also raises `min_game_version` in `mod.json` ([GAME_UPDATES.md](GAME_UPDATES.md)).

## Release checklist

1. Set the version in `mod.json` and add an entry to [RELEASE_NOTES.md](RELEASE_NOTES.md).
2. `python scripts/build.py --zip --install`.
3. Tests on the installed build. Every character must pass:
   - `test.py ui -c <id>`;
   - `test.py cards PRESTEST1 -c <id>`;
   - `test.py autoslay SEED -c <id>` (a winning run);
   - `test.py coop -c <id>` and `test.py coop -c <id> IRONCLAD`.
4. Try the zip on a clean install:
   1. Remove `mods/pres_mod`.
   2. Unzip and run `install.cmd`.
   3. Start the game and play a fight.
   4. Run `uninstall.cmd -DryRun`.
5. Commit, tag (`git tag vX.Y.Z`), push `master` and the tag, then `gh release create vX.Y.Z build/release/sts2-pres-mod-vX.Y.Z.zip --notes-file ...`.

## Steam Workshop steps

1. **Tool:** download `ModUploader.exe` from https://github.com/megacrit/sts2-mod-uploader (ask before downloading).
2. **Workspace:** double-click the uploader once. It creates a `NewModWorkspace` folder with:
   - `content/`, `workshop.json` and `image.png`;
   - a README describing every `workshop.json` field.
3. **Fill the workspace:**
   - `content/`: the files of `build/dist/pres_mod/` (manifest.json, pres_mod.dll, pres_mod.pck, mod_ids.txt). Check the generated README for whether it wants the folder or its contents.
   - `image.png`: `build/release/workshop_preview.png` (1280×720, ~660 KB; Steam wants under 1 MB).
   - `workshop.json`: title "sts2-pres-mod: The Donald", the description (the README.txt text works), and the visibility. Consider friends-only first.
4. **Upload:** run `ModUploader.exe upload -w <workspace>`. The uploader writes `mod_id.txt`; keep it for updates (same command, with `changeNote` set).
5. **Uninstall note:** Steam manages a Workshop copy (unsubscribing removes it); `uninstall.cmd` only knows `mods/pres_mod`.
   Workshop users therefore don't get the save cleanup. A run in progress with a mod character can't be loaded once the mod is gone,
   like with any removed mod, so the Workshop description should say: finish or abandon runs with The Donald before unsubscribing.
