param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectDir,

    [Parameter(Mandatory = $true)]
    [string] $DllPath,

    [Parameter(Mandatory = $true)]
    [string] $IntermediateDirectory
)

$ErrorActionPreference = "Stop"

$repoRoot = [IO.Path]::GetFullPath($ProjectDir).TrimEnd('\', '/')
$repoPrefix = $repoRoot + [IO.Path]::DirectorySeparatorChar
$dll = [IO.Path]::GetFullPath($DllPath)
$intermediateRoot = if ([IO.Path]::IsPathRooted($IntermediateDirectory)) {
    [IO.Path]::GetFullPath($IntermediateDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $IntermediateDirectory))
}

function Assert-FileExists {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required release file does not exist: $Path"
    }
}

function Reset-Directory {
    param([string] $Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the repository: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath | Out-Null
    return $fullPath
}

$thunderstoreSource = Join-Path $repoRoot "Thunderstore"
$manifestPath = Join-Path $thunderstoreSource "manifest.json"
$iconPath = Join-Path $thunderstoreSource "icon.png"
$readmePath = Join-Path $repoRoot "README.md"
$changelogPath = Join-Path $repoRoot "CHANGELOG.md"
$englishLocalizationPath = Join-Path $repoRoot "translations\kg.ValheimEnchantmentSystem.English.yml"

foreach ($requiredFile in @($dll, $manifestPath, $iconPath, $readmePath, $changelogPath, $englishLocalizationPath)) {
    Assert-FileExists $requiredFile
}

$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
if ($assemblyVersion.Build -lt 0) {
    throw "DLL assembly version must contain major, minor, and build components: $assemblyVersion"
}

$packageVersion = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
$manifestText = [IO.File]::ReadAllText($manifestPath, [Text.Encoding]::UTF8)
$manifest = $manifestText | ConvertFrom-Json
$versionPattern = '("version_number"\s*:\s*")[^"]*(")'
$versionMatches = [regex]::Matches($manifestText, $versionPattern)
if ($versionMatches.Count -ne 1) {
    throw "Expected exactly one version_number in $manifestPath, found $($versionMatches.Count)."
}

$versionMatch = $versionMatches[0]
$versionReplacement = $versionMatch.Groups[1].Value + $packageVersion + $versionMatch.Groups[2].Value
$updatedManifest = $manifestText.Substring(0, $versionMatch.Index) + $versionReplacement + $manifestText.Substring($versionMatch.Index + $versionMatch.Length)
$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($manifestPath, $updatedManifest, $utf8NoBom)

$packageName = [string]$manifest.name
if ([string]::IsNullOrWhiteSpace($packageName)) {
    throw "Thunderstore manifest name is empty."
}

$thunderstoreTemp = Reset-Directory (Join-Path $intermediateRoot "Thunderstore")
$nexusTemp = Reset-Directory (Join-Path $intermediateRoot "Nexus")
$artifactsDirectory = Join-Path $repoRoot "artifacts"
New-Item -ItemType Directory -Path $artifactsDirectory -Force | Out-Null

$dllName = [IO.Path]::GetFileName($dll)
Copy-Item -LiteralPath $dll -Destination (Join-Path $thunderstoreTemp $dllName)
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $thunderstoreTemp "README.md")
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $thunderstoreTemp "CHANGELOG.md")
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $thunderstoreTemp "manifest.json")
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $thunderstoreTemp "icon.png")
Copy-Item -LiteralPath $englishLocalizationPath -Destination (Join-Path $thunderstoreTemp "kg.ValheimEnchantmentSystem.English.yml")
Copy-Item -LiteralPath $dll -Destination (Join-Path $nexusTemp $dllName)

$expectedThunderstoreFiles = @($dllName, "README.md", "CHANGELOG.md", "manifest.json", "icon.png", "kg.ValheimEnchantmentSystem.English.yml") | Sort-Object
$actualThunderstoreFiles = Get-ChildItem -LiteralPath $thunderstoreTemp -File | Select-Object -ExpandProperty Name | Sort-Object
if (Compare-Object $expectedThunderstoreFiles $actualThunderstoreFiles) {
    throw "Thunderstore staging contents do not match the required six files."
}

$actualNexusFiles = @(Get-ChildItem -LiteralPath $nexusTemp -File | Select-Object -ExpandProperty Name)
if ($actualNexusFiles.Count -ne 1 -or $actualNexusFiles[0] -ne $dllName) {
    throw "Nexus staging must contain only $dllName."
}

$thunderstoreZip = Join-Path $artifactsDirectory "$packageName-$packageVersion-Thunderstore.zip"
$nexusZip = Join-Path $artifactsDirectory "$packageName-$packageVersion-Nexus.zip"
foreach ($zip in @($thunderstoreZip, $nexusZip)) {
    if (Test-Path -LiteralPath $zip) {
        Remove-Item -LiteralPath $zip -Force
    }
}

Compress-Archive -Path (Join-Path $thunderstoreTemp '*') -DestinationPath $thunderstoreZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $nexusTemp '*') -DestinationPath $nexusZip -CompressionLevel Optimal

Write-Host "Release package version: $packageVersion"
Write-Host "Thunderstore zip: $thunderstoreZip"
Write-Host "Nexus zip: $nexusZip"
