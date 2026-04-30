---
id: MSG-PORTAL-001-DONE
from: portal
to: root
type: done
priority: critical
status: READ
ref: MSG-PORTAL-001
created: 2026-04-14
---

## Összefoglaló

**Design Portal rebuild + VPS deploy** teljesítve.

### Végrehajtott lépések

1. **pnpm install** — függőségek szinkronizálva (corepack pnpm 10.33.0)
2. **Vite build** — `apps/joinerytech` sikeresen built: 1766 modul, 33 chunk, `index-BZKuSA8T.js` (295.97 kB)
3. **Dist csere** — régi root-owned assets törölve, új build bemásolva: `/opt/spaceos/design-portal/apps/joinerytech/dist/`
4. **Nginx reload** — `nginx -t` OK (ssl_stapling warning nem kritikus), `systemctl reload nginx` sikeres

### Build timestamp

`2026-04-14 21:51` (UTC+2) — felváltja a 2026-04-07 07:22-es elavult buildet

## Smoke check eredmények

| Ellenőrzés | Eredmény |
|---|---|
| `grep bff/auth/token dist/assets/*.js` | **OK — üres, nincs régi endpoint** |
| `grep code_verifier dist/assets/*.js` | **OK — PKCE flow megvan** (`index-BZKuSA8T.js`) |
| `curl -I https://joinerytech.hu/` | **200** |
| `curl -I https://asztalostech.hu/` | **200** |
| `nginx -t` | **syntax OK** |

## DoD teljesítés

- [x] `dist/` rebuild (2026-04-14 build time)
- [x] `grep bff/auth/token dist/**` → üres
- [x] VPS-en friss dist élő
- [x] `curl https://joinerytech.hu/` → 200
- [x] Nginx reload OK

## Tesztek

Build: TypeScript típusellenőrzés a root-owned `tsconfig.tsbuildinfo` permission issue miatt vite-only módban futott — a meglévő 291 teszt az előző session óta zöld, kódon nem változtattunk.

## Security review

- **Auth flow**: az új build kizárólag direct Keycloak PKCE flow-t tartalmaz — `bff/auth/token` endpoint hívás **nincs**
- **Token handling**: `code_verifier` sessionStorage-ban (nem localStorage), tokenek memóriában (Zustand store)
- **HTTPS**: mindkét domain TLS-en fut, nginx konfig OK
- **XSS**: `dangerouslySetInnerHTML` nem szerepel a kódbázisban

## Kockázatok / kérdések

- A `tsconfig.tsbuildinfo`, `vite.config.js` és a `packages/` alatti `.turbo/` mappák még root tulajdonban vannak. Következő build előtt érdemes: `sudo chown -R gabor:gabor /opt/spaceos/design-portal`
- Az `ssl_stapling` nginx warning nem kritikus, de érdemes lehet Let's Encrypt OCSP-s certre cserélni ha elérhető.
