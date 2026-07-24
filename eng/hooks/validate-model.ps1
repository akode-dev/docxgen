[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Template,

    [Parameter(Mandatory)]
    [string] $Model,

    [Parameter(Mandatory)]
    [string] $AssetsDir
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Push-Location $repositoryRoot
try {
    dotnet run `
        --project src/Akode.DocxGen.Cli/Akode.DocxGen.Cli.csproj `
        -- validate-model `
        --template $Template `
        --model $Model `
        --assets-dir $AssetsDir `
        --json

    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
