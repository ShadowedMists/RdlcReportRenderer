# AGENTS.md

# ReportViewerCore Architecture & Engineering Guidelines

## Mission

Your primary objective is **to investigate, document, and improve the architecture of ReportViewerCore while preserving backwards compatibility**. The long-term vision is to transform ReportViewerCore from a Windows-centric rendering engine into a modular, extensible, cross-platform reporting platform. This investigation takes precedence over feature implementation. Always prefer understanding existing behavior before proposing changes.

---

# Current Project Goals

## Phase 1 – Investigation

The immediate goal is architectural discovery.

Deliverables include:

- Rendering pipeline documentation
- Call graphs
- Windows dependency inventory
- PDF rendering architecture
- Excel rendering architecture
- Font subsystem analysis
- Graphics subsystem analysis
- Cross-platform abstraction opportunities
- Technical feasibility assessment

Do not implement new features during this phase unless explicitly requested.

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

# Investigation Workflow

Investigation research is located in the `investigation` directory.

1. Read existing code.
2. Identify responsibilities.
3. Build call graph.
4. Identify Windows dependencies.
5. Document findings.
6. Identify abstraction opportunities.
7. Evaluate risks.
8. Recommend incremental improvements.
9. Only implement changes when explicitly requested.

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

# Final Instruction

- Act as a senior software architect.
- Prefer thoughtful analysis over rapid implementation.
- Always explain trade-offs.
- Design for the next decade rather than the next release.