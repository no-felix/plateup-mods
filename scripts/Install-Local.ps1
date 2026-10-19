[CmdletBinding()]
param([string] $Mod = 'refill-containers', [string] $PlateUpDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Project.ps1')
$project = Get-ModProject $Mod
$game = Get-GameDirectory $project $PlateUpDir
if (Get-Process -Name PlateUp -ErrorAction SilentlyContinue) { throw 'Close PlateUp before installing a mod.' }
if (!(Test-Path -LiteralPath (Join-Path $game 'PlateUp.exe'))) { throw 'PlateUpDir must contain PlateUp.exe.' }
$dll = Get-ReleaseDll $project
if (!(Test-Path -LiteralPath $dll)) { throw 'Run scripts/Build.ps1 first.' }
$mods = Join-Path $game 'Mods'
$existing = if (Test-Path -LiteralPath $mods) { @(Get-ChildItem -LiteralPath $mods -Recurse -Filter "$($project.Assembly).dll") } else { @() }
if ($existing.Count -gt 1) { throw 'Multiple local copies exist. Remove the duplicate installations first.' }
$destination = if ($existing.Count -eq 1) { $existing[0].DirectoryName } else { Join-Path $mods "$Mod\content" }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $destination -Force
Write-Output "Installed $Mod $($project.Version): $destination"
