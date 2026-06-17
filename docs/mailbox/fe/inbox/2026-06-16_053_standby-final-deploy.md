---
id: MSG-FE-053
from: root
to: fe
type: task
priority: medium
status: READ
ref: MSG-FE-052
created: 2026-06-16
---

# FE-053 — Standby + Végső deploy koordináció

## Kontextus

FE-A track **KÉSZ** ✅ — 24/27 világ live, 671 teszt, deploy sikeres.

Kiváló munka. A maradék 3 világ (`hr`, `kontrolling`, `service`) az FE-B terminál feladata.

**Skill:** `/spaceos-terminal`

## Feladat

**Várj az FE-B DONE üzenetére.** Amint megérkezik (root értesít), végezd el a **végső deploy**-t:

```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm build
sudo cp -r dist/. /var/www/joinerytech/
sudo nginx -t && sudo systemctl reload nginx
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/
```

## Addig opcionálisan

Ha van kapacitás, ezeket érdemes átnézni:

1. **Mobil reszponzivitás** — az új világok (Tasks Kanban, EHS kockázat-mátrix, ExecBI 4 tab) mobilon jól néznek-e ki? Ha nem, kis CSS fix.

2. **HomeScreen rendezés** — 24 világ kártya a Home-on: az Admin nézet zsúfolt lehet. Fontold meg egy 2 soros scrollable grid-et vagy kategória-szeparátorokat.

3. **Loading state** — a mock adatok azonnal megjelennek, de ha valamelyik SlideOver-en hiányzik a loading skeleton, add hozzá.

**DONE feltétel:** Root értesítés után végső deploy + HTTP 200.
