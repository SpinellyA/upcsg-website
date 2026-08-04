@echo off
REM Runs an EF Core command with the connection string read from user-secrets.
REM
REM For cmd.exe users. `set VAR="value"` in cmd keeps the quotes as part of the value,
REM and an unquoted value is fine until something else in the line needs escaping -
REM either way the string reaching EF is not the one you pasted. This never puts it on
REM a command line at all.
REM
REM Usage:  scripts\ef.cmd database update
REM         scripts\ef.cmd migrations add AddSomething

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ef.ps1" %*
