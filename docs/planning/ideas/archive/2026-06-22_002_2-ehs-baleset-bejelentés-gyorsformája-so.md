---
id: IDEA-20260622-002
title: "2. EHS Baleset-Bejelentés Gyorsformája (sor a Mobil Navigation-ben)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. EHS Baleset-Bejelentés Gyorsformája (sor a Mobil Navigation-ben)

**Komponens:** `assembly.jsx` (vagy új `ehs-quick-report.jsx`)
**Típus:** ui-component + state-management
**Prioritás:** medium

A munkavédelem világához egy **kártya vagy modal-gomb** közvetlenül az oldalsáv/nav-ból (piros EHS akcent színnel), amely felnyit egy **3-lépéses mini-form**-ot: 
1. „Mi történt?" (kvázibaleset/baleset választó)
2. „Hol/kitől?" (munkahely + érintett személy)
3. „Pillanatfotó-megjegyzés" (szabad szöveg + képfeltöltés mock)

A bejelentés rögtön `bejelentve` státuszba kerül, SMS-notif szimulációval. Gyorsítja az azonnali bejelentés utat az üzemparancsnokságig.

**Kapcsolódó fájlok:**
- `page-ehs.jsx` (vagy szétválaszott UX)
- `data-ehs.js` (FSM: bejelentve)
- `app-store.jsx` (notif mock)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
