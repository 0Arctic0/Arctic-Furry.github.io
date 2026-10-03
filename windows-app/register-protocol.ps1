# Optional: registers FroststrapJoiner.exe as a handler for roblox:// links,
# so any roblox:// deep link anywhere on Windows opens it (and it instantly
# launches Froststrap). Run as the script suggests; edit the exe path first.

$exe = Join-Path $PSScriptRoot 'FroststrapJoiner.exe'
if (-not (Test-Path $exe)) { throw "Build FroststrapJoiner.exe first (run build.ps1). Missing: $exe" }

# NOTE: Froststrap itself usually registers the roblox:// protocol.
# Only run this if you want FroststrapJoiner to intercept links instead.
$cmd = "\"$exe\" \"%1\""

New-Item -Path 'HKCU:\Software\Classes\roblox\shell\open\command' -Force | Out-Null
Set-ItemProperty -Path 'HKCU:\Software\Classes\roblox' -Name '(Default)' -Value 'URL:Roblox Protocol'
Set-ItemProperty -Path 'HKCU:\Software\Classes\roblox' -Name 'URL Protocol' -Value ''
Set-ItemProperty -Path 'HKCU:\Software\Classes\roblox\shell\open\command' -Name '(Default)' -Value $cmd
Write-Host 'roblox:// links now open FroststrapJoiner. To undo, delete HKCU:\Software\Classes\roblox and re-install/re-open Froststrap.'
