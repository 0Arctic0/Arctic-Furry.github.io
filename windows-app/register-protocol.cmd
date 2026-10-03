@echo off
rem Optional: makes FroststrapJoiner handle roblox:// links for your user.
rem NOTE: Froststrap itself usually registers the roblox:// protocol.
set "EXE=%~dp0FroststrapJoiner.exe"
if not exist "%EXE%" (
  echo Build FroststrapJoiner.exe first - run build.cmd
  exit /b 1
)
reg add "HKCU\Software\Classes\roblox" /ve /d "URL:Roblox Protocol" /f >nul
reg add "HKCU\Software\Classes\roblox" /v "URL Protocol" /d "" /f >nul
reg add "HKCU\Software\Classes\roblox\shell\open\command" /ve /d "\"%EXE%\" \"%%1\"" /f >nul
echo roblox:// links now open FroststrapJoiner.
echo To undo: reg delete HKCU\Software\Classes\roblox /f  and re-open Froststrap.
pause
