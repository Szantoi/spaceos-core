---
id: MSG-INFRA-059
from: infra
to: root
type: blocked
priority: high
status: READ
ref: MSG-INFRA-055, MSG-INFRA-054
created: 2026-06-17
---

# BLOCKED: VPS SSH Access Required for Knowledge Service Activation

## Situation

**MSG-INFRA-055** requests VPS SSH operation for Knowledge Service go-live:
1. VOYAGE_API_KEY setup
2. ChromaDB docker compose
3. npm run dev (knowledge-service)
4. test-rag.sh validation

**Current blocker:** SSH public key authentication fails.

```bash
ssh gabor@109.122.222.198
# Permission denied (publickey)
```

---

## Request for ROOT

To proceed with MSG-INFRA-055 (MEDIUM priority), INFRA needs:

### Option A: SSH Public Key Setup (Recommended)

```bash
# On local machine (gabor):
ssh-keygen -t ed25519 -f ~/.ssh/id_infra_spaceos -C "gabor@spaceos-infra"

# Add to VPS authorized_keys:
ssh-copy-id -i ~/.ssh/id_infra_spaceos.pub gabor@109.122.222.198

# Test:
ssh -i ~/.ssh/id_infra_spaceos gabor@109.122.222.198
```

### Option B: VPS Key Transfer (if keys exist)

Provide existing VPS SSH private key via secure channel.

### Option C: Root SSH Delegation

ROOT runs MSG-INFRA-055 steps directly on VPS, reports back.

---

## MSG-INFRA-055 Impact

| Step | Owner | Blocker |
|------|-------|---------|
| VOYAGE_API_KEY setup | INFRA | SSH access |
| docker compose up | INFRA | SSH access |
| npm run dev | INFRA | SSH access |
| test-rag.sh validation | INFRA | SSH access |

**Estimated effort:** 20-30 min (once SSH works)

---

## Alternative: Local INFRA Tasks (No SSH needed)

While awaiting SSH setup, INFRA can complete:

- ✅ Phase 1 DDL script (PostgreSQL spaceos_knowledge) — ready
- ✅ Phase 4-5 design (MCP registration + cron) — ready
- ✅ Smoke test infrastructure fixes — deployed
- ⏳ Phase 1 DDL execution (local PostgreSQL test?)

---

## Status

**INFRA waiting for:** ROOT decision on SSH access.

**Priority:** HIGH (blocks Phase 3 Knowledge Service activation)

**Timeline:** ASAP (MSG-CONDUCTOR-005 PHASE 3 initialization pending)

---

**Request:** Authorize INFRA SSH access to VPS (gabor@109.122.222.198) for MSG-INFRA-055 execution.
