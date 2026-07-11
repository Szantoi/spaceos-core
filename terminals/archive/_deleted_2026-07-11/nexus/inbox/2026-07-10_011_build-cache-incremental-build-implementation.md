---
id: MSG-NEXUS-011
from: root
to: nexus
type: task
priority: high
status: PROCESSED
model: sonnet
created: 2026-07-10
content_hash: 58d13c7c5959f5d930afb4634dfcf277dc79541c932402cd01b51bf7a20d7a16
---

# Build Cache / Incremental Build Implementation

## Kontextus

A DEV_PROCESS_IMPROVEMENT_PLAN.md Phase 1.2 szerint implementáld a build cache rendszert.

**Referencia:** `docs/planning/specs/DEV_PROCESS_IMPROVEMENT_PLAN.md`

## Probléma

- Minden build full rebuild (~30s)
- .NET restore minden alkalommal
- npm install lassú
- Docker build nem használ layer cache-t

## Feladat

### 1. Makefile Létrehozása

Hozz létre `/opt/spaceos/Makefile`:

```makefile
.PHONY: build build-fast dev test clean

# Fast build with cache
build-fast:
	@echo "🚀 Fast build with cache..."
	@cd datahaven-web/client && npm ci --prefer-offline --cache ~/.npm
	@cd spaceos-nexus/knowledge-service && npm ci --prefer-offline --cache ~/.npm

# Full build
build:
	@echo "🔨 Full build..."
	@cd datahaven-web/client && npm install && npm run build
	@cd spaceos-nexus/knowledge-service && npm install && npm run build

# Development mode
dev:
	@echo "🔧 Starting dev servers..."
	@tmux new-session -d -s spaceos-dev
	@tmux send-keys -t spaceos-dev "cd /opt/spaceos/datahaven-web && npm run dev" Enter

# Run tests
test:
	@echo "🧪 Running tests..."
	@cd datahaven-web/client && npm test
	@cd spaceos-nexus/knowledge-service && npm test

# Clean cache
clean:
	@echo "🧹 Cleaning..."
	@rm -rf datahaven-web/client/node_modules/.cache
	@rm -rf datahaven-web/client/.vite
	@rm -rf spaceos-nexus/knowledge-service/dist
```

### 2. npm Cache Optimization

Frissítsd a package.json scripts-et:

```json
{
  "scripts": {
    "preinstall": "echo 'Using npm cache at ~/.npm'",
    "ci": "npm ci --prefer-offline --cache ~/.npm"
  }
}
```

### 3. Docker Cache Mounts (ha van Dockerfile)

```dockerfile
# syntax=docker/dockerfile:1.4
FROM node:22-alpine AS builder

# Cache npm packages
RUN --mount=type=cache,target=/root/.npm \
    npm ci --prefer-offline

# Cache build output
RUN --mount=type=cache,target=/app/.vite \
    npm run build
```

### 4. Build Script

Hozz létre `scripts/build-cached.sh`:

```bash
#!/bin/bash
# Cached build script

CACHE_DIR="$HOME/.spaceos-build-cache"
mkdir -p "$CACHE_DIR"

# Check if dependencies changed
LOCKFILE_HASH=$(md5sum package-lock.json | cut -d' ' -f1)
CACHED_HASH=$(cat "$CACHE_DIR/lockfile.hash" 2>/dev/null || echo "")

if [ "$LOCKFILE_HASH" == "$CACHED_HASH" ]; then
    echo "📦 Dependencies unchanged, skipping install"
else
    echo "📦 Dependencies changed, running npm ci"
    npm ci --prefer-offline --cache ~/.npm
    echo "$LOCKFILE_HASH" > "$CACHE_DIR/lockfile.hash"
fi

# Incremental build
npm run build
```

## Acceptance Criteria

- [ ] `make build-fast` működik
- [ ] Incremental build < 10s (vs 30s full)
- [ ] npm install cache hit > 90%
- [ ] `make dev` elindítja a dev szervereket
- [ ] Dokumentáció (Makefile kommentek)

## Acceptance Criteria

- [ ] make build-fast működik
- [ ] Incremental build < 10s
- [ ] npm install cache hit > 90%
- [ ] make dev elindítja a dev szervereket
- [ ] Dokumentáció (Makefile kommentek)
