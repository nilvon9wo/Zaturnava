# Coding Standards

The rules this repository's code is held to. They apply to anyone changing it, human or AI.
When a change is reviewed, this is the checklist.

**The authoritative style rules:**

- **`.editorconfig`**, at the repository root, ported from XFTY through Stipulate. Every rule in
  it is `severity = error` on purpose; see [Everything is `error`](#everything-is-error).
  `EnforceCodeStyleInBuild=true`, so `dotnet build` fails on almost all of them, IDE1006 naming
  included. What it misses (unnecessary usings, line length, wrapped-`)` placement) has its own
  gate; see [Quality gates](#quality-gates). Files end with a newline.
- **The house rules**, layered on top of the analyzers (not tool-enforced; reviewed by hand):
  1. Prefer functional over imperative. A `foreach` is acceptable where the alternative is a
     contorted LINQ chain; a bare `for`/`while` counter almost never is.
  2. Prefer explicit over implicit: never `var`, patterns included.
  3. Keep methods short: a method past ~10 lines is usually doing two things. A smell to
     investigate, not a hard limit to contort code around.
  4. Never more than one expression per line.
  5. Blocks must not be nested more than two deep.
  6. Never nest expressions; give the inner one a name.
  7. Use variables or methods to name the results of all complex expressions.
  8. Name classes, methods and variables to communicate intentions; never a single letter or an
     abbreviation.
  9. Nouns name objects; verbs name methods.
  10. Adjectives name interfaces, prefixed with `I`: an interface says what its implementers are
      able to do (`ICellNavigating`, `IRulesApplying`, `IGamePersisting`).
  11. Name booleans `isSomething`, `hasSomething`, `wasSomething`.
  12. Always use `this`, except for static members.
  13. Extract magic numbers and strings to constants.
  14. Prefer a ternary to if/else, on three lines: the condition, then each outcome.
  15. Don't nest a complicated expression inside a ternary: extract it first.
  16. Intention and separation of concerns over line count.
  17. Never use nested classes: a helper scoped to one file is a `file sealed class` at namespace
      scope.
  18. Avoid circular dependencies; the project dependencies are a table, enforced (see
      [Design](#design)).

---

## Everything is `error`

Every diagnostic in `.editorconfig` is `severity = error`, deliberately. A diagnostic is fixed,
never demoted to `warning` or `suggestion` to sit on a list that then gets ignored. The
compiler's own warnings and NuGet's are no exception: `TreatWarningsAsErrors` is on for every
project (`Directory.Build.props`), so a vulnerable transitive package (NU1902, NU1903) is pinned
to a fixed version rather than tolerated.

If a rule genuinely can't be satisfied at one site, restructure the code so that it is, rather
than suppress it. A `#pragma` or `[SuppressMessage]` is a last resort and carries a comment
saying why. A rule switched off for the whole repository (CA2007, below) says why in
`.editorconfig`.

---

## Naming

| Symbol | Style |
|---|---|
| types, namespaces, methods, non-private properties, type parameters | `PascalCase` |
| interfaces | `IPascalCase` |
| `const` and `static readonly` | `PascalCase` |
| private instance fields, including primary-constructor parameters held in fields | `_camelCase` |
| mutable private static fields | `s_camelCase` |
| parameters, locals, lambda parameters | `camelCase` |

Every assembly and namespace starts with `Net.NowhereAtAll.`, as XFTY's and Stipulate's do;
`Directory.Build.props` sets it from the project name, so the folders and projects keep the
short name (`Zaturnava.Engine` builds `Net.NowhereAtAll.Zaturnava.Engine.dll`).

Tests are named `<MethodUnderTest>_When<Condition>_<ExpectedOutcome>`; see [Testing](#testing).

---

## Formatting

- **Line length: 120, hard.** Roslyn reads `max_line_length` but never reports on it, so
  `scripts/check-line-layout.py` does.
- **One expression per line; one declaration per line.**
- **Expression bodies** wherever a member is one expression.
- **A wrapped call or declaration closes with `)` on its own line**, aligned with the statement
  that opened it. A trailing lambda block or collection expression doesn't count as wrapping.

```csharp
// no
return new GameRecord(
    id, rulesetId, controllers, actions, version);

// yes
return new GameRecord(
    id,
    rulesetId,
    controllers,
    actions,
    version
);
```

---

## Preconditions: Stipulate

Every non-test project references [Stipulate](https://github.com/nilvon9wo/Stipulate), the way
a package consumer would (`Directory.Build.props`). A parameter's precondition is a
DataAnnotations attribute — `[Required]`, `[Range]` — woven into a run-time check, never a
hand-written guard. Stipulate's analyzer holds every implementation to the contract of the
interface it implements, so an implementation restates its interface's attributes (ST1002),
test fakes included.

## Asynchrony: ConfigureAwait.Fody

Library code never needs its caller's synchronization context back after an `await`, so
ConfigureAwait.Fody weaves `ConfigureAwait(false)` into every await of every non-test project.
Never write `ConfigureAwait` by hand; CA2007, which cannot see the weaving, is off.

---

## Quality gates

CI (`.github/workflows/ci.yml`) fails the build on any of:

| Gate | Covers |
|---|---|
| `dotnet build` (`EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`) | compilation, with every warning an error, and every `.editorconfig` rule that runs in the build |
| `dotnet format Zaturnava.slnx --verify-no-changes --severity info` | whitespace and formatting, and a second pass over the style rules |
| `scripts/inspect-code.py` (ReSharper `inspectcode`) | what Roslyn misses, such as unnecessary usings, redundant casts and unresolvable doc-comment references; every finding fails |
| `scripts/check-line-layout.py` | the 120-character ceiling and wrapped-`)` placement |
| `scripts/run-tests.py`, in Debug and Release | every test, with **100% line and branch coverage** of each project; a module that never ran makes the run `INCONCLUSIVE`, not "Passed!" |
| `scripts/verify-doc-links.py` | every relative link and anchor in the docs resolves |
| `scripts/verify-gates.py` | every gate above still fires; see [ci](ci.md#proving-the-gates-fire) |

Run the same checks locally before pushing; see [local-development](local-development.md).

---

## Design

- **Ports and adapters.** The engine depends on nothing; the application layer declares the
  ports it needs (`IGamePersisting`, `IGameNotifying`); adapters implement them; the server is
  the only composition root. Each project may use exactly the projects its row in
  `Zaturnava.Architecture.Test`'s `MayUse` table lists, and the test fails on anything else —
  including a new project nobody added to the table. See
  [architecture §5](../architecture.md#5-projects-and-dependencies).
- **Replaceable by configuration.** Hosts, storage, origins and server addresses are read from
  configuration, never written into code.
- **Vendors stay in their adapters.** A third-party service is named only in its own adapter
  project (`Zaturnava.Hosting.Itch`) and in the composition root that selects it by a provider
  name; never in other code, comments or configuration keys.
- **The engine is pure.** No I/O, no clock, no randomness except through what is handed in.
- **Polymorphism over branching.** A type check made in more than one place is a missing type.
- **Immutability.** Records and immutable collections; derive rather than mutate.
- **Remove dead code** rather than working around it, or covering it. A branch whose input the
  type system rules out becomes a `!`, with a comment stating the invariant.
- **A refusal names its cause.** Every refused request maps to one `Refusal` and its status, with
  a message a client can show as it is.

---

## Testing

- **100% line and branch coverage**, enforced on every `dotnet test`; see
  [coverage-standards](coverage-standards.md).
- **One test class per unit under test**, or per behaviour of a large one.
- **One behaviour per test method.** A positive and a negative case are two behaviours; a
  `[Theory]` covers data rows of one behaviour. Rows that xUnit cannot serialize use
  `DisableDiscoveryEnumeration = true`.
- **The Act is exactly one statement.** Arrange declares, Act assigns, Assert reads.
- **AAA comments, verbatim**: `// Arrange`, `// Act`, `// Assert`; an Arrange with nothing to do
  says why (`// Arrange - nothing beyond the arguments`).
- **Names:** `<MethodUnderTest>_When<Condition>_<ExpectedOutcome>`, with no `Test` prefix:
  `Preflight_WhenTheOriginIsTheListedPage_AllowsIt`.
- **`Assert.Throws<TheSpecificException>`** as the Act, never a bare `Exception`, and never a
  try/catch.
- **Test data through [Xfty](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory)
  providers.** A provider holds each type's defaults; a test assigns a value only when the test
  depends on it. Entities therefore carry no property initializers that would compete with the
  provider's defaults.
- **Test against the real thing.** The server is tested through `WebApplicationFactory` and the
  real `HttpGameClient`, storage against real SQLite, the MCP server over in-memory pipes with
  the SDK's own client, and components with bUnit.
