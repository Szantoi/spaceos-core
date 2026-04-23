---
id: MSG-ARCH-005-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: SpaceOS_Q3_Planning_Brief_v1.md Téma 1
created: 2026-04-20
---

# FreeTier Arch Spec v4 — Implementation Ready

**Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` ✅ APPROVED (pipeline teljesítve)

## Review pipeline összefoglalója

| Stage | Findings | Effort delta |
|-------|---------|-------------|
| v2 database review | 7 | 0 nap |
| v3 security review | 15 | +1.5 nap |
| v4 backend review | 27 | +1.0 nap |
| **Összesen** | **49** | **+2.5 nap** |

**Becsült implementációs effort:** ~17.5 nap (Section 10 napi ütemterv kész)
**Test target:** 175+ (7 track × unit + integration + E2E)

---

## Legfontosabb döntések (D-11..D-26)

| # | Döntés |
|---|--------|
| D-11-REV | Magic link: 32 byte CSPRNG + SHA-256 DB + sikeres verify → összes unused token invalidálva |
| D-13-REV | Share token: SHA-256 hash + TokenPrefix DB-ben (nem plaintext) |
| D-14-REV | Session: server-side nonce cookie (`ft_sess` HttpOnly/Secure/SameSite=Lax) |
| D-18 | Rate limit fail-closed `POST /nest` + `POST /auth/magic-link`; `SemaphoreSlim(10)` guard |
| D-19 | Share URL token-stripping redirect (Referer szivárgás ellen) |
| D-20 | `spaceos_freetier_share_reader` dedikált role (least-privilege) |
| D-21 | JSONB: string backing + `.HasColumnType("jsonb")` (nem OwnsOne().ToJson()) |
| D-22 | Domain event dispatch: `SaveChangesAsync` override → MediatR |
| D-23 | Max 20 workspace/user limit |
| D-24 | DEFERRABLE FK circular ref: raw SQL migration + suppressTransaction |
| D-25 | Redis: AbortOnConnectFail=false, Polly retry 3x exponential |
| D-26 | `IClock` abstraction minden handler + domain factory-ban |

---

## DDL javítások (specben már bent, implementációba kötelező)

1. `freetier."Users"` + `"AuthenticatedAt" TIMESTAMPTZ` mező (SEC-15)
2. `freetier."ShareTokens"` column rename: `"Token"` → `"TokenHash"` VARCHAR(64) + `"TokenPrefix"` VARCHAR(8)
3. `UpgradeRequests."ContactEmail"` nullable (GDPR cascade)
4. Circular FK `DEFERRABLE INITIALLY DEFERRED` raw SQL migration-ben

---

## Kiadandó feladatok

### FREETIER-000 — Új FREETIER terminál setup

**Nincs még:** `spaceos-freetier-api` repo nem létezik. Root teendője:
1. Új polyrepo létrehozása
2. Új FREETIER terminál Claude Code session indítása
3. Első inbox üzenet kiadása Section 10 ütemterv alapján

### Sorrend

```
FREETIER terminál → 17.5 nap implementáció
                        ↕ (párhuzamos)
INFRA terminál    → Redis deploy + nginx vhost (FT-2, FT-3)
                    freetier DB schema user provision
                    (spaceos_freetier_share_reader role)
```

---

## Kapcsolódó nyitott feladatok (ARCH-003 + ADR-038)

Az alábbi 3 feladat Root outbox-ban már van (MSG-ARCH-003 + MSG-ARCH-004), de kiosztva még nincs:

| Task | Assignee | Blokkoló |
|------|----------|---------|
| NESTING-001 — WastePieces[] NuGet 1.1.0 | CUTTING terminál | — |
| INVENTORY-015 — Batch offcut endpoint | CUTTING terminál (Inventory repo) | — |
| CUTTING-042 — Handler + snapshot + event | CUTTING terminál | NESTING-001 + INVENTORY-015 DONE |

Sorrend: `NESTING-001 ‖ INVENTORY-015 → CUTTING-042`

---

## Root következő lépései

1. **FreeTier arch spec jóváhagyás Gábornál** — mutasd meg a v4.4 spec linkjét
2. **Jóváhagyás után:** FREETIER polyrepo létrehozása + terminál setup
3. **Párhuzamosan:** NESTING-001 + INVENTORY-015 kiadása CUTTING terminálnak
4. **FREETIER terminál indítás:** Section 10 napi ütemterv (nap 1.0 task) mint első inbox üzenet
