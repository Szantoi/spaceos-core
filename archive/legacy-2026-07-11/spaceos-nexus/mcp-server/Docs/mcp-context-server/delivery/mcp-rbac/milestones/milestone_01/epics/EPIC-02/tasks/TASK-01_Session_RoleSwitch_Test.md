---
title: "TASK-01: Session Közbeni Role-Csere Viselkedésének Tesztelése"
type: task
task: TASK-01
epic: EPIC-02
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
depends_on: EPIC-01/TASK-04
---

# 📋 TASK-01: Session Közbeni Role-Csere Viselkedésének Tesztelése

## Leírás

Az MCP kliensek (Claude Desktop, VS Code Copilot) az inicializáláskor lekérik az elérhető eszközlistát, és azt **cache-elik a session teljes idejére**. Ez azt jelenti, hogy ha az ágens futásidőben „sapkát cserél" (role-t vált), a kliens nem biztos, hogy frissített tool listát kap — **kapcsolat-újraindítás nélkül**.

Ez a TASK ezt a limitációt dokumentált kísérlettel vizsgálja meg.

### Teszt Forgatókönyv

1. MCP kliens csatlakozik `x-active-role: explorer` headerrel
2. Lekéri az elérhető tool-ok listáját → rögzítjük
3. Ugyanazon session-ben `x-active-role: backend_developer` headerrel új kérést küld
4. Lekéri az elérhető tool-ok listáját újra → összehasonlítjuk

### Elfogadható Eredmények

- **A eset**: A kliens frissített tool listát kap → az RBAC futásidőben is működik ✅
- **B eset**: A kliens a cached listát használja → csak session-init szintű RBAC lehetséges ⚠️ (dokumentálni kell)

## Elfogadási Kritériumok

- [x] A teszt futtatható MCP CLI kliensen vagy `mcp-rbac-test.ts`-ben HTTP hívással
- [x] Mindkét forgatókönyv (A és B) dokumentálva az eredményfájlban
- [x] Az észlelt viselkedés egyértelműen rögzítve (`session-init-only` VAGY `runtime-switchable`)

## Megvalósítási összefoglaló
A létrehozott `mcp-session-roleswitch.test.ts` E2E teszt futtatásával a **B eset** (session-init-only) bizonyosodott be. Az MCP MCP szerver az inicializáció pillanatában lévő header alapján regisztrálja az elérhető toolokat az adott `sessionId`-re. Ha session közben (ugyanazzal az ID-vel) váltunk szerepkört, a Tool lista változatlan marad az új lekérdezésnél is.
