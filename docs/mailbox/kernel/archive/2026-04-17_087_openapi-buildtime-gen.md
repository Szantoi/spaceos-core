---
id: MSG-KERNEL-087
from: root
to: kernel
type: task
priority: high
status: READ
ref: SPRINT5
created: 2026-04-17
---

# KERNEL-087 — OpenAPI build-time JSON generálás

## Kontextus

A Swagger spec jelenleg csak `IsDevelopment()` módban érhető el (Program.cs ~422. sor).
Production VPS-en `/openapi/v1.json` → 404. Ez blokkolja:
- TypeScript API client generálást (ADR-07)
- Swagger UI használatát fejlesztés közben VPS-en

## Tudásbázis referencia

- `docs/knowledge/context/KERNEL_CONTEXT.md` — terminál kontextus
- `docs/SpaceOS_Architecture_QA_20260417.md` — döntési háttér

## Feladat

### 1. `dotnet swagger tofile` integrálás

A Swashbuckle CLI tool (`Swashbuckle.AspNetCore.Cli`) generálja a spec-et publish időben:

```bash
# publish után:
dotnet swagger tofile \
  --output publish/openapi-v1.json \
  publish/SpaceOS.Kernel.Api.dll \
  v1
```

Ez a fájl kerüljön a `publish/` mappába, verziókövetett legyen a `docs/` alatt is
(pl. `docs/openapi/kernel-v1.json` — git-be commitálva).

### 2. Statikus kiszolgálás Production módban

A generált JSON fájlt az app serve-elje Production módban is:

```csharp
// Jelenleg (csak Dev):
if (app.Environment.IsDevelopment()) { app.UseSwagger(...); }

// Hozzáadni (mindkét módban):
app.MapGet("/openapi/v1.json", async (IWebHostEnvironment env) => {
    var path = Path.Combine(env.ContentRootPath, "openapi-v1.json");
    return File.Exists(path)
        ? Results.File(path, "application/json")
        : Results.NotFound();
}).AllowAnonymous();
```

Vagy egyszerűbben: `UseStaticFiles()` + a fájl `wwwroot/`-ba kerül.

### 3. Swagger UI Development módban marad

A `UseSwaggerUI` maradhat `IsDevelopment()` mögött — csak a JSON spec kell Production-ban.

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 1122 pass

# Spec generálás ellenőrzése:
dotnet publish -c Release -o /tmp/kernel-test-publish
dotnet swagger tofile --output /tmp/kernel-test-publish/openapi-v1.json \
  /tmp/kernel-test-publish/SpaceOS.Kernel.Api.dll v1
# → openapi-v1.json létrejött, valid JSON
```

## DONE feltételek

- [ ] `dotnet swagger tofile` generál valid `openapi-v1.json`-t
- [ ] `/openapi/v1.json` endpoint 200-at ad Production módban is
- [ ] `docs/openapi/kernel-v1.json` git-be commitálva
- [ ] Tesztszám ≥ 1122
- [ ] Commit hash
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
