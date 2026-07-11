# SpaceOS Terminals — Új Architektúra (2026-06-21)

## Áttekintés

A SpaceOS terminál struktúrája 19+ modul-alapú terminálról **8 szerep-alapú terminálra** változott.

## Új Terminálok

| Terminál | Szerep | Modell | Leírás |
|---|---|---|---|
| **Root** | Stratégia | `opus` | Közvetlen tervezés, stratégiai döntések, agent infra |
| **Conductor** | Orchestráció | `sonnet` | Feladatkiosztás, prioritások, koordináció |
| **Architect** | Tervezés | `opus` | Architektúra döntések, ADR, v1→v4 pipeline |
| **Librarian** | Tudáskezelés | `haiku` | Knowledge base, memória menedzsment |
| **Explorer** | Kutatás | `haiku` | Kódbázis feltérképezés, dependency analízis |
| **Backend** | Backend kód | `sonnet` | .NET 8 + Node.js, minden backend modul |
| **Frontend** | Frontend kód | `sonnet` | React/TypeScript, minden UI |
| **Designer** | UX/UI | `sonnet` | Design system, Figma, mockupok |

## Mappa Struktúra

```
/opt/spaceos/terminals/
├── root/               ← Stratégiai tervezés, közvetlen kommunikáció
│   ├── CLAUDE.md       ← Terminál identitás és szabályok
│   ├── inbox/          ← Bejövő feladatok
│   ├── outbox/         ← DONE/BLOCKED válaszok
│   └── archive/        ← Feldolgozott üzenetek
├── conductor/
├── architect/
├── librarian/
├── explorer/
├── backend/
├── frontend/
├── designer/
├── _legacy_archive/    ← Régi terminálok archívuma
└── README.md           ← Ez a fájl
```

## Migráció a Régi Rendszerről

### Régi terminálok → Új terminálok

| Régi | Új | Megjegyzés |
|---|---|---|
| root | root | Saját terminálba költözött |
| conductor | conductor | Változatlan |
| architect | architect | Változatlan |
| librarian | librarian | Változatlan |
| kernel | backend | Konszolidálva |
| orch | backend | Konszolidálva |
| joinery | backend | Konszolidálva |
| cutting | backend | Konszolidálva |
| identity | backend | Konszolidálva |
| inventory | backend | Konszolidálva |
| procurement | backend | Konszolidálva |
| sales | backend | Konszolidálva |
| abstractions | backend | Konszolidálva |
| nexus | backend | Konszolidálva |
| fe | frontend | Konszolidálva |
| fe2 | frontend | Konszolidálva |
| infra | backend | Konszolidálva |
| e2e | backend | Konszolidálva |

### Régi mailbox archívum

A régi mailbox struktúra megtalálható: `/opt/spaceos/terminals/_legacy_archive/mailbox_pre_migration/`

## Terminál Indítás

```bash
# Terminál session indítása
claude --model <model> --cwd /opt/spaceos/terminals/<terminal>/

# Példa: Backend terminál
claude --model sonnet --cwd /opt/spaceos/terminals/backend/
```

## Datahaven Dashboard

**URL:** https://datahaven.joinerytech.hu
**Auth Token:** `dev-token-spaceos-dashboard-2026`

A Dashboard automatikusan felismeri a terminálokat a `/opt/spaceos/terminals/` struktúrából.

## Kommunikáció

Minden terminál az inbox/outbox minta szerint kommunikál:

1. **Inbox olvasás:** `ls /opt/spaceos/terminals/<terminal>/inbox/`
2. **Feladat feldolgozás:** `status: UNREAD → READ`
3. **DONE outbox:** `/opt/spaceos/terminals/<terminal>/outbox/`
4. **Archive:** Feldolgozott üzenetek ide kerülnek

## Miért ez a változás?

1. **Egyszerűbb koordináció** — 7 terminál vs 19+
2. **Hatékonyabb kontextus** — Egy terminál ismeri az egész backend-et
3. **Gyorsabb fejlesztés** — Kevesebb handoff, kevesebb várakozás
4. **Tisztább felelősségek** — Szerep-alapú, nem modul-alapú

---

*Migráció dátuma: 2026-06-21*
