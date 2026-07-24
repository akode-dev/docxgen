[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repositoryRoot
try {
    dotnet restore Akode.DocxGen.sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet build Akode.DocxGen.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet test Akode.DocxGen.sln --configuration Release --no-build
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
