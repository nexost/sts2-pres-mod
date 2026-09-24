<#
.SYNOPSIS
  Removes sts2-pres-mod (mod id pres_mod) from Slay the Spire 2, including saves that depend on it.
.DESCRIPTION
  A run in progress as one of the mod's characters (or a run-history entry using its cards) can't be loaded once the mod is gone.
  Steam Cloud also re-downloads any modded save that is missing locally, so deleting files on disk isn't enough.
  So, if such saves exist and the mod is still installed, this starts the game once in a short cleanup mode:
  the mod backs those saves up and deletes them through the game's own save system (local + cloud), then quits.
  After that the mod folder is removed. Backups go to %APPDATA%\SlayTheSpire2\pres_mod_uninstall_backup\.
.PARAMETER GameDir
  Game folder. Found automatically through Steam if omitted.
.PARAMETER KeepSaves
  Only remove the mod files; leave all saves alone.
.PARAMETER NoLaunch
  Don't start the game for cleanup; move affected local save files to the backup folder instead
  (Steam Cloud may restore them later).
.PARAMETER RemoveTestData
  Also delete the automated-test save folders (modded_prestest, modded_bal*) and test output.
.PARAMETER DryRun
  Show what would happen without changing anything.
.PARAMETER Quiet
  No prompts or pause.
.PARAMETER SaveDirName
  (Testing) Which save folder to clean. Default 'modded' (the game's modded saves).
.PARAMETER KeepModFiles
  (Testing) Do the save cleanup but leave the mod installed.
#>
param(
    [string]$GameDir,
    [switch]$KeepSaves,
    [switch]$NoLaunch,
    [switch]$RemoveTestData,
    [switch]$DryRun,
    [switch]$Quiet,
    [string]$SaveDirName = 'modded',
    [switch]$KeepModFiles
)

$ErrorActionPreference = 'Stop'
$ModId = 'pres_mod'
$LegacyIds = @('trump_character')
$AppId = '2868840'
$UserData = Join-Path $env:APPDATA 'SlayTheSpire2'
$Stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$BackupRoot = Join-Path $UserData "pres_mod_uninstall_backup\$Stamp"

function Find-GameDir {
    $candidates = @()
    try {
        $steam = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath -replace '/', '\'
        $candidates += $steam
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $candidates += (Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' -AllMatches).Matches | ForEach-Object { $_.Groups[1].Value -replace '\\\\', '\' }
        }
    } catch { }
    $candidates += "${env:ProgramFiles(x86)}\Steam"
    foreach ($lib in ($candidates | Select-Object -Unique)) {
        if (Test-Path (Join-Path $lib "steamapps\appmanifest_$AppId.acf")) {
            $dir = Join-Path $lib 'steamapps\common\Slay the Spire 2'
            if (Test-Path (Join-Path $dir 'SlayTheSpire2.exe')) { return $dir }
        }
    }
    return $null
}

function Finish([int]$code) {
    if (-not $Quiet) { Write-Host ''; Read-Host 'Press Enter to close' | Out-Null }
    exit $code
}

function Get-ModIds([string]$modDir) {
    foreach ($p in @((Join-Path $modDir 'mod_ids.txt'), (Join-Path $PSScriptRoot "$ModId\mod_ids.txt"))) {
        if (Test-Path $p) { return @(Get-Content $p | Where-Object { $_.Trim() -ne '' } | ForEach-Object { $_.Trim() }) }
    }
    return @('CHARACTER.TRUMP')
}

# Run saves + history files in every modded profile that mention one of our model IDs.
function Find-AffectedSaves([string[]]$ids) {
    $hits = @()
    if (-not (Test-Path $UserData)) { return $hits }
    $patterns = $ids | ForEach-Object { '"' + $_ + '"' }
    $dirPattern = '\\' + [regex]::Escape($SaveDirName) + '\\profile\d\\saves\\'
    $files = Get-ChildItem -Path $UserData -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -match $dirPattern -and ($_.Name -like 'current_run*.save*' -or $_.Extension -eq '.run')
    }
    foreach ($f in $files) {
        $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
        if ($text -and ($patterns | Where-Object { $text.Contains($_) })) { $hits += $f }
    }
    return $hits
}

function Move-ToBackup($file) {
    $rel = $file.FullName.Substring($UserData.Length).TrimStart('\')
    $dest = Join-Path $BackupRoot $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Move-Item -LiteralPath $file.FullName -Destination $dest -Force
}

try {
    if (-not $GameDir) { $GameDir = Find-GameDir }
    if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir 'SlayTheSpire2.exe'))) {
        Write-Host 'Could not find Slay the Spire 2. Run again with -GameDir "<path to game folder>".' -ForegroundColor Red
        Finish 1
    }
    if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
        Write-Host 'Slay the Spire 2 is running. Close it first, then run the uninstaller again.' -ForegroundColor Red
        Finish 1
    }

    $modDir = Join-Path $GameDir "mods\$ModId"
    $installed = Test-Path (Join-Path $modDir "$ModId.dll")
    $ids = Get-ModIds $modDir
    if ($DryRun) { Write-Host '[dry run: nothing will be changed]' -ForegroundColor Yellow }

    # 1. Saves
    if ($KeepSaves) {
        Write-Host 'Leaving saves untouched (-KeepSaves).'
    } else {
        $affected = @(Find-AffectedSaves $ids)
        if ($affected.Count -eq 0) {
            Write-Host 'No saves use this mod.'
        } else {
            Write-Host "$($affected.Count) save file(s) use this mod:"
            $affected | ForEach-Object { Write-Host "  $($_.FullName.Substring($UserData.Length))" }
            if (-not $DryRun) {
                if ($installed -and -not $NoLaunch) {
                    Write-Host ''
                    Write-Host 'Starting the game briefly so the mod can remove them locally AND from Steam Cloud...' -ForegroundColor Cyan
                    $out = Join-Path $BackupRoot 'cloud_cleanup'
                    New-Item -ItemType Directory -Force -Path $out | Out-Null
                    $env:SteamAppId = $AppId
                    $env:SteamGameId = $AppId
                    $gameArgs = @('--pres-cleanup', '--pres-out', "`"$out`"")
                    if ($SaveDirName -ne 'modded') { $gameArgs += @('--pres-savedir', $SaveDirName) }
                    $proc = Start-Process -FilePath (Join-Path $GameDir 'SlayTheSpire2.exe') -ArgumentList $gameArgs -WorkingDirectory $GameDir -PassThru
                    if (-not $proc.WaitForExit(180000)) { $proc.Kill(); throw 'Cleanup timed out (game did not exit within 3 minutes).' }
                    $resultFile = Join-Path $out 'cleanup_result.json'
                    if (-not (Test-Path $resultFile)) { throw "Cleanup did not report back (exit code $($proc.ExitCode))." }
                    $result = Get-Content $resultFile -Raw | ConvertFrom-Json
                    Write-Host "  Removed $(@($result.removed).Count) file(s); backups in $out\backup" -ForegroundColor Green
                    foreach ($e in @($result.errors)) { if ($e) { Write-Host "  error: $e" -ForegroundColor Red } }
                    # Anything the game didn't handle (e.g. an unused profile) is moved locally.
                    foreach ($f in @(Find-AffectedSaves $ids)) { Move-ToBackup $f }
                } else {
                    if (-not $installed) { Write-Host 'Mod files are already gone, so cleaning locally only.' -ForegroundColor Yellow }
                    foreach ($f in $affected) { Move-ToBackup $f }
                    Write-Host "  Moved to $BackupRoot" -ForegroundColor Green
                    Write-Host '  Note: Steam Cloud may restore these files the next time the game runs with any mod installed.' -ForegroundColor Yellow
                }
            }
        }
    }

    # 2. Mod files
    if ($KeepModFiles) {
        Write-Host 'Leaving mod files installed (-KeepModFiles).'
    } elseif (Test-Path $modDir) {
        if ($DryRun) {
            Write-Host "Would remove $modDir"
        } else {
            Remove-Item -LiteralPath $modDir -Recurse -Force
            Write-Host "Removed $modDir" -ForegroundColor Green
        }
    } else {
        Write-Host 'Mod folder was not present.'
    }
    foreach ($old in $LegacyIds) {
        $oldDir = Join-Path $GameDir "mods\$old"
        if (-not $KeepModFiles -and (Test-Path $oldDir)) {
            if (-not $DryRun) { Remove-Item -LiteralPath $oldDir -Recurse -Force }
            Write-Host "$(if ($DryRun) { 'Would remove' } else { 'Removed' }) old version $oldDir"
        }
    }

    # 3. Optional test data
    if ($RemoveTestData) {
        $testDirs = @()
        foreach ($name in @('modded_prestest', 'modded_trumptest', 'modded_bal*')) {
            $testDirs += @(Get-ChildItem -Path $UserData -Recurse -Directory -Filter $name -ErrorAction SilentlyContinue)
        }
        foreach ($name in @('pres_test', 'trump_test')) {
            $testDirs += @(Get-Item (Join-Path $UserData $name) -ErrorAction SilentlyContinue)
        }
        foreach ($d in $testDirs) {
            if (-not $DryRun) { Remove-Item -LiteralPath $d.FullName -Recurse -Force }
            Write-Host "$(if ($DryRun) { 'Would remove' } else { 'Removed' }) test data $($d.FullName)"
        }
    }

    Write-Host ''
    if ($DryRun) {
        Write-Host 'Dry run complete. Nothing was changed.' -ForegroundColor Yellow
        Finish 0
    }
    Write-Host 'The presidents have left the Spire. Uninstall complete.' -ForegroundColor Green
    if (Test-Path $BackupRoot) { Write-Host "Backups: $BackupRoot" }
    Finish 0
} catch {
    Write-Host "Uninstall failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception -is [System.UnauthorizedAccessException]) {
        Write-Host 'Try running uninstall.cmd as administrator.' -ForegroundColor Yellow
    }
    Finish 1
}
