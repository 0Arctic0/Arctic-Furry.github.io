@echo off
rem Builds FroststrapJoiner.exe from this folder using the compiler that ships with Windows.
setlocal
set "CSC="
for /f "delims=" %%F in ('dir /b /on "%WINDIR%\Microsoft.NET\Framework64\v4.0.*\csc.exe" 2^>nul') do set "CSC=%WINDIR%\Microsoft.NET\Framework64\%%F"
if not defined CSC for /f "delims=" %%F in ('dir /b /on "%WINDIR%\Microsoft.NET\Framework\v4.0.*\csc.exe" 2^>nul') do set "CSC=%WINDIR%\Microsoft.NET\Framework\%%F"
if not defined CSC (
  echo No .NET Framework C# compiler found. Install .NET Framework 4.x or the dotnet SDK.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ "/out:%~dp0FroststrapJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0FroststrapJoiner.cs"
if exist "%~dp0FroststrapJoiner.exe" (
  echo Built: %~dp0FroststrapJoiner.exe
) else (
  echo Build did not produce FroststrapJoiner.exe
  exit /b 1
)
