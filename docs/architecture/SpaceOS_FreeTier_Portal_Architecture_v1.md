# SpaceOS — FreeTier Portal Frontend Architecture
## Anonymous Nesting Calculator · Magic-Link Auth · Upgrade Funnel

> **Verzió:** v1.0 — 2026-04-23
> **Státusz:** DRAFT — Architect spec, Gábor approval szükséges
> **Repo:** `spaceos-freetier-portal` (standalone React app, NEM Turborepo)
> **Domain:** `eszkozok.joinerytech.hu`
> **API backend:** `freetier.joinerytech.hu` (LIVE, 12 endpoint, cookie auth)
> **Stack:** React 18 + Vite + TypeScript + Tailwind CSS + React Query
> **Referencia portálok:** `spaceos-doorstar-portal/` (stack referencia), `design-portal/` (feature referencia)
> **Backend spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` (v4.5)

---

## 0. Architekturális döntések összefoglalója

| # | Döntés | Választás | Indoklás |
|---|--------|-----------|----------|
| FP-01 | Rendering | CSR (Vite SPA) + `<meta>` prerendering landing-re | Konzisztens a többi portállal; Google JavaScript-et renderel; SSR overkill v1-ben |
| FP-02 | State management | `@tanstack/react-query` v5 | Doorstar minta; server state cache + mutation invalidation; nincs extra state lib |
| FP-03 | Router | `react-router-dom` v7 | Doorstar minta; SPA fallback nginx-szel |
| FP-04 | Auth | Custom `AuthProvider` (cookie-based, NEM Keycloak) | FreeTier `ft_sess` HttpOnly cookie; nincs OIDC; saját session check |
| FP-05 | HTTP client | `axios` + interceptor | Doorstar minta; 401 → redirect to magic link |
| FP-06 | Validáció | `zod` v4 | Doorstar minta; SEC-08 limitek (500 part, 10000mm) client-side is |
| FP-07 | Form library | Natív React state (v1) | Kevés form (3: nesting input, magic-link email, upgrade); react-hook-form overkill |
| FP-08 | SVG nesting vizualizáció | `<svg>` direkt React JSX | Golden Rule #1: frontend rajzol; nincs 3D lib (v2 Three.js); lightweight |
| FP-09 | CSS framework | Tailwind CSS 3.x | Konzisztens; custom theme (JoineryTech brand) |
| FP-10 | Testing | Vitest + Testing Library + Playwright | Doorstar minta |
| FP-11 | Analytics | Plausible (self-hosted) | GDPR-compliant; `analytics.joinerytech.hu` |
| FP-12 | Captcha | Cloudflare Turnstile (invisible) | Backend már használja; cookieless, ingyenes |
| FP-13 | i18n | Nincs v1-ben (magyar only) | DACH = v2; i18n struktúra előkészítve de nem implementálva |
| FP-14 | Session check endpoint | `GET /api/freetier/auth/session` szükséges a backend-en | Jelenlegi API-ban nincs — page reload után a frontend nem tudja hogy érvényes-e a session |

---

## 1. Oldalstruktúra és routing

### 1.1 Route map

| Route | Oldal | Auth | Leírás |
|-------|-------|------|--------|
| `/` | `LandingPage` | anonymous | SEO landing + hero + kalkulátor CTA |
| `/kalkulator` | `CalculatorPage` | anonymous | Nesting input form + eredmény + "Mentsd el" CTA |
| `/kalkulator/eredmeny` | `ResultPage` | anonymous | SVG vizualizáció + statisztikák + PDF preview + CTA |
| `/auth/belepes` | `MagicLinkPage` | anonymous | Email input → magic link kérés |
| `/auth/ellenorzes` | `VerifyPendingPage` | anonymous | "Ellenőrizd az emailedet" tájékoztató |
| `/auth/verify` | `VerifyCallbackPage` | anonymous | Token verify → redirect /munkaterletek |
| `/munkaterletek` | `WorkspaceListPage` | **session** | Workspace lista + create |
| `/munkaterletek/:id` | `WorkspaceDetailPage` | **session** | Workspace részletek + revíziók + share + export |
| `/megosztott/:prefix/:token` | `SharedViewPage` | anonymous | Public share landing (token-stripping → cookie) |
| `/megosztott/nezet/:workspaceId` | `SharedWorkspacePage` | anonymous | Public share workspace view (token cookie-ből) |
| `/upgrade` | `UpgradePage` | **session** | Upgrade form (cégnév, adószám, stb.) |
| `/upgrade/koszonjuk` | `UpgradeThankYouPage` | **session** | Upgrade kérés visszaigazolás |
| `/*` | `NotFoundPage` | — | 404 |

### 1.2 Route struktúra kódban

```
src/
├── App.tsx                    # BrowserRouter + QueryClientProvider + AuthProvider
├── routes/
│   ├── index.tsx              # Route definitions
│   ├── ProtectedRoute.tsx     # Session guard → redirect /auth/belepes
│   └── AnonymousOnlyRoute.tsx # Ha már bejelentkezett → redirect /munkaterletek
```

### 1.3 ProtectedRoute pattern

```typescript
// Doorstar mintája adaptálva cookie auth-ra
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingSpinner />;
  if (!isAuthenticated) return <Navigate to="/auth/belepes" replace />;

  return <AppShell>{children}</AppShell>;
}
```

**Különbség Doorstar-tól:** Nincs Keycloak redirect — a `/auth/belepes` oldalra navigál.

---

## 2. UX / Conversion Funnel

### 2.1 Funnel fázisok

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1. LANDING (SEO)                                                    │
│    eszkozok.joinerytech.hu                                          │
│    "Ingyenes szabászat-optimalizáló a böngészőben"                  │
│    [Kipróbálom →]                                                    │
└────────────────────────┬────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 2. CALCULATOR (anonymous, 3/nap limit)                              │
│    Sheet méret + alkatrészek → [Optimalizálás]                      │
│    Turnstile invisible captcha                                      │
└────────────────────────┬────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 3. RESULT (anonymous, de CTA-k)                                     │
│    SVG vizualizáció + kihasználtság% + hulladék                     │
│    [📄 PDF letöltés] [💾 Mentés] [📤 Megosztás]                     │
│    "Mentsd el → regisztrálj email-lel" CTA                         │
└────────────────────────┬────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 4. MAGIC LINK (email capture — lead!)                               │
│    "Add meg az email címedet, küldünk egy belépési linket"          │
│    Turnstile captcha a formra                                       │
└────────────────────────┬────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 5. WORKSPACE (authenticated)                                        │
│    Mentett munkaterületek, revíziók, share, PDF export              │
│    [🚀 Upgrade — teljes funkciók a csapatodnak]                     │
└────────────────────────┬────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 6. UPGRADE (conversion goal — 5% target)                            │
│    Cégnév, adószám, kontakt → Slack webhook → manuális provisioning │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 CTA elhelyezési stratégia

| Hely | CTA | Trigger |
|------|-----|---------|
| Landing hero | "Kipróbálom ingyen" | Első látogatás |
| Result oldal (anonymous) | "Mentsd el az eredményt" | Sikeres nesting után |
| Result oldal (anonymous, 3. használat) | "Regisztrálj a korlátlan használathoz" | Rate limit közelít |
| Workspace lista (authenticated) | "Upgrade a csapatodnak" | Sidebar banner |
| Share landing | "Te is próbáld ki!" | Shared view footer |
| PDF footer | "Készült: eszkozok.joinerytech.hu" | Minden exportált PDF |

### 2.3 Plausible funnel mérési pontok

| Event | Trigger |
|-------|---------|
| `landing_view` | Landing page load |
| `calculator_open` | /kalkulator megnyitás |
| `nesting_submit` | POST /nest sikeres |
| `result_view` | Eredmény megjelenítés |
| `pdf_download` | PDF letöltés (anonymous) |
| `magic_link_request` | Email megadás |
| `magic_link_verify` | Sikeres verify |
| `workspace_save` | Workspace mentés |
| `share_generate` | Share token generálás |
| `upgrade_form_open` | /upgrade megnyitás |
| `upgrade_submit` | Upgrade kérés elküldve |

---

## 3. Nesting kalkulátor UX

### 3.1 Input form design

```
┌──────────────────────────────────────────────┐
│  📐 Alaplemez méret                          │
│  ┌──────────┐  ┌──────────┐  ┌────────────┐ │
│  │ Szélesség │  │ Magasság │  │ Mennyiség  │ │
│  │ 2800 mm   │  │ 2070 mm  │  │ 1          │ │
│  └──────────┘  └──────────┘  └────────────┘ │
│  ┌────────────────────────────────┐          │
│  │ Anyag (opcionális)             │          │
│  │ pl. MDF 18mm                   │          │
│  └────────────────────────────────┘          │
│                                              │
│  📦 Alkatrészek                              │
│  ┌────────┬────────┬────────┬───┬─────────┐ │
│  │ Név    │ Szél.  │ Mag.   │ db│ Erezetés│ │
│  ├────────┼────────┼────────┼───┼─────────┤ │
│  │Oldallap│ 500    │ 720    │ 2 │ ↕       │ │
│  │Polc    │ 460    │ 300    │ 4 │ ─       │ │
│  │[+ Sor hozzáadása]                       │ │
│  └─────────────────────────────────────────┘ │
│                                              │
│  🏷️ Címkézési stratégia                     │
│  ○ Teljes címke  ○ Csak név  ○ Csak méret   │
│  ○ Nincs címke                               │
│                                              │
│  [🔄 Optimalizálás]                          │
└──────────────────────────────────────────────┘
```

### 3.2 Validáció (SEC-08 limitek, client + server)

| Mező | Szabály | Hibaüzenet |
|------|---------|------------|
| Sheet szélesség | 1–10000 mm, integer | "A lemez szélessége 1 és 10000 mm között legyen" |
| Sheet magasság | 1–10000 mm, integer | "A lemez magassága 1 és 10000 mm között legyen" |
| Sheet mennyiség | 1–10 | "Maximum 10 lemez" |
| Part név | max 100 karakter | "A név maximum 100 karakter" |
| Part szélesség | 1–10000 mm, integer | "Az alkatrész szélessége 1 és 10000 mm között legyen" |
| Part magasság | 1–10000 mm, integer | "Az alkatrész magassága 1 és 10000 mm között legyen" |
| Part mennyiség | 1–999 | "Maximum 999 darab" |
| Part > sheet | szélesség ≤ sheet szélesség | "Az alkatrész nem lehet nagyobb a lemez méretnél" |
| Összes part | max 500 sor | "Maximum 500 alkatrész" |
| Grain direction | `vertical` / `horizontal` / `none` | — |

**Zod schema:**
```typescript
const nestingInputSchema = z.object({
  sheet: z.object({
    width_mm: z.number().int().min(1).max(10000),
    height_mm: z.number().int().min(1).max(10000),
    quantity: z.number().int().min(1).max(10),
    material: z.string().max(100).optional(),
  }),
  parts: z.array(z.object({
    name: z.string().max(100),
    width_mm: z.number().int().min(1).max(10000),
    height_mm: z.number().int().min(1).max(10000),
    quantity: z.number().int().min(1).max(999),
    grain_direction: z.enum(['vertical', 'horizontal', 'none']),
  })).min(1).max(500),
  label_strategy: z.enum(['FullLabel', 'PartNameOnly', 'DimensionOnly', 'NoLabel']),
});
```

### 3.3 Eredmény megjelenítés

```
┌──────────────────────────────────────────────────────────────────┐
│  ✅ Optimalizálás kész                                          │
│                                                                  │
│  ┌────────────────────────────────────┐  ┌──────────────────┐   │
│  │                                    │  │ Statisztikák     │   │
│  │    SVG VIZUALIZÁCIÓ                │  │                  │   │
│  │    (sheet layout + nested parts)   │  │ Kihasználtság:   │   │
│  │                                    │  │    ████████░ 87% │   │
│  │   ┌──────┐ ┌──────┐ ┌──────┐     │  │                  │   │
│  │   │Oldal │ │Oldal │ │ Polc │     │  │ Hulladék:        │   │
│  │   │ bal  │ │jobb  │ │      │     │  │    243 200 mm²   │   │
│  │   │      │ │      │ ├──────┤     │  │                  │   │
│  │   │      │ │      │ │ Polc │     │  │ Lemezek: 1 / 1   │   │
│  │   └──────┘ └──────┘ └──────┘     │  │ Alkatrészek: 6   │   │
│  │   ┌──────┐ ┌──────┐              │  │                  │   │
│  │   │ Polc │ │ Polc │   [hulladék] │  │ Vágási hossz:    │   │
│  │   └──────┘ └──────┘              │  │    4 120 mm      │   │
│  └────────────────────────────────────┘  └──────────────────┘   │
│                                                                  │
│  [📄 PDF letöltés]  [💾 Mentés munkaterületre]  [📤 Megosztás]  │
│                                                                  │
│  ─── Nem regisztráltál még? ──────────────────────────────────── │
│  Mentsd el az eredményedet, hogy bármikor visszanézhesd.         │
│  [Regisztrálok email-lel →]                                      │
└──────────────────────────────────────────────────────────────────┘
```

### 3.4 SVG nesting vizualizáció komponens

```typescript
// components/NestingVisualization.tsx
interface NestingVisualizationProps {
  sheets: SheetResult[];           // Backend válasz
  selectedSheet: number;           // Aktív sheet index
  labelStrategy: LabelStrategy;
  onSheetSelect: (index: number) => void;
}

// SVG rendering:
// - viewBox skálázás a sheet méretéhez
// - Alkatrészek: <rect> + <text> címke (LabelStrategy alapján)
// - Hulladék terület: szürke kitöltés
// - Grain direction: nyíl ikon az alkatrészen
// - Hover: alkatrész kijelölés + tooltip (név, méret, position)
// - Responsív: width="100%" + preserveAspectRatio
```

---

## 4. Auth flow (cookie-based, NEM Keycloak)

### 4.1 Auth state management

```typescript
// context/AuthContext.tsx
interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: FreeTierUser | null;      // { id, email, status }
}

// AuthProvider:
// 1. Mount → GET /api/freetier/auth/session (FP-14 — BACKEND HIÁNY!)
//    - 200 + user JSON → isAuthenticated = true
//    - 401 / no ft_sess cookie → isAuthenticated = false
// 2. Verify callback → POST /api/freetier/auth/verify
//    - 200 → Set-Cookie ft_sess → refetch session → redirect /munkaterletek
// 3. Logout → DELETE /api/freetier/auth/session (cookie törlés)
//    - Redirect → /
```

### 4.2 Hiányzó backend endpoint: `GET /api/freetier/auth/session`

**Probléma:** A jelenlegi API-ban nincs session check endpoint. Page reload után a frontend nem tudja, hogy a `ft_sess` cookie érvényes-e. Az egyetlen opció: egy protected endpointot hívni és 401-et kezelni — de ez fragilis és szemantikailag hibás.

**Javasolt megoldás — új endpoint a backend-en:**

```
GET /api/freetier/auth/session
  Cookie: ft_sess=<nonce>
  Response 200: { "userId": "uuid", "email": "user@example.com", "status": "ACTIVE", "expiresAt": "ISO" }
  Response 401: session expired / invalid
```

**Effort:** ~0.25 nap (backend oldalon: Redis lookup → user fetch → DTO return).

### 4.3 Magic link flow UI

```
1. /auth/belepes
   ┌──────────────────────────────────────┐
   │  🔗 Belépés email-lel               │
   │                                      │
   │  ┌──────────────────────────────┐    │
   │  │ Email cím                    │    │
   │  │ pelda@email.hu               │    │
   │  └──────────────────────────────┘    │
   │  [Turnstile widget]                  │
   │  [Belépési link küldése →]           │
   │                                      │
   │  Nincs szükség jelszóra.             │
   │  Küldünk egy egyszer használatos     │
   │  belépési linket az email címedre.   │
   └──────────────────────────────────────┘

2. /auth/ellenorzes
   ┌──────────────────────────────────────┐
   │  📬 Ellenőrizd az emailedet          │
   │                                      │
   │  Küldtünk egy belépési linket:       │
   │  pe***@email.hu                      │
   │                                      │
   │  Keresd a "JoineryTech belépés"      │
   │  tárgyú levelet. A link 15 percig    │
   │  érvényes.                           │
   │                                      │
   │  [Nem kaptam meg → újraküldés]       │
   └──────────────────────────────────────┘

3. /auth/verify?token=<base64url>
   ┌──────────────────────────────────────┐
   │  ⏳ Bejelentkezés...                 │
   │  (auto POST /api/freetier/auth/verify)
   │                                      │
   │  Siker → redirect /munkaterletek     │
   │  Hiba → "A link lejárt vagy már      │
   │          felhasználtad. Kérj újat."  │
   │          [Új link kérése →]          │
   └──────────────────────────────────────┘
```

### 4.4 401 handling (session expired mid-use)

```typescript
// api/client.ts — Axios response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Session expired — clear auth state, redirect
      authStore.clear();
      window.location.href = '/auth/belepes?expired=1';
    }
    return Promise.reject(error);
  }
);
```

---

## 5. SEO stratégia

### 5.1 Döntés: CSR + meta tag-ek (FP-01)

**Miért CSR és nem SSR/SSG:**
- Stack konzisztencia (Doorstar, Design Portal = mind CSR)
- Google Crawler 2024+ JavaScript-et renderel (Web Rendering Service)
- A landing page-nek nincs dinamikus tartalma — fix HTML + meta-k elegendőek
- Next.js bevezetése egyedül SEO miatt aránytalanul komplex lenne
- v2-ben SSG (vite-ssg) bevezethető ha szükséges

### 5.2 Meta tag-ek (index.html + React Helmet)

```html
<!-- index.html -->
<title>Ingyenes szabászat-optimalizáló | JoineryTech Eszközök</title>
<meta name="description" content="Optimalizáld a lapszabászati tervedet ingyen, a böngésződben. Hulladékcsökkentés, automatikus elrendezés, PDF export." />
<meta name="keywords" content="szabászat optimalizáló, lapszabász, nesting, hulladékcsökkentés, bútoripari szoftver" />

<!-- Open Graph -->
<meta property="og:title" content="Ingyenes szabászat-optimalizáló | JoineryTech" />
<meta property="og:description" content="Optimalizáld a szabászatodat ingyen. Próbáld ki regisztráció nélkül." />
<meta property="og:type" content="website" />
<meta property="og:url" content="https://eszkozok.joinerytech.hu" />
<meta property="og:image" content="https://eszkozok.joinerytech.hu/og-image.png" />
<meta property="og:locale" content="hu_HU" />

<!-- Twitter Card -->
<meta name="twitter:card" content="summary_large_image" />
```

### 5.3 Structured data (JSON-LD)

```json
{
  "@context": "https://schema.org",
  "@type": "WebApplication",
  "name": "JoineryTech Szabászat-optimalizáló",
  "url": "https://eszkozok.joinerytech.hu",
  "applicationCategory": "BusinessApplication",
  "operatingSystem": "Web",
  "offers": {
    "@type": "Offer",
    "price": "0",
    "priceCurrency": "HUF"
  },
  "description": "Ingyenes szabászat-optimalizáló a böngészőben. Hulladékcsökkentés, PDF export, munkaterület mentés."
}
```

### 5.4 robots.txt és sitemap.xml

```
# robots.txt
User-agent: *
Allow: /
Disallow: /auth/
Disallow: /munkaterletek/
Disallow: /upgrade/
Sitemap: https://eszkozok.joinerytech.hu/sitemap.xml
```

```xml
<!-- sitemap.xml (statikus, Vite plugin generálja) -->
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://eszkozok.joinerytech.hu/</loc>
    <changefreq>monthly</changefreq>
    <priority>1.0</priority>
  </url>
  <url>
    <loc>https://eszkozok.joinerytech.hu/kalkulator</loc>
    <changefreq>monthly</changefreq>
    <priority>0.9</priority>
  </url>
</urlset>
```

---

## 6. Responsive design

### 6.1 Breakpoints (Tailwind defaults)

| Breakpoint | Méret | Fő layout |
|------------|-------|-----------|
| `sm` | ≥640px | Mobile landscape |
| `md` | ≥768px | Tablet |
| `lg` | ≥1024px | Desktop |
| `xl` | ≥1280px | Wide desktop |

### 6.2 Mobile-first layout stratégia

| Elem | Mobile (<768px) | Desktop (≥1024px) |
|------|-----------------|-------------------|
| Nesting form | Egymás alatt: sheet → parts table → button | Kétoszlopos: bal form, jobb preview |
| Result | SVG felül, stats alatta | SVG bal (60%), stats jobb (40%) |
| Part input table | Kardalakú: név felül, méretek alatta | Sor: név · szél · mag · db · irány · [x] |
| Workspace lista | Card layout | Table layout |
| CTA | Full-width gomb | Inline gomb |

### 6.3 Touch-friendly input

- Input mezők: min `h-12` (48px) — WCAG touch target
- Part "törlés" gomb: `h-10 w-10` (40px) piros ikon
- Sheet számok: `type="number"` + `inputMode="numeric"` + spinner arrows
- Mobile: sheet preset gombok (2800x2070, 2440x1220, stb.) — a leggyakoribb méretekre
- Swipe: workspace card lista balra húzás → "Törlés" akció (v2)

---

## 7. Brand és design

### 7.1 Színpaletta (Tailwind theme extend)

```javascript
// tailwind.config.js
module.exports = {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50:  '#f0fdf4',    // lightest green
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e',    // primary CTA
          600: '#16a34a',    // primary hover
          700: '#15803d',    // heading text
          800: '#166534',
          900: '#14532d',    // dark background
        },
        surface: {
          DEFAULT: '#ffffff',
          muted: '#f8fafc',    // slate-50
          border: '#e2e8f0',   // slate-200
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],    // mm értékekhez
      },
    },
  },
  plugins: [],
};
```

**Brand döntések:**
- **Zöld** elsődleges szín — "fenntarthatóság, hulladékcsökkentés" üzenet
- **Inter** font — modern, olvasható, ingyenes (Google Fonts CDN)
- **JetBrains Mono** — méret értékekhez (mm, %, darab) — technikai precizitás
- Doorstar logó **nem jelenik meg** — ez JoineryTech brand, nem Doorstar

### 7.2 Logo és header

```
┌─────────────────────────────────────────────────────────────┐
│  🔧 JoineryTech Eszközök          [Kalkulátor] [Belépés]   │
│  ───────────────────────────────────────────────────────────│
```

- Logo: `JoineryTech` szöveg + eszköz ikon (SVG, nem raszterkép)
- Navbar: sticky top, `bg-white shadow-sm`, responsive hamburger mobile-on
- Authenticated navbar: `[Munkaterületek] [Upgrade] [user@email.hu ▼]`

---

## 8. Analytics és tracking

### 8.1 Plausible integráció

```html
<!-- index.html -->
<script defer data-domain="eszkozok.joinerytech.hu"
  src="https://analytics.joinerytech.hu/js/script.js"></script>
```

### 8.2 Egyedi event tracking

```typescript
// utils/analytics.ts
export function trackEvent(name: string, props?: Record<string, string>) {
  if (window.plausible) {
    window.plausible(name, { props });
  }
}

// Használat:
trackEvent('nesting_submit', { parts_count: '12', sheet_count: '1' });
trackEvent('magic_link_request');
trackEvent('upgrade_submit', { company: 'redacted' });
```

### 8.3 Funnel dashboard (Plausible Goals)

| Goal | Mérési pont | Konverzió |
|------|-------------|-----------|
| Landing → Calculator | `calculator_open` / `landing_view` | ~40% target |
| Calculator → Result | `nesting_submit` / `calculator_open` | ~70% target |
| Result → Magic Link | `magic_link_request` / `result_view` | ~15% target |
| Magic Link → Workspace | `workspace_save` / `magic_link_verify` | ~60% target |
| Workspace → Upgrade | `upgrade_submit` / unique authenticated users | ~5% target |

---

## 9. External integráció

### 9.1 Cloudflare Turnstile

| Form | Turnstile mód | Miért |
|------|---------------|-------|
| `POST /nest` (kalkulátor) | **Invisible** (managed) | Ne akadályozza a fő UX-ot; bot védelem elegendő |
| `POST /auth/magic-link` | **Visible** widget | Email bombing megelőzés; explicit felhasználói interakció |
| `POST /upgrade` | **Invisible** | Ritka akció, ne legyen súrlódás |

```typescript
// components/TurnstileWidget.tsx
// @marsidev/react-turnstile (MIT, 2.5KB)
import { Turnstile } from '@marsidev/react-turnstile';

<Turnstile
  siteKey={import.meta.env.VITE_TURNSTILE_SITE_KEY}
  onSuccess={(token) => setTurnstileToken(token)}
  options={{ theme: 'light', size: 'normal' }}
/>
```

**Token küldése:** `cf-turnstile-response` header-ben a POST requesthez.

### 9.2 Plausible

Lásd §8.1 — self-hosted `analytics.joinerytech.hu`, `<script>` tag.

---

## 10. Security

### 10.1 CSP policy (nginx)

```nginx
add_header Content-Security-Policy "
  default-src 'self';
  script-src 'self' https://challenges.cloudflare.com https://analytics.joinerytech.hu;
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: blob:;
  connect-src 'self' https://freetier.joinerytech.hu https://challenges.cloudflare.com;
  frame-src https://challenges.cloudflare.com;
  font-src 'self' https://fonts.gstatic.com;
  object-src 'none';
  base-uri 'self';
" always;
```

### 10.2 További security headerek (nginx)

```nginx
add_header X-Frame-Options "DENY" always;
add_header X-Content-Type-Options "nosniff" always;
add_header Referrer-Policy "no-referrer" always;              # D-19: share token leak megelőzés
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
```

### 10.3 XSS megelőzés

- React JSX: default escaping — `{userInput}` automatikusan escaped
- **SOHA** `dangerouslySetInnerHTML` — nincs use case rá
- SVG: alkatrész nevek escaped render előtt (React `<text>` elemben)
- Zod validáció: input sanitization mielőtt backend-re megy

### 10.4 CORS és Cookie SameSite

**Probléma:** A frontend (`eszkozok.joinerytech.hu`) és a backend API (`freetier.joinerytech.hu`) **különböző subdomain**. A `ft_sess` cookie `SameSite=Lax` beállítású — ez same-site de cross-origin:

- `joinerytech.hu` = same site → **`SameSite=Lax` működik cross-subdomain!**
- Cookie `Domain=.joinerytech.hu` beállítással mindkét subdomain-en érvényes
- `credentials: 'include'` kell az axios/fetch hívásokban

**Backend CORS config:**
```
Access-Control-Allow-Origin: https://eszkozok.joinerytech.hu
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: GET, POST, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, cf-turnstile-response
```

### 10.5 Env var-ok (soha nem secret)

```
VITE_API_BASE_URL=https://freetier.joinerytech.hu
VITE_TURNSTILE_SITE_KEY=0x4AAAA...   # publikus site key, NEM secret key
VITE_PLAUSIBLE_DOMAIN=eszkozok.joinerytech.hu
```

**VITE_ prefix:** Vite beépíti a bundle-be — soha nem kerülhet ide secret!

---

## 11. Testing stratégia

### 11.1 Teszt piramis

| Szint | Tool | Scope | Target |
|-------|------|-------|--------|
| Unit | Vitest + Testing Library | Komponensek, hookok, utils | ~50 |
| Integration | Vitest + MSW | API hívások, auth flow, form submission | ~25 |
| E2E | Playwright | Teljes funnel, cross-page flow | ~10 |
| **Total** | | | **~85** |

### 11.2 Unit test prioritás

| Komponens | Teszt focus | db |
|-----------|-------------|-----|
| `NestingInputForm` | Validáció, part hozzáadás/törlés, submit | ~8 |
| `NestingVisualization` | SVG render, label strategy, sheet select | ~6 |
| `AuthProvider` | Session check, login/logout state | ~6 |
| `ProtectedRoute` | Redirect, loading state | ~4 |
| `WorkspaceList` | Empty state, loading, error | ~4 |
| `UpgradeForm` | Validáció, submit | ~4 |
| `useNesting` hook | Submit, loading, error, rate limit | ~4 |
| `useAuth` hook | Session lifecycle | ~4 |
| `useWorkspaces` hook | CRUD, invalidation | ~4 |
| `analytics` util | Event tracking, plausible mock | ~3 |
| Zod schemas | Edge cases, invalid input | ~3 |

### 11.3 E2E teszt szcenáriók (Playwright)

| # | Szcenárió | Prioritás |
|---|-----------|-----------|
| 1 | Anonymous nesting: form → submit → result SVG → PDF download | P0 |
| 2 | Magic link: email → verify → redirect to workspaces | P0 |
| 3 | Workspace save: auth → nesting → save → workspace list shows it | P0 |
| 4 | Share: generate link → open in incognito → share view renders | P1 |
| 5 | Upgrade: fill form → submit → thank you page | P1 |
| 6 | Rate limit: 4th anonymous nesting → error message | P1 |
| 7 | Session expired: 401 → redirect to /auth/belepes?expired=1 | P1 |
| 8 | Mobile: full flow on 375px viewport | P2 |
| 9 | SEO: landing page meta tags, robots.txt, sitemap.xml | P2 |
| 10 | Accessibility: keyboard navigation, screen reader | P2 |

### 11.4 MSW (Mock Service Worker) setup

```typescript
// mocks/handlers.ts
export const handlers = [
  http.post('/api/freetier/nest', () => {
    return HttpResponse.json(mockNestingResult);
  }),
  http.get('/api/freetier/auth/session', () => {
    return HttpResponse.json(mockUser);
  }),
  http.post('/api/freetier/auth/magic-link', () => {
    return new HttpResponse(null, { status: 202 });
  }),
  // ...
];
```

---

## 12. Deploy topológia

### 12.1 Build pipeline

```bash
# Build
cd /opt/spaceos/spaceos-freetier-portal
npm ci
npm run build    # → dist/

# Deploy
sudo rm -rf /opt/spaceos/spaceos-freetier-portal/dist.bak
sudo cp -r /opt/spaceos/spaceos-freetier-portal/dist \
           /opt/spaceos/spaceos-freetier-portal/dist.bak
# ... új build másolása ...
```

### 12.2 Nginx vhost

```nginx
# /etc/nginx/sites-available/spaceos-freetier-portal
server {
    listen 443 ssl http2;
    server_name eszkozok.joinerytech.hu;

    ssl_certificate     /etc/letsencrypt/live/joinerytech.hu/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/joinerytech.hu/privkey.pem;

    # Security headers (§10.1, §10.2)
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' https://challenges.cloudflare.com https://analytics.joinerytech.hu; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self' https://freetier.joinerytech.hu https://challenges.cloudflare.com; frame-src https://challenges.cloudflare.com; font-src 'self' https://fonts.gstatic.com; object-src 'none'; base-uri 'self';" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "no-referrer" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;

    # Static files root
    root /opt/spaceos/spaceos-freetier-portal/dist;

    # Hashed assets — immutable cache
    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # SPA fallback (react-router)
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Rate limit zone
    limit_req_zone $binary_remote_addr zone=freetier_static:1m rate=50r/s;
}

# HTTP → HTTPS redirect
server {
    listen 80;
    server_name eszkozok.joinerytech.hu;
    return 301 https://$host$request_uri;
}
```

**Fontos:** Az API proxy **NEM** ebben a vhost-ban van — a frontend közvetlenül hívja `https://freetier.joinerytech.hu`-t (CORS). Ez eltér a Doorstar mintától (ahol `/bff/` proxy van).

### 12.3 DNS

```
eszkozok.joinerytech.hu.  A  109.122.222.198
```

### 12.4 Certbot

A `joinerytech.hu` wildcard cert-et használja (vagy SAN bővítés):
```bash
certbot certonly --nginx -d eszkozok.joinerytech.hu
# VAGY ha a meglévő cert-hez adjuk:
certbot certonly --expand -d joinerytech.hu -d eszkozok.joinerytech.hu -d freetier.joinerytech.hu
```

---

## 13. Fejlesztési fázisolás

### Phase 1 — MVP (launch-hoz szükséges)

| # | Feature | Effort | Prioritás |
|---|---------|--------|-----------|
| 1 | Repo scaffold (Vite + React + TS + Tailwind + config) | 0.5 nap | P0 |
| 2 | Landing page (hero, CTA, meta tags, JSON-LD) | 1.0 nap | P0 |
| 3 | Nesting calculator form (input, validáció, Turnstile) | 2.0 nap | P0 |
| 4 | SVG nesting vizualizáció + result oldal | 2.0 nap | P0 |
| 5 | Magic link auth flow (email, verify, AuthProvider) | 1.5 nap | P0 |
| 6 | Workspace CRUD (list, detail, save, revisions) | 2.0 nap | P0 |
| 7 | Share (generate, revoke, public view) | 1.5 nap | P0 |
| 8 | Upgrade form | 0.5 nap | P0 |
| 9 | Responsive design + mobile | 1.0 nap | P0 |
| 10 | Nginx vhost + deploy + DNS | 0.5 nap | P0 |
| 11 | Unit tests (~50) | 2.0 nap | P0 |
| 12 | E2E tests (~5 P0 szcenárió) | 1.0 nap | P0 |
| 13 | Plausible analytics + funnel events | 0.5 nap | P1 |
| **Phase 1 total** | | **16.0 nap** | |

### Phase 2 — Post-launch

| # | Feature | Effort | Trigger |
|---|---------|--------|---------|
| 14 | Sheet preset selector (gyakori méretek) | 0.5 nap | User feedback |
| 15 | PDF export frontend (preview + download) | 1.0 nap | Backend PDF endpoint kész |
| 16 | i18n (EN mirror) | 2.0 nap | DACH belépés (2027) |
| 17 | SSG landing pages (vite-ssg) | 1.0 nap | SEO metrics mutatják hogy kell |
| 18 | Dark mode | 0.5 nap | User feedback |
| 19 | Import/export (JSON file upload) | 1.0 nap | Power user kérés |
| 20 | E2E tests bővítés (~5 P2) | 0.5 nap | Phase 1 stabil |
| 21 | Accessibility audit (WCAG 2.1 AA) | 1.0 nap | Legal requirement |
| 22 | Three.js 3D visualization | 3.0 nap | v2 scope |
| **Phase 2 total** | | **~10.5 nap** | |

### Phase 1 napi ütemterv

| Nap | Feladat | Output |
|-----|---------|--------|
| 1 | Repo scaffold + Vite config + Tailwind theme + ESLint + CLAUDE.md | Üres app fut |
| 2 | Landing page + router setup + meta tags + sitemap + robots.txt | SEO-ready landing |
| 3-4 | Nesting calculator form + Zod validáció + Turnstile widget | Form működik |
| 5-6 | SVG vizualizáció + result page + stat cards + label strategy | Teljes calc UX |
| 7 | AuthProvider + magic link pages + verify callback | Auth flow E2E |
| 8 | 401 interceptor + ProtectedRoute + session check | Session management |
| 9-10 | Workspace list + detail + save + revision history | CRUD kész |
| 11 | Share generate + revoke + public share view | Share flow kész |
| 12 | Upgrade form + responsive pass (mobile layout) | Mobile kész |
| 13 | Nginx vhost + certbot + DNS + deploy script + smoke test | LIVE |
| 14-15 | Unit tests (50) + Vitest setup + MSW mocks | Test coverage |
| 16 | E2E tests (5 szcenárió) + Playwright setup + Plausible events | QA done |

---

## 14. Projekt struktúra

```
spaceos-freetier-portal/
├── CLAUDE.md                          # Terminál szabályok
├── README.md
├── package.json
├── vite.config.ts
├── tailwind.config.js
├── tsconfig.json
├── tsconfig.app.json
├── postcss.config.js
├── index.html                         # SPA entry + meta tags + JSON-LD
├── public/
│   ├── robots.txt
│   ├── sitemap.xml
│   ├── og-image.png                   # Open Graph preview image
│   └── favicon.svg
├── src/
│   ├── main.tsx                       # ReactDOM entry
│   ├── App.tsx                        # Providers + Router
│   ├── routes/
│   │   ├── index.tsx                  # Route definitions
│   │   ├── ProtectedRoute.tsx
│   │   └── AnonymousOnlyRoute.tsx
│   ├── pages/
│   │   ├── LandingPage.tsx
│   │   ├── CalculatorPage.tsx
│   │   ├── ResultPage.tsx
│   │   ├── MagicLinkPage.tsx
│   │   ├── VerifyPendingPage.tsx
│   │   ├── VerifyCallbackPage.tsx
│   │   ├── WorkspaceListPage.tsx
│   │   ├── WorkspaceDetailPage.tsx
│   │   ├── SharedViewPage.tsx
│   │   ├── SharedWorkspacePage.tsx
│   │   ├── UpgradePage.tsx
│   │   ├── UpgradeThankYouPage.tsx
│   │   └── NotFoundPage.tsx
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppShell.tsx           # Header + main + footer
│   │   │   ├── Navbar.tsx
│   │   │   ├── Footer.tsx
│   │   │   └── LoadingSpinner.tsx
│   │   ├── calculator/
│   │   │   ├── SheetInput.tsx
│   │   │   ├── PartsTable.tsx
│   │   │   ├── PartRow.tsx
│   │   │   ├── LabelStrategySelector.tsx
│   │   │   └── SheetPresets.tsx        # Phase 2
│   │   ├── visualization/
│   │   │   ├── NestingVisualization.tsx
│   │   │   ├── SheetSvg.tsx
│   │   │   ├── PartRect.tsx
│   │   │   └── StatCards.tsx
│   │   ├── workspace/
│   │   │   ├── WorkspaceCard.tsx
│   │   │   ├── RevisionList.tsx
│   │   │   └── ShareManager.tsx
│   │   ├── auth/
│   │   │   ├── MagicLinkForm.tsx
│   │   │   └── SessionExpiredBanner.tsx
│   │   └── common/
│   │       ├── Button.tsx
│   │       ├── Input.tsx
│   │       ├── Card.tsx
│   │       ├── Badge.tsx
│   │       ├── Modal.tsx
│   │       ├── ErrorBoundary.tsx
│   │       └── TurnstileWidget.tsx
│   ├── context/
│   │   └── AuthContext.tsx
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useNesting.ts
│   │   ├── useWorkspaces.ts
│   │   ├── useWorkspace.ts
│   │   ├── useRevisions.ts
│   │   ├── useShare.ts
│   │   └── useUpgrade.ts
│   ├── api/
│   │   ├── client.ts                  # Axios instance + 401 interceptor
│   │   ├── nestingApi.ts
│   │   ├── authApi.ts
│   │   ├── workspacesApi.ts
│   │   ├── shareApi.ts
│   │   └── upgradeApi.ts
│   ├── schemas/
│   │   ├── nestingInput.ts            # Zod schema (SEC-08)
│   │   ├── magicLink.ts
│   │   └── upgradeRequest.ts
│   ├── types/
│   │   ├── api.ts                     # API response types
│   │   ├── nesting.ts                 # NestingInput, NestingResult, SheetResult
│   │   └── workspace.ts
│   ├── utils/
│   │   ├── analytics.ts              # Plausible wrapper
│   │   └── format.ts                 # mm formatter, % formatter
│   └── styles/
│       └── globals.css               # @tailwind directives + Inter font
├── tests/
│   ├── setup.ts                      # Vitest setup + MSW
│   ├── mocks/
│   │   ├── handlers.ts               # MSW request handlers
│   │   ├── data.ts                   # Mock responses
│   │   └── server.ts                 # MSW setupServer
│   ├── unit/                         # Component + hook tests
│   └── e2e/                          # Playwright specs
│       ├── playwright.config.ts
│       └── specs/
├── .env.example
├── .env.production
└── .gitignore
```

---

## 15. Nyitott kérdések / ADR jelöltek

| # | Kérdés | Hatás | Javaslat |
|---|--------|-------|----------|
| **FP-14** | `GET /api/freetier/auth/session` endpoint hiányzik a backend-en | Page reload után a frontend nem tudja ellenőrizni a session érvényességét; "próba 401" fragilis | Backend bővítés szükséges (~0.25 nap) — alternatíva: first protected request 401 check, de ez UX flicker-t okoz |
| **FP-15** | CORS config a FreeTier API-n | `freetier.joinerytech.hu` → `eszkozok.joinerytech.hu` cross-origin; cookie küldéshez `credentials: include` + explicit `Access-Control-Allow-Origin` kell (nem `*`) | Backend CORS middleware bővítés |
| **FP-16** | Cookie domain: `.joinerytech.hu` | Ha a `ft_sess` cookie domain-je `freetier.joinerytech.hu`, a frontend (`eszkozok.`) nem küldi. `Domain=.joinerytech.hu`-ra kell állítani | Backend cookie beállítás módosítás |
| **FP-17** | `DELETE /api/freetier/auth/session` (logout) hiányzik | A jelenlegi API-ban nincs explicit logout endpoint; client-side cookie törlés nem elegendő (Redis session marad) | Backend bővítés szükséges |
| **FP-18** | PDF letöltés frontend trigger | `GET /workspaces/{id}/export/pdf` binary response → blob download. A backend BE-21 `ExportWorkspacePdfQuery` implementálandó | Backend dependency |

---

## 16. Összefoglaló — Dependencies

### Backend bővítés szükséges (Portal előfeltétel)

| Endpoint | Miért | Effort |
|----------|-------|--------|
| `GET /api/freetier/auth/session` | Session check page reload-ra (FP-14) | 0.25 nap |
| `DELETE /api/freetier/auth/session` | Explicit logout (FP-17) | 0.25 nap |
| CORS middleware (`eszkozok.joinerytech.hu`) | Cross-origin API hívások (FP-15) | 0.25 nap |
| Cookie `Domain=.joinerytech.hu` | Cross-subdomain cookie sharing (FP-16) | 0.1 nap |
| **Total backend delta** | | **~0.85 nap** |

### Infra előfeltétel

| Feladat | Effort |
|---------|--------|
| DNS: `eszkozok.joinerytech.hu` → VPS | 0.1 nap |
| Certbot: SAN bővítés vagy wildcard | 0.1 nap |
| Nginx vhost létrehozás | 0.25 nap |
| **Total infra** | **~0.45 nap** |
