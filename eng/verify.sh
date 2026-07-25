#!/usr/bin/env sh
set -eu

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo_root"

dotnet restore DocxGen.sln
dotnet build DocxGen.sln --configuration Release --no-restore
dotnet test DocxGen.sln --configuration Release --no-build
