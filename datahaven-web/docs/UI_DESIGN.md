# Datahaven Web Dashboard — UI Design Specification

## 1. Áttekintés

A Datahaven Web Dashboard egy real-time monitoring és management felület a Datahaven message queue rendszerhez. A cél egy tiszta, informatív, dark theme-alapú dashboard, ami gyorsan áttekinthetővé teszi a rendszer állapotát.

---

## 2. Design Rendszer

### 2.1 Színpaletta

```
┌─────────────────────────────────────────────────────────────┐
│  DARK THEME COLOR PALETTE                                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Backgrounds                                                │
│  ├── Primary    #0f1419   █████  Fő háttér                 │
│  ├── Secondary  #1a1f26   █████  Kártyák, panelek          │
│  └── Card       #242b33   █████  Kiemelt elemek            │
│                                                             │
│  Text                                                       │
│  ├── Primary    #e7e9ea   █████  Főszöveg                  │
│  └── Secondary  #8b98a5   █████  Másodlagos, címkék        │
│                                                             │
│  Accents                                                    │
│  ├── Blue       #1d9bf0   █████  Linkek, info, primary     │
│  ├── Green      #00ba7c   █████  Online, success, acked    │
│  ├── Yellow     #ffd400   █████  Warning, pending          │
│  ├── Red        #f4212e   █████  Error, critical, offline  │
│  └── Purple     #7856ff   █████  Score, highlight          │
│                                                             │
│  Border         #2f3336   █████  Szegélyek, elválasztók    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Tipográfia

```
Font Stack: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif

Heading 1 (H1)     24px / 700 weight  — Oldal címek
Heading 2 (H2)     16px / 600 weight  — Panel címek
Body               14px / 400 weight  — Törzsszöveg
Small              12px / 400 weight  — Meta info, badges
Micro              10px / 600 weight  — Priority/status tags
Monospace          14px / Menlo, Monaco, monospace — Payload, kód
```

### 2.3 Spacing & Layout

```
Base unit: 4px

Spacing scale:
  xs:  4px   (0.25rem)
  sm:  8px   (0.5rem)
  md:  16px  (1rem)
  lg:  24px  (1.5rem)
  xl:  32px  (2rem)
  2xl: 48px  (3rem)

Border radius:
  sm:  4px   — Badges, tags
  md:  6px   — Buttons, inputs
  lg:  12px  — Cards, panels

Max content width: 1600px
Panel max-height: 400px (scrollable)
```

---

## 3. Layout Struktúra

### 3.1 Teljes Oldal

```
┌─────────────────────────────────────────────────────────────────────┐
│ HEADER                                                         64px │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Logo + Title                               Connection Status    │ │
│ └─────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ MAIN CONTENT                                          padding: 32px │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ STATS GRID (4 kártya)                                           │ │
│ │ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                     │ │
│ │ │ Total  │ │Pending │ │ Acked  │ │Daemons │                     │ │
│ │ └────────┘ └────────┘ └────────┘ └────────┘                     │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ ┌────────────────────┐ ┌──────────────────────────────────────────┐ │
│ │ DAEMONS PANEL      │ │ MESSAGES PANEL                          │ │
│ │                    │ │                                          │ │
│ │ 1/3 szélesség      │ │ 2/3 szélesség                            │ │
│ │                    │ │                                          │ │
│ │ max-height: 400px  │ │ max-height: 400px                        │ │
│ └────────────────────┘ └──────────────────────────────────────────┘ │
│                                                                     │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ KNOWLEDGE SEARCH PANEL                                          │ │
│ │ Teljes szélesség                                                │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Reszponzív Breakpoints

```
Desktop:   > 1024px   — 2 oszlopos layout (1/3 + 2/3)
Tablet:    768-1024px — 1 oszlopos layout, teljes szélesség
Mobile:    < 768px    — 1 oszlopos, kisebb padding
```

---

## 4. Komponensek

### 4.1 Header

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│  ┌──────────────────────────────────┐  ┌──────────────────────────┐ │
│  │ Datahaven                        │  │ ● Connected              │ │
│  │ Message Queue Dashboard          │  │   vagy                   │ │
│  └──────────────────────────────────┘  │ ○ Disconnected           │ │
│                                        └──────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

- Logo/Title: bal oldalon, #1d9bf0 (blue) színű "Datahaven"
- Subtitle: szürke, kisebb betűméret
- Connection status: jobb oldalon, zöld/piros dot + szöveg
```

### 4.2 Stat Card

```
┌────────────────────────┐
│                        │
│    1,234               │  <- stat-value: 40px, bold
│    Total Messages      │  <- stat-label: 12px, uppercase, secondary
│                        │
└────────────────────────┘

Variánsok (színkódolt stat-value):
- Default:  fehér (#e7e9ea)
- Pending:  sárga (#ffd400)
- Acked:    zöld (#00ba7c)
- Daemons:  kék (#1d9bf0)

Kártya:
- Background: #242b33
- Border: 1px solid #2f3336
- Border-radius: 12px
- Padding: 24px
```

### 4.3 Panel (Card Container)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Panel Header                                               Actions  │
│─────────────────────────────────────────────────────────────────────│
│                                                                     │
│ Panel Content                                                       │
│ (scrollable, max-height: 400px)                                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

Panel Header:
- H2 cím bal oldalon
- Action buttons (Refresh, Filter dropdown) jobb oldalon
- Border-bottom: 1px solid #2f3336
- Padding: 16px 24px
```

### 4.4 Daemon List Item

```
┌─────────────────────────────────────────────────────────────────────┐
│ ●  kernel                                                      [3]  │
│    Backend kernel daemon                                            │
└─────────────────────────────────────────────────────────────────────┘

Elemek:
- Status dot: 10px, zöld (online) vagy piros (offline)
- Daemon ID: 14px, bold, primary color
- Description: 12px, secondary color
- Pending badge: sárga háttér, fekete szöveg, border-radius 12px
- Border-bottom: 1px solid #2f3336 (kivéve utolsó)
- Padding: 12px 0
```

### 4.5 Message List Item

```
┌─────────────────────────────────────────────────────────────────────┐
│ Build project                                              [HIGH]   │
│ conductor → kernel   ┃   PENDING   ┃   2024-06-19 12:34            │
└─────────────────────────────────────────────────────────────────────┘

Elemek:
- Subject: 14px, bold, primary — kattintható (modal megnyitás)
- Priority badge:
  - CRITICAL: piros háttér (#f4212e), fehér szöveg
  - HIGH: sárga háttér (#ffd400), fekete szöveg
  - MEDIUM: kék háttér (#1d9bf0), fehér szöveg
  - LOW: szürke háttér (#2f3336), szürke szöveg
- Meta sor: 12px, secondary color
  - from → to (nyíl: →)
  - Status badge (PENDING: sárga, ACKED: zöld)
  - Timestamp
- Hover: background #1a1f26, kurzor pointer
- Kattintásra: Message Detail Modal megnyílik
```

### 4.6 Priority & Status Badges

```
Priority Badges:
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ CRITICAL │ │  HIGH    │ │ MEDIUM   │ │  LOW     │
│ #f4212e  │ │ #ffd400  │ │ #1d9bf0  │ │ #2f3336  │
│ white    │ │ black    │ │ white    │ │ #8b98a5  │
└──────────┘ └──────────┘ └──────────┘ └──────────┘

Status Badges:
┌──────────┐ ┌──────────┐
│ PENDING  │ │  ACKED   │
│ #ffd400  │ │ #00ba7c  │
│ black    │ │ white    │
└──────────┘ └──────────┘

Stílus:
- Padding: 2px 8px
- Border-radius: 4px
- Font-size: 10px
- Font-weight: 600
- Text-transform: uppercase
```

### 4.7 Knowledge Search

```
┌─────────────────────────────────────────────────────────────────────┐
│ Knowledge Search                                    ● Online (441)  │
│─────────────────────────────────────────────────────────────────────│
│ ┌─────────────────────────────────────────────────────────┐ ┌─────┐ │
│ │ Search knowledge base...                                │ │Search│ │
│ └─────────────────────────────────────────────────────────┘ └─────┘ │
│─────────────────────────────────────────────────────────────────────│
│                                                                     │
│ docs/security/JWT_GUIDE.md                                          │
│ JWT (JSON Web Token) is a compact, URL-safe means of...            │
│ Score: 89.2%                                              #7856ff   │
│                                                                     │
│ docs/api/AUTH_PATTERNS.md                                           │
│ Authentication patterns used in the SpaceOS platform...            │
│ Score: 76.5%                                                        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

Elemek:
- Health status: jobb felső sarok, zöld/piros dot + doc count
- Search input: teljes szélesség - button, placeholder szöveg
- Search button: kék háttér (#1d9bf0), fehér szöveg
- Result item:
  - Source: kék (#1d9bf0), 12px
  - Content: secondary color, 14px, max 2 sor, ellipsis
  - Score: lila (#7856ff), 12px
```

### 4.8 Message Detail Modal

```
┌─────────────────────────────────────────────────────────────────────┐
│ Message Details                                                  ✕  │
│─────────────────────────────────────────────────────────────────────│
│                                                                     │
│ ID           42                                                     │
│ From         conductor                                              │
│ To           kernel                                                 │
│ Type         task                                                   │
│ Subject      Build project                                          │
│ Priority     [HIGH]                                                 │
│ Status       [PENDING]                                              │
│ Created      2024-06-19 12:34:56                                    │
│                                                                     │
│ Payload                                                             │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ {                                                               │ │
│ │   "action": "build",                                            │ │
│ │   "target": "spaceos-kernel"                                    │ │
│ │ }                                                               │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

Elemek:
- Modal overlay: rgba(0,0,0,0.7)
- Modal content: #242b33, max-width 600px, max-height 80vh
- Header: border-bottom, close button (✕) jobb oldalon
- Body: key-value párok, scrollable
- Payload: monospace font, #1a1f26 háttér, syntax highlight (opcionális)
```

### 4.9 Auth Overlay

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│                                                                     │
│              ┌─────────────────────────────────┐                    │
│              │                                 │                    │
│              │   Authentication Required       │                    │
│              │                                 │                    │
│              │   ┌─────────────────────────┐   │                    │
│              │   │ Enter token...          │   │                    │
│              │   └─────────────────────────┘   │                    │
│              │                                 │                    │
│              │   ┌─────────────────────────┐   │                    │
│              │   │        Login            │   │                    │
│              │   └─────────────────────────┘   │                    │
│              │                                 │                    │
│              │   Invalid token                 │  <- error (piros)  │
│              │                                 │                    │
│              └─────────────────────────────────┘                    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

Elemek:
- Overlay: rgba(0,0,0,0.9)
- Auth box: #242b33, padding 32px, max-width 400px
- Title: center, 20px
- Input: teljes szélesség, password type
- Button: teljes szélesség, kék háttér
- Error: piros szöveg (#f4212e), center
```

### 4.10 Buttons

```
Primary Button:
┌────────────────┐
│    Search      │  Background: #1d9bf0
└────────────────┘  Color: white
                    Hover: opacity 0.9
                    Padding: 12px 24px
                    Border-radius: 6px

Secondary Button:
┌────────────────┐
│   Refresh      │  Background: #1a1f26
└────────────────┘  Border: 1px solid #2f3336
                    Color: #e7e9ea
                    Hover: background #2f3336
                    Padding: 8px 16px
```

### 4.11 Form Elements

```
Text Input:
┌────────────────────────────────────────┐
│ Placeholder text...                    │
└────────────────────────────────────────┘
- Background: #1a1f26
- Border: 1px solid #2f3336
- Color: #e7e9ea
- Placeholder: #8b98a5
- Padding: 12px 16px
- Border-radius: 6px
- Focus: border-color #1d9bf0

Select Dropdown:
┌────────────────────────────────┐
│ All Status                   ▼ │
└────────────────────────────────┘
- Ugyanaz mint input
- Custom arrow icon
```

---

## 5. Interakciók

### 5.1 Real-time Updates (SSE)

```
Esemény: új üzenet érkezik
  1. Stats kártyák animáltan frissülnek (pulse/flash effekt)
  2. Message list tetejére új elem kerül (fade-in animáció)
  3. Daemon pending badge frissül

Animációk:
- Pulse: 0.3s scale transform 1.0 → 1.05 → 1.0
- Fade-in: 0.2s opacity 0 → 1
- Slide-in: 0.3s translateY(-10px) → 0
```

### 5.2 Connection Status

```
Connected:
- Zöld dot pulsál (subtle)
- "Connected" szöveg

Disconnected:
- Piros dot
- "Disconnected" szöveg
- Auto-reconnect 5 másodperc múlva (progress indicator opcionális)

Reconnecting:
- Szürke dot
- "Reconnecting..." szöveg (opcionális spinner)
```

### 5.3 Loading States

```
Panel loading:
┌─────────────────────────────────────────┐
│                                         │
│          Loading daemons...             │
│          (spinner vagy text)            │
│                                         │
└─────────────────────────────────────────┘

Empty state:
┌─────────────────────────────────────────┐
│                                         │
│          No messages found              │
│          (italic, secondary color)      │
│                                         │
└─────────────────────────────────────────┘

Error state:
┌─────────────────────────────────────────┐
│                                         │
│   Failed to load: Connection refused    │
│   (piros szöveg)                        │
│                                         │
└─────────────────────────────────────────┘
```

### 5.4 Hover & Focus States

```
Message item hover:
- Background: #1a1f26
- Cursor: pointer
- Subtle left border (opcionális): 3px solid #1d9bf0

Button hover:
- Primary: opacity 0.9
- Secondary: background #2f3336

Input focus:
- Border-color: #1d9bf0
- Outline: none
- Box-shadow: 0 0 0 2px rgba(29,155,240,0.2)
```

---

## 6. Ikonok

```
Használt ikonok (egyszerű SVG vagy Unicode):

● ○  — Status dots (filled/outline circle)
→    — Arrow (from → to)
✕    — Close button
▼    — Dropdown arrow
⟳    — Refresh/reload

Alternatíva: Heroicons, Lucide, vagy Phosphor icon set
```

---

## 7. Accessibility

```
Követelmények:
- Minimum kontrasztarány: 4.5:1 (WCAG AA)
- Keyboard navigation: Tab, Enter, Escape
- Focus visible: minden interaktív elem
- ARIA labels: button, modal, status
- Screen reader: live regions SSE frissítésekhez

Kontrasztok ellenőrzése:
- Primary text (#e7e9ea) on bg (#0f1419): 13.5:1 ✓
- Secondary text (#8b98a5) on bg (#0f1419): 6.2:1 ✓
- Blue accent (#1d9bf0) on bg (#0f1419): 5.8:1 ✓
```

---

## 8. Figma/Sketch Export Segédlet

```
Komponens hierarchia:

Dashboard
├── Header
│   ├── Logo
│   ├── Subtitle
│   └── ConnectionStatus
├── StatsGrid
│   └── StatCard (4x)
├── TwoColumnLayout
│   ├── DaemonsPanel
│   │   ├── PanelHeader
│   │   └── DaemonList
│   │       └── DaemonItem (n×)
│   └── MessagesPanel
│       ├── PanelHeader
│       │   └── FilterDropdown
│       └── MessageList
│           └── MessageItem (n×)
├── KnowledgePanel
│   ├── PanelHeader
│   │   └── HealthStatus
│   ├── SearchBox
│   │   ├── SearchInput
│   │   └── SearchButton
│   └── ResultsList
│       └── ResultItem (n×)
├── MessageModal
│   ├── ModalHeader
│   └── ModalBody
└── AuthOverlay
    └── AuthBox
```

---

## 9. Implementációs Megjegyzések

### Jelenlegi állapot vs. terv

A `public/css/styles.css` és `public/index.html` már implementálja ennek a specnek a nagy részét. Fejlesztési lehetőségek:

1. **Animációk hozzáadása** — Jelenleg nincsenek
2. **Loading spinnerek** — Jelenleg csak szöveg
3. **Keyboard navigation** — Részlegesen kész
4. **Mobile optimalizáció** — Alap breakpoint van, de finomítható

### CSS Custom Properties (már implementálva)

```css
:root {
  --bg-primary: #0f1419;
  --bg-secondary: #1a1f26;
  --bg-card: #242b33;
  --text-primary: #e7e9ea;
  --text-secondary: #8b98a5;
  --accent-blue: #1d9bf0;
  --accent-green: #00ba7c;
  --accent-yellow: #ffd400;
  --accent-red: #f4212e;
  --accent-purple: #7856ff;
  --border-color: #2f3336;
}
```
