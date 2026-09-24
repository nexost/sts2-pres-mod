STS2-PRES-MOD - playable presidents for Slay the Spire 2 (The Donald, more coming)
===================================================================================

INSTALL
  1. Close the game.
  2. Double-click install.cmd.
  3. Start the game. The first time it sees mods it asks you to allow them.

UNINSTALL
  Double-click uninstall.cmd.
  If a run in progress (or run history) uses one of the mod's characters, the uninstaller starts the game for a few
  seconds so the mod can remove those saves locally and from Steam Cloud (backups are kept in
  %APPDATA%\SlayTheSpire2\pres_mod_uninstall_backup). Then it deletes the mod.
  Options (run from a terminal):
    uninstall.cmd -KeepSaves       only remove the mod files
    uninstall.cmd -NoLaunch        don't start the game; move affected local saves to the backup folder
    uninstall.cmd -RemoveTestData  also delete automated-test saves
    uninstall.cmd -DryRun          show what would happen

NOTES
  - Modded play uses separate save profiles; your normal (unmodded) saves are never touched.
  - Replaces the older "trump_character" mod: the installer removes it.
  - Built for Slay the Spire 2 v0.107.1. Game updates may require a mod update.
