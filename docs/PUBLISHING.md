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
