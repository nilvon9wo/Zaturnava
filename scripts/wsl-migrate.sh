#!/usr/bin/env bash
# Add an Entity Framework Core migration, named for what it changes, for each database the server can keep
# games in, under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-migrate.sh AddPlayers
#
# Each database has its own migrations project (src/Persistence/Zaturnava.Persistence.EfCore.<Database>), which
# builds its own GameDbContext at design time, so no server and no connection is needed. The tool comes from
# dotnet-tools.json.
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
name="${1:-}"
if [[ ! "$name" =~ ^[A-Z][A-Za-z0-9]*$ ]]; then
    echo "Name the migration in PascalCase, letters and digits only: wsl-migrate.sh AddPlayers"
    exit 1
fi
# MSBuild reads these as properties, in the tool's own builds too: the build goes to artifacts/wsl/ (git-ignored),
# as wsl-test.sh's does, apart from Windows' own bin/ and obj/, so the two never clash.
export UseArtifactsOutput=true
export ArtifactsPath="$PWD/artifacts/wsl"
dotnet tool restore >/dev/null || exit 1
for database in SqlServer Sqlite; do
    project="src/Persistence/Zaturnava.Persistence.EfCore.$database"
    echo "== $database"
    dotnet build "$project" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
    dotnet ef migrations add "$name" --project "$project" --output-dir Migrations --no-build 2>&1 | tail -5 || exit 1
done
