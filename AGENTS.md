# AGENTS.md

# ReportViewerCore Architecture & Engineering Guidelines

## Mission

Your primary objective is **to implement cross-platform Excel and PDF rendering for ReportViewerCore, enabling the reporting engine to run on both Windows and Linux platforms while preserving backwards compatibility**. The long-term vision is to transform ReportViewerCore from a Windows-centric rendering engine into a modular, extensible, cross-platform reporting platform that supports Windows, Linux, and macOS. Always refer to architectural documentation before proposing changes, and prioritize implementation efficiency while maintaining code quality.

---

# Current Project Goals

## Immediate Priorities

**Do not treat the phase lists below as current status** — they drift. `TODO.md` is the single source of truth for what's done/in-progress/not-started; check it first.

**Excel Rendering:** image-format and background-image abstractions (`ImageFormatType`, `IImageProvider`) are complete.

**Chart & Gauge Rendering (Primary — active):** re-targeting the existing vendored GDI+ engines to SkiaSharp behind their existing rendering seams (Ports & Adapters), not replacing either engine with an external library — see `docs/rendering-abstractions.md` for the design and `tasks/chart-gdi-type-abstraction.md`/`tasks/gauge-gdi-type-abstraction.md` for progress. An earlier decision to replace the Chart engine with OxyPlot was retracted (`docs/decisions.md`) after its supporting evidence didn't hold up on review — don't resurrect it without re-reading that retraction first.

**PDF Rendering (Secondary — not started, high risk):** blocked on Metafile/EMF having no cross-platform equivalent, a deeper architectural blocker than Chart/Gauge's. See `tasks/pdf-render-callstack-analysis.md`.

---

## Long-Term Vision

The architecture should support:

- Windows rendering
- Linux rendering
- macOS rendering
- Third-party rendering engines
- Future rendering technologies

The rendering system should become a platform rather than a collection of built-in renderers.

---

# Engineering Principles

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

---

# Cross Platform Design

Platform-specific behavior should exist only behind interfaces.

Examples include:

- Text measurement
- Font resolution
- Graphics context
- Image loading
- File system interactions
- Printing
- Native drawing APIs

Never allow application logic to directly depend on System.Drawing or Windows APIs.

---

# Adapter Pattern

Prefer adapters for platform implementations.

Example:

IGraphicsContext
↓
WindowsGraphicsContext
LinuxGraphicsContext
MacGraphicsContext

Similarly:

ITextMeasurer
↓
WindowsTextMeasurer
LinuxTextMeasurer
ITestTextMeasurer

---

# Rendering Extensibility

Design rendering as a plugin architecture.

Preferred concepts include:

- IRenderer
- IRenderingExtension
- IOutputWriter
- IGraphicsContext
- IFontResolver

Future third-party renderers should be loadable without modifying the core engine.

Potential implementations include:

- QuestPDF
- SkiaSharp
- PDFSharp
- ImageSharp
- OpenXML
- Custom renderers

---

# Plugin Architecture

Favor a discoverable plugin model.

Possible mechanisms include:

- Reflection
- Assembly scanning
- Dependency Injection
- MEF-style discovery
- Explicit registration

Do not tightly couple renderer implementations to the core library.

---

# Dependency Injection

- Avoid service location.
- Prefer constructor injection.
- Dependencies should be interfaces whenever practical.

---

# Testing Philosophy

Every abstraction should have at least three implementations:

- Production implementation
- Platform implementation
- Test implementation

Example:

ITextMeasurer
↓
WindowsTextMeasurer
LinuxTextMeasurer
RecordingTextMeasurer

Test implementations should verify behavior rather than visual output.

---

# Test Design

Prefer:

- Behavioral testing
- Golden master tests
- Snapshot tests
- Recording adapters
- Deterministic rendering
- Avoid pixel-perfect comparisons whenever possible.

---

# Reverse Engineering Guidelines

This project is primarily an architectural investigation.

When analyzing code:

- Read before modifying.
- Document before refactoring.
- Trace call graphs.
- Identify responsibilities.
- Record evidence.
- Never speculate.

Every architectural conclusion should reference source files.

---

# Documentation Standards

When generating documentation:

Explain:

- Purpose
- Responsibilities
- Dependencies
- Extension points
- Risks
- Technical debt
- Unknowns

Prefer diagrams and call graphs where appropriate.

---

# Architecture Decision Records

For significant decisions generate ADRs including:

- Context
- Problem
- Options
- Decision
- Consequences
- Future considerations

---

# Technical Specifications

When generating technical specifications include:

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

---

# Product Requirements Documents

When generating PRDs include:

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

---

# Quality Attributes

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

---

# Risk Analysis

Every proposal should include:

- Technical risks
- Migration risks
- Performance risks
- Compatibility risks
- Testing impact
- Suggested mitigations

---

# Preferred Design Patterns

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

---

# Coding Standards

Write code that is:

- Small
- Readable
- Well documented
- Incrementally refactorable
- Easy to test
- Easy to extend
- Favor explicitness over cleverness.

---

# Git & Commit Guidelines

**CRITICAL:** Commit work in relevant batches, grouping like behavior. Code should build and tests pass.

- Document changes in `TODO.md` as you progress
- Create internal documentation in the `docs` folder
- Ensure `docs` folder reflects current codebase state
- Use feature branches ONLY when user explicitly requests commits

---

# Documentation Guidelines

## Work Summaries: Keep Brief & Executive-Focused

When communicating work completed, summaries ARE acceptable IF kept brief and focused on executive information.

### ✅ DO Create Brief Work Summaries When:
- Reporting to the user at end of significant tasks
- Documenting major changes in working copy
- Providing quick status update with changes made
- Communicating results of multi-step work

### ✅ Brief Summary Format:
```markdown
## ✅ Task Completed: [Task Name]

### Changes Made
| Item | Status | Details |
|------|--------|---------|
| Item 1 | ✅ Done | Brief description |
| Item 2 | ✅ Done | Brief description |

### Key Points
- 📝 Brief summary of what was done
- 📝 What's next
```

**Keep it short:** 100-300 lines max, focus on executive summary + tables.

### ❌ Do NOT Create Standalone Summary Files

Do not create separate summary markdown files that duplicate information or exist only as documentation (e.g., `*_SUMMARY.md`, `*_UPDATE_SUMMARY.md`, `WORK_SUMMARY.md`, etc.).

**Why:** Separate summary files create clutter and should have authoritative information in `TODO.md` or `docs/` instead.

**Examples to avoid:**
- `AGENTS_UPDATE_SUMMARY.md` (standalone file)
- `CHANGES_SUMMARY.md` (standalone file)
- `PROGRESS_SUMMARY.md` (standalone file)
- `UPDATE_NOTES.md` (standalone file)

**Instead:**
- Update `TODO.md` with task progress and status
- Update `docs/` folder with architectural changes
- If summarizing to user: use brief inline summary in conversation

**Exception:** Standalone summary files may be created if explicitly requested by the user in the current task.

### ✅ Style Guidelines for Summaries

- Use **emojis** liberally (✅, ❌, 🎯, 📝, etc.)
- Use **tables** for lists (much clearer than bullet points)
- Keep **formatting clean** with clear sections
- Include **executive summary** of changes
- Stay **brief and actionable** (no unnecessary detail)

---

# Progress Tracking

## Task Documentation

Use `TODO.md` for:
- Discrete task lists with checkboxes
- Progress tracking per phase
- Blocking issues and dependencies
- Current work status
- Completed task markers

Update `TODO.md` continuously as you:
1. Identify new tasks
2. Begin work on a phase
3. Complete milestones
4. Hit blockers
5. Change priorities

---

# Internal Documentation

## Documentation Folder (`docs/`)

The `docs` folder contains developer-facing documentation that must be kept current with the codebase.

**Purpose:** Enable developers to understand the rendering architecture without reading the full codebase.

**Actual structure** (keep this list in sync with `docs/README.md` — that file is the authoritative index):

```
docs/
├── README.md                   - Index of the documents below
├── rendering-abstractions.md   - Renderer interfaces + Chart/Gauge Ports & Adapters design
├── architecture-map.md         - End-to-end render flow
├── platform-support.md         - Current Windows/Linux/macOS support matrix and known gaps
├── decisions.md                - Architecture decisions and why
├── build-and-test.md           - Local build/test commands
├── renderer-extension-guide.md - How to add another renderer implementation
├── troubleshooting.md          - Common issues and known quirks
└── examples.md                 - Small usage examples
```

**Keep Up-To-Date:**
- After implementing major features, update the relevant doc above (not a new standalone file)
- When discovering a new Windows dependency or cross-platform gap, update `docs/platform-support.md`
- When changing architecture or making a durable decision, update `docs/rendering-abstractions.md`/`docs/decisions.md`
- Before code review, verify docs match implementation
- Link to specific files and line numbers in docs
- Durable facts (architecture, gaps, decisions) belong in `docs/`; session-by-session narrative belongs in a `tasks/*.md` file's own history — or nowhere, once the milestone is done and its facts have moved to `docs/`

**Usage:**
- Developers read `docs/README.md` first, then `docs/rendering-abstractions.md`
- Agents reference docs before proposing changes
- Code reviews check docs for accuracy
- New team members use docs as onboarding

## Task documents (`tasks/`)

Each `tasks/*.md` file tracks one migration/investigation. Keep them lean:
- Once a milestone is fully done, its "what we tried/reverted/found" narrative should be deleted, not accumulated — replace it with a one-line status in a milestone table, and move any durable fact (an architecture decision, a permanent gap, a resolved gotcha) into `docs/`.
- Keep only what's needed to *resume* work: exact blockers, file/line references, what's been tried and ruled out — for items that are still open.
- If a whole document becomes fully superseded (its proposed work is done and nothing durable remains outside what's now in `docs/`), delete it or shrink it to a one-line pointer, rather than leaving it to be rediscovered and re-read by a future session.

## Conventions established during the Chart/Gauge GDI+ migration

These apply to any similar incremental type-abstraction or interface-introduction effort:

- **Dual-overload strategy:** don't retype an existing method/field in place if it still has real concrete callers. Add a new, separately-named interface-typed sibling instead (e.g. `GetHatchBrushResource` next to `GetHatchBrush`), and migrate real callers to it one at a time. This is what keeps a large migration incremental and revert-safe.
- **Bridge-at-the-sink:** when a concrete resource can't reasonably be retyped at its source (a public model property, a self-contained legacy geometry helper), wrap/reconstruct it into the interface type only at the point it's consumed, rather than forcing the source to change.
- **The "large atomic pass" trap:** shared concrete fields/arrays on a helper class often look individually convertible per-getter, but are all consumed together by one call downstream — converting one getter without the whole class, its producers, and its consumer in one pass just adds unreachable dead code. Identify these and do them as one deliberate pass, not sliced.
- **Verification gate:** every increment must have `dotnet build` (0 errors) + full test suite passing + zero baseline diffs before being considered done. For previously-uncovered render paths, generate a "before" baseline via `git stash push --keep-index` on just the engine files being converted, render through the pre-conversion code, pop the stash, and confirm byte-for-byte match.
- Don't force an abstraction whose semantics can't be verified end-to-end — document a genuine gap honestly rather than risk a subtly wrong port.

---

# Implementation Workflow

Implementation follows the current project goals:

1. Review relevant `docs/` files
2. Check `TODO.md` for current phase
3. Read existing code and architecture
4. Identify Windows dependencies
5. Refer to analysis documents (tasks/ folder)
6. Implement changes incrementally
7. Update `docs/` folder to reflect changes
8. Mark tasks complete in `TODO.md`
9. Commit work in relevant batches where code builds and tests pass

---

# Success Criteria

Success is measured by:

- Improved architecture
- Better separation of concerns
- Clear extension points
- Cross-platform readiness
- Comprehensive documentation
- Incremental change
- No unnecessary rewrites
- Backwards compatibility
- Long-term maintainability

---

# Key Documentation References

Before starting work, review these files in order:

1. **TODO.md** - Current tasks, progress, and documentation index
2. **docs/rendering-abstractions.md** - Rendering architecture (Excel/PDF renderer factory + Chart/Gauge Ports & Adapters)
3. **docs/decisions.md** / **docs/platform-support.md** - Why things are built this way, and current known gaps
4. **tasks/chart-gdi-type-abstraction.md** / **tasks/gauge-gdi-type-abstraction.md** - active Chart/Gauge migration progress
5. **tasks/pdf-render-callstack-analysis.md** - PDF migration roadmap (not started)

These contain:
- Complete call stack analysis
- Windows dependency inventory
- Implementation roadmaps with effort estimates
- Risk assessments
- Success criteria
- Specific file references and line numbers

---

# Final Instructions

- Act as a senior software architect and implementation lead
- Always refer to architectural documentation before proposing changes
- Keep `TODO.md` and `docs/` folder synchronized with current work
- Prefer incremental implementation over large rewrites
- Always explain trade-offs and risks
- Design for cross-platform maintainability
- Track progress continuously in `TODO.md`