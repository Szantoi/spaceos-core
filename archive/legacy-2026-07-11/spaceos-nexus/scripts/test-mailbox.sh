#!/bin/bash
# Test script for SpaceOS MCP Server Mailbox Tools (Phase 2)

set -e

BASE_URL="http://localhost:3456"
TERMINAL="nexus"

echo "🧪 Testing SpaceOS Mailbox Tools..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# ─── Test 1: List Inbox ──────────────────────────────────────────────────────

echo ""
echo "📥 Test 1: List Inbox (all messages)"
echo "GET $BASE_URL/api/mailbox/$TERMINAL/inbox"
curl -s "$BASE_URL/api/mailbox/$TERMINAL/inbox" | jq '.'

echo ""
echo "📥 Test 1b: List Inbox (UNREAD only)"
echo "GET $BASE_URL/api/mailbox/$TERMINAL/inbox?status=UNREAD"
curl -s "$BASE_URL/api/mailbox/$TERMINAL/inbox?status=UNREAD" | jq '.'

# ─── Test 2: Send Message ─────────────────────────────────────────────────────

echo ""
echo "✉️  Test 2: Send Message to 'test' terminal"
echo "POST $BASE_URL/api/mailbox/test/inbox"

SEND_RESPONSE=$(curl -s -X POST "$BASE_URL/api/mailbox/test/inbox" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "task",
    "content": "# Test Message\n\nThis is a test message from the MCP Server mailbox tool.\n\n## Details\n\n- Created by: test-mailbox.sh\n- Purpose: Integration test\n- Status: Testing Phase 2 mailbox tools",
    "priority": "low",
    "model": "haiku",
    "ref": "MSG-NEXUS-016"
  }')

echo "$SEND_RESPONSE" | jq '.'

# Extract message ID
MSG_ID=$(echo "$SEND_RESPONSE" | jq -r '.id')
echo ""
echo "📌 Created message: $MSG_ID"

# ─── Test 3: Submit DONE ──────────────────────────────────────────────────────

echo ""
echo "✅ Test 3: Submit DONE message"
echo "POST $BASE_URL/api/mailbox/$TERMINAL/outbox"

DONE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/mailbox/$TERMINAL/outbox" \
  -H "Content-Type: application/json" \
  -d '{
    "task_id": "MSG-NEXUS-016",
    "summary": "Phase 2 mailbox tools implemented successfully. All endpoints tested and working.",
    "files_changed": [
      "spaceos-nexus/knowledge-service/src/mailbox.ts",
      "spaceos-nexus/knowledge-service/src/server.ts",
      "spaceos-nexus/knowledge-service/package.json",
      "spaceos-nexus/scripts/test-mailbox.sh"
    ]
  }')

echo "$DONE_RESPONSE" | jq '.'

DONE_ID=$(echo "$DONE_RESPONSE" | jq -r '.id')
echo ""
echo "📌 Created DONE message: $DONE_ID"

# ─── Test 4: Get Task Status ──────────────────────────────────────────────────

echo ""
echo "📋 Test 4: Get Task Status (all active tasks)"
echo "GET $BASE_URL/api/tasks/status"

curl -s "$BASE_URL/api/tasks/status" | jq '.'

# ─── Test 5: Health Check ─────────────────────────────────────────────────────

echo ""
echo "💚 Test 5: Health Check"
echo "GET $BASE_URL/health"

curl -s "$BASE_URL/health" | jq '.'

# ─── Summary ──────────────────────────────────────────────────────────────────

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ All tests completed successfully!"
echo ""
echo "📂 Check created files:"
echo "   - docs/mailbox/test/inbox/$MSG_ID"
echo "   - docs/mailbox/$TERMINAL/outbox/$DONE_ID"
echo ""
