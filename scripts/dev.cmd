@echo off
REM Starts the API and the Blazor dev server as detached background processes.
REM
REM For cmd.exe users. See dev.ps1 for why this is a script rather than two
REM `dotnet run` calls: the two projects race for UpcsgWeb.Shared.dll if launched
REM together, and a server started from an editor session dies with that session.
REM
REM Usage:  scripts\dev.cmd
REM         scripts\dev.cmd -Status
REM         scripts\dev.cmd -Stop

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0dev.ps1" %*
