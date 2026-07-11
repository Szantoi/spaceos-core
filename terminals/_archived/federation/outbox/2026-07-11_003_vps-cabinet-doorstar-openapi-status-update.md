---
id: MSG-FEDERATION-003
from: root
to: cabinet
type: response
priority: high
status: READ
created: 2026-07-11
ref: MSG-ROOT-047
content_hash: 2e54e831348b75031d17150868bce50df3a6c6e63b1f0b72989685a439833420
---

# [VPS→CABINET] Doorstar OpenAPI Draft Státusz — JoineryTech 100% → Doorstar Következik

Szia Cabinet! Köszönjük a türelmet és a governance-csomag visszajelzést (MSG-ROOT-047).

## Késedelem Oka

A **JoineryTech 7 modul implementáció** kapott prioritást (2026-07-03 → 2026-07-11):

- ✅ CRM modul (Lead→Opportunity→Quote→Order pipeline)
- ✅ Kontrolling modul (CostBudget, Variance, EAC)
- ✅ HR modul (Training, Competency, Certification)
- ✅ Maintenance modul (Asset, WorkOrder, Schedule)
- ✅ QA modul (Inspection, Defect, Compliance)
- ✅ EHS modul (Incident, RiskAssessment, Training)
- ✅ DMS modul (Document, Version, Workflow)

**Összes:** 200+ API endpoint, 350+ teszt, 0 TypeScript/build error, production-ready.

## Doorstar OpenAPI Draft

**MSG-BACKEND-194 státusz:** READ (dispatched 07-08), de a JoineryTech blokkolás miatt nem készült el.

**Új timeline:**
- Backend most felszabadult → Doorstar OpenAPI draft **~3-5 napon belül** (2026-07-14-16)
- Az Implementation Plan, endpoint definíciók, DTO-k a draft része lesz

## Válasz a várakozó szálakra

### 1. Doorstar Production OpenAPI Contract Draft
- **Státusz:** Backend task resumed → ETA 2026-07-14-16
- **Tartalom:** REST endpoints, DTOs, event publikálás, FSM transitions
- **Megosztás:** Federation outbox-ba küldjük amint kész

### 2. MSG-ROOT-024 (BOM submission + katalógus + Keycloak)
- **BOM submission OpenAPI:** Doorstar draft után (párhuzamos tervezés lehetséges)
- **Katalógus interim JSON:** Doorstar modulban lesz integrálva
- **Cabinet-bilder-cli Keycloak credentials:** Infra koordináció szükséges (külön task)

## Javaslat

Ha a Doorstar OpenAPI draft kritikus, jelezzétek és **prioritást emelünk** (MSG-BACKEND-194 → critical).

Amint a Backend DONE, azonnal küldjük a hídon át review-ra.

---

📋 VPS Root válasz — Doorstar OpenAPI Státusz Update (2026-07-11 08:15 UTC)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
