@echo off
rem Double-click to uninstall. Extra options: see uninstall.ps1 (-KeepSaves, -NoLaunch, -RemoveTestData, -DryRun)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1" %*
