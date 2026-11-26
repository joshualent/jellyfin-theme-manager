[CmdletBinding()]
param(
    # A folder made by Build-Package.ps1, e.g. artifacts/build-20261019-095639 (or ...-jf12.3.0)
    [Parameter(Mandatory)][string]$Package,
    # Defaults to the version in Directory.Build.props.
    [string]$Version,
    # The Jellyfin version the package was built for, as Jellyfin writes it, e.g. 12.4.0.0. Defaults to DefaultJellyfinVersion in Directory.Build.props.
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$TargetAbi,
    [string]$Changelog = 'Preview alpha.',
    # Where the ZIP is downloaded from. Defaults to the GitHub release for this version (tag v<major.minor.patch>).
    [string]$BaseUrl
)
# Builds the Jellyfin plugin repository: repository/manifest.json plus one ZIP per plugin version and Jellyfin version.
# Add the manifest's raw URL in Jellyfin under Dashboard -> Plugins -> Repositories.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$props = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup
if (!$Version) { $Version = $props.Version + '.0' }
$defaultAbi = $props.DefaultJellyfinVersion + '.0'
if (!$TargetAbi) { $TargetAbi = $defaultAbi }
# Repository that hosts the manifest and the release ZIPs.
$githubRepo = 'joshualent/jellyfin-theme-manager'
$owner = 'joshualent'
if (!$BaseUrl) { $BaseUrl = "https://github.com/$githubRepo/releases/download/v" + $Version.Substring(0, $Version.LastIndexOf('.')) }
$repository = Join-Path $projectRoot 'repository'
$guid = '267f4d8e-f04c-49ab-9373-f721912cee86'
# Jellyfin downloads this when it installs the plugin and shows it on the plugin card.
$imageUrl = "https://raw.githubusercontent.com/$githubRepo/main/logo.png"
$description = 'Seasonal and permanent themes, a personal theme per user, library banners and theme packs for Jellyfin Web.'
$overview = 'Each user chooses their own theme; server branding is not changed. Install File Transformation to show themes in the normal Jellyfin Web address.'
# Invariant culture: a Danish locale would write 13.24.08, which Jellyfin cannot parse.
$timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
# The default Jellyfin version has no suffix; other Jellyfin versions get one.
$jellyfin = $TargetAbi.Substring(0, $TargetAbi.LastIndexOf('.'))
$zipName = if ($TargetAbi -eq $defaultAbi) { "theme-manager_$Version.zip" } else { "theme-manager_$Version-jf$jellyfin.zip" }

$stage = Join-Path ([IO.Path]::GetTempPath()) ('tm-repo-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    # Only the plugin's own assemblies: never Jellyfin or framework DLLs, docs or local logs.
    foreach ($name in 'Jellyfin.Plugin.ThemeManager.dll', 'ThemeManager.Core.dll') {
        Copy-Item -LiteralPath (Join-Path $projectRoot $Package "ThemeManager_$Version" $name) -Destination $stage
    }
    # Standard themes; the plugin copies them into the server's pack folder on first start (DefaultPacks).
    Copy-Item -LiteralPath (Join-Path $projectRoot 'default-packs') -Destination (Join-Path $stage 'default-packs') -Recurse
    Copy-Item -LiteralPath (Join-Path $projectRoot 'logo.png') -Destination $stage
    [ordered]@{
        category = 'General'; changelog = $Changelog; description = $description; guid = $guid; name = 'Theme Manager'
        overview = $overview; owner = $owner; targetAbi = $TargetAbi; timestamp = $timestamp; version = $Version
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'meta.json') -Encoding utf8
    New-Item -ItemType Directory -Path $repository -Force | Out-Null
    $zip = Join-Path $repository $zipName
    if (Test-Path $zip) { throw "$zipName already exists. Published versions are never overwritten; bump the version." }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
}
finally { Remove-Item -LiteralPath $stage -Recurse -Force }

$manifestPath = Join-Path $repository 'manifest.json'
$existing = if (Test-Path $manifestPath) { @((Get-Content $manifestPath -Raw | ConvertFrom-Json)[0].versions) } else { @() }
$entry = [ordered]@{
    version = $Version; changelog = $Changelog; targetAbi = $TargetAbi; sourceUrl = "$BaseUrl/$zipName"
    checksum = (Get-FileHash -LiteralPath $zip -Algorithm MD5).Hash; timestamp = $timestamp
}
# One entry per plugin version and Jellyfin version; Jellyfin offers each server only the entries for its own version.
$versions = @($entry) + @($existing | Where-Object { -not ($_.version -eq $Version -and $_.targetAbi -eq $TargetAbi) })
$versions = $versions | Sort-Object -Property @{ Expression = { [version]$_.version }; Descending = $true }, @{ Expression = { [version]$_.targetAbi }; Descending = $true }
$manifest = @([ordered]@{
    guid = $guid; name = 'Theme Manager'; description = $description; overview = $overview; owner = $owner; category = 'General'; imageUrl = $imageUrl
    versions = @($versions)
})
ConvertTo-Json -InputObject $manifest -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Output "Repository updated: $manifestPath ($zipName for Jellyfin $jellyfin, MD5 $($entry.checksum))"
