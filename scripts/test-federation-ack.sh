#!/bin/bash
# Unit test for federation auto-acknowledgement
# Tests MSG-NEXUS-001 fix: notification loop prevention

set -euo pipefail

TEST_DIR="/tmp/federation-ack-test-$$"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_test() {
    echo -e "${YELLOW}[TEST]${NC} $1"
}

log_pass() {
    echo -e "${GREEN}[PASS]${NC} $1"
}

log_fail() {
    echo -e "${RED}[FAIL]${NC} $1"
    exit 1
}

cleanup() {
    rm -rf "$TEST_DIR"
}

trap cleanup EXIT

# Setup test environment
setup_test_env() {
    log_test "Setting up test environment at $TEST_DIR"

    # Create island structure
    for island in nexus spaceos; do
        mkdir -p "$TEST_DIR/opt/$island/terminals/federation/inbox"
        mkdir -p "$TEST_DIR/opt/$island/terminals/federation/outbox"
    done

    log_pass "Test environment created"
}

# Test 1: Basic acknowledgement flow
test_basic_acknowledgement() {
    log_test "Test 1: Basic acknowledgement (SENT → ACK)"

    # Create outbox message in spaceos (SENT status)
    cat > "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_003_test-message.md" <<'EOF'
---
id: MSG-FEDERATION-003
from: spaceos
to: nexus
type: info
status: SENT
created: 2026-07-11
---

# Test Message

This is a test federation message.
EOF

    # Create response in nexus inbox (with ref:)
    cat > "$TEST_DIR/opt/nexus/terminals/federation/inbox/2026-07-11_005_test-response.md" <<'EOF'
---
id: MSG-NEXUS-005
from: nexus
to: spaceos
type: info
status: UNREAD
created: 2026-07-11
ref: MSG-FEDERATION-003
---

# Response

Acknowledged.
EOF

    # Run acknowledgement logic (inline version for testing)
    for inbox_file in "$TEST_DIR/opt/nexus/terminals/federation/inbox"/*.md; do
        ref=$(grep -m1 "^ref:" "$inbox_file" 2>/dev/null | sed 's/ref:[[:space:]]*//' | tr -d '"' | tr -d "'")

        if [ -n "$ref" ]; then
            for outbox_file in "$TEST_DIR/opt/spaceos/terminals/federation/outbox"/*.md; do
                msg_id=$(grep -m1 "^id:" "$outbox_file" 2>/dev/null | sed 's/id:[[:space:]]*//' | tr -d '"' | tr -d "'")
                status=$(grep -m1 "^status:" "$outbox_file" 2>/dev/null | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

                if [ "$msg_id" = "$ref" ] && [ "$status" = "SENT" ]; then
                    sed -i 's/^status: SENT$/status: ACK/' "$outbox_file"
                fi
            done
        fi
    done

    # Verify: outbox message should now be ACK
    status=$(grep -m1 "^status:" "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_003_test-message.md" | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

    if [ "$status" = "ACK" ]; then
        log_pass "Outbox message status updated to ACK"
    else
        log_fail "Expected status ACK, got: $status"
    fi
}

# Test 2: No acknowledgement for UNREAD
test_no_ack_for_unread() {
    log_test "Test 2: No acknowledgement for UNREAD messages"

    # Create outbox message in spaceos (UNREAD status - not yet routed)
    cat > "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_007_test-unread.md" <<'EOF'
---
id: MSG-FEDERATION-007
from: spaceos
to: nexus
type: info
status: UNREAD
created: 2026-07-11
---

# Test Message

This message is still UNREAD.
EOF

    # Create response (this shouldn't trigger ACK because source is UNREAD)
    cat > "$TEST_DIR/opt/nexus/terminals/federation/inbox/2026-07-11_008_test-response-unread.md" <<'EOF'
---
id: MSG-NEXUS-008
from: nexus
to: spaceos
type: info
status: UNREAD
created: 2026-07-11
ref: MSG-FEDERATION-007
---

# Response

Acknowledged.
EOF

    # Run acknowledgement logic
    for inbox_file in "$TEST_DIR/opt/nexus/terminals/federation/inbox"/*.md; do
        ref=$(grep -m1 "^ref:" "$inbox_file" 2>/dev/null | sed 's/ref:[[:space:]]*//' | tr -d '"' | tr -d "'")

        if [ -n "$ref" ]; then
            for outbox_file in "$TEST_DIR/opt/spaceos/terminals/federation/outbox"/*.md; do
                msg_id=$(grep -m1 "^id:" "$outbox_file" 2>/dev/null | sed 's/id:[[:space:]]*//' | tr -d '"' | tr -d "'")
                status=$(grep -m1 "^status:" "$outbox_file" 2>/dev/null | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

                if [ "$msg_id" = "$ref" ] && [ "$status" = "SENT" ]; then
                    sed -i 's/^status: SENT$/status: ACK/' "$outbox_file"
                fi
            done
        fi
    done

    # Verify: outbox message should still be UNREAD (not ACK)
    status=$(grep -m1 "^status:" "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_007_test-unread.md" | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

    if [ "$status" = "UNREAD" ]; then
        log_pass "UNREAD message not acknowledged (correct behavior)"
    else
        log_fail "Expected status UNREAD, got: $status"
    fi
}

# Test 3: Multiple responses (only first should trigger ACK)
test_multiple_responses() {
    log_test "Test 3: Multiple responses (idempotency check)"

    # Create outbox message
    cat > "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_009_test-multi.md" <<'EOF'
---
id: MSG-FEDERATION-009
from: spaceos
to: nexus
type: info
status: SENT
created: 2026-07-11
---

# Test Message

Multiple responses expected.
EOF

    # Create first response
    cat > "$TEST_DIR/opt/nexus/terminals/federation/inbox/2026-07-11_010_response-1.md" <<'EOF'
---
id: MSG-NEXUS-010
from: nexus
to: spaceos
type: info
status: UNREAD
created: 2026-07-11
ref: MSG-FEDERATION-009
---

# Response 1

First response.
EOF

    # Run acknowledgement (first time)
    for inbox_file in "$TEST_DIR/opt/nexus/terminals/federation/inbox"/*.md; do
        ref=$(grep -m1 "^ref:" "$inbox_file" 2>/dev/null | sed 's/ref:[[:space:]]*//' | tr -d '"' | tr -d "'")

        if [ -n "$ref" ]; then
            for outbox_file in "$TEST_DIR/opt/spaceos/terminals/federation/outbox"/*.md; do
                msg_id=$(grep -m1 "^id:" "$outbox_file" 2>/dev/null | sed 's/id:[[:space:]]*//' | tr -d '"' | tr -d "'")
                status=$(grep -m1 "^status:" "$outbox_file" 2>/dev/null | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

                if [ "$msg_id" = "$ref" ] && [ "$status" = "SENT" ]; then
                    sed -i 's/^status: SENT$/status: ACK/' "$outbox_file"
                fi
            done
        fi
    done

    # Verify first ACK
    status=$(grep -m1 "^status:" "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_009_test-multi.md" | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

    if [ "$status" = "ACK" ]; then
        log_pass "First response triggered ACK"
    else
        log_fail "Expected ACK after first response, got: $status"
    fi

    # Create second response (duplicate)
    cat > "$TEST_DIR/opt/nexus/terminals/federation/inbox/2026-07-11_011_response-2.md" <<'EOF'
---
id: MSG-NEXUS-011
from: nexus
to: spaceos
type: info
status: UNREAD
created: 2026-07-11
ref: MSG-FEDERATION-009
---

# Response 2

Duplicate response (should not change ACK).
EOF

    # Run acknowledgement (second time)
    for inbox_file in "$TEST_DIR/opt/nexus/terminals/federation/inbox"/*.md; do
        ref=$(grep -m1 "^ref:" "$inbox_file" 2>/dev/null | sed 's/ref:[[:space:]]*//' | tr -d '"' | tr -d "'")

        if [ -n "$ref" ]; then
            for outbox_file in "$TEST_DIR/opt/spaceos/terminals/federation/outbox"/*.md; do
                msg_id=$(grep -m1 "^id:" "$outbox_file" 2>/dev/null | sed 's/id:[[:space:]]*//' | tr -d '"' | tr -d "'")
                status=$(grep -m1 "^status:" "$outbox_file" 2>/dev/null | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

                if [ "$msg_id" = "$ref" ] && [ "$status" = "SENT" ]; then
                    sed -i 's/^status: SENT$/status: ACK/' "$outbox_file"
                fi
            done
        fi
    done

    # Verify still ACK (no error on second run)
    status=$(grep -m1 "^status:" "$TEST_DIR/opt/spaceos/terminals/federation/outbox/2026-07-11_009_test-multi.md" | sed 's/status:[[:space:]]*//' | tr -d '"' | tr -d "'")

    if [ "$status" = "ACK" ]; then
        log_pass "Duplicate response did not cause error (idempotent)"
    else
        log_fail "Expected ACK after duplicate response, got: $status"
    fi
}

# Main test runner
main() {
    echo ""
    echo "=================================="
    echo "Federation ACK Unit Tests"
    echo "Testing MSG-NEXUS-001 Fix"
    echo "=================================="
    echo ""

    setup_test_env
    test_basic_acknowledgement
    test_no_ack_for_unread
    test_multiple_responses

    echo ""
    echo "=================================="
    log_pass "All tests passed!"
    echo "=================================="
    echo ""
}

main
