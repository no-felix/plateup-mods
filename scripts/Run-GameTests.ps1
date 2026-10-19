[CmdletBinding()]
param([string] $Mod = 'refill-containers', [string] $PlateUpDir)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Project.ps1')
$project = Get-ModProject $Mod
$game = Get-GameDirectory $project $PlateUpDir
if (Get-Process -Name PlateUp -ErrorAction SilentlyContinue) { throw 'Close PlateUp before running game tests.' }
$exe = Join-Path $game 'PlateUp.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'PlateUpDir must contain PlateUp.exe.' }
$tests = @(Get-ChildItem -LiteralPath (Join-Path $project.Directory 'tests') -Recurse -Filter '*.csproj')
if ($tests.Count -ne 1) { throw 'Expected one native test project.' }
& dotnet build $tests[0].FullName -c Release "-p:PlateUpDir=$game" --disable-build-servers -m:1
if ($LASTEXITCODE -ne 0) { throw 'Game test build failed.' }
$mods = [IO.Path]::GetFullPath((Join-Path $game 'Mods'))
$installed = if (Test-Path -LiteralPath $mods) { @(Get-ChildItem -LiteralPath $mods -Recurse -Filter "$($project.Assembly)*.dll") } else { @() }
$dll = Get-ReleaseDll $project
if ($installed.Count -and ($installed.Count -ne 1 -or $installed[0].Name -ne "$($project.Assembly).dll" -or
    (Get-FileHash -LiteralPath $installed[0].FullName).Hash -ne (Get-FileHash -LiteralPath $dll).Hash)) {
    throw 'Update the local mod with Install-Local.ps1 and remove duplicate or test copies before testing.'
}
$testDir = [IO.Path]::GetFullPath((Join-Path $mods "$Mod.TestRun"))
if (!$testDir.StartsWith($mods + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test path must be inside Mods.' }
if (Test-Path -LiteralPath $testDir) { throw "Test directory already exists: $testDir" }
$run = Join-Path $RepositoryRoot ("artifacts\tests\$Mod\" + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $run | Out-Null
$log = Join-Path $run 'player.log'
$process = $null
try {
    $content = Join-Path $testDir 'content'
    New-Item -ItemType Directory -Path $content | Out-Null
    if (!$installed.Count) { Copy-Item -LiteralPath $dll -Destination $content }
    $testXml = [xml](Get-Content -LiteralPath $tests[0].FullName -Raw)
    $testDll = Join-Path $tests[0].DirectoryName "bin\Release\$($project.Framework)\$($testXml.Project.PropertyGroup.AssemblyName).dll"
    Copy-Item -LiteralPath $testDll -Destination $content
    $arguments = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
        '-plateup-mod-tests', '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $exe -WorkingDirectory $game -WindowStyle Hidden -PassThru -ArgumentList $arguments
    if (!$process.WaitForExit(60000)) { throw "Game test timed out. Inspect $log" }
    $output = Get-Content -LiteralPath $log -Raw
    $report = Join-Path $run 'coverage.md'
    $rows = [regex]::Matches($output, 'PLATEUP_MOD_REPORT: ([^\r\n]+)') | ForEach-Object { $_.Groups[1].Value }
    $lines = @("# $Mod $($project.Version) native test coverage", '') + $rows
    $lines | Set-Content -LiteralPath $report
    if ($process.ExitCode -ne 0 -or $output -notmatch 'PLATEUP_MOD_TESTS: PASS' -or $output -match 'PLATEUP_MOD_TESTS: FAIL') {
        throw "Game integration failed. Inspect $log"
    }
    Write-Output "PASS: $report"
}
finally {
    if ($null -ne $process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if (Test-Path -LiteralPath $testDir) { Remove-Item -LiteralPath $testDir -Recurse -Force }
}
