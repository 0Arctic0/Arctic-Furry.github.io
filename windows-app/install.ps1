# One-time install: fetches the latest FroststrapJoiner from GitHub and builds it.
# Run from anywhere in PowerShell:
#   powershell -ExecutionPolicy Bypass -Command "iwr https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app/install.ps1 -UseBasicParsing | iex"

$ErrorActionPreference = 'Stop'
$base = 'https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app/'
$dir = Join-Path $env:LOCALAPPDATA 'FroststrapJoiner'
New-Item -ItemType Directory -Force -Path $dir | Out-Null

Write-Host "Downloading latest FroststrapJoiner from GitHub..."
foreach ($file in 'FroststrapJoiner.cs','version.txt') {
    Invoke-WebRequest -UseBasicParsing -Uri ($base + $file) -OutFile (Join-Path $dir $file)
}

$csc = Get-ChildItem "$(Join-Path $env:WINDIR 'Microsoft.NET\Framework64')\v4.0.*\csc.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $csc) {
    $csc = Get-ChildItem "$(Join-Path $env:WINDIR 'Microsoft.NET\Framework')\v4.0.*\csc.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
}
if (-not $csc) { throw 'No .NET Framework C# compiler found. Install .NET Framework 4.x.' }

$src = Join-Path $dir 'FroststrapJoiner.cs'
$out = Join-Path $dir 'FroststrapJoiner.exe'
Write-Host "Building with $($csc.FullName)..."
& $csc.FullName /nologo /target:winexe /optimize+ "/out:$out" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $src
if (-not (Test-Path $out)) { throw 'Build failed.' }

$version = (Get-Content (Join-Path $dir 'version.txt') -First 1).Trim()
Write-Host "Installed FroststrapJoiner v$version to: $out" -ForegroundColor Green
Write-Host 'The app also self-updates: it checks GitHub on launch and installs new versions with one click.'
Write-Host "Tip: pin it - New-Item -ItemType Shortcut -Path \"$env:USERPROFILE\Desktop\Froststrap Joiner.lnk\" -Value \"$out\""
