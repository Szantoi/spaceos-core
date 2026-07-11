# ADR-059: Telegram Integráció Claude Channels Alapon

> **Státusz:** BLOCKED (CPU korlátozás)
> **Dátum:** 2026-07-04
> **Döntéshozó:** Root
> **Érintettek:** Minden terminál, Conductor, Backend

---

## ⚠️ BLOCKER: VPS CPU Korlátozás

**Probléma:** A Claude Code Channels plugin `2.1.80+` verziót igényel, de a VPS CPU nem támogatja az új binárist.

| Paraméter | Jelenlegi | Szükséges |
|-----------|-----------|-----------|
| **Claude Code verzió** | `2.0.62` ✅ működik | `2.1.80+` ❌ timeout |
| **CPU** | QEMU Virtual 2.5+ | Modern x86-64 (AVX?) |
| **Bun runtime** | `1.1.38` ✅ | ✅ |

**Tesztelve:** 2026-07-04
- `npm install -g @anthropic-ai/claude-code@latest` → `2.1.201` települ
- `claude --version` → timeout (nem fut le)
- Rollback: `npm install -g @anthropic-ai/claude-code@2.0.62` → működik

**Lehetséges megoldások:**
1. **VPS upgrade** — modernebb CPU-val rendelkező VPS
2. **Hybrid megoldás** — saját Telegram kód + régi Claude
3. **Várni** — hátha Anthropic kiad egy kompatibilisebb buildet

---

## Kontextus

A SpaceOS Telegram integrációja több iteráción ment keresztül:

1. **v1:** Egyszerű webhook → root inbox
2. **v2 (ADR-049):** Multi-bot + chat sessions (`spaceos-*-chat`)
3. **Probléma:** Duplikált session-ök, naming zavar, túlbonyolított

**2026-os kutatás eredménye:** A Claude Code hivatalos Channels plugin-ja és a Telegram bot-to-bot API elegánsabb megoldást kínál.

---

## Döntés

### Áttérünk a Claude Code Channels + Bot-to-Bot architektúrára

```
┌─────────────────────────────────────────────────────────────────┐
│                         TELEGRAM                                 │
│                                                                  │
│   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│   │ @sarkany │  │ @maestro │  │ @vasokol │  │ @neon    │  ...   │
│   │ (root)   │  │(conduct) │  │(backend) │  │(frontend)│        │
│   └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘        │
│        │             │             │             │               │
│        │    Bot-to-Bot Communication (opt-in)    │               │
│        │◄───────────────────────────────────────►│               │
│        │             │             │             │               │
└────────┼─────────────┼─────────────┼─────────────┼───────────────┘
         │             │             │             │
         ▼             ▼             ▼             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CLAUDE CODE CHANNELS                          │
│                                                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│   │ spaceos-root │  │spaceos-cond. │  │spaceos-back. │  ...     │
│   │   --channels │  │   --channels │  │   --channels │          │
│   │   telegram   │  │   telegram   │  │   telegram   │          │
│   └──────────────┘  └──────────────┘  └──────────────┘          │
│                                                                  │
│   Minden terminál a SAJÁT botjával kommunikál                    │
│   Channel plugin push-olja az üzeneteket a sessionbe             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Architektúra Részletek

### 1. Terminál ↔ Bot Mapping (változatlan)

| Terminál | Bot | Username |
|----------|-----|----------|
| root | Sárkány | @sarkany_fekete_kod_bot |
| conductor | Maestro | @maestro_felete_kod_bot |
| backend | Vasököl | @vasokol_fekete_kod_bot |
| frontend | Neon | @neon_fekete_kod_bot |
| architect | Árnyék | @arnyek_felete_kod_bot |
| librarian | Krónikás | @kronikas_fekete_kod_bot |
| explorer | Nyomkereső | @nyomkereso_fekete_kod_bot |
| designer | Vízió | @vizio_fekete_kod_bot |

### 2. Session Indítás (ÚJ)

**Régi mód:**
```bash
# Work session
claude --model sonnet
# + Külön chat session
claude --model haiku  # spaceos-root-chat
```

**Új mód:**
```bash
# Egy session, Telegram channel-lel
claude --model sonnet --channels plugin:telegram@claude-plugins-official
```

A `--channels` flag:
- Elindítja a Telegram plugin-t
- Beregisztrálja a bot token-t
- Push-olja az üzeneteket a sessionbe
- Claude közvetlenül válaszolhat

### 3. Multi-Agent Kommunikáció (Bot-to-Bot)

**Telegram Bot API 2026 május:**
```
@maestro_bot → @sarkany_bot: "[CONDUCTOR→ROOT] EPIC-JT-CRM unblocked"
@sarkany_bot → @arnyek_bot: "[ROOT→ARCHITECT] Review needed: ADR-059"
```

**Opt-in beállítás:**
```bash
# BotFather-ben minden botnak
/mybots → @sarkany_bot → Bot Settings → Allow Bot-to-Bot → Enable
```

**Előnyök:**
- Terminálok Telegram-on keresztül is koordinálhatnak
- User látja a kommunikációt (átláthatóság)
- Nem kell MCP/inbox minden aprósághoz

### 4. Message Flow

#### User → Terminál

```
User: @vasokol fixeld a NuGet ugyét
         │
         ▼
   @vasokol_bot (Telegram)
         │
         ▼
   Claude Channels Plugin
         │
         ▼
   spaceos-backend session
         │
         ▼
   Claude feldolgozza, válaszol
         │
         ▼
   Telegram reply (ugyanaz a bot)
```

#### Terminál → Terminál (Bot-to-Bot)

```
Backend: "NuGet fix kész, Frontend tesztelheti"
         │
         ▼
   @vasokol_bot.sendMessage(@neon_bot)
         │
         ▼
   @neon_bot receives (bot-to-bot channel)
         │
         ▼
   spaceos-frontend session
         │
         ▼
   Frontend Claude látja az üzenetet
```

#### Broadcast (Group Chat)

```
Conductor: "Napi standup - mindenki státuszt!"
         │
         ▼
   SpaceOS Team Group (Telegram group)
         │
         ▼
   Minden bot látja → minden session értesül
```

---

## Implementációs Terv

### Phase 1: Claude Channels Setup (1-2 nap)

1. **Minden terminál session indítás módosítása:**
   ```bash
   # sessionStarter.ts
   const channelFlag = `--channels plugin:telegram@claude-plugins-official`;
   const cmd = `claude --model ${model} ${channelFlag}`;
   ```

2. **Bot token környezeti változók:**
   ```bash
   # .env.telegram
   TELEGRAM_ROOT_TOKEN=8835562910:AAE...
   TELEGRAM_CONDUCTOR_TOKEN=8096244204:AAG...
   # ...
   ```

3. **Régi chat session kód eltávolítása:**
   - `chatSessionStarter.ts` → DELETE
   - `-chat` session logika → DELETE
   - `spaceos-*-chat` session-ök → nem jönnek létre többé

### Phase 2: Bot-to-Bot Engedélyezés (1 nap)

1. **BotFather konfig minden botra:**
   ```
   /mybots → Select bot → Bot Settings → Allow Bot-to-Bot → ON
   ```

2. **Manager bot beállítás (Conductor):**
   ```
   /mybots → @maestro_bot → Transfer Ownership → (optional, ha managed bots kellenek)
   ```

3. **MCP tool bot-to-bot küldéshez:**
   ```typescript
   // mcp.ts - új tool
   telegram_agent_message: {
     from_terminal: string,    // küldő
     to_terminal: string,      // címzett
     message: string,
     priority?: 'info' | 'action' | 'urgent'
   }
   ```

### Phase 3: Group Chat Setup (opcionális)

1. **SpaceOS Team group létrehozása**
2. **Minden bot hozzáadása**
3. **Broadcast MCP tool:**
   ```typescript
   telegram_broadcast: {
     from_terminal: string,
     message: string,
     tag?: 'standup' | 'alert' | 'done' | 'blocked'
   }
   ```

---

## Eltávolítandó Kód

### Törlendő fájlok

```
spaceos-nexus/knowledge-service/src/
├── chatSessionStarter.ts          # DELETE
├── telegram/
│   ├── multiBotManager.ts         # DELETE (polling logic)
│   └── ...                        # REVIEW - megtartandó részek
```

### Módosítandó fájlok

```
spaceos-nexus/knowledge-service/src/
├── sessionStarter.ts              # --channels flag hozzáadása
├── pipeline/telegramBot.ts        # Egyszerűsítés - csak webhook fallback
├── mcp.ts                         # Új bot-to-bot tools
```

### Törlendő session típusok

- `spaceos-*-chat` → nem kell többé
- Külön chat/work session logika → egyetlen session

---

## Döntési Mátrix

| Szempont | Régi (ADR-049) | Új (ADR-059) |
|----------|----------------|--------------|
| Session-ök száma | 2× (work + chat) | 1× (unified) |
| Telegram polling | 8 bot × 2s | 0 (push-based) |
| Multi-agent TG | Nincs | Bot-to-bot native |
| Karbantartás | Komplex | Egyszerű |
| Official support | Nincs | Claude Channels |

---

## Kockázatok

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|--------------|-------|-----------|
| Channels plugin instabil | Közepes | Magas | Fallback webhook |
| Bot-to-bot spam | Alacsony | Közepes | Rate limiting |
| Offline üzenet elvesztés | Közepes | Alacsony | Inbox fallback |

---

## Acceptance Criteria

- [ ] Minden terminál `--channels` flag-gel indul
- [ ] User üzenet → terminál session → válasz működik
- [ ] Bot-to-bot kommunikáció tesztelve (min. 2 terminál között)
- [ ] Régi `-chat` session kód törölve
- [ ] Group chat broadcast működik (opcionális)
- [ ] Dokumentáció frissítve

---

## Függőségek

- Claude Code Channels plugin (research preview) → production stability?
- Telegram Bot API bot-to-bot → minden botra engedélyezve?
- MCP tool-ok frissítése

---

## Döntéshozók

- [x] Root - terv készítés
- [ ] Backend - implementáció review
- [ ] Conductor - koordináció

---

## Hivatkozások

- [ADR-049: Dual Session Architecture](./ADR-049-dual-session-chat-work-architecture.md)
- [Kutatás eredménye](../patterns/TELEGRAM_2026_RESEARCH.md)
- [Claude Code Channels Docs](https://code.claude.com/docs/en/channels)
