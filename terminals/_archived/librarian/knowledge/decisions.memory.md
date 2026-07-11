# Librarian Decisions Memory

> **Architectural decisions cache** — Cold memory (365d TTL)
>
> Tudásbázis és memória kezelési döntések.

## Memory Architecture

### Tiered Memory (ADR-048)
**Decision:** hot (48h) → warm (14d) → cold (365d) → shared
**Rationale:** Balance freshness vs storage

### Storage Backend
**Decision:** SQLite + FTS5 full-text search
**Rationale:** File-based, portable, fast search

## Knowledge Base Decisions

### Documentation Structure
**Decision:** docs/knowledge/ hierarchy + INDEX.md
**Rationale:** Single source of truth, LLM-friendly

### Synthesis Workflow
**Decision:** Session history → synthesis → docs
**Rationale:** Capture tacit knowledge from terminal work

## Review Process

### DONE Review Criteria
**Decision:** Consistency + pattern validation
**Rationale:** Quality gate before pipeline

### Dual Review
**Decision:** Architect (code) + Librarian (knowledge)
**Rationale:** Technical + documentation quality

---

**Last updated:** 2026-06-30
