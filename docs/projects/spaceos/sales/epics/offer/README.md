# Ajánlat Kezelés Epic

## Goal

Teljes ajánlatkezelési workflow implementálása:

1. Ajánlat létrehozása termékekből
2. Árazás, kedvezmények kezelése
3. PDF generálás, branding
4. Email küldés ügyfélnek
5. Ügyfél jóváhagyás (portálon)
6. Megrendelés konverzió

## User Stories

### US-1: Ajánlat létrehozás
**Mint** értékesítő
**Szeretném** új ajánlatot létrehozni
**Hogy** az ügyfélnek elküldhessem

**AC:**
- [ ] Ügyfél kiválasztása
- [ ] Termékek hozzáadása katalógusból
- [ ] Mennyiség, ár módosítás
- [ ] Megjegyzés mező

### US-2: PDF Export
**Mint** értékesítő
**Szeretném** PDF-et generálni
**Hogy** az ügyfélnek küldhessem

**AC:**
- [ ] Cég logo, branding
- [ ] Tételek táblázat
- [ ] Összesítés, ÁFA
- [ ] Érvényesség dátum

### US-3: Ügyfél jóváhagyás
**Mint** ügyfél
**Szeretném** az ajánlatot online jóváhagyni
**Hogy** ne kelljen papíron aláírni

**AC:**
- [ ] Egyedi link generálás
- [ ] Megtekintés portálon
- [ ] Elfogadás gomb
- [ ] Aláírás (opcionális)

## Technical Notes

### API Endpoints
```
POST   /api/offers              - Ajánlat létrehozás
GET    /api/offers/:id          - Ajánlat lekérés
PUT    /api/offers/:id          - Ajánlat módosítás
POST   /api/offers/:id/pdf      - PDF generálás
POST   /api/offers/:id/send     - Email küldés
POST   /api/offers/:id/approve  - Ügyfél jóváhagyás
```

### State Machine
```
DRAFT → SENT → VIEWED → APPROVED → CONVERTED
                    ↘ REJECTED
                    ↘ EXPIRED
```
