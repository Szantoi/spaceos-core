---
id: MSG-ORCH-068
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: —
created: 2026-04-15
---

# MSG-ORCH-068 — Security Review (Q2 Pre-launch)

## Feladat

Futtass teljes biztonsági önellenőrzést az Orchestrator (BFF) kódbázison.
Ez egy **review feladat** — kódot csak ha kritikus sérülékenységet találsz.

## Ellenőrzési területek

### 1. Authentication & Authorization (requireAuth middleware)
- [ ] Minden védett route-on fut a `requireAuth` middleware?
- [ ] `tid` claim validáció: hiányzó `tid` → 401 (R-16 fix, b3860ac)? Van kivétel?
- [ ] Token expiry: lejárt token → 401 (nem 200)?
- [ ] A `/bff/health` és hasonló diagnosztikai endpointok nyilvánosak-e szándékosan?

### 2. Proxy Security
- [ ] Upstream (Kernel) hibáknál mi szivárog ki a kliensnek? (502 body tartalma?)
- [ ] Request header injection lehetséges-e? (pl. `X-SpaceOS-Brand` felülírható a kliens által?)
- [ ] SSRF: a proxy célcíme hardcoded (127.0.0.1:5000) vagy konfigurálható? Ha konfigurálható, van-e allowlist?
- [ ] Timeout van-e beállítva az upstream kéréseken?

### 3. LLM Tool Calling (ha van aktív implementáció)
- [ ] Az LLM-nek küldött prompt tartalmaz-e user-inputot szanitizálás nélkül? (Prompt injection)
- [ ] Az LLM által visszaadott tool paramétereket validálja-e a kód mielőtt felhasználja?
- [ ] Rate limiting van-e az LLM endpointokon?

### 4. SSE (Server-Sent Events)
- [ ] Az SSE kapcsolat le tud-e maradni örökre? Van-e timeout/keepalive?
- [ ] Cross-origin SSE: CORS konfig megfelelő?

### 5. Input & Output
- [ ] `Content-Type` header ellenőrzés bejövő request-eknél?
- [ ] Response-ban nem kerül vissza szanitizálatlan user input (XSS)?
- [ ] JSON parse hibáknál nem szivárog stack trace?

### 6. Secrets & Config
- [ ] API key-ek, JWKS URL-ek `.env`-ben vannak, nem a kódban?
- [ ] `.env` fájl a `.gitignore`-ban?
- [ ] CORS `origin` allowlist le van-e szűkítve (nem `*`)?

### 7. OWASP Top 10 rapid check
- [ ] A1 — Broken Access Control: minden BFF route mögött van auth?
- [ ] A3 — Injection: query paraméterek sanitizálva upstream továbbítás előtt?
- [ ] A5 — Security Misconfiguration: dev/debug módok kikapcsolva production-ban?
- [ ] A7 — XSS: response-okban nincs `text/html` Content-Type user adattal?
- [ ] A10 — SSRF: proxy URL fixált?

## DoD

- [ ] Minden terület ellenőrizve
- [ ] Talált problémák: kritikus / közepes / alacsony szinten kategorizálva
- [ ] Ha kritikus találat: `status: BLOCKED` → azonnali jelzés
