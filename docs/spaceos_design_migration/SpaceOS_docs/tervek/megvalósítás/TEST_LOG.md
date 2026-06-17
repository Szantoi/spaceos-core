# SpaceOS — Teszt napló

**Dátum:** 2026-03-30
**Tesztelő:**
**Környezet:** localhost (dev)

| Szolgáltatás | URL | Státusz |
|---|---|---|
| Design Portal | http://localhost:5173 | |
| Orchestrator | http://localhost:3000/bff/health | |
| Kernel API | http://localhost:5000/healthz | |

---

## 1. Bejelentkezés

**Lépések:**
1. Nyisd meg: http://localhost:5173/login
2. Username: `admin`, Password: `admin`
3. Kattints: Sign in

**Várható eredmény:** Átirányítás a Dashboard-ra, bal oldali menü látható.

- [X] OK
- [ ] Hiba: Lentebb kellet veni a jelszó méretelenörzését 6 karakteről 1 carakterre.

---

## 2. Dashboard

**Lépések:**
1. Bejelentkezés után automatikusan megjelenik
2. Ellenőrizd a statisztika kártyákat és az audit eseményeket

**Várható eredmény:** Kártyák betöltenek (kezdetben 0 értékekkel), audit log üres.

- [ ] OK
- [ ] Hiba: nem jelennek meg kártyák. Így nem tudom megállapytani, hogy ez a helyes működés-e.

---

## 3. Tenant létrehozása

**Lépések:**
1. Menj: Tenants
2. Kattints: New Tenant
3. Név: `TestCorp`
4. Mentés

**Várható eredmény:** `TestCorp` megjelenik a listában.

- [x] OK
- [ ] Hiba: Nem jelenik meg a listában az újonnan létrehozott Tenants.
   Tenant name
   TestCorp
   CreateCancel
   No records found.

---

## 4. Második Tenant létrehozása

**Lépések:**
1. Menj: Tenants → New Tenant
2. Név: `AnotherCorp`
3. Mentés

**Várható eredmény:** Két tenant látható a listában.

- [x] OK megjelet a két érték a gombokkal
- [ ] Hiba:

---

## 5. Tenant részletei

**Lépések:**
1. Kattints a `TestCorp`-ra
2. Ellenőrizd a részletes nézetet

**Várható eredmény:** Tenant adatai + kapcsolódó facility lista (egyelőre üres).

- [x] OK az oldal betöltödik, de nem jelenik meg érték
- [ ] Hiba:

---

## 6. Facility létrehozása

**Lépések:**
1. Menj: Facilities → New Facility
2. Név: `HQ Budapest`
3. Tenant: `TestCorp`
4. Mentés

**Várható eredmény:** `HQ Budapest` megjelenik a listában.

- [x] OK
Bevitt adatok:
Filter by tenant:
TestCorp
Tenant
TestCorp
Facility name
HQ Budapest

megjelent.

- [ ] Hiba: Az all tanens nem jelenít meg semmit.

---

## 7. Második Facility létrehozása

**Lépések:**
1. Menj: Facilities → New Facility
2. Név: `Branch Debrecen`
3. Tenant: `TestCorp`
4. Mentés

**Várható eredmény:** Két facility látható.

- [x] OK
- [ ] Hiba:

---

## 8. WorkStation regisztrálása

**Lépések:**
1. Menj: WorkStations → Register
2. Név: `WS-001`
3. Facility: `HQ Budapest`
4. Mentés

**Várható eredmény:** `WS-001` megjelenik `AVAILABLE` státuszban.

- [x] OK megjelenik
- [ ] Hiba:

---

## 9. WorkStation FSM átmenetek

**Lépések:**
1. Kattints a `WS-001`-re
2. Próbáld az átmeneteket sorban:
   - `AVAILABLE → IN_USE`
   - `IN_USE → CLOSED_DONE`

**Várható eredmény:** Minden lépésnél a státusz frissül, az elérhető gombok változnak.

- [x] AVAILABLE → IN_USE OK
- [x] IN_USE → CLOSED_DONE OK
- [ ] Hiba:

---

## 10. Space Layer létrehozása

**Lépések:**
1. Menj: Space Layers → New
2. Facility: `HQ Budapest`
3. Intent JSON: `{}`
4. Mentés

**Várható eredmény:** Space Layer megjelenik a listában.

- [ ] OK
- [ ] Hiba:

---

## 11. Space Layer szerkesztése

**Lépések:**
1. Kattints a létrehozott Space Layer-re
2. Módosítsd az intent JSON-t, pl.: `{"floors": 3}`
3. Mentés

**Várható eredmény:** Frissített intent látható.

- [x] OK json-t lehetet kézzel frisíteni.
- [ ] Hiba: nincs delet

---

## 12. Flow Epic létrehozása

**Lépések:**
1. Menj: Flow Epics → New Epic
2. Cím: `Első projekt`
3. Tenant: `TestCorp`
4. Facility: `HQ Budapest`
5. Mentés

**Várható eredmény:** Epic megjelenik a Kanban táblán (első oszlopban).

- [x] OK
- [ ] Hiba: nincs kanba tábla

---

## 13. Flow Epic mozgatása (Kanban)

**Lépések:**
1. Menj: Flow Epics
2. Húzd az `Első projekt` epicet a következő oszlopba

**Várható eredmény:** Epic az új oszlopban jelenik meg.

- [x] OK
- [ ] Hiba:

---

## 14. B2B Delegálás

**Lépések:**
1. Nyisd meg az `Első projekt` epic részleteit
2. Keresd a Delegate gombot
3. Delegálj egy másik tenant-ra (`AnotherCorp`)

**Várható eredmény:** Delegálás sikeres, epic frissül.

- [x] OK
- [ ] Hiba: Nem lehet elenörizni a delegálást mert ha nincs pontosan azonos ID-ju Facilities akkor nem lehet leszürni.

---

## 15. Audit Log

**Lépések:**
1. Menj: Audit
2. Ellenőrizd az eseményeket
3. Próbáld a szűrőket (tenant, dátum)

**Várható eredmény:** Az összes korábbi művelet (tenant, facility, workstation, epic) szerepel az audit logban.

- [x] OK
- [ ] Hiba: A szürés nem jól müködik.
1. szűrés:
Audit
Entity Type
WorkStationRegisteredEvent
From
2026. 04. 01.
To
2026. 04. 02.
Filter
Created At	Event Type	Aggregate ID	State Hash
2026. 04. 01. 18:28:34	TenantCreatedEvent	d21f469b-5bb4-486e-9f5b-3079d0a74447	88624313

2. szűrés
Audit
Entity Type
WorkStationRegisteredEvent
From
2026. 04. 02.
To
2026. 04. 02.
Filter
No audit entries found

3. szűrés:
Audit
Entity Type
e.g. FlowEpic
From
2026. 04. 02.
To
2026. 04. 02.
Filter
No audit entries found

4. szűrés

Audit
Entity Type
e.g. FlowEpic
From
2026. 04. 02.
To
2026. 04. 03.
Filter
Created At	Event Type	Aggregate ID	State Hash
2026. 04. 02. 8:32:54	SpaceLayerRegisteredEvent	e481034e-b4de-49fc-b34e-67d63188a897	a3609d62
2026. 04. 02. 8:32:19	WorkStationRegisteredEvent	e481034e-b4de-49fc-b34e-67d63188a897	84832ec2
2026. 04. 02. 8:32:04	FacilityCreatedEvent	1b4c40d3-f6a7-497a-a37d-e76f587c8bc6	6de34010
2026. 04. 02. 8:31:47	TenantCreatedEvent	1b4c40d3-f6a7-497a-a37d-e76f587c8bc6	ccb1c303

Javítás után: A dátum szűrő működik. De a tipus szűrés nem történik meg.
Entity Type
0
From
2026. 04. 01.
To
2026. 04. 02.
Filter
Created At	Event Type	Aggregate ID	State Hash
2026. 04. 02. 8:32:54	SpaceLayerRegisteredEvent	e481034e-b4de-49fc-b34e-67d63188a897	a3609d62
2026. 04. 02. 8:32:19	WorkStationRegisteredEvent	e481034e-b4de-49fc-b34e-67d63188a897	84832ec2
2026. 04. 02. 8:32:04	FacilityCreatedEvent	1b4c40d3-f6a7-497a-a37d-e76f587c8bc6	6de34010
2026. 04. 02. 8:31:47	TenantCreatedEvent	1b4c40d3-f6a7-497a-a37d-e76f587c8bc6	ccb1c303
2026. 04. 01. 18:28:34	TenantCreatedEvent	d21f469b-5bb4-486e-9f5b-3079d0a74447	88624313

---

## 16. AI Chat

**Lépések:**
1. Menj: Chat
2. Írj: `List all tenants`
3. Várj a válaszra

**Várható eredmény:** Az LLM (mock módban) válaszol, és visszaadja a tenant listát.

- [ ] OK
- [ ] Hiba:

---

## 17. Kijelentkezés

**Lépések:**
1. Keresd a logout gombot (jobb felső sarok)
2. Kattints rá

**Várható eredmény:** Visszakerülsz a login oldalra, védett oldalak nem elérhetők.

- [x] OK
- [ ] Hiba:

---

## 18. Designer szerepkör teszt

**Lépések:**
1. Jelentkezz be: username: `designer`, password: `designer`
2. Próbálj meg Admin-only műveletet (pl. Tenant törlése)

**Várható eredmény:** A művelet tiltott, hibaüzenet jelenik meg.

- [x] OK
- [ ] Hiba: Nincsenek Delet végpontok
Megjelenek a gombok és be lehet lépni a szerkesztésbe. De semmi se történik. Valami felvilllan de gyorsan etünik.


---

## Összefoglalás

| Teszt | Státusz | Megjegyzés |
|---|---|---|
| 1. Bejelentkezés | ✅ PASS | Jelszó minimum 4 karakterre csökkentve |
| 2. Dashboard | ⚠️ PARTIAL | Statisztika kártyák nem jelennek meg |
| 3. Tenant létrehozása | ✅ PASS | |
| 4. Második Tenant | ✅ PASS | |
| 5. Tenant részletei | ✅ PASS | |
| 6. Facility létrehozása | ✅ PASS | |
| 7. Második Facility | ✅ PASS | |
| 8. WorkStation regisztrálása | ✅ PASS | |
| 9. WorkStation FSM | ✅ PASS | AVAILABLE → IN_USE → CLOSED_DONE |
| 10. Space Layer létrehozása | ✅ PASS | |
| 11. Space Layer szerkesztése | ✅ PASS | Delete nincs implementálva (archiválás tervezve) |
| 12. Flow Epic létrehozása | ✅ PASS | |
| 13. Flow Epic Kanban | ✅ PASS | |
| 14. B2B Delegálás | ✅ PASS | |
| 15. Audit Log | ✅ PASS | Dátum és típus szűrő javítva |
| 16. AI Chat | ⏳ PENDING | Gemini free tier rate limit — tesztelés folyamatban |
| 17. Kijelentkezés | ✅ PASS | |
| 18. Designer szerepkör | ✅ PASS | 403 visszaadva, admin gombok elrejtve |

---

## Általános észrevételek / Hibák

### Javított hibák (2026-04-01 – 2026-04-02)

| # | Hiba | Javítás | Réteg |
|---|---|---|---|
| 1 | Login 6 karakter minimum | min(1)-re csökkentve | Portal |
| 2 | `/api/auth/token` → `/auth/token` URL hiba | URL javítva | Portal |
| 3 | Proxy 404/408 — body parser elnyelte a stream-et | Proxy mountolása `express.json()` elé helyezve | Orchestrator |
| 4 | `(tenants ?? []).map is not a function` | PagedList `.items` kezelés javítva | Portal |
| 5 | Enum értékek eltérés (WorkStationStatus, TradeType, WorkflowPhase) | Enums szinkronizálva Kernel OpenAPI alapján | Portal |
| 6 | WorkStation FSM státusz számként érkezett | `JsonStringEnumConverter` globálisan hozzáadva | Kernel |
| 7 | Audit events 500 hiba | SQLite-kompatibilis mezők | Kernel |
| 8 | Audit szűrő — dátum To exclusive | End-of-day normalizálás az endpoint-ban | Kernel |
| 9 | Audit szűrő — típus szűrő nem működött | `eventType` paraméter hozzáadva a teljes stackhez | Kernel |
| 10 | 403 hibák console-ban Designer esetén | TanStack Query retry kikapcsolva 4xx-re | Portal |
| 11 | Admin gombok láthatók Designernek | `useIsAdmin()` hook alapú role-check | Portal |
| 12 | Delete gomb nem létező route-ra navigált | Route javítva → detail oldalra navigál | Portal |
| 13 | Soft delete hiányzott | `IsArchived` flag + Archive() minden entitáson | Kernel |
| 14 | OpenAI/Gemini provider hiányzott | `OpenAIProvider` implementálva | Orchestrator |

### Nyitott kérdések

| # | Kérdés | Prioritás |
|---|---|---|
| 1 | Dashboard statisztika kártyák üresek | P3 |
| 2 | AI Chat Gemini rate limit (free tier) | P2 |
| 3 | Space Layer / Facility detail — nincs delete gomb a UI-ban | P3 |

