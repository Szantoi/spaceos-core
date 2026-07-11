# ADR-060: CLI-Agnosztikus Telegram Architektúra

> **Státusz:** APPROVED
> **Dátum:** 2026-07-04
> **Döntéshozó:** Root
> **Érintettek:** Minden terminál, Backend, Infra

---

## Kontextus

A SpaceOS fejlesztési célja, hogy a CLI-t (jelenleg Claude Code) le lehessen cserélni bármilyen más agentre:
- Saját fejlesztésű agent
- Más LLM provider CLI-ja (OpenAI, Gemini, stb.)
- Lokális LLM (Ollama, llama.cpp)

**Követelmények:**
1. Telegram integráció NEM függhet Claude Code specifikus feature-öktől
2. **Chat session-ök MARADNAK** — saját CLAUDE.md-vel az identitás szeparáció miatt
3. **MCP Tool preferált** — jobb logolhatóság, flow debugging
4. **DB tárolás** — bejövő ÉS kimenő üzenetek is
5. **Teljes kontextus** — CLI agent kapja a conversation history-t (in + out)

---

## Döntés

### CLI-Agnosztikus Rétegzett Architektúra

```
┌─────────────────────────────────────────────────────────────────┐
│                         TELEGRAM LAYER                           │
│                      (Provider-agnosztikus)                      │
│                                                                  │
│   Webhook/Polling → Message Router → SQLite DB (in+out)         │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SESSION LAYER                               │
│              (Work + Chat sessions, saját CLAUDE.md)             │
│                                                                  │
│   spaceos-root         ← Work session (inbox feladatok)         │
│   spaceos-root-chat    ← Chat session (Telegram, saját identity)│
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      INJECTION LAYER                             │
│           (tmux send-keys + FULL conversation context)           │
│                                                                  │
│   Új üzenet érkezik:                                            │
│   1. DB-be mentés (direction: 'in')                             │
│   2. Conversation history lekérés (in + out)                    │
│   3. Formázott kontextus injection a chat session-be            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                       RESPONSE LAYER                             │
│                      (MCP Tool preferált)                        │
│                                                                  │
│   CLI → MCP telegram_reply → DB mentés (out) → Telegram send    │
│                                                                  │
│   Audit trail: Minden MCP hívás logolva                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Session Architektúra

### Work vs Chat Session Szeparáció

```
terminals/root/
├── CLAUDE.md              ← Work session identity (stratégiai döntések, koordináció)
├── CLAUDE-CHAT.md         ← Chat session identity (Telegram válaszok, gyors kérdések)
├── inbox/
├── outbox/
└── ...

tmux sessions:
├── spaceos-root           ← Work session (claude --model opus/sonnet)
└── spaceos-root-chat      ← Chat session (claude --model haiku)
```

### Miért külön CLAUDE.md?

| Szempont | Work Session | Chat Session |
|----------|--------------|--------------|
| **Cél** | Inbox feladatok, kód írás | Telegram válaszok, gyors koordináció |
| **Model** | Opus/Sonnet | Haiku (gyors, olcsó) |
| **Kontextus** | Teljes codebase | Conversation history |
| **Identitás** | "Te a ROOT terminál vagy..." | "Te a ROOT CHAT session vagy..." |
| **MCP Tools** | Minden tool | telegram_reply, request_work_session |

### Chat Session CLAUDE.md Struktúra

```markdown
# CLAUDE.md — SpaceOS Root Chat Session

Te a ROOT terminál **CHAT** session-je vagy.

## SZEREPED
- Telegram üzenetek megválaszolása
- Gyors kérdések kezelése
- Komplex feladatok delegálása work session-nek

## TELEGRAM VÁLASZ
Ha [TG @user conv:ID] üzenetet kapsz:
1. MINDIG használd: mcp__spaceos-knowledge__telegram_reply
2. MINDIG add meg: from_terminal: "root"
3. A conversation history KONTEXTUSKÉNT érkezik — használd!

## KOMPLEX FELADAT
Ha kód írás, fájl módosítás, hosszú munka kell:
→ mcp__spaceos-knowledge__request_work_session
→ "A kérést átadtam a work session-nek."

## NE CSINÁLJ
- NE írj kódot
- NE módosíts fájlokat
- NE dolgozz inbox feladatokon
```

---

## Részletes Design

### 1. Telegram Layer (változatlan, CLI-független)

```typescript
// telegramGateway.ts — NEM függ CLI-tól

interface TelegramGateway {
  // Bejövő üzenetek
  onMessage(handler: (msg: IncomingMessage) => void): void;

  // Kimenő üzenetek
  sendMessage(chatId: number, text: string): Promise<boolean>;

  // Bot-to-bot (multi-agent)
  sendToBot(targetBot: string, message: string): Promise<boolean>;
}

interface IncomingMessage {
  chatId: number;
  userId: number;
  username: string;
  text: string;
  messageId: number;
  receivedByBot: string;  // melyik bot kapta
}
```

### 2. Injection Layer (tmux-alapú, CLI-agnosztikus)

```typescript
// sessionInjector.ts — működik BÁRMILYEN CLI-val

interface SessionInjector {
  // Üzenet küldése tmux session-be KONTEXTUSSAL
  injectWithContext(
    sessionName: string,
    message: IncomingMessage,
    conversationHistory: ConversationMessage[]
  ): boolean;

  // Session létezik-e
  exists(sessionName: string): boolean;

  // Session indítása tetszőleges paranccsal
  start(sessionName: string, command: string, workdir: string): boolean;
}
```

**Kulcs:** A `tmux send-keys` működik BÁRMILYEN CLI-val, ami stdin-ről olvas.

### 3. Conversation Context Injection (ÚJ!)

```typescript
// contextBuilder.ts — Teljes conversation history formázása

function buildContextForInjection(
  newMessage: IncomingMessage,
  history: ConversationMessage[]
): string {
  const lines: string[] = [];

  // Header
  lines.push(`[TG @${newMessage.username} conv:${newMessage.conversationId}]`);
  lines.push('');

  // Conversation history (ha van)
  if (history.length > 0) {
    lines.push('--- Conversation History ---');
    for (const msg of history) {
      const prefix = msg.direction === 'in' ? `👤 @${newMessage.username}` : `🤖 ${msg.fromTerminal}`;
      const time = msg.createdAt.slice(11, 16); // HH:MM
      lines.push(`[${time}] ${prefix}: ${msg.content}`);
    }
    lines.push('--- End History ---');
    lines.push('');
  }

  // New message
  lines.push(`Új üzenet: ${newMessage.text}`);

  return lines.join('\n');
}

// Példa output:
// [TG @gabor conv:42]
//
// --- Conversation History ---
// [14:30] 👤 @gabor: Mi a státusza a backend-nek?
// [14:31] 🤖 root: Jelenleg IDLE, nincs aktív task.
// [14:35] 👤 @gabor: Indíts egy NuGet upcate-t
// [14:36] 🤖 root: request_work_session elküldve a Conductor-nak.
// --- End History ---
//
// Új üzenet: Hogy áll a NuGet?
```

### 4. DB Tárolás Flow

```
Telegram üzenet érkezik
         │
         ▼
┌─────────────────────────────┐
│  1. conversation_messages   │
│     INSERT (direction: in)  │
│     + conversation_id       │
│     + telegram_message_id   │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│  2. History lekérés         │
│     SELECT * FROM           │
│     conversation_messages   │
│     WHERE conv_id = ?       │
│     ORDER BY created_at     │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│  3. Context build + inject  │
│     tmux send-keys          │
│     → chat session          │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│  4. CLI válaszol            │
│     MCP telegram_reply      │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│  5. conversation_messages   │
│     INSERT (direction: out) │
│     + from_terminal         │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│  6. Telegram API send       │
└─────────────────────────────┘
```

### 3. Response Layer (több stratégia)

#### Opció A: MCP Tool (ha CLI támogatja)
```typescript
// Az agent MCP-n keresztül válaszol
mcp__spaceos-knowledge__telegram_reply({
  chat_id: 12345,
  message: "Válasz szövege",
  from_terminal: "backend"
});
```

#### Opció B: Stdout Capture (univerzális)
```typescript
// paneCapture.ts — tmux pane tartalom figyelése

interface PaneCapture {
  // Figyeli a session outputját
  watch(sessionName: string, pattern: RegExp, callback: (match: string) => void): void;
}

// Példa: [TELEGRAM_REPLY:12345] Válasz szövege
const REPLY_PATTERN = /\[TELEGRAM_REPLY:(\d+)\]\s*(.+)/;

paneCapture.watch('spaceos-backend', REPLY_PATTERN, (match) => {
  const [_, chatId, message] = match;
  telegram.sendMessage(parseInt(chatId), message);
});
```

#### Opció C: File-Based Response
```typescript
// Az agent ír egy fájlba, watcher küldi el
// terminals/backend/telegram-outbox/reply-12345.txt
```

---

## CLI Adapter Interface

```typescript
// cliAdapter.ts — CLI-specifikus konfiguráció

interface CLIAdapter {
  name: string;                          // "claude", "aider", "custom"
  startCommand: (model: string) => string;  // indító parancs
  supportsStdin: boolean;                // tud-e stdin-ről olvasni
  supportsMCP: boolean;                  // van-e MCP támogatás
  responseMethod: 'mcp' | 'stdout' | 'file';  // hogyan válaszol
  promptPrefix?: string;                 // opcionális prefix az injektált üzenethez
}

const ADAPTERS: Record<string, CLIAdapter> = {
  claude: {
    name: 'claude',
    startCommand: (model) => `claude --model ${model}`,
    supportsStdin: true,
    supportsMCP: true,
    responseMethod: 'mcp',
  },
  aider: {
    name: 'aider',
    startCommand: (model) => `aider --model ${model}`,
    supportsStdin: true,
    supportsMCP: false,
    responseMethod: 'stdout',
    promptPrefix: '/ask ',
  },
  custom: {
    name: 'custom',
    startCommand: () => './agent.sh',
    supportsStdin: true,
    supportsMCP: false,
    responseMethod: 'file',
  },
};
```

---

## Konfiguráció

```yaml
# config/terminals.yaml

terminals:
  root:
    cli: claude
    model: opus
    telegram_bot: sarkany_fekete_kod_bot

  backend:
    cli: claude
    model: sonnet
    telegram_bot: vasokol_fekete_kod_bot

  # Példa: custom agent
  experimental:
    cli: custom
    command: "./my-agent --config agent.yaml"
    telegram_bot: null  # nincs dedikált bot
    response_method: file
```

---

## Message Flow (CLI-agnosztikus)

### Telegram → Session

```
User: @vasokol fixeld a NuGet ugyét
         │
         ▼
   Telegram Gateway (webhook)
         │
         ▼
   Message Router
   - Melyik terminál? → backend
   - Van session? → igen
         │
         ▼
   Session Injector
   - tmux send-keys -t spaceos-backend '[TG @user] fixeld a NuGet ugyét'
         │
         ▼
   CLI (bármilyen) látja az üzenetet stdin-en
```

### Session → Telegram

```
CLI output: [TELEGRAM_REPLY:12345] Kész, NuGet frissítve!
         │
         ▼
   Pane Capture (stdout figyelés)
   VAGY
   MCP Tool hívás
   VAGY
   File watcher
         │
         ▼
   Telegram Gateway
   - sendMessage(12345, "Kész, NuGet frissítve!")
```

---

## Multi-Agent (Bot-to-Bot)

```typescript
// agentBridge.ts — terminálok közötti kommunikáció

interface AgentBridge {
  // Telegram-on keresztül
  sendViaTelegram(from: string, to: string, message: string): Promise<boolean>;

  // Direkt injection (gyorsabb, de nincs audit trail)
  sendDirect(from: string, to: string, message: string): boolean;

  // Broadcast minden terminálnak
  broadcast(from: string, message: string): Promise<void>;
}

// Használat:
bridge.sendViaTelegram('backend', 'frontend', 'API kész, tesztelheted');
// → @vasokol_bot küld @neon_bot-nak (ha bot-to-bot engedélyezve)
// → VAGY: injection spaceos-frontend session-be
```

---

## Implementációs Fázisok

### Phase 1: Chat Session Identity (CLAUDE-CHAT.md) ✅ DONE (2026-07-04)
- [x] `terminals/*/CLAUDE-CHAT.md` létrehozása minden terminálhoz (8 terminál)
- [x] Chat session starter módosítása: CLAUDE-CHAT.md használata
- [x] Work/Chat session naming convention: `spaceos-{terminal}` vs `spaceos-{terminal}-chat`

### Phase 2: Conversation Context Injection ✅ DONE (2026-07-04)
- [x] `contextBuilder.ts` létrehozása — history formázás, tmux escape
- [x] `injectTelegramWithContext()` implementálása chatSessionStarter.ts-ben
- [x] History lekérés integrálása (bejövő + kimenő a conversation_messages táblából)
- [ ] Teszt: conversation history megjelenik a CLI-ban (manual test szükséges)

### Phase 3: MCP Tool DB Integration ✅ DONE (2026-07-04)
- [x] `telegram_reply` tool módosítása: DB-be mentés (direction: out)
- [x] Conversation auto-create ha nincs conversationId
- [ ] `get_telegram_history` tool bővítése (in + out) — opcionális

### Phase 4: Multi-Agent Bridge — PENDING
- [ ] `agentBridge.ts` implementálása
- [ ] Bot-to-bot kommunikáció tesztelése
- [ ] Fallback: direkt injection

### Phase 5: CLI Adapter (opcionális, más CLI-hoz) — PENDING
- [ ] `cliAdapter.ts` interface
- [ ] Claude, Aider, custom agent támogatás
- [ ] stdout capture fallback (nem MCP-képes CLI-khoz)

---

## Módosítandó/Új Kód

```
spaceos-nexus/knowledge-service/src/
├── chatSessionStarter.ts          # MÓDOSÍT: CLAUDE-CHAT.md használat
├── telegram/
│   ├── contextBuilder.ts          # ÚJ: conversation history formázás
│   ├── conversationManager.ts     # MÓDOSÍT: out message tárolás
│   └── multiBotManager.ts         # MÓDOSÍT: context injection
├── mcp.ts                         # MÓDOSÍT: telegram_reply DB mentés

terminals/*/
├── CLAUDE.md                      # MARAD: work session identity
└── CLAUDE-CHAT.md                 # ÚJ: chat session identity
```

---

## Döntési Mátrix

| Szempont | Claude-függő | CLI-agnosztikus |
|----------|--------------|-----------------|
| Claude Code upgrade | ❌ Blokkolt | ✅ Független |
| Más CLI használata | ❌ Nem lehet | ✅ Bármilyen |
| Karbantartás | Közepes | Egyszerűbb |
| MCP támogatás | ✅ Natív | ⚠️ Csak ha CLI támogatja |

---

## Acceptance Criteria

- [ ] Telegram üzenet → bármilyen CLI session működik
- [ ] CLI válasz → Telegram működik (legalább 1 módszerrel)
- [ ] Chat session kód törölve
- [ ] Legalább 2 különböző CLI tesztelve (Claude + 1 másik)
- [ ] Bot-to-bot kommunikáció működik

---

## Hivatkozások

- [ADR-059: Telegram Claude Channels](./ADR-059-telegram-claude-channels-architecture.md) (BLOCKED)
- [Kutatás eredménye](../patterns/TELEGRAM_2026_RESEARCH.md)
- [Marveen projekt](https://github.com/Szotasz/marveen) — eredeti tmux injection minta
