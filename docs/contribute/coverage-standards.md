# Coverage Standards

## 100% line and branch coverage, on every run

Every `Zaturnava.*.Test` project measures the one project it tests and **fails below 100% line
and 100% branch coverage**: every guard, every `switch` arm, both sides of every ternary and
null-coalescing operator. It is not a CI extra: the threshold is part of every `dotnet test`,
local or CI, wired once in `Directory.Build.props` and `Directory.Build.targets`:

- `coverlet.MTP`, not `coverlet.collector`, a VSTest data collector that silently measures
  nothing under the Microsoft Testing Platform runner that `global.json` selects.
- `--coverlet-include "[Net.NowhereAtAll.<Project>]*"`, derived from the test project's name
  (`Zaturnava.Engine.Test` measures `Net.NowhereAtAll.Zaturnava.Engine`), so neither the test
  assembly, the projects it also references, nor xunit's own DLLs drag the numbers down.
- An explicit `--coverlet-exclude`. Left to its default, coverlet.MTP excludes every assembly
  loaded alongside the tests, cut to its first two name segments: any
  `Net.NowhereAtAll.Zaturnava.*` assembly becomes `[Net.NowhereAtAll.*]*`, which excludes the
  measured assembly too, and the threshold fails on "No coverable data".
- `Zaturnava.Architecture.Test` isn't measured: it checks how the other assemblies depend on one
  another, so it measures no one assembly.

**Release counts too.** Optimized IL branches differently from Debug: a condition that one
configuration compiles away can be an uncovered branch in the other. CI runs the tests in both.

**When the bar is hard to reach, the code is usually the problem.** A branch no test can reach
is dead code: remove it rather than cover it. A property pattern on a reference type carries a
null test; if the value can't be null, a pattern over its values (or a member of the type
itself) leaves that unreachable branch out.

A run that can't reach 100% by design, such as one filtered to a single class, opts out:

```shell
dotnet test --project Zaturnava.Engine.Test -p:ZaturnavaCoverage=false --filter-class "*MoveGeneratorTest"
```

## The one escape hatch: `ZaturnavaUnmeasuredFile`

A file that only its real host can run — a program's entry point, started by the operating
system, by a browser's WebAssembly runtime, or by an MCP client over stdio — is named in its
test project's `ZaturnavaUnmeasuredFile`, with the reason in a comment beside it, and in a
comment in the file itself. `Directory.Build.targets` turns it into
`--coverlet-exclude-by-file`.

Such a file holds nothing but the call into code that is measured: the composition
(`AddZaturnavaWeb`, `AddZaturnavaMcp`) or the command (`SelfPlayCommand.Run`) is in a file of
its own, and tested. Today three files use it, all `Program.cs`: those of Web, Mcp and
BotRunner. The server's `Program.cs` needs none: `WebApplicationFactory` runs it.

## The gate is proven, not assumed

`scripts/verify-gates.py` adds a class no test covers to `Zaturnava.Contracts` and requires its
tests to fail, with every test passing, and the coverage report to show the class was measured.
A coverlet that measures nothing also fails the threshold, but for the wrong reason. See
[ci](ci.md#proving-the-gates-fire).
