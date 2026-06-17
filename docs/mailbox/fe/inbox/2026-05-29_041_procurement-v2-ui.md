---
id: MSG-FE-041
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-PROCUREMENT-015-DONE
created: 2026-05-29
---

# FE-041 — Procurement v2 UI

## Háttér

A Procurement v2 backend DEPLOYED (`c5f1292` · 5006 · 136/136 teszt). A design teljes:
`docs/tasks/new/FE_Design_Requirements_2026.md` 1a–1d szekció.

## Feladat

Négy új terület implementálása a `ProcurementPage`-en belül, vagy önálló sub-oldalként.

### 1a. Beszerzési igénylés (PurchaseRequisition)

**API:**
- `GET /procurement/api/v2/requisitions` — lista
- `POST /procurement/api/v2/requisitions` — létrehozás
- `GET /procurement/api/v2/requisitions/{id}` — részletek
- `POST /procurement/api/v2/requisitions/{id}/approve` — jóváhagyás
- `POST /procurement/api/v2/requisitions/{id}/reject` — visszautasítás

**UI elemek:**
- Igénylés lista: táblázat (igénylésszám, anyagkód, mennyiség, státusz pill, benyújtó, dátum)
- Létrehozás: Drawer (anyagkód, mennyiség, egység, preferált szállító opcionális, megjegyzés)
- Részletek: SlideOver (adatlap + FSM akciók: Approve / Reject — csak `procurement.approver` role)
- SoD jelzés: ha jóváhagyó = igénylő → figyelmeztető badge, akció tiltva

**FSM:** `Draft → Approved → ConvertedToPO / Rejected`

---

### 1b. Szállítói számla (SupplierInvoice)

**API:**
- `GET /procurement/api/v2/invoices` — lista
- `POST /procurement/api/v2/invoices` — rögzítés
- `GET /procurement/api/v2/invoices/{id}` — részletek
- `POST /procurement/api/v2/invoices/{id}/approve` — jóváhagyás
- `POST /procurement/api/v2/invoices/{id}/approve-with-variance` — emelt jogkör
- `POST /procurement/api/v2/invoices/{id}/dispute` — vita

**UI elemek:**
- Számla lista: számlaszám, szállító, összeg, státusz, dátum
- Rögzítés: Drawer/form (szállító, PO hivatkozás, tételsorok anyag+mennyiség+egységár+ÁFA, összegek auto-számítása)
- Részletek SlideOver: fejléc + tételsorok + Three-Way Match eredmény szekció
- ApproveWithVariance: emelt jogkör + összeg-küszöb jelzés
- SoD: variance jóváhagyó = rögzítő → tiltva

**FSM:** `Received → Matched / Exception → Approved / Disputed`

---

### 1c. Three-Way Match nézet (SupplierInvoice SlideOver-ben)

- PO qty vs. Delivered qty vs. Invoiced qty — soronkénti összehasonlítás
- Tolerancia jelzés: Zöld (OK) / Sárga (tolerancián belül) / Piros (Exception) pill
- Exception részletek: variancia%, mi okozta
- Akciók: Approve / Dispute / ApproveWithVariance — jogkör alapján

---

### 1d. Árlista (PriceList)

**API:**
- `GET /procurement/api/v2/pricelists` — lista
- `POST /procurement/api/v2/pricelists` — új árlista
- `POST /procurement/api/v2/pricelists/{id}/activate` — aktiválás
- Tételek: `POST /procurement/api/v2/pricelists/{id}/entries` — tömeges tételek

**UI elemek:**
- Árlista lista: szállítónkénti táblázat, anyag, egységár, deviza, érvényesség, státusz
- Új árlista: Drawer (szállító, tételek + érvényességi időszak)
- FSM akciók: `Draft → Active → Expired`
- Best-price kiemelés: adott anyagnál melyik szállítónál a legolcsóbb — vizuális jelzés

---

## Mock fallback

Minden SlideOver / Drawer legyen mock adattal azonnali megjelenítéssel, API válasz esetén frissüljön.

## Implementáció

- **Skill:** `/spaceos-terminal` segítségével
- **Build gate:** `pnpm build` — 0 TS hiba
- **Test gate:** `pnpm test` — minden zöld, min. +30 új teszt (az 4 területhez)
- **Agent használat:** engedélyezett sub-agent-ek párhuzamos munkához

## DONE kritériumok

- [ ] 4 terület implementálva (1a–1d)
- [ ] Mock fallback minden komponensben
- [ ] `pnpm build` → 0 hiba
- [ ] `pnpm test` → minden zöld (+30 legalább)
- [ ] Outbox DONE üzenet commit hash-sel
