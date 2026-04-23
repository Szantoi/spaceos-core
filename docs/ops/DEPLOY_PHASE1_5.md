# Sprint D Phase 1.5 — Deploy Checklist

> **Státusz:** Code DONE, Deploy BLOCKED (sudo szükséges)
> **Dátum:** 2026-04-07

## Előfeltételek

- EC P-256 kulcspár már generálva: `/opt/spaceos/SpaceOS.Kerner/SpaceOS.Kernel.Api/keys/ec-private.pem` + `ec-public.pem`
- Kernel publish kész: `/tmp/spaceos-kernel-publish/`
- Orchestrator build kész, RS256→ES256 támogatás hozzáadva

## Deploy lépések (sudo szükséges)

### 1. Kernel deploy

```bash
# Publish fájlok másolása
sudo cp -r /tmp/spaceos-kernel-publish/* /opt/spaceos/spaceos-kernel/publish/

# EC kulcsok másolása a publish könyvtárba
sudo cp /opt/spaceos/SpaceOS.Kerner/SpaceOS.Kernel.Api/keys/ec-private.pem \
        /opt/spaceos/SpaceOS.Kerner/SpaceOS.Kernel.Api/keys/ec-public.pem \
        /opt/spaceos/spaceos-kernel/publish/keys/

# appsettings frissítése — EC kulcs útvonalak
sudo tee /opt/spaceos/spaceos-kernel/publish/appsettings.Development.json <<'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SpaceOS.dev.db"
  },
  "Jwt": {
    "PrivateKeyPath": "keys/ec-private.pem",
    "PublicKeyPath": "keys/ec-public.pem"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
EOF

# Kernel service újraindítása
sudo systemctl restart spaceos-kernel
```

### 2. Orchestrator .env frissítése

```bash
# Már konfiguálva: /opt/spaceos/spaceos.orchestrator/.env
# Cserélni kell:
#   JWT_ALGORITHM=RS256 → JWT_ALGORITHM=ES256
#   + JWT_EC_PRIVATE_KEY_PATH=../SpaceOS.Kerner/SpaceOS.Kernel.Api/keys/ec-private.pem
#   + JWT_EC_PUBLIC_KEY_PATH=../SpaceOS.Kerner/SpaceOS.Kernel.Api/keys/ec-public.pem

cd /opt/spaceos/spaceos.orchestrator
npm run build
npx pm2 restart spaceos-orchestrator
```

### 3. E2E tesztek futtatása

```bash
cd /opt/spaceos/e2e && npm test
```

### 4. Elvárt eredmény

- 23/23 teszt fájl PASS
- ES256 warning-ok eltűnnek
- Refresh token tesztek aktiválódnak
- RLS tesztek élesednek

## Jelenlegi ideiglenes állapot

Az Orchestrator RS256-ra van konfigurálva, hogy a régi Kernel-lel (ES256 ephemeral) ne ütközzön.
A deploy után mindkét service a közös EC kulcspárt használja.
