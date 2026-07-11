# CLAUDE.md — src/types/

**Module:** Shared TypeScript type definitions
**Rule:** No logic here — pure types and interfaces only.

---

## What lives here

| File | Purpose |
|---|---|
| `llm.types.ts` | `ILlmProvider`, `Message`, `ToolSchema`, `LlmResponse`, `ChatRequest`, `ChatResponse` |
| `kernel.types.ts` | TypeScript mirrors of every C# DTO — kept in sync with Kernel API |

## Rules

- `llm.types.ts` owns the `ILlmProvider` interface — this is the LLM seam
- `kernel.types.ts` mirrors **must** match the C# DTOs exactly (field names, types, optionality)
- When a C# DTO changes → update the mirror here first, then update all consumers
- No `class` — interfaces and `type` only
- No imports from other `src/` modules — types are a dependency of everything, they depend on nothing

## Sync checklist (when C# Kernel changes)

- [ ] New property in C# aggregate → add to the corresponding DTO interface here
- [ ] Renamed field → rename here, TypeScript will surface all usages
- [ ] New `FsmState` value → add to the `FsmState` union type
- [ ] New `TradeType` → add to `TradeType` union
