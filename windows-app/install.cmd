@echo off
rem One-time install: downloads the latest FroststrapJoiner from GitHub and builds it.
rem Download this file and double-click it (or run it from cmd).
setlocal
set "DIR=%LOCALAPPDATA%\FroststrapJoiner"
set "BASE=https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app"

echo === FroststrapJoiner install ===
echo.

mkdir "%DIR%" 2>nul

echo [1/3] Downloading latest source from GitHub...
curl -fsSL -o "%DIR%\FroststrapJoiner.cs" "%BASE%/FroststrapJoiner.cs"
if errorlevel 1 goto :fail
curl -fsSL -o "%DIR%\version.txt" "%BASE%/version.txt"
if errorlevel 1 goto :fail

echo [2/3] Looking for the C# compiler that ships with Windows...
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo.
  echo ERROR: no .NET Framework C# compiler found. Install .NET Framework 4.x.
  goto :end_error
)
echo       Using %CSC%

echo [3/3] Building FroststrapJoiner.exe...
"%CSC%" /nologo /target:winexe /optimize+ "/out:%DIR%\FroststrapJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%DIR%\FroststrapJoiner.cs"
if not exist "%DIR%\FroststrapJoiner.exe" (
  echo.
  echo ERROR: build failed - see the messages above.
  goto :end_error
)

set /p VERSION=<"%DIR%\version.txt"
echo.
echo === Done! Installed FroststrapJoiner v%VERSION% to: ===
echo %DIR%\FroststrapJoiner.exe
echo.
echo The app also self-updates from GitHub automatically.
echo.
choice /c YN /n /m "Start it now? [Y/N] "
if errorlevel 2 goto :end_ok
start "" "%DIR%\FroststrapJoiner.exe"
goto :end_ok

:fail
echo.
echo ERROR: download failed - check your internet connection (curl must be available, Windows 10+).
goto :end_error

:end_ok
endlocal
exit /b 0

:end_error
endlocal
pause
exit /b 1
pause
