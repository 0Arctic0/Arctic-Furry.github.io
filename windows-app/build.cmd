@echo off
rem Builds ArcticJoiner.exe from this folder using the compiler that ships with Windows.
rem Just double-click it, or run it from cmd. Needs icon.ico in this folder.
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: no .NET Framework C# compiler found. Install .NET Framework 4.x or the dotnet SDK.
  pause
  exit /b 1
)
if not exist "%~dp0icon.ico" (
  echo ERROR: icon.ico not found next to build.cmd.
  pause
  exit /b 1
)
echo Building with %CSC% ...
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%~dp0icon.ico" /resource:"%~dp0ArcticJoiner.cs",ArcticJoiner.cs "/out:%~dp0ArcticJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0ArcticJoiner.cs"
if not exist "%~dp0ArcticJoiner.exe" (
  echo ERROR: build failed - see the messages above.
  pause
  exit /b 1
)
echo.
echo Built: %~dp0ArcticJoiner.exe
echo The source code is embedded inside the exe.
echo.
echo You can close this window now.
pause
