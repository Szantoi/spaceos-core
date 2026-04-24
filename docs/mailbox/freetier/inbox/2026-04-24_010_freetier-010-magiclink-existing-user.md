---
id: MSG-FREETIER-010
from: root
to: freetier
type: task
priority: critical
status: READ
created: 2026-04-24
---

# FREETIER-010 — Magic link existing user bug (duplicate key 23505)

> **BUG:** `POST /auth/magic-link` → 500: `23505: duplicate key value violates unique constraint "IX_Users_EmailHash"`
> **Root cause:** `RequestMagicLinkCommandHandler` mindig INSERT-et próbál, nem ellenőrzi hogy az emailHash már létezik-e

## Fix

A `RequestMagicLinkCommandHandler.Handle()` logikája:

```
1. Hash email → emailHash
2. Keresés: IFreeTierUserRepository.GetByEmailHashAsync(emailHash)
3. HA nem létezik → FreeTierUser.Register(emailHash) + repo.Add()
4. HA létezik → használd a meglévő usert
5. MagicLinkToken.Generate() → mindkét esetben
6. Token persist + email küldés
```

A jelenlegi kód valószínűleg kihagyja a 2-3 lépést és mindig a Register-t hívja.

## Tesztek (+2)

1. Első magic link kérés → 202, user létrehozva
2. Második magic link kérés UGYANAZZAL az email-lel → 202 (nem 500!), új token generálva, user NEM duplikálva

## Definition of Done

- [ ] Meglévő user felismerése emailHash alapján
- [ ] Második kérés → 202 (nem duplicate key error)
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 170 pass
- [ ] Outbox DONE
