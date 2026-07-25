[CmdletBinding()]
param(
    [string] $OutputPath = 'THIRD-PARTY-NOTICES.md'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'package-license-allowlist.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Third-party notices')
$lines.Add('')
$lines.Add('Akode.DocxGen uses the following reviewed NuGet packages.')
$lines.Add('Copyright remains with each package owner. The SPDX license')
$lines.Add('identifier shown here is the package license recorded during review.')
$lines.Add('')
$lines.Add('| Package | Version | License |')
$lines.Add('|---|---:|---|')
foreach ($package in $manifest.packages | Sort-Object id, version) {
    $lines.Add("| $($package.id) | $($package.version) | $($package.license) |")
}

$lines.Add('')
$lines.Add('The authoritative dependency gate is')
$lines.Add('`eng/package-license-allowlist.json`; CI rejects unreviewed package')
$lines.Add('IDs, versions, or licenses.')
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path $repositoryRoot $OutputPath
}

$content = [string]::Join("`n", $lines) + "`n"
[System.IO.File]::WriteAllText(
    $resolvedOutput,
    $content,
    [System.Text.UTF8Encoding]::new($false))
