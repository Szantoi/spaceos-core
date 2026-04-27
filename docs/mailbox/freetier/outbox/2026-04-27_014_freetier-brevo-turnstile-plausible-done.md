---
id: MSG-FREETIER-014-DONE
from: freetier
to: root
type: done
ref: MSG-FREETIER-014
status: READ
created: 2026-04-27
---

# FREETIER-014 DONE — Brevo + Turnstile production-ready

## Elvégzett munka

### 1. Brevo client (production-ready)

**Fájl:** `Infrastructure/External/BrevoEmailClient.cs`

- **Logging hozzáadva:** `ILogger<BrevoEmailClient>` DI injektálva
  - `LogWarning` — API key hiányzik (dev mode)
  - `LogInformation` — sikeres email küldés
  - `LogError` — HTTP hiba, összes retry kimerült
- **429 retry logika:** max 3 retry, exponenciális delay (1s / 2s / 4s)
- **Response status check:** non-success → LogError, nem dob exception (fail-safe)
- **Header idempotencia:** `Remove("api-key")` + `TryAddWithoutValidation` (újrahasználható client)

### 2. Turnstile client (production-ready)

**Fájl:** `Infrastructure/External/TurnstileHttpClient.cs`

- **5s timeout:** `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter(5s)`
- **Timeout handling:** `OperationCanceledException` → `false` (nem engedi át)
- **HTTP error handling:** non-success status → `false` + `LogError`
- **`HttpRequestException` catch:** network error → `false` + `LogError`
- **Dev bypass megtartva:** ha `TURNSTILE_SECRET` üres → `true`
- **Logging hozzáadva:** `ILogger<TurnstileHttpClient>` DI injektálva

### 3. Plausible analytics

Nincs `IAnalyticsService` interfész a codebase-ban, a spec "opcionális"-nak jelöli. **Nem implementálva** — külön task-ként hozhatja a Root ha szükséges.

### 4. Operátori teendők (Gábor)

Az API kulcsokat az alábbi env változókba / appsettings-be kell felvenni az éles szerveren:

| Service | Env változó / Config kulcs | Értéke |
|---|---|---|
| Brevo | `BREVO_API_KEY` vagy `Brevo:ApiKey` | Brevo API kulcs |
| Brevo | `Brevo:SenderEmail` | `no-reply@joinerytech.hu` (default) |
| Turnstile | `TURNSTILE_SECRET` vagy `Turnstile:SecretKey` | Cloudflare Turnstile secret |

Ha ezek hiányoznak: dev bypass aktív (email skip, Turnstile mindig `true`).

### 5. Tesztek (+3)

- `SendMagicLink_Brevo429_RetriesThreeTimes` — 3×429 után 200-on sikerül, 4 HTTP hívás ✅
- `SendMagicLink_AllRetriesFail_DoesNotThrow` — összes retry 429 → nem dob, 4 hívás ✅
- `ValidateAsync_HttpError_ReturnsFalse` — HTTP 500 → false ✅

## Build & Test

```
dotnet build  → 0 error, 0 warning ✅
dotnet test   → 179 pass (51 domain + 62 application + 66 integration) ✅
```

## Definition of Done

- [x] Brevo client production-ready (logging, error handling, 429 retry)
- [x] Turnstile client production-ready (5s timeout, error handling)
- [x] `dotnet build` 0 error
- [x] `dotnet test` ≥ 178 pass (179 ✅)
- [x] Outbox DONE — API kulcsok operátori feladat (fent jelezve)
