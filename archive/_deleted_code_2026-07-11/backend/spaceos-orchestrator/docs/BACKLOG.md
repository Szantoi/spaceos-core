# SpaceOS.Orchestrator — Master Backlog

**Project:** `spaceos-orchestrator` · Node.js 20 LTS · TypeScript · Express  
**Build:** 0 errors · 0 warnings · **Tests:** 50 passing (8 test files)
**Last updated:** 2026-03-28 (E11–E17 all CLOSED_DONE)

---

## What Is This Layer?

The Orchestrator is **Layer 3** of the SpaceOS architecture (see SpaceOS Master Manifesto).  
It sits between the React frontend and the C# Kernel:

```
React DesignPortal
    ↓  /bff/chat   (NLP → agentic loop)
    ↓  /bff/api/*  (passthrough proxy)
SpaceOS Orchestrator  ← THIS PROJECT
    ↓  POST /api/*  (JWT forwarded)
SpaceOS.Kernel.Api (C# .NET 8)
    ↕
LLM Provider (Anthropic / OpenAI / Mock)
```

**The Kernel never knows about the LLM. The LLM never knows about the Kernel.**  
The Orchestrator is the only component that knows both.

---

## Epic Status Board

| Epic | Title | Priority | Status | Owner |
|------|-------|----------|--------|-------|
| [E11](epics/E11_ORCH_BOOTSTRAP/EPIC.md) | Project Bootstrap & Health | 🔴 P1 | `CLOSED_DONE` | — |
| [E12](epics/E12_ORCH_LLM_ABSTRACTION/EPIC.md) | LLM Provider Abstraction | 🔴 P1 | `CLOSED_DONE` | — |
| [E13](epics/E13_ORCH_TOOL_REGISTRY/EPIC.md) | Tool Registry & Kernel Action Dispatch | 🔴 P1 | `CLOSED_DONE` | — |
| [E14](epics/E14_ORCH_INTERPRETER/EPIC.md) | Interpreter Service (Agentic Loop) | 🔴 P1 | `CLOSED_DONE` | — |
| [E15](epics/E15_ORCH_PROXY/EPIC.md) | Kernel Proxy & Auth Middleware | 🟡 P2 | `CLOSED_DONE` | — |
| [E16](epics/E16_ORCH_TESTS/EPIC.md) | Unit & Integration Tests | 🟡 P2 | `CLOSED_DONE` | — |
| [E17](epics/E17_ORCH_DEPLOY/EPIC.md) | VPS Deployment (pm2 + nginx) | 🟢 P3 | `CLOSED_DONE` | — |

---

## FSM States

```
BACKLOG_READY → IN_DEV → CODE_REVIEW → REVIEW_FAILED → IN_DEV
                                     → CLOSED_DONE
```

---

## Dependency Graph

```
E11 (Bootstrap)
  └─ must complete before everything else

E12 (LLM Abstraction)
  └─ depends on E11

E13 (Tool Registry)
  └─ depends on E11
  └─ independent of E12 (tools and LLM are separate concerns)

E14 (Interpreter / Agentic Loop)
  └─ depends on E12 + E13 (needs both LLM + tools)

E15 (Proxy + Auth)
  └─ depends on E11
  └─ independent of E12–E14

E16 (Tests)
  └─ depends on E12 + E13 + E14 + E15

E17 (Deploy)
  └─ depends on all (deploy only when green)
```

**Recommended execution order:** E11 → E12 + E13 (parallel) → E14 → E15 → E16 → E17

---

## Definition of Done (Global)

- [x] All Acceptance Criteria in EPIC.md checked
- [x] `npm run build` → 0 TypeScript errors
- [x] `npm test` → 50/50 passed (8 test files)
- [x] REVIEW_REPORT.md generated — no open violations
- [x] CLAUDE_ORCH.md layer rules respected
- [x] No TODO/FIXME in committed code
- [x] `.env.example` kept in sync with all env vars used
