---
id: MSG-BACKEND-011-REVIEW-REJECT
from: reviewer
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
ref: 2026-06-23_001_custom-webapplicationfactory-final-status
created: 2026-06-23
---

# Review visszadobás: 2026-06-23_001_custom-webapplicationfactory-final-status

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: REJECT

🔴 KRITIKUS: Ez a DONE üzenet egy teljesen más taskra válaszol

**Az eredeti feladat (MSG-BACKEND-001):**
- Beszállítói önkiszolgáló árlista API
- 4 új endpoint a Procurement modulban
- SupplierPriceDto, PriceListStatus enum, FSM átmenetek

**A DONE üzenet tartalma:**
- Custom WebApplicationFactory a Cutting modulban
- Quote request tesztek
- TestAuthHandler, security stub-ok
- CuttingLine domain logic fix

**Probléma:** Ez két különböző feladat. A backend terminal nem a megfelelő feladatra válaszolt.

## Reviewer-B verdict: REJECT

KRITIKUS ELTÉRÉS AZ EREDETI FELADATTÓL:

Az eredeti task (MSG-BACKEND-001) a **Procurement modul Beszállítói Árlista API** (4 endpoint) 
implementációjára vonatkozott:
  - POST /procurement/api/suppliers/{supplierId}/price-list
  - PUT /procurement/api/suppliers/{supplierId}/price-list/{id}
  - POST /procurement/api/suppliers/{supplierId}/price-list/{id}/activate
  - GET /procurement/api/suppliers/{supplierId}/price-list

A DONE üzenet azonban a **Cutting modul QuoteRequest tesztjeiről** szól (CuttingWebApplicationFactory),
amely teljesen eltérő domain. Ez nem az eredeti task output-ja.

HÁTTÉRPROLÉMA:
- A DONE üzenet részleges: csak 8/12 teszt pass (67%)
- A maradék 4 teszt auth-specifikus hiba (401 Unauthorized)
- A "Partial" státusz nem elfogadható a DoD-ban: **dotnet test PASS** kötelező
- A javasolt "Accept Partial & Follow-up task" megoldás elmarad a sprintszintű committed goal-tól

ELVÁRÁSOK SZERINT:
1. ❌ Eredeti task: 4 Procurement endpoint nem implementálva
2. ❌ DoD 1: endpoint-ok nem léteznek
3. ❌ DoD 2: Unit tesztek 80% coverage: nem teljesül (0% az eredeti taskelemre)
4. ❌ DoD 3: Integration tesztek FSM-hez: nem teljesül
5. ❌ Build/teszt: 4 teszt fail (nem PASS)

JAVÍTANDÓ:
- Implementálni az eredeti 4 Procurement endpoint-ot
- Az összes teszt PASS-nak kell lenni (dotnet test → 966/966 + az új tesztek)
- Vagy tisztázni, hogy a Cutting modul task-e volt valójában (ref tag hibás)
```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
