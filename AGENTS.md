# AGENTS.md

# ReportViewerCore Architecture & Engineering Guidelines

## Mission

Your primary objective is **to implement cross-platform Excel and PDF rendering for ReportViewerCore, enabling the reporting engine to run on both Windows and Linux platforms while preserving backwards compatibility**. The long-term vision is to transform ReportViewerCore from a Windows-centric rendering engine into a modular, extensible, cross-platform reporting platform that supports Windows, Linux, and macOS. Always refer to architectural documentation before proposing changes, and prioritize implementation efficiency while maintaining code quality.

---

# Coding Guidelines & Conventions

Engineering principles, cross-platform/adapter design conventions, testing philosophy, design-pattern preferences, documentation-format standards (ADRs/tech specs/PRDs), quality attributes, risk analysis, and the lessons learned during the Chart/Gauge GDI+ migration all live in **`docs/coding-standards.md`** — read it before proposing or reviewing code changes.

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
├── coding-standards.md         - Coding guidelines, design conventions, and migration lessons learned
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
2. **docs/coding-standards.md** - Coding guidelines, design conventions, and migration lessons learned
3. **docs/rendering-abstractions.md** - Rendering architecture (Excel/PDF renderer factory + Chart/Gauge Ports & Adapters)
4. **docs/decisions.md** / **docs/platform-support.md** - Why things are built this way, and current known gaps
5. **tasks/chart-gdi-type-abstraction.md** / **tasks/gauge-gdi-type-abstraction.md** - active Chart/Gauge migration progress
6. **tasks/pdf-text-shaping-abstraction.md** - active PDF text-shaping work and remaining gaps (`tasks/pdf-render-callstack-analysis.md` is the historical call-chain trace that preceded it)

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