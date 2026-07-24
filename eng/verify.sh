#!/usr/bin/env sh
set -eu

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo_root"

dotnet restore Akode.DocxGen.sln
dotnet build Akode.DocxGen.sln --configuration Release --no-restore
dotnet test Akode.DocxGen.sln --configuration Release --no-build
