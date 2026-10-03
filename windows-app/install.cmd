@echo off
rem One-time install: downloads the latest FroststrapJoiner from GitHub and builds it.
rem Just download this file and double-click it (or run it from cmd).
setlocal
set "DIR=%LOCALAPPDATA%\FroststrapJoiner"
set "BASE=https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app"

mkdir "%DIR%" 2>nul

echo Downloading latest FroststrapJoiner from GitHub...
curl -fsSL -o "%DIR%\FroststrapJoiner.cs" "%BASE%/FroststrapJoiner.cs"
if errorlevel 1 goto :fail
curl -fsSL -o "%DIR%\version.txt" "%BASE%/version.txt"
if errorlevel 1 goto :fail

set "CSC="
for /f "delims=" %%F in ('dir /b /on "%WINDIR%\Microsoft.NET\Framework64\v4.0.*\csc.exe" 2^>nul') do set "CSC=%WINDIR%\Microsoft.NET\Framework64\%%F"
if not defined CSC for /f "delims=" %%F in ('dir /b /on "%WINDIR%\Microsoft.NET\Framework\v4.0.*\csc.exe" 2^>nul') do set "CSC=%WINDIR%\Microsoft.NET\Framework\%%F"
if not defined CSC (
  echo No .NET Framework C# compiler found. Install .NET Framework 4.x.
  exit /b 1
)

echo Building with %CSC% ...
"%CSC%" /nologo /target:winexe /optimize+ "/out:%DIR%\FroststrapJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%DIR%\FroststrapJoiner.cs"
if not exist "%DIR%\FroststrapJoiner.exe" (
  echo Build failed.
  exit /b 1
)

set /p VERSION=<"%DIR%\version.txt"
echo.
echo Installed FroststrapJoiner v%VERSION% to:
echo   %DIR%\FroststrapJoiner.exe
echo The app also self-updates from GitHub on its own.
echo.
choice /c YN /m "Start it now"
if errorlevel 2 exit /b 0
start "" "%DIR%\FroststrapJoiner.exe"
exit /b 0

:fail
echo Download failed - check your internet connection.
exit /b 1
