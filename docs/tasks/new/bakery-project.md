---
id: BAKERY-V1
title: Pékség vertikál — első üzleti vonal
status: new
priority: medium
assignee: root (tervez), FE + Backend terminálok
epic: bakery
created: 2026-06-15
updated: 2026-06-15
docs:
  - docs/tasks/new/joinerytech/CLAUDE.md (ÉSZAKI CSILLAG szekció — domén-független mag)
  - docs/vision/SpaceOS_Vision_Master.md
---

# Pékség vertikál — tervdokumentum

## Üzleti cél

A SpaceOS platform domén-független mag rétegének első, asztalostól különböző alkalmazása. Egy pékség (receptúra-alapú, sarzsgyártás, HACCP, napi sütés-ütemezés, bolti értékesítés) üzemeltetésére ugyanazokat a magmodulokat alkalmazza — más **domén-adapterrel**.

## Az ÉSZAKI CSILLAG architektúra alkalmazása

A `docs/tasks/new/joinerytech/CLAUDE.md` „ÉSZAKI CSILLAG" szekciójában meghatározott 3 réteg:

### 1. Domén-független MAG (ugyanaz, mint JoineryTechnél)

| Magmodul | Pékség-felhasználás |
|---|---|
| CRM / lead pipeline | Nagykereskedelmi partnerek, viszonteladók |
| Ajánlat → Rendelés → Számla pénzügyi gerinc | Napi rendelések, előfizetések, nagyker |
| Katalógus / cikkszám-törzs | Termékek (kenyér, süti, különlegesség) |
| Raktár (lot/zóna/mozgás) | Alapanyag-raktár (liszt, cukor, vajak) + Kész termék |
| Beszerzés (RFQ→PO→bevét) | Alapanyag-rendelés szállítóktól |
| HR / jelenlét | Pékek, eladók műszak-tervezés |
| Kontrolling (EAC, fedezet) | Receptúra-önköltség, fedezet-számítás |
| EHS | HACCP + munkavédelmi előírások |
| Dokumentumtár (DMS) | Receptúrák, lejárati lapok, HACCP naplók |
| Feladat-aggregátor | Napi sütési feladatok összesítő |
| Vezetői BI | Napi forgalom, termék-fedezet, legjobb termékek |
| AI munkaterület | Receptúra-optimalizálás, élelmiszer-trendek |

### 2. Domén-ADAPTER — Pékség specifikus

Új `bakery_domain.md` dokumentum (mint a `woodwork_domain.md` az asztaloshoz):

| Adapterelem | Leírás |
|---|---|
| **Receptúra (BOM)** | Alapanyag-komponensek + arányok (kg-ban) per-termék |
| **Sarzs-gyártás** | Egy sarzs = N darab termék 1 keverésből |
| **Lejárat / HACCP** | Lot-szintű lejárat, FIFO kivét, kritikus kontroll pontok |
| **Napi sütés-ütemezés** | Kemence kapacitás-tervező (sarzsonként hőfok + idő) |
| **Receptúra-skálázás** | Ha kell 200 db, mennyi liszt/cukor? (automatikus szorzás) |
| **Kovászos folyamat** | Előtészta + érési idő + célhőmérséklet (multi-lépéses) |
| **Eltarthatóság kategóriák** | Friss (1 nap) / Hűtött (5 nap) / Fagyasztott (90 nap) |

### 3. Márka / megjelenés

- Új brand portal: `pekseg.portal` / valódi domain (később)
- Warm tones: amber/orange/stone paletta
- Pékség-specifikus ikonok, terminológia

## Technikai implementálás terv

### Fázis 1 — Platform adaptálás (Q3 2026)

1. **`bakery_domain.md` megírása** — az asztalos `woodwork_domain.md` mintájára
2. **Receptúra (BOM) store modul** — a katalógus BOM-on épülve, de tömeg-alapú (kg, liter, db) + sarzs-szorzó
3. **Lejárat-modul** — lot-szintű `expiresAt` + FIFO kivét
4. **Sütés-ütemező** — a Gyártásütemező (ProdSched) domenológiai adaptere
5. **HACCP napló** — az EHS modul adaptere (hőmérséklet + kritikus pont mérések)

### Fázis 2 — Pékség frontend portal (Q4 2026)

1. Új brand portal (React app, pékség színek/copy)
2. Receptúra-néző (BOM vizualizáció, skálázás)
3. Napi terv nézet (mi sül ma + mennyi alapanyag kell)
4. Kasszaterminál (bolti eladás, gyors nyugta)
5. Ügyfélportál (nagyker rendelés, hetirend)

## Első ügyfél célpont

**TBD** — Budapest környéki kézműves pékség, 3-10 fő, előfizetéses modell

## Blokkolók / döntések szükségesek

- [ ] Domain név és brand
- [ ] Első ügyfél azonosítása (pilot)
- [ ] Lejárat-modul: integrálódik-e a meglévő raktár lot-modelljébe? (javasolt: igen)
- [ ] HACCP naplózás: DMS-be mint dokumentum VAGY külön modul?
- [ ] Árszint: ugyanaz mint JoineryTech VAGY eltérő (valószínűleg alacsonyabb — simpleabb üzlet)

## Kapcsolódó fájlok

- Prototípus ÉSZAKI CSILLAG: `/opt/spaceos/docs/tasks/new/joinerytech/CLAUDE.md`
- Asztalos domain tudás: `/opt/spaceos/docs/tasks/new/joinerytech/woodwork_domain.md`
- SpaceOS vízió: `docs/vision/SpaceOS_Vision_Master.md`
