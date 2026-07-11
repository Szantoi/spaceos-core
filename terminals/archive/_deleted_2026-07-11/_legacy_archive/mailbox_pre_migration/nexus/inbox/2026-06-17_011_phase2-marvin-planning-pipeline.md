---
id: MSG-NEXUS-011
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-17
---

# NEXUS-011 — Phase 2: Marvin Planning Pipeline

## Context

Phase 1 COMPLETE ✅ (Knowledge Service + ChromaDB + Voyage AI).
Most jön a Phase 2: Marvin Planning Pipeline integráció.

## Feladatok

A ROADMAP (`docs/agent-infrastructure/ROADMAP.md`) Phase 2 alapján:

### 1. Marvin telepítés
```bash
pip install marvin
# vagy Poetry/uv ha az a projekt standard
```

### 2. Marvin konfiguráció
- OpenAI API key beállítás (OPENAI_API_KEY env)
- Vagy Anthropic backend ha támogatott

### 3. Planning pipeline integráció
- `plan-scan.sh` → Marvin AI functions
- Strukturált ötlet generálás
- Scoring/prioritizálás

### 4. Tesztelés
- Marvin működik-e a meglévő knowledge base-zel
- Planning flow E2E teszt

## Definition of Done

- [ ] Marvin telepítve és működik
- [ ] Legalább 1 planning function implementálva
- [ ] Dokumentáció frissítve (ROADMAP.md checkbox-ok)
- [ ] DONE outbox a részletekkel

## Reference

- ROADMAP: `docs/agent-infrastructure/ROADMAP.md` (Phase 2 szekció)
- Marvin docs: https://www.askmarvin.ai/

---

**ROOT Approval:** ✅ Go ahead
**Timeline:** Amíg kész nem lesz
