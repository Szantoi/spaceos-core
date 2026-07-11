#!/bin/bash
# SpaceOS Cached Build Script (MSG-NEXUS-011)
#
# Intelligent build script with lockfile hash checking to skip unnecessary npm installs.
# Skips npm ci if package-lock.json hasn't changed since last build.
#
# Usage:
#   ./scripts/build-cached.sh <project_dir> [build_command]
#
# Examples:
#   ./scripts/build-cached.sh datahaven-web/client
#   ./scripts/build-cached.sh spaceos-nexus/knowledge-service
#   ./scripts/build-cached.sh datahaven-web/client "npm run build"

set -e

# ─── Configuration ────────────────────────────────────────────────────────────

PROJECT_DIR="${1:?Error: Project directory required (e.g., datahaven-web/client)}"
BUILD_CMD="${2:-npm run build}"
CACHE_DIR="$HOME/.spaceos-build-cache"
NPM_CACHE="$HOME/.npm"

# ─── Setup ────────────────────────────────────────────────────────────────────

mkdir -p "$CACHE_DIR"

# Resolve absolute project path
if [[ "$PROJECT_DIR" = /* ]]; then
  ABS_PROJECT_DIR="$PROJECT_DIR"
else
  ABS_PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)/$PROJECT_DIR"
fi

if [ ! -d "$ABS_PROJECT_DIR" ]; then
  echo "❌ Error: Project directory not found: $ABS_PROJECT_DIR"
  exit 1
fi

cd "$ABS_PROJECT_DIR"

PROJECT_NAME=$(basename "$ABS_PROJECT_DIR")
LOCKFILE="package-lock.json"
HASH_FILE="$CACHE_DIR/${PROJECT_NAME}.lockfile.hash"

echo "🚀 SpaceOS Cached Build: $PROJECT_NAME"
echo "   Project: $ABS_PROJECT_DIR"
echo "   Cache:   $CACHE_DIR"
echo ""

# ─── Lockfile Hash Check ──────────────────────────────────────────────────────

if [ ! -f "$LOCKFILE" ]; then
  echo "⚠️  Warning: $LOCKFILE not found, skipping dependency check"
  SKIP_INSTALL=false
else
  # Calculate current lockfile hash
  if command -v md5sum &> /dev/null; then
    CURRENT_HASH=$(md5sum "$LOCKFILE" | cut -d' ' -f1)
  elif command -v md5 &> /dev/null; then
    # macOS
    CURRENT_HASH=$(md5 -q "$LOCKFILE")
  else
    echo "⚠️  Warning: Neither md5sum nor md5 found, cannot check hash"
    SKIP_INSTALL=false
  fi

  # Load cached hash
  CACHED_HASH=""
  if [ -f "$HASH_FILE" ]; then
    CACHED_HASH=$(cat "$HASH_FILE")
  fi

  # Compare hashes
  if [ -n "$CURRENT_HASH" ] && [ "$CURRENT_HASH" = "$CACHED_HASH" ]; then
    echo "📦 Dependencies unchanged (hash: ${CURRENT_HASH:0:8}...)"
    echo "   Skipping npm install"
    SKIP_INSTALL=true
  else
    if [ -n "$CACHED_HASH" ]; then
      echo "📦 Dependencies changed"
      echo "   Old hash: ${CACHED_HASH:0:8}..."
      echo "   New hash: ${CURRENT_HASH:0:8}..."
    else
      echo "📦 No cached hash found (first build or cache cleared)"
    fi
    SKIP_INSTALL=false
  fi
fi

# ─── Install Dependencies ─────────────────────────────────────────────────────

if [ "$SKIP_INSTALL" = false ]; then
  echo ""
  echo "→ Running npm ci --prefer-offline..."
  START_INSTALL=$(date +%s)

  if npm ci --prefer-offline --cache "$NPM_CACHE" --loglevel=error; then
    END_INSTALL=$(date +%s)
    INSTALL_DURATION=$((END_INSTALL - START_INSTALL))
    echo "  ✓ Dependencies installed (${INSTALL_DURATION}s)"

    # Save hash for next build
    if [ -n "$CURRENT_HASH" ]; then
      echo "$CURRENT_HASH" > "$HASH_FILE"
    fi
  else
    echo "  ❌ npm ci failed"
    exit 1
  fi
else
  echo "  ✓ Using cached node_modules"
fi

# ─── Build ────────────────────────────────────────────────────────────────────

echo ""
echo "→ Running build: $BUILD_CMD"
START_BUILD=$(date +%s)

if eval "$BUILD_CMD" > /dev/null 2>&1; then
  END_BUILD=$(date +%s)
  BUILD_DURATION=$((END_BUILD - START_BUILD))
  echo "  ✓ Build complete (${BUILD_DURATION}s)"
else
  echo "  ❌ Build failed"
  exit 1
fi

# ─── Summary ──────────────────────────────────────────────────────────────────

TOTAL_DURATION=$((END_BUILD - START_INSTALL))
echo ""
echo "✅ $PROJECT_NAME build successful"

if [ "$SKIP_INSTALL" = false ]; then
  echo "   Install: ${INSTALL_DURATION}s"
fi

echo "   Build:   ${BUILD_DURATION}s"

if [ "$SKIP_INSTALL" = false ]; then
  echo "   Total:   ${TOTAL_DURATION}s"
else
  echo "   Total:   ${BUILD_DURATION}s (install skipped)"
fi

echo ""
echo "Next build will use cache if package-lock.json unchanged"
echo "Cache file: $HASH_FILE"
