---
processed: 2026-07-03
id: MSG-DESIGNER-001
from: conductor
to: designer
type: info
priority: medium
status: READ
model: haiku
created: 2026-07-01
ref: MSG-CONDUCTOR-060
content_hash: f3bdffd3b2ba96dc5949b97fc5813f9238ed58c1778ee24aad6d2f2b4356023d
---

# Designer UX Coordination Role — Confirmed ✅

## Összefoglaló

A Conductor megerősíti a Designer terminál **UX koordinációs szerepkörét** a JoineryTech UI fejlesztésben.

**Referencia:** ROOT task MSG-CONDUCTOR-060 (JoineryTech UI Fejlesztés Koordináció)

## Szerepkör Definíció

A Designer terminál mostantól **felügyeli a vizuális megjelenést** minden Frontend task-nál:

### 1. UI Review Scope

**Minden Frontend major UI change előtt Designer review kötelező:**
- New pages/components
- Layout changes (grid, navigation, spacing)
- Color system changes
- Typography changes
- Responsive breakpoint changes

**Kis változások (NEM kell review):**
- Button label change
- Single component prop tweak
- Bug fixes (no visual change)

### 2. Design System Konzisztencia

A Designer biztosítja hogy minden új komponens:
- Követi a Design System szabályokat
- Konzisztens színpalettát használ
- Betartja a spacing system-et (8px grid)
- WCAG AA accessibility követelményeknek megfelel

### 3. Mobile-First & Single-Screen Focus

Minden új UI-nak:
- Mobile viewport-ot prioritásként kezelni (360px-520px)
- Single-screen focus — egy funkció egy képernyőn, NEM több tab/modal egymás felett
- Touch target minimum: 44px (iOS guideline)

## Workflow Integráció

### Frontend Task → Designer Review

**Amikor Conductor Frontend task-ot ad ki:**
1. Ha **major UI change**, a task inbox-ban szerepel: `designer_review: true`
2. Frontend implementálja a feature-t
3. Frontend **NEM** küldi DONE-t közvetlenül
4. Frontend Designer-nak küld **REVIEW REQUEST** outbox-ba
5. Designer review (2-4 óra SLA)
6. Designer APPROVE → Frontend DONE-t küld
7. Designer REJECT → Frontend javítás után újra review

**Példa workflow:**
```
MSG-FRONTEND-065 (új KPI Card komponens)
  ↓ implementáció
Frontend outbox → MSG-DESIGNER-REQ-001 (review request)
  ↓ 2 óra
Designer outbox → MSG-FRONTEND-065-APPROVED vagy REJECTED
  ↓
Frontend outbox → MSG-CONDUCTOR-065-DONE (ha approved)
```

### Autonomous Design Tasks

A Designer **proaktívan** dolgozhat:
- Design System frissítések
- Component library audit
- Accessibility audit
- Mobile responsive audit

**Ezeket NEM kell Conductor-tól várni, saját kezdeményezés.**

## Jelenleg Folyamatban

**MSG-FRONTEND-064** (Bento Grid Layout) már Designer spec-cel dolgozik:
- ✅ Design spec: `docs/design/datahaven-dashboard-bento-grid-spec.md`
- ✅ CSS variables: `datahaven-web/client/src/styles/theme-dark-bento.css`
- ✅ MSG-DESIGNER-020 DONE

**Ezt a mintát használd tovább!**

## Kommunikáció

### MCP Tools (KÖTELEZŐ)

Minden Designer kommunikáció MCP-n keresztül:
- **Inbox olvasás:** `mcp__spaceos-knowledge__list_inbox(terminal: "designer")`
- **Üzenet küldés:** `mcp__spaceos-knowledge__send_message(...)`
- **DONE küldés:** `mcp__spaceos-knowledge__submit_done(...)`

**NE** használj közvetlen fájl írást/olvasást mailbox-hoz!

### Telegram Reply (ha user kérdez)

Ha `[TG @user chat:CHATID]` formátumú üzenet érkezik:
```
mcp__spaceos-knowledge__telegram_reply
  chat_id: <CHATID>
  message: "A válaszod"
  from_terminal: "designer"
```

## Acceptance Criteria

- [ ] Designer role acknowledged
- [ ] Workflow integráció megértve
- [ ] MCP tools használat confirmed
- [ ] Proactive design tasks scope megértve

## Következő Lépések

1. ✅ Jelenlegi task (MSG-DESIGNER-020) DONE feldolgozva
2. 🔄 Várj Frontend review request-ekre
3. 🔄 (Opcionális) Autonomous design tasks (Component library audit, Accessibility review)

---

**Conductor note:** Ez egy INFO üzenet, NEM task. Nincs DONE outbox szükséges, csak acknowledgement ha kérdés van.
