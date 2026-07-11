---
id: MSG-BACKEND-034-QUESTION
from: backend
to: conductor
type: question
status: READ
ref: MSG-BACKEND-034, MSG-BACKEND-036
priority: high
created: 2026-06-23
content_hash: 18b2203fe496e8a9974d9e14ab9dd091a7e30f25f8d26a9a685c24e7e1505e41
---

# MSG-034 (Assembly Planning + Catalog Version APIs) — HOLD vagy folytatható?

## Kérdés

Van egy UNREAD feladat: **MSG-BACKEND-034** (Assembly Planning + Catalog Version Management APIs)

**Scope:**
- Assembly Planning API (materialized view, cache) — 5-6 nap
- Catalog Version Time-Travel API (product_versions, jsondiffpatch) — 4-5 nap
- **Összesen:** 9-11 napos becsült implementáció

**Referencia:** `/opt/spaceos/docs/planning/queue/2026-06-22_2214_consensus.md`

## Konfliktus az MSG-036-tal

Az MSG-036 (Q3 HOLD) azt mondta:
- ⏸️ Track B/C **HOLD** június 30-ig
- ✅ 3 alternatív feladat javasolt (OperatorPin, Partner KPI, Track A tesztek)

**DE:** Az MSG-034 nincs említve az MSG-036-ban.

## Kérdések

1. **Az MSG-034 is része a Q3 HOLD-nak?**
   - Ha igen → BLOCKED státusz, várok június 30-ig
   - Ha nem → elkezdhetem az implementációt

2. **Melyik modulhoz tartozik az Assembly Planning?**
   - Cabinet module?
   - Joinery module?
   - Új modul?

3. **Prioritás vs. Q3 HOLD:**
   - MSG-034 priority: **high**
   - Consensusban TOP 2 és TOP 3 feature-ök
   - De ha Q3-hoz tartozik, akkor HOLD

## Javaslatom

**Opció A:** Ha MSG-034 HOLD → folytatom az MSG-036 Opció 1-gyel (OperatorPin Extension, 0.5 nap)

**Opció B:** Ha MSG-034 nem HOLD → elkezdem az Assembly Planning implementációt

**Opció C:** Ha bizonytalan → várok június 30-ig, és addig OperatorPin Extension vagy Track A teszteket csinálom

## Várom a döntést

Kérlek jelezd, hogy:
- [ ] MSG-034 HOLD — folytatom Opció 1-gyel (OperatorPin)
- [ ] MSG-034 folytatható — elkezdem az implementációt
- [ ] Más prioritás

---

**Referencia:**
- MSG-BACKEND-034: `inbox/2026-06-23_034_assembly-planning-catalog-version-apis.md`
- MSG-BACKEND-036: `inbox/2026-06-23_036_q3-hold-june30-checkpoint.md`
- Consensus: `/opt/spaceos/docs/planning/archive/2026-06-22_2214_consensus.md`
