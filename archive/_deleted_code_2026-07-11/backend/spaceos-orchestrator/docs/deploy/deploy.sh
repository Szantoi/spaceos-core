#!/usr/bin/env bash
# deploy.sh — SpaceOS Orchestrator production deploy script
# Usage: bash deploy.sh
# Requires: node 20 LTS, npm, pm2 installed globally

set -euo pipefail

APP_DIR="/opt/spaceos-orchestrator"
APP_NAME="spaceos-orchestrator"

echo "[deploy] Starting deployment of ${APP_NAME}"
echo "[deploy] Directory: ${APP_DIR}"

# 1. Enter project directory
cd "${APP_DIR}"

# 2. Install production dependencies (clean install)
echo "[deploy] Installing dependencies..."
npm ci --omit=dev

# 3. Compile TypeScript
echo "[deploy] Building TypeScript..."
npm run build

# 4. Reload pm2 (zero-downtime if already running, start otherwise)
echo "[deploy] Reloading pm2 process..."
if pm2 describe "${APP_NAME}" > /dev/null 2>&1; then
    pm2 reload ecosystem.config.js --env production
else
    pm2 start ecosystem.config.js --env production
fi

# 5. Persist pm2 process list across reboots
pm2 save

echo "[deploy] Deployment complete."
pm2 status "${APP_NAME}"
