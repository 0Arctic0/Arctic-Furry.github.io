# Builds FroststrapJoiner.exe using the C# compiler that ships with Windows.
# Run from PowerShell:  powershell -ExecutionPolicy Bypass -File .\build.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'FroststrapJoiner.cs'
$out = Join-Path $root 'FroststrapJoiner.exe'

# Prefer dotnet if available, otherwise fall back to .NET Framework csc.
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    Write-Host 'Building with dotnet...'
    & dotnet build -c Release -o $root (Join-Path $root 'FroststrapJoiner.csproj')
} else {
    Write-Host 'Building with the built-in .NET Framework compiler...'
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    if (-not (Test-Path $csc)) { throw 'No C# compiler found. Install .NET Framework 4.x or the dotnet SDK.' }
    & $csc /nologo /target:winexe /optimize+ /out:$out /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $src
}

if (Test-Path $out) {
    Write-Host "Built: $out" -ForegroundColor Green
} else {
    throw 'Build did not produce FroststrapJoiner.exe'
}
