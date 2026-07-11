---
id: epic_review_epic_02
title: "Epic Review Report: EPIC-02"
type: report
scope: project
last_updated: 2026-02-26
---

# **📝 Epic Review Report: [EPIC-02 - MCP Client Cache Limitation Testing]**

**Cél:** Ez a dokumentum az Epic lezárásának technikai pillére. Feladata, hogy a fejlesztés során szerzett tapasztalatokat aggregálja, rögzítse a technikai adósságot, és javaslatokat tegyen a globális tudásbázis (Global Skills/Standards) fejlesztésére.

## **1\. Általános adatok**

* **Tech Lead:** JoineryTech.Flow Core System
* **Dátum:** 2026-02-26
* **Érintett komponensek:** MCP E2E Testing, Session Caching, ADR Documentation

## **2\. Technikai Összefoglaló**

Sikeresen vizsgáltuk az MCP kliensek munkamenet szintű cache-elési limitációját. A `mcp-session-roleswitch-test.ts` E2E teszt reprodukálta a jelenséget: menet közbeni szerepkörváltás (Role Switch) esetén a kliensek (mint pl. a Cursor vagy a Claude Desktop) jellemzően nem invokálják újra automatikusan az eszközkészlet-frissítési protokollokat. A kutatás eredményét és a "Session-Init Only RBAC" mitgációs stratégiát az `ADR-009` dokumentumban rögzítettük.

## **3\. Tervtől való eltérések (Deviations)**

A terv szerint a mitigation kiértékelést elvégeztük, és kiderült, hogy natív, azonnali kliens oldali refresh jelenleg MCP limitáció miatt nem garantálható minden környezetben. Ez nem volt eltérés a feladat szintjén, maga a "nyomozás" volt a Task.

## **4\. Technikai Adósság (Tech Debt Registry)**

* [x] **Tétel**: A későbbiekben érdemes figyelni az MCP specifikáció fejlődését (pl. hivatalos `tools/list` invalidálás). | **Kockázat**: Közepes | **Javasolt javítás**: Monitorozni kell az MCP TypeScript SDK frissítéseit a repóban.

## **5\. Kalibráció és Tudásmenedzsment (Lessons Learned)**

### **🛠️ Globális Skill/Szabvány frissítések**

* Nincs azonnali teendő, az ADR rögzítette a döntést, amely mostantól a Core architekturális iránymutatása lett.

### **📄 Sablon és Workflow javítások**

* Nincs megjegyzés.

## **6\. QA és Teljesítmény tapasztalatok**

A teszt megbízhatóan reprodukálja a kliens állapot-tartásából (stateful behavior) adódó inkonzisztenciát.

## **7\. Readiness Check (Architect Sign-off előtt)**

* [x] Minden Task státusza "Done".
* [x] Minden Task rendelkezik Implementation Report-tal és QA Sign-off-fal. (Részben az Acceptance Criteria Checklist helyettesíti a projekt szintjén)
* [x] A kritikus technikai adósságok rögzítve lettek a backlogban.
* [x] A globális tudásbázis frissítési javaslatok elkészültek. (Esetünkben ADR formájában rögzítve).

**Tech Lead értékelése:** Sikeres

---

## **8. 🔄 Calibration Instructions (For Knowledge Steward)**

Nincsenek specifikus kalibrációs instrukciók ehhez az Epic-hez az ADR-en túl.
