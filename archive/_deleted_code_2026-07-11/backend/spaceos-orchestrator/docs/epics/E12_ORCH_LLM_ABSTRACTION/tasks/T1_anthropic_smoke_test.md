# Task T1 — AnthropicProvider Manual Smoke Test

**Epic:** E12 — LLM Provider Abstraction
**Type:** Manual test (real API key required — no automated test)
**Status:** `BACKLOG_READY`

---

## Goal

Verify that `AnthropicProvider.complete()` makes a real Anthropic API call and returns a valid `LlmResponse`.

---

## Prerequisites

- A valid `ANTHROPIC_API_KEY` from [console.anthropic.com](https://console.anthropic.com)
- `npm run build` passes with 0 TypeScript errors

---

## Steps

1. Copy `.env.example` to `.env` (if not already present):
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and set:
   ```
   LLM_PROVIDER=anthropic
   ANTHROPIC_API_KEY=sk-ant-<your-real-key>
   ```

3. Start the dev server:
   ```bash
   npm run dev
   ```

4. Send a test request:
   ```bash
   curl -s -X POST http://localhost:3000/bff/chat \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <valid-jwt>" \
     -d '{"messages":[{"role":"user","content":"Say hello in one sentence."}]}' \
     | jq .
   ```

5. Confirm the response contains:
   - `finishReason: "stop"`
   - `content` is a non-empty string
   - `toolCalls` is an empty array

---

## Pass Criteria

- [ ] Anthropic API call completes within 10 seconds
- [ ] `LlmResponse.content` contains a human-readable reply
- [ ] `LlmResponse.finishReason` is `"stop"`
- [ ] No SDK error thrown to the caller

---

## Fail Criteria / Rollback

If the call fails:
- Check `ANTHROPIC_API_KEY` is set and not expired
- Check `LLM_PROVIDER=anthropic` is set in `.env`
- Server logs will show the SDK error — do not surface raw errors to the client
- Switch back to `LLM_PROVIDER=mock` to restore functionality without a key

---

## Notes

- This test is intentionally manual — the Anthropic API requires a live key with billing enabled
- The model is hardcoded to `claude-sonnet-4-20250514` in `anthropic.provider.ts`
- Never commit a real `ANTHROPIC_API_KEY` to the repository
