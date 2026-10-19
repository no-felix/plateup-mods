# PlateUp Mods

Independent PlateUp! mods by no-felix. Each mod keeps its implementation, tests and Workshop artwork in its own directory. Build and packaging scripts are shared.

| Mod | Description |
|---|---|
| [Refill Containers](mods/refill-containers/README.md) | Refill matching empty trays and pots using the normal grab action or a combiner. |

## Build and test

Requirements: Windows, .NET SDK 8 or later, and an installed Steam copy of PlateUp. Projects reference the assemblies shipped with the game; no NuGet downloads or Unity editor are required.

Set the game directory in an ignored `Local.Build.props` file at the repository root:

```xml
<Project>
  <PropertyGroup>
    <PlateUpDir>F:\SteamLibrary\steamapps\common\PlateUp\PlateUp</PlateUpDir>
  </PropertyGroup>
</Project>
```

Without this file, the default is the standard Steam installation under Program Files (x86). Scripts also accept `-PlateUpDir` for an explicit override.

```powershell
.\scripts\Build.ps1
.\scripts\Install-Local.ps1 -Mod refill-containers
.\scripts\Run-GameTests.ps1 -Mod refill-containers
.\scripts\Package.ps1 -Mod refill-containers
```

Close PlateUp before installation or game tests. Build accepts `-Mod` to compile one mod instead of the solution. Test results and candidate reports are written under `artifacts/tests`. See [testing](docs/TESTING.md) for coverage and remaining release checks.

## Repository layout

```text
mods/<mod-name>/
  src/          Production project
  tests/        Native game tests
  workshop/     Preview image, description and published item metadata
  README.md     Player-facing behavior and installation
scripts/        Shared build, test, install and package commands
```

To add another mod, create its directory under `mods/`, add its projects to `PlateUpMods.sln`, and give its assembly a unique name. Each module has its own release version and Workshop item ID.

## Upload to Steam Workshop

1. Run `scripts/Package.ps1 -Mod refill-containers`. The uploader folder is `artifacts/workshop/refill-containers`, containing `content`, `preview.png` and `description.txt`. A versioned ZIP is also created in `artifacts`.
2. With Steam running, launch `PlateUp_Data/ModUploader.exe` from the game installation.
3. Select the uploader folder, which is the parent of `content`. Set the title to **Refill Containers**, select `preview.png`, and use the text from `description.txt`. Choose the **Tweaks** tag and **Private** visibility for the first upload.
4. Subscribe to the private item, remove the local copy, restart the game and complete the [release checks](docs/TESTING.md). Change visibility to **Public** when those checks pass.
5. Copy the uploader-generated `plateup_mod_metadata.json` into `mods/refill-containers/workshop/`. Reuse that item ID for updates. Increment the mod project's version, rebuild and upload to the existing item.

Do not copy game assemblies or test DLLs into `content`. Do not keep a local and subscribed copy of the same mod active together. Uploading source to GitHub does not publish the Steam Workshop item.

## License

[MIT](LICENSE).
