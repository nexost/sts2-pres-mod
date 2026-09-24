STS2-PRES-MOD v{VERSION} - playable presidents for Slay the Spire 2
===================================================================

A lighthearted caricature mod. The jokes are aimed at the persona, and the walls and deportations are aimed at
the Spire's monsters.

CHARACTERS
  The Donald - builds a Wall that grows in stages, Deports the Spire's riff-raff, makes deals with Gold, posts
  Tweets. 88 cards plus the Tweet token, 9 relics, 3 potions, his own lines with every Ancient.
  (More presidents are planned.)

REQUIREMENTS
  Slay the Spire 2 v{GAME_VERSION} or later, on Windows. Game updates can break mods: if the game updated and the mod
  misbehaves, check for a mod update.

INSTALL
  1. Close the game.
  2. Double-click install.cmd. It finds the game through Steam.
     (Or copy the "pres_mod" folder into "Slay the Spire 2\mods\" yourself.)
  3. Start the game. The first time it sees mods it asks you to allow them.
  4. Pick The Donald on the character select screen. He's unlocked from the start.

CO-OP
  Every player needs the same version of the mod. The Donald works with any mix of characters.
  Coalition Wall and Trickle Down are co-op-only cards.

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
  - Something wrong? The game's log is %APPDATA%\SlayTheSpire2\logs\godot.log (lines with "pres_mod" are ours).
