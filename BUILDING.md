# Building ValheimEnchantmentSystem

## Prerequisites

1. .NET SDK installed (`dotnet --version`).
2. Game publicized assemblies available.
3. Dependency binaries resolvable (`ServerSync.dll`, `YamlDotNet.dll`).

## Configure Valheim managed assemblies path

Set one of the following before build:

- `VALHEIM_DIR`
  - Example: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
- `VALHEIM_MANAGED_DIR`
  - Example: `C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\publicized_assemblies`

`VALHEIM_MANAGED_DIR` has priority when both are set.

## Dependency resolution order

### ServerSync.dll

The build resolves `ServerSync.dll` in this order:

1. `Libs/ServerSync.dll`
2. `VALHEIM_SERVER_SYNC_DLL` (exact file path)
3. `$(VALHEIM_DIR)\BepInEx\plugins\ServerSync.dll`

### YamlDotNet.dll

The build resolves `YamlDotNet.dll` in this order:

1. `packages/YamlDotNet.16.3.0/lib/net47/YamlDotNet.dll`
2. `Libs/YamlDotNet.dll`
3. `VALHEIM_YAMLDOTNET_DLL` (exact file path)

### PowerShell example

```powershell
$env:VALHEIM_DIR="C:\Program Files (x86)\Steam\steamapps\common\Valheim"
dotnet build ValheimEnchantmentSystem.csproj
```

Optional explicit dependency paths:

```powershell
$env:VALHEIM_SERVER_SYNC_DLL="C:\path\to\ServerSync.dll"
$env:VALHEIM_YAMLDOTNET_DLL="C:\path\to\YamlDotNet.dll"
dotnet build ValheimEnchantmentSystem.csproj
```

## Build

```powershell
nuget restore ValheimEnchantmentSystem.sln
dotnet build ValheimEnchantmentSystem.csproj
dotnet build ValheimEnchantmentSystem.csproj -c Release
```

## Notes

- The project fails early with a clear error if Valheim publicized assemblies are missing.
- The project fails early if `ServerSync.dll` or `YamlDotNet.dll` cannot be resolved.
- Build copies to `ThunderstorePackage/` are best-effort and do not fail the build if the target file is locked by another process.
