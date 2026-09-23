<#
.SYNOPSIS
  Installs The Donald (trump_character) mod into Slay the Spire 2.
.PARAMETER GameDir
  Game folder. Found automatically through Steam if omitted.
.PARAMETER Quiet
  No pause at the end (for scripted use).
#>
param(
    [string]$GameDir,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$ModId = 'trump_character'
$AppId = '2868840'

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

try {
    if (-not $GameDir) { $GameDir = Find-GameDir }
    if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir 'SlayTheSpire2.exe'))) {
        Write-Host 'Could not find Slay the Spire 2. Run again with -GameDir "<path to game folder>".' -ForegroundColor Red
        Finish 1
    }
    if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
        Write-Host 'Slay the Spire 2 is running. Close it first, then run the installer again.' -ForegroundColor Red
        Finish 1
    }

    $src = Join-Path $PSScriptRoot $ModId
    if (-not (Test-Path (Join-Path $src "$ModId.dll"))) {
        Write-Host "Mod files not found next to the installer ($src)." -ForegroundColor Red
        Finish 1
    }

    $dest = Join-Path $GameDir "mods\$ModId"
    $upgrade = Test-Path $dest
    if ($upgrade) { Remove-Item -LiteralPath $dest -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item -Path (Join-Path $src '*') -Destination $dest -Recurse -Force

    $version = (Get-Content (Join-Path $dest 'manifest.json') -Raw | ConvertFrom-Json).version
    Write-Host ("{0} The Donald v{1}" -f ($(if ($upgrade) { 'Updated' } else { 'Installed' }), $version)) -ForegroundColor Green
    Write-Host "  -> $dest"
    Write-Host ''
    Write-Host 'Notes:'
    Write-Host '  - The first time the game sees mods it asks you to allow them.'
    Write-Host '  - Modded play uses separate save profiles; your normal saves are untouched.'
    Write-Host '  - Use uninstall.cmd to remove the mod cleanly (it also cleans saves that use it).'
    Finish 0
} catch {
    Write-Host "Install failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception -is [System.UnauthorizedAccessException]) {
        Write-Host 'Try running install.cmd as administrator.' -ForegroundColor Yellow
    }
    Finish 1
}
