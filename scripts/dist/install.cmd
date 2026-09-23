@echo off
rem Double-click to install. Extra options: see install.ps1 (e.g. install.cmd -GameDir "D:\Games\Slay the Spire 2")
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
