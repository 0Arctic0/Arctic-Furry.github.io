@echo off
rem One-time install: downloads the latest ArcticJoiner from GitHub, builds it,
rem and puts a single ArcticJoiner.exe into your Downloads\ArcticJoiner folder.
setlocal
set "DIR=%USERPROFILE%\Downloads\ArcticJoiner"
set "BASE=https://raw.githubusercontent.com/Arctic00Fox/Arctic-Furry.github.io/main/windows-app"

echo === ArcticJoiner install ===
echo.

mkdir "%DIR%" 2>nul

echo [1/3] Downloading latest source from GitHub...
curl -fsSL -o "%DIR%\ArcticJoiner.cs" "%BASE%/ArcticJoiner.cs?nocache=%RANDOM%%RANDOM%"
if errorlevel 1 goto :fail
curl -fsSL -o "%DIR%\version.txt" "%BASE%/version.txt?nocache=%RANDOM%%RANDOM%"
if errorlevel 1 goto :fail
curl -fsSL -o "%DIR%\icon.ico" "%BASE%/icon.ico?nocache=%RANDOM%%RANDOM%"
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

echo [3/3] Building ArcticJoiner.exe...
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%DIR%\icon.ico" /resource:"%DIR%\ArcticJoiner.cs",ArcticJoiner.cs "/out:%DIR%\ArcticJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%DIR%\ArcticJoiner.cs"
if not exist "%DIR%\ArcticJoiner.exe" (
  echo.
  echo ERROR: build failed - see the messages above.
  goto :end_error
)

rem The source is embedded inside the exe, so tidy the folder down to just the app.
del "%DIR%\ArcticJoiner.cs" "%DIR%\version.txt" "%DIR%\icon.ico" >nul 2>&1

echo.
echo === Done! Installed ArcticJoiner.exe to: ===
echo %DIR%\ArcticJoiner.exe
echo.
echo The app also self-updates from GitHub automatically.
echo.
choice /c YN /n /m "Start it now? [Y/N] "
if errorlevel 2 goto :end_ok
start "" "%DIR%\ArcticJoiner.exe"
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
