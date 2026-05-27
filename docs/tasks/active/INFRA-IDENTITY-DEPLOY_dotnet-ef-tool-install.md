---
id: INFRA-IDENTITY-DEPLOY
title: dotnet-ef 8.x tool telepítés + Identity modul deploy prep
status: new
priority: high
assignee: VPS Operator
epic: identity-v1
blocked_by: Identity Track E DONE
created: 2026-05-27
updated: 2026-05-27
docs:
  - docs/tasks/active/IDENTITY-V1_modules-identity.md
---

# INFRA: dotnet-ef 8.x + Identity deploy előkészítés

## Probléma

A szerveren telepített `dotnet-ef` tool v10.0.7 — .NET 8.0 runtime-mal inkompatibilis.
A Track C migration kézzel készült raw SQL-lel (funkcionálisan helyes), de a jövőbeli
migration workflow-hoz szükséges a kompatibilis tool.

## Teendők

### 1. dotnet-ef 8.x telepítés

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
# vagy update ha létezik:
dotnet tool update --global dotnet-ef --version 8.0.11
```

### 2. PostgreSQL — identity DB + role létrehozás

```sql
CREATE DATABASE spaceos_identity;
CREATE ROLE identity_app WITH LOGIN PASSWORD '<erős_jelszó>';
GRANT CONNECT ON DATABASE spaceos_identity TO identity_app;
-- A migration futtatja a GRANT-okat a sémán belül
```

### 3. dotnet ef database update futtatás

```bash
cd /opt/spaceos/backend/spaceos-modules-identity
dotnet ef database update --project Identity.Infrastructure --startup-project Identity.Api
```

### 4. Keycloak — `spaceos-identity-service` client létrehozás

- Client ID: `spaceos-identity-service`
- Client Secret: generálni + VPS env-be menteni
- Service account roles: `manage-users` (realm scope)

### 5. .env fájl létrehozás

```
/opt/spaceos/backend/spaceos-modules-identity/.env
```

Tartalma (secret):
```
DB_PASSWORD=<identity_app jelszó>
KEYCLOAK_CLIENT_SECRET=<spaceos-identity-service secret>
REDIS_PASSWORD=<redis auth>
```

### 6. systemd service

```
/etc/systemd/system/spaceos-modules-identity.service
```
Minta: `spaceos-orchestrator.service` alapján, port 5008.

### 7. nginx route

```nginx
location /identity/ {
    proxy_pass http://127.0.0.1:5008/identity/;
}
```

## Előfeltétel

- Identity Track E DONE + elfogadva
- **P0-1 (JWT RS256) lezárva** — production GA blocker
