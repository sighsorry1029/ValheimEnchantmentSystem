param(
    [Parameter(Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [switch] $Check
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$pluginFile = Join-Path $repoRoot "ValheimEnchantmentSystem.cs"
$assemblyInfoFile = Join-Path $repoRoot "Properties\AssemblyInfo.cs"
$manifestFile = Join-Path $repoRoot "Thunderstore\manifest.json"
$releaseDll = Join-Path $repoRoot "bin\Release\kg.ValheimEnchantmentSystem.dll"

function Read-Utf8Text {
    param([string] $Path)
    return [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
}

function Write-Utf8TextPreserveBom {
    param([string] $Path, [string] $Text)

    $bytes = [IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($hasBom))
}

function Get-ModVersion {
    $text = Read-Utf8Text $pluginFile
    $match = [regex]::Match($text, 'public\s+const\s+string\s+ModVersion\s*=\s*"([^"]+)"')
    if (-not $match.Success) {
        throw "Could not find ModVersion in $pluginFile."
    }

    return $match.Groups[1].Value
}

function Assert-AssemblyVersionLink {
    $text = Read-Utf8Text $assemblyInfoFile
    $typeName = 'kg.ValheimEnchantmentSystem.ValheimEnchantmentSystem.ModVersion'
    if ($text -notmatch [regex]::Escape("AssemblyVersion($typeName)")) {
        throw "AssemblyVersion does not reference ModVersion."
    }
    if ($text -notmatch [regex]::Escape("AssemblyFileVersion($typeName)")) {
        throw "AssemblyFileVersion does not reference ModVersion."
    }
}

if ($Check) {
    Assert-AssemblyVersionLink
    $modVersion = Get-ModVersion
    $manifestVersion = (Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json).version_number

    if (-not (Test-Path -LiteralPath $releaseDll -PathType Leaf)) {
        throw "Release DLL does not exist. Run a Release build before -Check."
    }

    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($releaseDll).Version
    $dllVersion = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
    [pscustomobject]@{
        ModVersion = $modVersion
        AssemblyVersion = $assemblyVersion.ToString()
        Manifest = $manifestVersion
    } | Format-List

    if ($dllVersion -ne $modVersion -or $manifestVersion -ne $modVersion) {
        throw "Release version fields are not synchronized."
    }

    Write-Host "Release version fields are synchronized."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Pass -Version x.y.z or use -Check after a Release build."
}

$pluginText = Read-Utf8Text $pluginFile
$pattern = '(public\s+const\s+string\s+ModVersion\s*=\s*")[^"]+("\s*;)'
$matches = [regex]::Matches($pluginText, $pattern)
if ($matches.Count -ne 1) {
    throw "Expected exactly one ModVersion declaration, found $($matches.Count)."
}

$match = $matches[0]
$replacement = $match.Groups[1].Value + $Version + $match.Groups[2].Value
$updated = $pluginText.Substring(0, $match.Index) + $replacement + $pluginText.Substring($match.Index + $match.Length)
Write-Utf8TextPreserveBom $pluginFile $updated
Assert-AssemblyVersionLink
Write-Host "Updated ModVersion to $Version. A Release build will update the DLL and Thunderstore manifest."
