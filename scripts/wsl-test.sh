#!/usr/bin/env bash
# Build the solution and run test projects under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-test.sh [--release] [Zaturnava.X.Test ...]
#
# With no names, every test project runs. With --release, the Release build is tested, as CI does too: optimized IL
# branches differently, so full coverage in Debug does not imply it in Release. WSL starts in the current folder, so run it from the repository's
# root. The build goes to artifacts/wsl/ (git-ignored), apart from Windows' own bin/ and obj/, so the two never
# clash. For a machine whose application control blocks the test assemblies a Windows build makes.
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
artifacts=(-p:UseArtifactsOutput=true "-p:ArtifactsPath=$PWD/artifacts/wsl")
if [ "${1:-}" = "--release" ]; then
    artifacts+=(-c Release)
    shift
fi
# A failed build stops here, rather than testing what an earlier build left.
dotnet build Zaturnava.slnx "${artifacts[@]}" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
projects=("$@")
if [ ${#projects[@]} -eq 0 ]; then
    mapfile -t projects < <(find tests -name "Zaturnava.*.Test.csproj" -printf "%f\n" | sed 's/\.csproj$//' | sort)
fi
for project in "${projects[@]}"; do
    echo "== $project"
    path=$(dirname "$(find tests -name "$project.csproj" | head -1)")
    python3 scripts/run-tests.py --project "$path" "${artifacts[@]}" --no-build 2>&1 \
        | grep -E "failed|[Ee]xit code|[Ee]rror|Assert\.|Expected:|Actual:|Exception|summary|total:|[Tt]hreshold|below|\| " | head -30
done
