@echo off
rem Builds FroststrapJoiner.exe from this folder using the compiler that ships with Windows.
rem Just double-click it, or run it from cmd.
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: no .NET Framework C# compiler found. Install .NET Framework 4.x or the dotnet SDK.
  pause
  exit /b 1
)
echo Building with %CSC% ...
"%CSC%" /nologo /target:winexe /optimize+ "/out:%~dp0FroststrapJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0FroststrapJoiner.cs"
if not exist "%~dp0FroststrapJoiner.exe" (
  echo ERROR: build failed - see the messages above.
  pause
  exit /b 1
)
echo.
echo Built: %~dp0FroststrapJoiner.exe
echo.
echo You can close this window now.
pause
