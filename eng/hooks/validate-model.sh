#!/usr/bin/env sh
set -eu

if [ "$#" -ne 3 ]; then
  echo "usage: validate-model.sh <template.docx> <model.json> <assets-dir>" >&2
  exit 2
fi

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$repo_root"

dotnet run \
  --project src/Akode.DocxGen.Cli/Akode.DocxGen.Cli.csproj \
  -- validate-model \
  --template "$1" \
  --model "$2" \
  --assets-dir "$3" \
  --json
