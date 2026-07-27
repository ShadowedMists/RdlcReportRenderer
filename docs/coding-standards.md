# Coding Standards & Engineering Conventions

This document collects the coding guidelines, design conventions, and engineering lessons that apply to work in this repository. `AGENTS.md` covers *how to operate* (workflow, docs upkeep, commit habits); this document covers *how to write and design code* here.

## Engineering Principles

Prefer:

- Composition over inheritance
- Dependency Injection
- SOLID principles
- Clean Architecture
- Hexagonal Architecture where appropriate
- Explicit interfaces
- Small cohesive components
- Immutable models where practical
- Testability
- Separation of concerns

Avoid:

- Static platform checks throughout the codebase
- Platform-specific logic leaking into business logic
- Tight coupling
- Hidden dependencies
- Global state

## Cross-Platform Design

Platform-specific behavior should exist only behind interfaces.

Examples include:

- Text measurement
- Font resolution
- Graphics context
- Image loading
- File system interactions
- Printing
- Native drawing APIs

Never allow application logic to directly depend on `System.Drawing` or Windows APIs.

## Adapter Pattern

Prefer adapters for platform implementations.

Example:

```
IGraphicsContext
↓
WindowsGraphicsContext
LinuxGraphicsContext
MacGraphicsContext
```

Similarly:

```
ITextMeasurer
↓
WindowsTextMeasurer
LinuxTextMeasurer
TestTextMeasurer
```

## Rendering Extensibility

Design rendering as a plugin architecture.

Preferred concepts include:

- `IRenderer`
- `IRenderingExtension`
- `IOutputWriter`
- `IGraphicsContext`
- `IFontResolver`

Future third-party renderers should be loadable without modifying the core engine.

Potential implementations include:

- QuestPDF
- SkiaSharp
- PDFSharp
- ImageSharp
- OpenXML
- Custom renderers

## Plugin Architecture

Favor a discoverable plugin model.

Possible mechanisms include:

- Reflection
- Assembly scanning
- Dependency Injection
- MEF-style discovery
- Explicit registration

Do not tightly couple renderer implementations to the core library.

## Dependency Injection

- Avoid service location.
- Prefer constructor injection.
- Dependencies should be interfaces whenever practical.

## Testing Philosophy

Every abstraction should have at least three implementations:

- Production implementation
- Platform implementation
- Test implementation

Example:

```
ITextMeasurer
↓
WindowsTextMeasurer
LinuxTextMeasurer
RecordingTextMeasurer
```

Test implementations should verify behavior rather than visual output.

## Test Design

Prefer:

- Behavioral testing
- Golden master tests
- Snapshot tests
- Recording adapters
- Deterministic rendering
- Avoid pixel-perfect comparisons whenever possible.

## Code Investigation Guidelines

This project is, in large part, an architectural investigation (porting a Windows-only rendering engine to be cross-platform). When analyzing code:

- Read before modifying.
- Document before refactoring.
- Trace call graphs.
- Identify responsibilities.
- Record evidence.
- Never speculate.

Every architectural conclusion should reference source files (and ideally line numbers).

## Coding Standards

Write code that is:

- Small
- Readable
- Well documented
- Incrementally refactorable
- Easy to test
- Easy to extend
- Favor explicitness over cleverness.

## Preferred Design Patterns

Favor:

- Adapter
- Strategy
- Facade
- Factory
- Composition
- Decorator
- Dependency Injection
- Ports and Adapters
- Repository (where appropriate)

Avoid unnecessary abstraction. Do not introduce design patterns unless they clearly simplify the architecture.

## Quality Attributes

Evaluate all proposals against:

- Maintainability
- Extensibility
- Portability
- Testability
- Performance
- Reliability
- Developer Experience
- Backwards Compatibility
- Security
- Complexity

## Risk Analysis

Every non-trivial proposal should consider:

- Technical risks
- Migration risks
- Performance risks
- Compatibility risks
- Testing impact
- Suggested mitigations

## Documentation Standards

When generating documentation, explain:

- Purpose
- Responsibilities
- Dependencies
- Extension points
- Risks
- Technical debt
- Unknowns

Prefer diagrams and call graphs where appropriate. (Where these belong day-to-day — `docs/` vs. `tasks/` vs. `TODO.md` — is covered in `AGENTS.md`'s "Internal Documentation" section.)

### Architecture Decision Records

For significant decisions, generate ADRs including:

- Context
- Problem
- Options
- Decision
- Consequences
- Future considerations

### Technical Specifications

When generating technical specifications, include:

- Overview
- Goals
- Non-goals
- Requirements
- Constraints
- Architecture
- Interfaces
- Risks
- Testing strategy
- Migration strategy
- Open questions

### Product Requirements Documents

When generating PRDs, include:

- Problem Statement
- Goals
- Success Metrics
- User Stories
- Acceptance Criteria
- Non-functional Requirements
- Performance
- Security
- Accessibility
- Maintainability
- Deployment considerations
- Future roadmap

## Conventions established during the Chart/Gauge GDI+ migration

These apply to any similar incremental type-abstraction or interface-introduction effort:

- **Dual-overload strategy:** don't retype an existing method/field in place if it still has real concrete callers. Add a new, separately-named interface-typed sibling instead (e.g. `GetHatchBrushResource` next to `GetHatchBrush`), and migrate real callers to it one at a time. This is what keeps a large migration incremental and revert-safe.
- **Bridge-at-the-sink:** when a concrete resource can't reasonably be retyped at its source (a public model property, a self-contained legacy geometry helper), wrap/reconstruct it into the interface type only at the point it's consumed, rather than forcing the source to change.
- **The "large atomic pass" trap:** shared concrete fields/arrays on a helper class often look individually convertible per-getter, but are all consumed together by one call downstream — converting one getter without the whole class, its producers, and its consumer in one pass just adds unreachable dead code. Identify these and do them as one deliberate pass, not sliced.
- **Verification gate:** every increment must have `dotnet build` (0 errors) + full test suite passing + zero baseline diffs before being considered done. For previously-uncovered render paths, generate a "before" baseline via `git stash push --keep-index` on just the engine files being converted, render through the pre-conversion code, pop the stash, and confirm byte-for-byte match.
- Don't force an abstraction whose semantics can't be verified end-to-end — document a genuine gap honestly rather than risk a subtly wrong port.
- **A scene rendering without throwing does not mean it rendered correctly.** The most serious bug found in this migration (`SkiaGraphicsPath.AddLine` silently fragmenting a multi-segment polyline into disconnected zero-area segments) threw no exception anywhere — it was only caught by eyeballing a rendered PNG against its GDI+ counterpart. Treat "compiles and runs" and "renders the right pixels" as two separate claims; verify both before promoting a baseline, especially for a newly-reached code path.
- **A "blocked"/"architecturally blocked" label can outlive the reasoning that produced it.** Twice in this migration (Gauge's `XamlRenderer.cs`, and Chart's D3-vs-"3D rendering is impossible" conflation), a scoping conclusion from an earlier pass turned out not to hold once re-derived from the current code. Before accepting or extending a "blocked" note in a task doc, re-trace the actual blocker yourself rather than trusting the label — it costs one grep pass and has twice turned "not started" into "actually already easy."
- **A virtual method's overrides all share one signature — trace the whole override family before concluding a return-type change is blocked.** When one override in the family has a genuine concrete-type entanglement (e.g. Chart's `Draw3DSurface`/`Draw3DPolygon` family, entangled via `ClipTopPoints`/`ClipBottomPoints`) and the others don't, the fix is to convert the entangled dependency first, not to declare the whole family permanently blocked — the entanglement is usually narrower than "the whole virtual slot." Converting it later (Milestone D3-real) confirmed it: a re-grep found `ClipTopPoints`/`ClipBottomPoints` actually declared on `LineChart.cs`, not `AreaChart.cs` as the original scoping note said — even a correct top-line conclusion ("this is where the entanglement is") can carry a wrong file/line citation forward; re-verify the citation, not just the claim, before resuming from an old note.
- **When prototyping against a native-interop library, prefer an already-proven high-level wrapper over hand-rolling the lower-level API, even for a throwaway prototype.** PDF's HarfBuzzSharp shaping prototype hand-built `HarfBuzzSharp.Face`/`Font` from a raw font blob and hit an intermittent native access violation that only appeared after many cumulative shape calls across a test run — never within one isolated test. Switching to `SkiaSharp.HarfBuzz.SKShaper` (already proven stable elsewhere in this repo) fixed it. The lesson generalizes beyond this one bug: composing several independently-tested prototypes together (not just unit-testing each in isolation) is often what surfaces this class of defect — budget a composition/integration step before trusting any prototype, especially one touching native memory. See `docs/decisions.md`'s `SKShaper` entry.
- **A public API's declared return type can be a harder wall than any internal abstraction gap.** `ChartImage.GetImage(float):Bitmap` cannot be fixed by any amount of interface-typing internally — it's an external contract requiring a concrete `System.Drawing.Bitmap`, which can't even be constructed on Linux. Before scoping "make X cross-platform," check whether X's real production caller actually goes through the method with the hard external contract, or through a sibling method (here, `Chart.Save(Stream,...)`/`ChartImage.SaveImage`) that was already abstracted — conflating the two wastes effort chasing a permanently unfixable entry point instead of the reachable one.
- **A planned increment's mechanism working in isolation doesn't mean its precondition holds.** PDF's base-14 text path was going to be extended with bidi/RTL reordering (correct itemization → correct visual order for Hebrew/Arabic runs). The reordering logic worked, but was dead code: base-14 fonts only support `/Encoding /WinAnsiEncoding`, which has zero Hebrew/Arabic/Cyrillic/Greek/CJK code points, so there were no real glyphs to reorder. A passing test suite would not have caught this — it took reasoning about what the *drawn output* would actually contain, not just what the *code* would do, to notice the precondition was false. Surface this kind of finding as a scope question rather than shipping the mechanism as if it were real progress. See `docs/decisions.md`'s "reverse the font-embedding deferral" entry (2026-07-26).
