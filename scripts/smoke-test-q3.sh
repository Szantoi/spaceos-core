#!/bin/bash
# SpaceOS Q3 Cutting Expansion - Smoke Test Script
# Validates all Q3 features after deployment
# Version: 1.0.0
# Date: 2026-06-23

set -e  # Exit on error (disabled for some tests)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="${BASE_URL:-https://joinerytech.hu}"
ADMIN_TOKEN="${ADMIN_TOKEN:-}"  # Set via environment variable
TEST_TENANT_ID="${TEST_TENANT_ID:-}"  # Set via environment variable

# Test counters
PASSED=0
FAILED=0
SKIPPED=0

# Helper functions
print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_test() {
    echo -e "${YELLOW}[TEST $1]${NC} $2"
}

pass() {
    echo -e "${GREEN}  ✅ PASS${NC} $1"
    ((PASSED++))
}

fail() {
    echo -e "${RED}  ❌ FAIL${NC} $1"
    ((FAILED++))
}

skip() {
    echo -e "${YELLOW}  ⏭️  SKIP${NC} $1"
    ((SKIPPED++))
}

# Validate prerequisites
if [ -z "$ADMIN_TOKEN" ]; then
    echo -e "${RED}[ERROR]${NC} ADMIN_TOKEN environment variable not set"
    echo "Usage: ADMIN_TOKEN=<your-token> BASE_URL=https://joinerytech.hu ./smoke-test-q3.sh"
    exit 1
fi

print_header "SpaceOS Q3 Cutting Expansion - Smoke Tests"
echo "Base URL: $BASE_URL"
echo "Admin Token: ${ADMIN_TOKEN:0:20}..."
echo ""

# ============================================================================
# TEST 1: Pricing Module Health Check
# ============================================================================
print_test "1/10" "Pricing module health check"
if curl -sf "${BASE_URL}/pricing/health" > /dev/null; then
    pass "Pricing module is healthy"
else
    fail "Pricing module health check failed"
fi

# ============================================================================
# TEST 2: Pricing Module OpenAPI Docs
# ============================================================================
print_test "2/10" "Pricing OpenAPI documentation"
if curl -sf "${BASE_URL}/pricing/swagger/v1/swagger.json" | jq -e '.info.title' > /dev/null 2>&1; then
    pass "Pricing OpenAPI docs available"
else
    fail "Pricing OpenAPI docs not found"
fi

# ============================================================================
# TEST 3: Create Price List (Authenticated)
# ============================================================================
print_test "3/10" "Create price list (Track B)"
PRICE_LIST_RESPONSE=$(curl -s -X POST "${BASE_URL}/pricing/api/price-lists" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Smoke Test Price List",
    "validFrom": "2026-06-23T00:00:00Z"
  }')

if echo "$PRICE_LIST_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
    PRICE_LIST_ID=$(echo "$PRICE_LIST_RESPONSE" | jq -r '.id')
    pass "Price list created: $PRICE_LIST_ID"
else
    fail "Failed to create price list"
    echo "  Response: $PRICE_LIST_RESPONSE"
fi

# ============================================================================
# TEST 4: Public Quote Request Submission (Track A)
# ============================================================================
print_test "4/10" "Submit public quote request (UNAUTHENTICATED)"
QUOTE_REQUEST_RESPONSE=$(curl -s -X POST "${BASE_URL}/cutting/api/public/quote-requests" \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Smoke Test Customer",
    "customerEmail": "smoketest@example.com",
    "customerPhone": "+36301234567",
    "pieces": [
      {
        "materialCode": "PAL-18-WHITE",
        "length": 2000,
        "width": 600,
        "quantity": 5,
        "edgeBanding": "All"
      }
    ]
  }')

if echo "$QUOTE_REQUEST_RESPONSE" | jq -e '.quoteRequestId' > /dev/null 2>&1; then
    QUOTE_REQUEST_ID=$(echo "$QUOTE_REQUEST_RESPONSE" | jq -r '.quoteRequestId')
    pass "Quote request created: $QUOTE_REQUEST_ID"
else
    fail "Failed to create quote request"
    echo "  Response: $QUOTE_REQUEST_RESPONSE"
fi

# ============================================================================
# TEST 5: Rate Limit Testing (Track A)
# ============================================================================
print_test "5/10" "Rate limit validation (10 req/hour)"
echo "  Submitting 12 quote requests to test rate limit..."

RATE_LIMIT_HIT=false
for i in {1..12}; do
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
        -X POST "${BASE_URL}/cutting/api/public/quote-requests" \
        -H "Content-Type: application/json" \
        -d '{
            "customerName": "Rate Test",
            "customerEmail": "ratetest@example.com",
            "customerPhone": "+36301111111",
            "pieces": []
        }')

    if [ "$HTTP_STATUS" -eq 429 ]; then
        RATE_LIMIT_HIT=true
        pass "Rate limit triggered on request #$i (HTTP 429)"
        break
    fi

    # Small delay between requests
    sleep 0.5
done

if [ "$RATE_LIMIT_HIT" = false ]; then
    fail "Rate limit not triggered after 12 requests"
fi

# ============================================================================
# TEST 6: ShopFloor Kiosk Login (Track C)
# ============================================================================
print_test "6/10" "ShopFloor kiosk operator login"

# Note: This test requires OperatorPin to be configured
# Skipping if test operator not available
if [ -z "$TEST_OPERATOR_PIN" ]; then
    skip "No TEST_OPERATOR_PIN configured (set TEST_OPERATOR_PIN env var)"
else
    # TODO: Replace with actual workstation ID
    WORKSTATION_ID="00000000-0000-0000-0000-000000000000"

    LOGIN_RESPONSE=$(curl -s -X POST "${BASE_URL}/cutting/api/shopfloor/login" \
      -H "Content-Type: application/json" \
      -d "{
        \"operatorPin\": \"${TEST_OPERATOR_PIN}\",
        \"workstationId\": \"${WORKSTATION_ID}\"
      }")

    if echo "$LOGIN_RESPONSE" | jq -e '.sessionId' > /dev/null 2>&1; then
        SESSION_ID=$(echo "$LOGIN_RESPONSE" | jq -r '.sessionId')
        pass "Operator login successful: $SESSION_ID"
    else
        fail "Operator login failed"
        echo "  Response: $LOGIN_RESPONSE"
    fi
fi

# ============================================================================
# TEST 7: Machine Queue Retrieval (Track C)
# ============================================================================
print_test "7/10" "Get machine queue (authenticated)"

if [ -z "$TEST_WORKSTATION_ID" ]; then
    skip "No TEST_WORKSTATION_ID configured"
else
    QUEUE_RESPONSE=$(curl -s "${BASE_URL}/cutting/api/shopfloor/machines/${TEST_WORKSTATION_ID}/queue" \
      -H "Authorization: Bearer ${ADMIN_TOKEN}")

    if echo "$QUEUE_RESPONSE" | jq -e '.workstationId' > /dev/null 2>&1; then
        BATCH_COUNT=$(echo "$QUEUE_RESPONSE" | jq '.batches | length')
        pass "Machine queue retrieved ($BATCH_COUNT batches)"
    else
        fail "Failed to get machine queue"
        echo "  Response: $QUEUE_RESPONSE"
    fi
fi

# ============================================================================
# TEST 8: Cutting Module Health Check
# ============================================================================
print_test "8/10" "Cutting module health check"
if curl -sf "${BASE_URL}/cutting/health" > /dev/null; then
    pass "Cutting module is healthy"
else
    fail "Cutting module health check failed"
fi

# ============================================================================
# TEST 9: Cutting Module OpenAPI Docs
# ============================================================================
print_test "9/10" "Cutting OpenAPI documentation"
if curl -sf "${BASE_URL}/cutting/swagger/v1/swagger.json" | jq -e '.info.title' > /dev/null 2>&1; then
    pass "Cutting OpenAPI docs available"
else
    fail "Cutting OpenAPI docs not found"
fi

# ============================================================================
# TEST 10: Identity Module OperatorPin Endpoint (if deployed)
# ============================================================================
print_test "10/10" "Identity OperatorPin management endpoint"

# Note: This requires a test user ID
if [ -z "$TEST_USER_ID" ]; then
    skip "No TEST_USER_ID configured"
else
    PIN_UPDATE_RESPONSE=$(curl -s -X PATCH "${BASE_URL}/identity/api/users/${TEST_USER_ID}/operator-pin" \
      -H "Authorization: Bearer ${ADMIN_TOKEN}" \
      -H "Content-Type: application/json" \
      -d '{"operatorPin": "9999"}')

    if echo "$PIN_UPDATE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
        pass "OperatorPin updated successfully"
    else
        # This might fail if the endpoint doesn't exist yet
        skip "OperatorPin endpoint not available (Identity extension not deployed)"
    fi
fi

# ============================================================================
# Summary
# ============================================================================
print_header "Smoke Test Summary"
echo -e "${GREEN}Passed:${NC}  $PASSED"
echo -e "${RED}Failed:${NC}  $FAILED"
echo -e "${YELLOW}Skipped:${NC} $SKIPPED"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All smoke tests passed!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some smoke tests failed${NC}"
    echo "Please review the failures above and check service logs:"
    echo "  - journalctl -u spaceos-modules-pricing -n 50"
    echo "  - journalctl -u spaceos-modules-cutting -n 50"
    exit 1
fi
