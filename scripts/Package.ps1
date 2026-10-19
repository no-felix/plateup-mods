[CmdletBinding()]
param([string] $Mod = 'refill-containers', [string] $PlateUpDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Project.ps1')
$project = Get-ModProject $Mod
& (Join-Path $PSScriptRoot 'Build.ps1') -Mod $Mod -PlateUpDir $PlateUpDir
$package = Join-Path $RepositoryRoot "artifacts\workshop\$Mod"
$content = Join-Path $package 'content'
New-Item -ItemType Directory -Path $content -Force | Out-Null
$dll = Get-ReleaseDll $project
$unexpected = Get-ChildItem -LiteralPath $content -Recurse -File | Where-Object Name -ne "$($project.Assembly).dll"
if ($unexpected) { throw 'Unexpected files in the package content directory.' }
Copy-Item -LiteralPath $dll -Destination $content -Force
Copy-Item -LiteralPath (Join-Path $RepositoryRoot 'LICENSE'), (Join-Path $project.Directory 'README.md') -Destination $package -Force
foreach ($name in @('preview.png', 'description.txt')) {
    Copy-Item -LiteralPath (Join-Path $project.Directory "workshop\$name") -Destination $package -Force
}
$metadata = Join-Path $project.Directory 'workshop\plateup_mod_metadata.json'
if (Test-Path -LiteralPath $metadata) { Copy-Item -LiteralPath $metadata -Destination $package -Force }
# Retain metadata written by ModUploader when rebuilding the same item.
$zip = Join-Path $RepositoryRoot "artifacts\$Mod-$($project.Version).zip"
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Output "Workshop folder: $package"
Write-Output "Archive: $zip"
