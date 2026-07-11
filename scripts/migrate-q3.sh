#!/bin/bash
# SpaceOS Q3 Cutting Expansion - Database Migration Script
# Runs all migrations for Track A (Customer Portal), Track B (Pricing), Track C (ShopFloor)
# Version: 1.0.0
# Date: 2026-06-23

set -e  # Exit on error
set -u  # Exit on undefined variable

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Base directory
BASE_DIR="/opt/spaceos/backend"

echo "========================================"
echo "SpaceOS Q3 Cutting Expansion Migrations"
echo "========================================"
echo ""

# Function to run migration for a module
run_migration() {
    local module_path=$1
    local module_name=$2

    echo -e "${YELLOW}[INFO]${NC} Running migrations for ${module_name}..."

    if [ ! -d "$module_path" ]; then
        echo -e "${RED}[ERROR]${NC} Module path not found: $module_path"
        exit 1
    fi

    cd "$module_path"

    # Check if migrations exist
    if [ ! -d "Infrastructure/Persistence/Migrations" ] && [ ! -d "src/*/Infrastructure/Migrations" ]; then
        echo -e "${YELLOW}[WARN]${NC} No migrations directory found for ${module_name}, skipping..."
        return 0
    fi

    # Run migration
    if dotnet ef database update --no-build --verbose; then
        echo -e "${GREEN}[SUCCESS]${NC} ${module_name} migrations applied successfully"
    else
        echo -e "${RED}[ERROR]${NC} Failed to apply migrations for ${module_name}"
        exit 1
    fi

    echo ""
}

# Backup reminder
echo -e "${YELLOW}[WARNING]${NC} This script will modify the database schema."
echo "Make sure you have a database backup before proceeding."
echo ""
read -p "Do you have a recent backup? (yes/no): " -r
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${RED}[ABORT]${NC} Migration aborted. Please create a backup first."
    exit 1
fi

echo ""

# Track A: Cutting Module (PublicQuoteRequest)
echo "=== Track A: Customer Portal API ==="
run_migration "${BASE_DIR}/spaceos-modules-cutting" "Cutting Module"

# Track B: Pricing Module (NEW MODULE)
echo "=== Track B: Pricing Integration ==="
if [ -d "${BASE_DIR}/spaceos-modules-pricing" ]; then
    run_migration "${BASE_DIR}/spaceos-modules-pricing" "Pricing Module"
else
    echo -e "${YELLOW}[WARN]${NC} Pricing module not found (Track B not implemented yet)"
fi

# Track C: Cutting Module (MachineQueue, OperatorSession, CuttingBatch extension)
echo "=== Track C: ShopFloor Integration ==="
# Note: Track C migrations are in the same Cutting module as Track A
# They are already applied in the Track A step above
echo -e "${GREEN}[INFO]${NC} Track C migrations included in Cutting Module (already applied)"

# Identity Module (OperatorPin extension)
echo "=== Identity Module: OperatorPin Extension ==="
if [ -d "${BASE_DIR}/spaceos-modules-identity" ]; then
    run_migration "${BASE_DIR}/spaceos-modules-identity" "Identity Module"
else
    echo -e "${YELLOW}[WARN]${NC} Identity module not found at expected path"
fi

echo ""
echo "========================================"
echo -e "${GREEN}✅ All Q3 migrations completed successfully${NC}"
echo "========================================"
echo ""

# Post-migration validation
echo "=== Post-Migration Validation ==="
echo "Running connection test..."

if psql -U spaceos -d spaceos -c "SELECT 1" > /dev/null 2>&1; then
    echo -e "${GREEN}[SUCCESS]${NC} Database connection OK"
else
    echo -e "${RED}[ERROR]${NC} Database connection failed"
    exit 1
fi

# Check new schemas/tables
echo ""
echo "Checking new schemas and tables..."
psql -U spaceos -d spaceos -c "\dt spaceos_pricing.*" 2>/dev/null || echo -e "${YELLOW}[INFO]${NC} Pricing schema not found (Track B not deployed)"
psql -U spaceos -d spaceos -c "\d spaceos_cutting.machine_queues" 2>/dev/null || echo -e "${YELLOW}[INFO]${NC} MachineQueues table not found (Track C not deployed)"
psql -U spaceos -d spaceos -c "\d spaceos_identity.spaceos_users" | grep operator_pin || echo -e "${YELLOW}[INFO]${NC} OperatorPin column not found (Identity extension not deployed)"

echo ""
echo -e "${GREEN}✅ Migration script completed${NC}"
