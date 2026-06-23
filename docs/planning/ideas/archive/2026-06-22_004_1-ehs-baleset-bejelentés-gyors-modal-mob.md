---
id: IDEA-20260622-004
title: "1. **EHS Baleset-Bejelentés Gyors Modal (Mobile-First)**"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. **EHS Baleset-Bejelentés Gyors Modal (Mobile-First)**

**Komponens:** `page-ehs.jsx` → új `EhsIncidentQuickReport` modal  
**Típus:** ui-component + state-management  
**Prioritás:** high

A munkavédelem világ (4.9) mobil gyorsjelentésére van szükség: 3 lépéses modal (ikon-válogatás: típus/súlyosság, helyszín, fényképet) lokális камеrа-hozzáféréssel, majd `localStorage`-ba mentés + onboarding-alert az EHS managernek. Az app-store-ba új action: `reportIncidentFromMobile()`. Valós eset: üzemi baleset bejelentése 30 másodpercen belül.

**Kapcsolódó fájlok:**
- `app-store.jsx` → `ehs.incidents[]`, új action
- `page-ehs.jsx` → modal trigger gomb
- `data-ehs.js` → initial state

---

---
*Automatikusan generálva a JoineryTech prototípusból*
