$RepositoryRoot = Split-Path $PSScriptRoot -Parent

function Get-ModProject([ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string] $Mod) {
    $directory = Join-Path $RepositoryRoot "mods\$Mod"
    $projects = @(Get-ChildItem -LiteralPath (Join-Path $directory 'src') -Recurse -Filter '*.csproj')
    if ($projects.Count -ne 1) { throw "Expected one production project in mods/$Mod/src." }
    $xml = [xml](Get-Content -LiteralPath $projects[0].FullName -Raw)
    [pscustomobject]@{
        Name = $Mod
        Directory = $directory
        Project = $projects[0].FullName
        Assembly = [string]$xml.Project.PropertyGroup.AssemblyName
        Version = [string]$xml.Project.PropertyGroup.Version
        Framework = [string]$xml.Project.PropertyGroup.TargetFramework
    }
}

function Get-GameDirectory($Project, [string] $Requested) {
    if ($Requested) { return [IO.Path]::GetFullPath($Requested) }
    $path = & dotnet msbuild $Project.Project -getProperty:PlateUpDir -nologo
    if ($LASTEXITCODE -ne 0) { throw 'Could not resolve PlateUpDir from the project configuration.' }
    return [IO.Path]::GetFullPath(([string]$path).Trim())
}

function Get-ReleaseDll($Project) {
    Join-Path (Split-Path $Project.Project -Parent) "bin\Release\$($Project.Framework)\$($Project.Assembly).dll"
}
