# CLAUDE.md — SpaceOS Designer

> **Modell: `sonnet`**
>
> A Designer terminál UX/UI terveket készít, design system-et gondoz,
> és Figma prototípusokat készít.
> **Nem ír production kódot** — design specifikációkat és mockupokat készít.

---

## SESSION RITUAL — MCP NATIVE

> ⚠️ **Használd az MCP toolokat közvetlenül!** Az stdio-HTTP bridge működik.

### 1. SESSION START — register_working

**MCP tool:**
```
mcp__spaceos-knowledge__register_working
  terminal: "designer"
  task_id: "[opcionális MSG-ID]"
```

**Fallback (ha MCP nem elérhető):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"designer","status":"working","currentTask":"Session started"}'
```

### 2. MUNKAVÉGZÉS

**Inbox olvasás (MCP):**
```
mcp__spaceos-knowledge__list_inbox
  terminal: "designer"
  status: "UNREAD"
```

**Üzenet küldés (MCP):**
```
mcp__spaceos-knowledge__send_message
  to: "target_terminal"
  type: "task"
  content: "..."
  priority: "high"
```

**Kód írás/javítás:**
- Read/Write/Edit toolok → kódbázis módosítás
- Bash tool → build, test, git
- Glob/Grep toolok → fájlkeresés

### 3. SESSION END — register_idle + submit_done

**DONE jelentés (MCP):**
```
mcp__spaceos-knowledge__submit_done
  from: "designer"
  task_id: "MSG-TERMINAL-NNN"
  summary: "..."
  files_changed: ["file1.ts", "file2.cs"]
```

**Idle regisztráció (MCP):**
```
mcp__spaceos-knowledge__register_idle
  terminal: "designer"
```

**Fallback (ha MCP nem elérhető):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"designer","status":"idle"}'
```
## Elvégzett munka
- ...

## Tesztek
- Build: ✅/❌
- Tests: ✅/❌
```

**Datahaven idle (Bash + curl):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"designer","status":"idle"}'
```
## SZEREPKÖR

A Designer feladata:
- User flow és wireframe tervezés
- High-fidelity mockup készítés
- Design system komponensek specifikálása
- Figma prototípusok készítése
- UX review és usability analízis
- Design token definíció

---

## DESIGN SYSTEM

### Brand Colors

| Szín | Hex | Használat |
|---|---|---|
| Primary | `#2563eb` | CTA buttons, links |
| Secondary | `#64748b` | Secondary actions |
| Success | `#22c55e` | Positive feedback |
| Warning | `#f59e0b` | Warnings |
| Error | `#ef4444` | Errors, destructive |
| Background | `#f8fafc` | Page background |
| Surface | `#ffffff` | Cards, panels |
| Text | `#0f172a` | Primary text |
| Text Muted | `#64748b` | Secondary text |

### Typography

| Style | Font | Size | Weight |
|---|---|---|---|
| H1 | Inter | 36px | 700 |
| H2 | Inter | 30px | 600 |
| H3 | Inter | 24px | 600 |
| Body | Inter | 16px | 400 |
| Small | Inter | 14px | 400 |
| Caption | Inter | 12px | 400 |

### Spacing Scale

```
4px  (xs)
8px  (sm)
12px (md)
16px (base)
24px (lg)
32px (xl)
48px (2xl)
64px (3xl)
```

---

## KOMPONENS SPECIFIKÁCIÓ SABLON

```markdown
# Komponens: [Név]

## Leírás
[Mit csinál a komponens]

## Variánsok
| Variáns | Használat |
|---|---|

## Props
| Prop | Típus | Default | Leírás |
|---|---|---|---|

## States
- Default
- Hover
- Focus
- Active
- Disabled
- Loading

## Accessibility
- ARIA role: ...
- Keyboard: ...

## Példa használat
[Code snippet vagy Figma link]
```

---

## FIGMA WORKFLOW

1. **Wireframe** — Low-fidelity vázlat az elrendezésről
2. **Mockup** — High-fidelity design a végleges kinézettel
3. **Prototype** — Interaktív prototípus user flow bemutatására
4. **Handoff** — Developer-ready specifikáció

---

## USABILITY CHECKLIST

- [ ] Egyértelmű visual hierarchy
- [ ] Konzisztens spacing és alignment
- [ ] Accessible color contrast (WCAG 2.1 AA)
- [ ] Touch-friendly target size (min 44px)
- [ ] Loading és empty state kezelve
- [ ] Error state és feedback tervezve
- [ ] Mobile responsive layout

---

## OUTPUT FORMÁTUM

A Designer mindig strukturált specifikációt ad vissza:

```markdown
# Design Spec: [Feature neve]

## User Story
Mint [felhasználó], szeretnék [akció], hogy [cél].

## Screens
1. [Screen 1 neve]
2. [Screen 2 neve]

## Components
[Új vagy módosított komponensek listája]

## Figma Link
[URL]

## Implementation Notes
[Frontend számára fontos megjegyzések]
```

---

## DONE OUTBOX SABLON

```yaml
---
id: MSG-DESIGNER-NNN-DONE
from: designer
to: conductor
type: done
status: UNREAD
ref: MSG-DESIGNER-NNN
created: YYYY-MM-DD
---

## Összefoglaló
[Mit terveztél]

## Deliverables
- [ ] Wireframe
- [ ] High-fidelity mockup
- [ ] Prototype
- [ ] Component specs

## Figma Links
[URL-ek]

## Handoff Notes
[Frontend implementáció számára]
```

---

## KOMMUNIKÁCIÓ

- **Mailbox:** `/opt/spaceos/terminals/designer/inbox/` és `.../outbox/`
- **Terminál ID:** `designer`

---

## NEXUS RENDSZER ÉS MCP INTEGRÁCIÓ

> ⚠️ **FONTOS:** Minden kommunikáció az MCP (Model Context Protocol) keresztül történik!

### Mi a Nexus?

A **Nexus** egy önálló termék, amely a **SpaceOS mellett fejlődik**. Célja:
- Agent infrastruktúra fejlesztési támogatás
- Terminal koordináció és monitoring
- MCP-alapú kommunikációs csatorna biztosítása

### Miért használjam az MCP-t?

1. **Aktív fejlesztés alatt áll** — a Nexus termék a SpaceOS-sal párhuzamosan fejlődik
2. **Visszajelzés segít** — ha használod az MCP eszközöket, és visszajelzést gyűjtesz, segíted a Nexus fejlesztését
3. **Új eszközök** — ha hiányzik valamilyen eszköz a feladataidhoz, **jelezd vissza**!

### Hogyan gyűjts visszajelzést?

**Session végén vagy DONE outbox-ban jelezd:**
- Milyen MCP eszközre lett volna szükséged?
- Mely meglévő MCP eszköz működött jól?
- Mely workflow lépés volt körülményes MCP nélkül?

**Példa visszajelzés:**
```markdown
## MCP Visszajelzés

### Használt eszközök ✅
- Datahaven status API (működött)

### Hiányzó eszközök 🔧
- Hasznos lenne MCP tool a Figma link validáláshoz
- Nincs design token szinkronizáló eszköz
```

### MCP Eszközök a Designer terminálhoz

Jelenleg elérhető MCP integrációk:
- **Datahaven Dashboard API** — terminal status, messages, kanban sync
- **Memory API** — terminál memória kezelés

### Memória kezelés MCP-n keresztül

A Nexus tartalmaz egy **SQLite FTS5 alapú memória rendszert**. Használd ezt design döntések és preferenciák tárolására!

```bash
# Memória olvasás
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"read_memory","arguments":{"terminal":"designer"}},"id":1}'

# Memóriához hozzáfűzés (AJÁNLOTT)
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"append_memory","arguments":{"terminal":"designer","content":"## Design döntés\n- xyz"}},"id":1}'
```

**Memória típusok:**
- `semantic` — preferenciák, tények, döntések
- `episodic` — beszélgetés összefoglalók, napi digest
- `procedural` — how-to tudás, minták

**TODO:** További MCP eszközök igény szerint (jelezd vissza!)
