[CmdletBinding()]
param(
    [string] $Mod,
    [string] $PlateUpDir,
    [ValidateSet('Debug', 'Release')][string] $Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Project.ps1')
$target = if ($Mod) { (Get-ModProject $Mod).Project } else { Join-Path $RepositoryRoot 'PlateUpMods.sln' }
$arguments = @('build', $target, '-c', $Configuration, '--disable-build-servers', '-m:1')
if ($PlateUpDir) { $arguments += "-p:PlateUpDir=$PlateUpDir" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
