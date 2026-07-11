# Datahaven Instance

Instance path: `/opt/spaceos/datahaven`
Core path: `/home/gabor/datahaven-core`

## Használat

```bash
# Kérdés-válasz
./dh "Mi az RLS policy?"

# Memory-val
./dh -m "És hogyan kapcsolódik a JWT-hez?"
```

## Konfiguráció

```bash
cp config/.env.example config/.env
# Szerkeszd a config/.env fájlt
```

## Struktúra

```
knowledge/    # Tudásbázis (.md fájlok)
rules/        # Üzleti szabályok
daemons/      # Daemon definíciók (terminálok)
memory/       # Beszélgetés memory (JSONL)
cache/        # Válasz cache
config/       # Konfiguráció (.env)
```
