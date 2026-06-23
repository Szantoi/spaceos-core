---
id: IDEA-20260622-004
title: "1. CRM Lead-Pipeline Kanban View (Drag-&-Drop státuszváltás)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. CRM Lead-Pipeline Kanban View (Drag-&-Drop státuszváltás)

**Komponens:** `page-crm.jsx` / új `CrmKanban.jsx`  
**Típus:** ui-component  
**Prioritás:** high

A meglévő CRM lead-FSM (`uj → kapcsolat → minosites → nurturing → konvertalva`) visual kanban táblára az assembly/nesting view analógiájára. Drag-and-drop az `updateLeadStatus(leadId, newStatus)` action-t triggerelné, live SLA-számlálók (napok a státuszban), és csapatsűrítés (lead hozzárendelés avatar-ral). Tailwind card-layout, 5 oszlop, `localStorage` commit után optimista UI.

**Kapcsolódó fájlok:**
- `page-crm.jsx` (parent)
- `data-crm.js` (lead FSM & store)
- `app-store.jsx` (updateLeadStatus action)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
