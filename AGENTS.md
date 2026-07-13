# AGENTS.md

# ReportViewerCore Architecture & Engineering Guidelines

## Mission

Your primary objective is **to implement cross-platform Excel and PDF rendering for ReportViewerCore, enabling the reporting engine to run on both Windows and Linux platforms while preserving backwards compatibility**. The long-term vision is to transform ReportViewerCore from a Windows-centric rendering engine into a modular, extensible, cross-platform reporting platform that supports Windows, Linux, and macOS. Always refer to architectural documentation before proposing changes, and prioritize implementation efficiency while maintaining code quality.

---

# Current Project Goals

## Immediate Priorities

The current focus is on implementing cross-platform Excel and PDF rendering:

**Excel Rendering (Primary - Low Risk):**
- Phase 4: ImageFormatType enum implementation (2-3 days)
- Phase 5: IImageProvider abstraction for chart images (3-4 days)
- Phase 6: Testing and cross-platform validation (2-3 days)
- **Status:** 60% complete (core infrastructure + analysis done)

**PDF Rendering (Secondary - High Risk):**
- Phase 1: SkiaSharp graphics library migration (2-3 weeks)
- Phase 2: Font handling abstraction (3-5 days)
- Phase 3: Image encoding migration (2-3 days)
- Phase 4: Platform abstraction (1-2 days)
- **Status:** Analysis complete, implementation planning ready

**Shared Challenges (Both renderers):**
- Chart library evaluation and replacement (blocking both)
- See: `tasks/chart-image-abstraction-analysis.md` for details

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

**CRITICAL:** Do not make any commits to the repository unless explicitly instructed.

- All work should be left in the working copy for review
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

**Structure:**

```
docs/
├── ARCHITECTURE.md            - System architecture overview
├── RENDERING_PIPELINE.md      - Detailed rendering flow
├── WINDOWS_DEPENDENCIES.md    - Current Windows dependency inventory
├── CROSS_PLATFORM_STRATEGY.md - Plan for cross-platform support
├── EXCEL_RENDERING.md         - Excel-specific details
├── PDF_RENDERING.md           - PDF-specific details
├── CHART_RENDERING.md         - Chart/gauge rendering analysis
├── IMPLEMENTATION_PLAN.md     - Current implementation roadmap
└── API_REFERENCE.md           - Key interfaces and patterns
```

**Keep Up-To-Date:**
- After implementing major features, update relevant docs
- When discovering new Windows dependencies, update WINDOWS_DEPENDENCIES.md
- When changing architecture, update ARCHITECTURE.md
- Before code review, verify docs match implementation
- Link to specific files and line numbers in docs

**Usage:**
- Developers read `docs/ARCHITECTURE.md` first
- Agents reference docs before proposing changes
- Code reviews check docs for accuracy
- New team members use docs as onboarding

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
9. Leave work in working copy (no commits)

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

1. **ANALYSIS_DELIVERABLES.md** - Master index of all analysis
2. **ANALYSIS_STATUS.md** - Completion status and next steps
3. **TODO.md** - Current tasks and progress
4. **tasks/rendering-comparison-analysis.md** - Excel vs PDF scope
5. **tasks/excel-render-callstack-analysis.md** - Excel details
6. **tasks/pdf-render-callstack-analysis.md** - PDF details
7. **tasks/chart-image-abstraction-analysis.md** - Shared challenge

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
- Leave all code in working copy (no commits unless explicitly instructed)
- Track progress continuously in `TODO.md`