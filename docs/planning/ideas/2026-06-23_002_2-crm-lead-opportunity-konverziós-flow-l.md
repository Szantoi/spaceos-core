---
id: IDEA-20260623-002
title: "2. CRM Lead→Opportunity Konverziós Flow Lépéskalauz"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 2. CRM Lead→Opportunity Konverziós Flow Lépéskalauz

**Komponens:** `assembly.jsx` → CRM világ / Lead-to-Opp transition  
**Típus:** ui-component + state-management  
**Prioritás:** high

Egy wizard-jellegű modal (3-4 lépés), amely a LEAD `uj → kapcsolat → minősítés` FSM végén megjelenik (státusz: `minositett`). A felhasználó egy gombra katt → modal nyílik → Lead alapadatai pre-filled → "Opportunity-ként indítsuk?" kérdés + CRM-alosztályozás (Új/Bővítés/Helyettesítés) → `convertLeadToOpp` action → CUSTOMERS-be új ügyfél + OPPORTUNITY létrehozása `nyitott` státusszal. SLA-dátum auto-számolt.

**Kapcsolódó fájlok:**
- `assembly.jsx`
- `app-store.jsx` (crm.leads + crm.opportunities state)
- `catalog-world-view.jsx` (CRM-nézet frissítés)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
