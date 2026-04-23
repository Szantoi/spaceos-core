# SpaceOS — Doorstar Portal Kézi Tesztelési Útmutató

**URL:** https://joinerytech.hu
**Felhasználó:** doorstar-admin (teljes hozzáférés) / doorstar-designer (korlátozott)
**Utolsó frissítés:** 2026-04-18

---

## Bejelentkezés

| Lépés | Elvárt eredmény |
|---|---|
| https://joinerytech.hu megnyitása | Átirányítás → Keycloak login oldal |
| doorstar-admin + jelszó megadása | Átirányítás → `/callback` → Dashboard |
| Oldal újratöltése (F5) | Visszadobás → Keycloak login (memória-only auth) |
| Kijelentkezés (**Topbar jobb felső sarokban** — "Logout" gomb, a felhasználónév mellett) | Keycloak logout → `https://joinerytech.hu/` → login oldal |

[2026-04-18-06:51] - A logout nem müködik. kattintásra nem történik semmi nem írányt át a kijelentkezési oldalr és bejelentkezve maradok.

---

## 1. Dashboard (`/`)

**Státuszkártyák (4 db):**
| Kártya | Kattintásra navigál |
|---|---|
| Nyitott rendelések (szám) | `/procurement` |
| MDF 18mm készlet (lap db) | `/inventory` |
| Szállítók száma | `/suppliers` |
| Utolsó vágóterv neve | `/cutting` |

**Gyors műveletek:**
| Gomb | Hatás |
|---|---|
| `+ Új rendelés` | Navigál → `/procurement` |
| `+ Bevételezés` | Navigál → `/inventory` |
| `Szállítók →` | Navigál → `/suppliers` |

**Ellenőrzendő:** betöltés közben skeleton animáció jelenik meg (nem fehér képernyő).

---

## 2. Szállítók (`/suppliers`)

**Lista:**
- Táblázat: Név · E-mail · Telefon · Cím
- Ha üres: "Nincs szállító rögzítve" üzenet

**`+ Új szállító` gomb → modal:**
| Mező | Típus | Kötelező |
|---|---|---|
| Név | szöveg | ✅ |
| E-mail | szöveg | ❌ |
| Telefon | szöveg | ❌ |
| Cím | szöveg | ❌ |

- `Mentés` → modal bezárul, lista frissül
- `Mégse` → modal bezárul, nincs változás
- Üres névvel mentés → validációs hibaüzenet jelenik meg

---

## 3. Készlet (`/inventory`)

### ⚠️ Ismert bug (PORTAL-017 — javítás folyamatban)
Az `areaM2` mező `undefined` esetén a Készlet tab crashel. Fix kiadva, deploy várat magára.

**Anyagtípus szűrő (pill gombok):**
- `Összes` · `MDF 18mm` · `MDF 16mm` · `HDF 3mm` · `Forgácslap` · `ABS él`
- Aktív gomb: kék háttér

**Tab bar:**

### Készlet tab
- Kártyák: Lapok száma (db) · Terület (m²)
- `+ Bevételezés` gomb → modal:

| Mező | Típus | Szabály |
|---|---|---|
| Anyagtípus | dropdown | kötelező |
| Vastagság (mm) | szám | ≥ 1 |
| Lapszám (db) | szám, egész | ≥ 1 |
| Terület (m²) | szám | ≥ 0.01 |
| Hivatkozás | szöveg | kötelező |
| Dátum | dátum | kötelező |

### Maradékok tab
- Lista: méret (cm × cm) · terület (m²) · anyagtípus
- Ha üres: "Nincs maradék." üzenet

### Trend tab
- Táblázat: Dátum · Anyagtípus · Terület (m²)
- Ha üres: "Nincs trend adat." üzenet

---

## 4. Vágótervek (`/cutting`)

**Lista (bal oldal):**
- Kártyák: terv neve · dátum · státusz badge (Draft/In_Progress/Done)
- Kártyára kattintás → jobb panel megnyílik (NestingResult)
- Ismét kattintás → panel bezárul

**NestingResult panel (jobb oldal):**
- Kiválasztott terv esetén vágási elrendezés adatok
- Ha nincs terv kiválasztva: üres állapot

**`+ Új vágóterv` gomb → modal:**
| Mező | Típus | Kötelező |
|---|---|---|
| Név | szöveg | ✅ |
| Dátum | dátum | ✅ |

- `Létrehozás` → modal bezárul, terv megjelenik a listában
- `Mégse` → modal bezárul

---

## 5. Beszerzés (`/procurement`)

**Tab bar: Rendelések / Szállítások**

### Rendelések tab
- Táblázat: Szállító neve · Összeg (Ft) · Várható szállítás · Státusz · Létrehozva
- Státusz badge-ek: Draft/Submitted (szürke) · Delivered (zöld) · Cancelled (piros)
- Ha üres: "Nincs rögzített rendelés" üzenet

**`+ Új rendelés` gomb → modal:**
*(szállítói adatok, összeg, várható szállítási dátum)*

### Szállítások tab
- Ha üres: "Nincs rögzített szállítás" üzenet
- `+ Szállítás rögzítése` gomb → modal *(szállítási rekord rögzítése)*

---

## 6. Chat (`/chat`)

- Chat felület AI asszisztenssel
- Üzenet begépelése + Enter/Küldés gomb
- Válasz buborékban jelenik meg
- Tool call-ok: `ToolsUsedBadge` jelzi mely eszközöket használt az AI
- Tool eredmény kártyák: `ToolResultCard` expandálható

---

## 7. SpaceOS Admin oldalak

> Ezek az oldalak a SpaceOS platform adminisztrációjához tartoznak. Doorstar soft launch során ritkábban használtak.

### Bérlők (`/tenants`) — csak admin
- Lista: bérlők neve, típusa
- `+ Új bérlő` gomb → form (Név, Típus, Modulok)
- Sorra kattintás → részletoldal (`/tenants/:id`)

### Létesítmények (`/facilities`)
- Lista: létesítmény neve, bérlő
- Részletoldal: munkaállomások, térlaprecsok, flow epics áttekintés
- Gombok: `+ Új létesítmény`

### Munkaállomások (`/workstations`)
- Lista: munkaállomás neve, státusz badge
- Részletoldal: státusz változtatás gomb, hozzárendelt SpaceLayer
- `+ Új munkaállomás` gomb

### Tér rétegek (`/spacelayers`)
- Lista: réteg neve, létesítmény
- `+ Új tér réteg` gomb → form (Név, Létesítmény, Geometria)

### Flow Epics (`/flowepics`)
- Kanban nézet: Draft / Active / Closed oszlopok
- Kártyára kattintás → részletoldal
- Részletoldal: stage-ek listája, delegálás gomb (`DelegateEpicDialog`)
- `+ Új flow epic` gomb

### Audit eseménynapló (`/audit`) — csak admin
- Táblázat: esemény típusa, bérlő, időbélyeg, hash
- Szűrés: dátum, bérlő, eseménytípus
- Hash lánc integritás jelzés

### Csomópontok (`/nodes`)
- Federation node-ok listája
- NodeManifest CRUD műveletek

### Szinkron jelek (`/sync`)
- Szinkronizációs jelek monitorozása

---

## Hibák és elvárások

| Hibaüzenet | Elvárt viselkedés |
|---|---|
| "Nincs adat" szöveg | Normális üres állapot, nem crash |
| 405 Method Not Allowed (console) | Pre-existing gap (cutting GET list), nem blokkoló |
| "Unexpected Application Error" | Bug → jelezd Root terminálnak |
| Redirect loop (újra és újra login) | DEBUG-001 regresszió → azonnali jelzés |

---

## Tesztelési sorrend javaslat

```
1. Bejelentkezés ✓
2. Dashboard betölt, kártyák látszanak ✓
3. Szállítók → táblázat + új szállító hozzáadása ✓
4. Készlet → Bevételezés rögzítése (anyag + db + m²) ✓
5. Készlet → Maradékok tab + Trend tab ✓
6. Vágótervek → új terv létrehozása + kártyára kattintás ✓
7. Beszerzés → új rendelés + szállítás tab ✓
8. Chat → üzenet küldés, AI válasz ✓
9. Flow Epics → kanban nézet + részletoldal ✓
10. Audit → eseménylista látható ✓
```
