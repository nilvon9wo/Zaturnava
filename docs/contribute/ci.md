# CI

`.github/workflows/ci.yml` runs on every push to `main` and every pull request, whatever it
merges into.

## The Linux job

1. Restore and build, with every `.editorconfig` rule and every compiler warning an error.
2. `dotnet format --verify-no-changes --severity info`.
3. `scripts/inspect-code.py`: ReSharper's `inspectcode`; every finding fails.
4. `scripts/check-line-layout.py`: line length and wrapped `)`.
5. `scripts/run-tests.py`: every test project, each with its 100% coverage gate.
6. The same build and tests in Release: optimized IL branches differently, so Debug coverage
   doesn't imply Release coverage.
7. `scripts/verify-doc-links.py`.
8. `scripts/verify-gates.py`.

## The Windows job

Builds and runs the tests on Windows, where the weaving (Stipulate, ConfigureAwait.Fody) runs
with Windows paths and quoting, and where a Windows client would be built.

## Proving the gates fire

A gate that silently stops running looks exactly like a gate that passes.
`scripts/verify-gates.py` proves each one still catches what it should, and undoes every
temporary change however it ends:

| Gate | Proven by |
|---|---|
| build, format, layout, inspect | `StyleCanary/`: deliberately non-compliant code, each violation tagged `// expect: <gate>:<rule>` on the line the gate must report. Every tag must be matched, and every gate must have at least one. |
| coverage | A temporary class no test covers, in `Zaturnava.Contracts`: its tests must fail with every test passing, and the coverage report must show the class was measured. |
| `run-tests.py`'s INCONCLUSIVE verdict | One test module's apphost (`Zaturnava.Bots.Test`'s) is hidden, so it can't start; the verdict must be INCONCLUSIVE, not "Passed!". |
| documentation links | A temporary page with a broken link: `verify-doc-links.py` must report it. |

When you add a rule to `.editorconfig` or a gate to CI, add its canary.
