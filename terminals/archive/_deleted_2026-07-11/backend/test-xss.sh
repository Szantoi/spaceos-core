#!/bin/bash

# XSS Prevention Test Script
# Tests XSS sanitization in PUT /api/planning/domain-focus

BASE_URL="http://localhost:3456"
ENDPOINT="/api/planning/domain-focus"

echo "=========================================="
echo "XSS Prevention Test - Datahaven API"
echo "=========================================="
echo ""

# Test 1: <script> tag injection
echo "Test 1: <script> tag injection"
PAYLOAD1='{"criteria":"<script>alert(\"XSS\")</script>Normal text"}'
echo "Payload: $PAYLOAD1"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD1" > /tmp/xss-test-1.json
RESULT1=$(cat /tmp/xss-test-1.json | grep -o "criteria" | wc -l)
CLEAN1=$(cat /tmp/xss-test-1.json | grep -o "<script" | wc -l)
if [ "$CLEAN1" -eq 0 ]; then
  echo "✅ PASS - <script> tag removed"
else
  echo "❌ FAIL - <script> tag not sanitized!"
fi
echo ""

# Test 2: <img> with onerror
echo "Test 2: <img> with onerror event handler"
PAYLOAD2='{"criteria":"<img src=x onerror=alert(1)>Text"}'
echo "Payload: $PAYLOAD2"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD2" > /tmp/xss-test-2.json
CLEAN2=$(cat /tmp/xss-test-2.json | grep -o "onerror" | wc -l)
if [ "$CLEAN2" -eq 0 ]; then
  echo "✅ PASS - onerror event handler removed"
else
  echo "❌ FAIL - onerror not sanitized!"
fi
echo ""

# Test 3: <svg> with onload
echo "Test 3: <svg> with onload event handler"
PAYLOAD3='{"criteria":"<svg onload=alert(1)>Text</svg>"}'
echo "Payload: $PAYLOAD3"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD3" > /tmp/xss-test-3.json
CLEAN3=$(cat /tmp/xss-test-3.json | grep -o "onload" | wc -l)
if [ "$CLEAN3" -eq 0 ]; then
  echo "✅ PASS - onload event handler removed"
else
  echo "❌ FAIL - onload not sanitized!"
fi
echo ""

# Test 4: <iframe> with javascript: URL
echo "Test 4: <iframe> with javascript: URL"
PAYLOAD4='{"criteria":"<iframe src=\"javascript:alert(1)\">Text</iframe>"}'
echo "Payload: $PAYLOAD4"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD4" > /tmp/xss-test-4.json
# Note: iframe tag is not removed by sanitizeCriteria, but should be considered
echo "✅ INFO - iframe not explicitly sanitized (may need enhancement)"
echo ""

# Test 5: Stored XSS - Save malicious criteria and reload
echo "Test 5: Stored XSS - Save and verify persistence"
PAYLOAD5='{"criteria":"# Safe Title\\n\\n<script>alert(5)</script>\\n\\nSafe content"}'
echo "Payload: $PAYLOAD5"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD5" > /tmp/xss-test-5-put.json

# Reload to check stored XSS
sleep 1
curl -s -X GET "$BASE_URL$ENDPOINT" > /tmp/xss-test-5-get.json
STORED_CLEAN=$(cat /tmp/xss-test-5-get.json | grep -o "<script" | wc -l)
if [ "$STORED_CLEAN" -eq 0 ]; then
  echo "✅ PASS - No stored XSS (script tag removed)"
else
  echo "❌ FAIL - Stored XSS detected!"
fi
echo ""

# Test 6: onclick event handler
echo "Test 6: onclick event handler"
PAYLOAD6='{"criteria":"<div onclick=\"alert(6)\">Click me</div>"}'
echo "Payload: $PAYLOAD6"
curl -s -X PUT "$BASE_URL$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD6" > /tmp/xss-test-6.json
CLEAN6=$(cat /tmp/xss-test-6.json | grep -o "onclick" | wc -l)
if [ "$CLEAN6" -eq 0 ]; then
  echo "✅ PASS - onclick event handler removed"
else
  echo "❌ FAIL - onclick not sanitized!"
fi
echo ""

echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo "See detailed results in /tmp/xss-test-*.json"
echo ""
echo "Cleanup:"
echo "rm /tmp/xss-test-*.json"
