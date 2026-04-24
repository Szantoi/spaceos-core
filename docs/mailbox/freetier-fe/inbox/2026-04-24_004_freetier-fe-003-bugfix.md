---
id: MSG-FREETIER-FE-003
from: root
to: freetier-fe
type: task
priority: critical
status: READ
ref: MSG-TESTER-039-DONE
created: 2026-04-24
---

# FREETIER-FE-003 — BUG-FT-003 + BUG-FT-004 fix

> **Teszter 11/15 PASS.** Két bug blokkolja a core flow-t.

---

## BUG-FT-003: Nesting payload mismatch (CRITICAL)

**Tünet:** "Számolás" → API 400
**Root cause:** FE és API payload formátum nem egyezik.

**FE küld:**
```json
{"sheet":{"width_mm":2800,"height_mm":2070},"parts":[{"name":"x","width_mm":500,"height_mm":300,"quantity":1,"grain_direction":"None"}],"label_strategy":"FullLabel"}
```

**API vár:**
```json
{"input":{"sheet":{"widthMm":2800,"heightMm":2070},"parts":[{"name":"x","widthMm":500,"heightMm":300,"quantity":1,"grainDirection":"None"}],"labelStrategy":"FullLabel"}}
```

**3 fix a FE-ben:**
1. `{input: ...}` wrapper hozzáadás a request body-hoz
2. snake_case → camelCase (`width_mm` → `widthMm`, `height_mm` → `heightMm`, `grain_direction` → `grainDirection`)
3. `label_strategy` → `labelStrategy`

**Érintett fájl:** `src/api/nestingApi.ts` (vagy ahol a POST /nest hívás van)

Ellenőrizd az API tényleges várt formátumot:
```bash
curl -s -X POST http://127.0.0.1:5010/api/freetier/nest \
  -H "Content-Type: application/json" \
  -d '{"input":{"sheet":{"widthMm":2800,"heightMm":2070},"parts":[{"name":"test","widthMm":500,"heightMm":300,"quantity":1,"grainDirection":"None"}],"labelStrategy":"FullLabel"}}'
```

---

## BUG-FT-004: Magic link — hiányzó turnstileToken

**Tünet:** "Belépési link küldése" → 400 (validation error: TurnstileToken NotEmpty)
**Root cause:** FE nem küld `turnstileToken` mezőt a magic link request-ben.

**Fix:** A Turnstile widget nincs implementálva (site key nélkül). Küldj egy dummy `"turnstileToken": "dev"` értéket amíg nincs production Turnstile:

```typescript
// authApi.ts — requestMagicLink
const response = await apiClient.post('/api/freetier/auth/magic-link', {
  email,
  turnstileToken: 'dev', // TODO: Cloudflare Turnstile widget integration
});
```

Az API dev bypass-t használ ha nincs `TURNSTILE_SECRET` env var → a `"dev"` token elfogadásra kerül.

---

## CSP fix (bonus — Google Fonts)

A teszter jelezte: CSP blokkolja a Google Fonts-ot. Ez az nginx CSP header-ben van — a `style-src`-ből hiányzik `https://fonts.googleapis.com`. De ez INFRA fix (nginx config), nem FE feladat. Jelezd a DONE-ban.

---

## Definition of Done

- [ ] POST /nest → 200 (nem 400!) — helyes payload formátum
- [ ] POST /auth/magic-link → 202 (nem 400!) — turnstileToken küldve
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 71 pass (meglévő tesztek nem törtek el)
- [ ] Outbox DONE
