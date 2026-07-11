---
title: "📋 Dev A — EPIC-14 Phase 1 Pre-Sprint Checklist"
subtitle: "Verify Everything Is Ready Before Tuesday 2026-03-18"
created: 2026-03-09
owner: "Dev A"
epic: "EPIC-14"
phase: "Phase 1"
type: "quick-reference"
---

# 📋 Dev A — EPIC-14 Phase 1 Pre-Sprint Checklist

**Print this. Use it. Check it off as you go.**

---

## ✅ Pre-Sprint Setup (Do This Monday 2026-03-17)

### 1. Documentation Review

- [ ] **Read:** `Docs/.../epic_14/goal.md` (EPIC vision + 5 key principles)
- [ ] **Read:** `Docs/.../epic_14/state.md` (Task breakdown, context)
- [ ] **Read:** `Docs/.../TECH-LEAD-EPIC-14-DECISION-WARRANT_2026-03-09.md` (Decision: OPTION A ✅ approved)
- [ ] **Read:** `EPIC-14-PHASE-1-KICKOFF.md` (This sprint's full roadmap)

### 2. Task Assignment Files

- [ ] **Read:** `devs/dev-a/TASK-14-01/TASK-14-01-ASSIGNMENT.md` (Transport abstraction, 28 AC)
- [ ] **Read:** `devs/dev-a/TASK-14-02/TASK-14-02-ASSIGNMENT.md` (HTTP transport, 24 AC)
- [ ] **Read:** `devs/dev-a/TASK-14-05/TASK-14-05-ASSIGNMENT.md` (Stdio transport, 26 AC)

### 3. Code Review

- [ ] **Review:** `src/mcp/transports/HTTPTransport.ts` (PoC—understand what's there)
- [ ] **Review:** `src/mcp/transports/StdioTransport.ts` (PoC—understand what's there)
- [ ] **Review:** `src/mcp/index.ts` (Main server file—where you'll integrate factory)

### 4. Dependencies Check

```bash
# Verify all npm packages installed
npm list express cors readline body-parser

# Should show installed versions without errors
```

- [ ] **Express** installed (for HTTP transport)
- [ ] **Cors** middleware installed (for HTTP CORS support)
- [ ] **Readline** available (Node.js built-in, no install needed)
- [ ] **Vitest or Jest** configured (for testing)

### 5. EPIC-11 Verification

```bash
# Ensure EPIC-11 tests still pass (baseline for backward compat)
npm run test -- src/tests/epic-11-*.test.ts

# Should see: ✓ All EPIC-11 tests passing
```

- [ ] **EPIC-11 tests passing** (baseline established)
- [ ] **No regressions** from recent EPIC-14 prep work

### 6. Git Setup

```bash
# Create feature branch for TASK-14-01
git checkout -b feature/task-14-01-transport-abstraction

# Verify you're on the right branch
git status
```

- [ ] **Git branch** created for TASK-14-01
- [ ] **Main branch** is clean (no uncommitted changes)
- [ ] **Upstream tracking** configured (for easy PR)

---

## 🎯 Tuesday 2026-03-18 — TASK-14-01 Checklist

### Phase 1: Understand (5 min)

- [ ] Re-read TASK-14-01-ASSIGNMENT.md (Steps 1-2)
- [ ] Confirm 28 AC are clear and testable
- [ ] Ask Tech Lead if any AC is ambiguous

### Phase 2: Design (15 min)

- [ ] Sketch out ITransport interface on paper or whiteboard
- [ ] List environment variables (MCP_TRANSPORT, MCP_PORT, MCP_HOST)
- [ ] Identify error codes that need TransportError enum

### Phase 3: File Architecture (30 min)

- [ ] Create folder: `src/mcp/transport/` (if doesn't exist)
- [ ] Plan: 3 files to create (ITransport.ts, TransportFactory.ts, ErrorDiagnoser.ts)
- [ ] Plan: 1 file to modify (src/mcp/index.ts)

### Phase 4: Code (4 hours)

**Checklist as you code:**

- [ ] **ITransport.ts** created
  - [ ] Interface definition (initialize, shutdown, isHealthy, getTransportInfo)
  - [ ] TransportInfo interface
  - [ ] TransportError enum
  - [ ] Exported from index

- [ ] **TransportFactory.ts** created
  - [ ] Factory.create() method
  - [ ] Reads MCP_TRANSPORT env var
  - [ ] Returns correct transport instance
  - [ ] Error handling for invalid env values
  - [ ] Logged to console/logger

- [ ] **ErrorDiagnoser.ts** created
  - [ ] Maps OS errors to TransportError codes
  - [ ] Provides remediation hints
  - [ ] Examples: EADDRINUSE → PORT_IN_USE + "Try MCP_PORT=3001"

- [ ] **src/mcp/index.ts** modified
  - [ ] Import TransportFactory
  - [ ] Call TransportFactory.create()
  - [ ] Call transport.initialize() before registerTool()
  - [ ] Add SIGTERM handler for graceful shutdown

### Phase 5: Testing (1.5 hours)

- [ ] **Unit tests** written (src/tests/unit/transport.factory.test.ts)
  - [ ] Test: Factory creates stdio by default
  - [ ] Test: Factory creates HTTP when MCP_TRANSPORT=http
  - [ ] Test: Invalid env value throws error
  - [ ] Test: ErrorDiagnoser maps errors correctly
  - [ ] Coverage: >80%

- [ ] **Type check** passes

  ```bash
  npx tsc --noEmit
  # Expected: No errors
  ```

- [ ] **Lint** passes

  ```bash
  npm run lint src/mcp/transport*
  # Expected: No issues
  ```

### Phase 6: Integration Check (1 hour)

```bash
# Verify server still starts with default (stdio) transport
MCP_TRANSPORT=stdio npm run server

# Should log: "Stdio transport initialized"
# Should listen to stdin
# Ctrl+C to exit
```

- [ ] **Starts with stdio** (default)
- [ ] **Logs transport selection** at startup
- [ ] **Graceful shutdown** on Ctrl+C

### Phase 7: PR & Documentation (1 hour)

- [ ] **Implementation summary** stub created (template in task file)
- [ ] **Commit message:** `TASK-14-01: Transport abstraction foundation`
- [ ] **PR description** includes:
  - [ ] What was built (ITransport interface, factory pattern)
  - [ ] How to test (`MCP_TRANSPORT=stdio` vs `MCP_TRANSPORT=http`)
  - [ ] Links to task spec
  - [ ] Notes for reviewer

### EOD Checkpoint: TASK-14-01

```
✅ All 28 AC understood and coded
✅ Unit tests 80%+, all passing
✅ Type checks pass (no 'any')
✅ Linting clean
✅ PR ready for review
✅ Ready for Dev C to review or Tech Lead approval
```

- [ ] **TASK-14-01 COMPLETE** — PR submitted for review

---

## 🎯 Wednesday 2026-03-19 — TASK-14-02 Checklist

### Pre-Task

```bash
# Ensure TASK-14-01 is merged (or approved, at least)
git checkout main
git pull origin main

# Create new branch for TASK-14-02
git checkout -b feature/task-14-02-http-transport
```

- [ ] **TASK-14-01 merged** (or at least approved)
- [ ] **Main branch** updated
- [ ] **New branch** created for TASK-14-02

### Phases 1-3: Understand & Design (1 hour)

- [ ] **Task understood:** HTTP transport = production-grade server
- [ ] **24 AC reviewed** and clear
- [ ] **Files planned:** httpTransport.ts, httpTransportConfig.ts, tests
- [ ] **Express setup** sketchedout (port, CORS, health check)

### Phase 4: Code (3 hours)

**Checklist as you code:**

- [ ] **httpTransportConfig.ts** created
  - [ ] HTTPTransportConfig interface (port, host, corsOrigin, etc.)
  - [ ] DEFAULT_HTTP_CONFIG with env var reads
  - [ ] Validation logic

- [ ] **httpTransport.ts** extended (PoC → production)
  - [ ] Express app setup with CORS middleware
  - [ ] JSON body parser
  - [ ] GET /health endpoint
  - [ ] initialize() → app.listen()
  - [ ] shutdown() → graceful drain (200ms)
  - [ ] isHealthy() → server.listening
  - [ ] getTransportInfo() → type='http', endpoint, capabilities
  - [ ] Connection tracking (activeConnections Set)
  - [ ] Error handling (EADDRINUSE → PORT_IN_USE)

- [ ] **Implements ITransport** (from TASK-14-01)
  - [ ] No abstract methods left unimplemented ✅

### Phase 5: Testing (2 hours)

```bash
# Unit tests
npm run test -- src/tests/unit/http-transport.test.ts

# Coverage check
npm run test:coverage -- src/mcp/transports/httpTransport.ts
# Expected: >80%
```

- [ ] **Unit tests** written (factory, config, connection tracking)
- [ ] **Coverage >80%**
- [ ] **All tests passing**

### Phase 6: Manual Testing (1 hour)

```bash
# Start server with HTTP transport
MCP_TRANSPORT=http npm run server

# In another terminal, test health check
curl http://localhost:3000/health

# Expected: 200 OK, JSON response with status + uptime
{
  "status": "healthy",
  "uptime": 2.345
}

# Ctrl+C to shutdown gracefully
```

- [ ] **Server starts** on <http://localhost:3000>
- [ ] **Health check** responds within 100ms
- [ ] **Graceful shutdown** works (Ctrl+C)
- [ ] **Connection limit** monitoring works

### Phase 7: PR & Documentation (1 hour)

- [ ] **Setup guide** created (HTTP deployment + CORS config)
- [ ] **Commit message:** `TASK-14-02: HTTP transport production implementation`
- [ ] **PR description** includes test results + deployment notes

### EOD Checkpoint: TASK-14-02

```
✅ All 24 AC implemented and tested
✅ Health check responds (<100ms)
✅ Graceful shutdown working
✅ Unit tests 80%+, all passing
✅ PR ready for review
```

- [ ] **TASK-14-02 COMPLETE** — PR submitted for review

---

## 🎯 Thursday 2026-03-20 — TASK-14-05 Checklist

### Pre-Task

```bash
# Ensure TASK-14-02 is merged
git checkout main
git pull origin main

# Create new branch for TASK-14-05
git checkout -b feature/task-14-05-stdio-transport
```

- [ ] **TASK-14-02 merged**
- [ ] **Main branch** updated
- [ ] **New branch** created for TASK-14-05

### Phases 1-3: Understand & Design (1 hour)

- [ ] **Task understood:** Stdio = default, embedded, backward-compat mode
- [ ] **26 AC reviewed** (focus on AC-14-05-23-24: backward compat!)
- [ ] **JSON-RPC 2.0 spec** reviewed (jsonrpc.org)
- [ ] **Existing EPIC-11 tests** identified (will run as compat check)

### Phase 4: Code (3 hours)

**Checklist as you code:**

- [ ] **jsonRpcHandler.ts** created
  - [ ] JsonRpcRequest interface (jsonrpc, method, params, id)
  - [ ] JsonRpcResponse interface
  - [ ] JsonRpcError interface
  - [ ] ERROR_CODES constant
  - [ ] parseMessage() method
  - [ ] sendResponse() method
  - [ ] sendError() method

- [ ] **stdioTransport.ts** extended (PoC → production)
  - [ ] readline interface setup (`terminal: false`)
  - [ ] handleLine() method for JSON-RPC parsing
  - [ ] handleClose() for pipe closure
  - [ ] handleError() for error recovery
  - [ ] Terminal mode detection (process.stdin.isTTY)
  - [ ] initialize() → readline.createInterface()
  - [ ] shutdown() → readline.close()
  - [ ] isHealthy() → initialized && !shuttingDown
  - [ ] getTransportInfo() → type='stdio', endpoint='stdio://embedded'
  - [ ] Error handling for invalid JSON
  - [ ] SIGTERM + SIGINT handlers (in main server)

- [ ] **Implements ITransport** (from TASK-14-01)
  - [ ] No abstract methods left ✅

### Phase 5: Testing (2 hours)

```bash
# Unit tests for stdio + JSON-RPC
npm run test -- src/tests/unit/stdio-transport.test.ts

# Coverage check
npm run test:coverage -- src/mcp/transports/stdioTransport.ts
# Expected: >80%

# CRITICAL: Backward compatibility test
npm run test -- src/tests/epic-11-backward-compat.test.ts
# Expected: ALL EPIC-11 tests still passing (zero regressions)
```

- [ ] **Unit tests** written and passing
- [ ] **Coverage >80%**
- [ ] **EPIC-11 backward compat tests** passing (CRITICAL!)

### Phase 6: Manual Testing (1 hour)

```bash
# Test 1: Verify server starts with stdio (default)
npm run server

# Should output: "Stdio transport initialized (listening on stdin)"
# Ctrl+C to exit

# Test 2: Send JSON-RPC message via stdin
echo '{"jsonrpc":"2.0","method":"bootstrap_agent","params":{},"id":1}' | npm run server

# Should output JSON-RPC response
# (exact response depends on tool router from TASK-14-03+)

# Test 3: Verify TTY warning
# Run in terminal (not piped): npm run server
# Should see warning: "⚠️ Stdin is terminal (TTY). For embedded mode, pipe JSON-RPC messages."
```

- [ ] **Server starts** with stdio (default)
- [ ] **JSON-RPC messages** parsed correctly
- [ ] **TTY warning** displays
- [ ] **Graceful shutdown** works
- [ ] **No EPIC-11 regressions** (tests passing)

### Phase 7: PR & Documentation (1 hour)

- [ ] **Stdio specs** created (JSON-RPC examples, error responses)
- [ ] **Commit message:** `TASK-14-05: Stdio transport production implementation`
- [ ] **PR description** includes:
  - [ ] Backward compat verification (EPIC-11 tests passing)
  - [ ] Manual test results
  - [ ] JSON-RPC spec compliance notes
  - [ ] Example messages

### EOD Checkpoint: TASK-14-05

```
✅ All 26 AC implemented and tested
✅ JSON-RPC 2.0 compliant
✅ EPIC-11 backward compat verified (zero regressions!)
✅ Unit tests 80%+, all passing
✅ PR ready for review
```

- [ ] **TASK-14-05 COMPLETE** — PR submitted for review

---

## 📊 Sprint Status Board

Copy this table and update daily:

| Task | Mon 3/17 | Tue 3/18 | Wed 3/19 | Thu 3/20 | Fri 3/21 | Status |
|:-----|:---------|:---------|:---------|:---------|:---------|:-------|
| **Pre-Sprint Setup** | ✅ | — | — | — | — | Complete |
| **TASK-14-01** | — | 🔄 | ✅ Merged | ✅ | ✅ | ✅ DONE |
| **TASK-14-02** | — | — | 🔄 | ✅ Merged | ✅ | 🔄 IP |
| **TASK-14-05** | — | — | — | 🔄 | ✅ Merged | 📋 READY |
| **Phase 1 Complete** | — | — | — | — | ✅ | 📋 NOW |

---

## 🔄 Daily Standup Template

**Use this in daily standups (Tue-Fri):**

```markdown
## Dev A — EPIC-14 Phase 1 Daily Update

**Date:** March 20, 2026

### 🎯 Yesterday's Commitments
- [ ] TASK-14-01: Transport abstraction (DONE ✅)
- [ ] TASK-14-02: HTTP transport (DONE ✅)

### ✅ Today's Achievements
- TASK-14-02 PR merged after technical review
- HTTP health check verified responding <100ms
- All AC-14-02 tests passing (80%+ coverage)

### 🎯 Today's Commitments
- Complete TASK-14-05 (Stdio transport)
- Run full EPIC-11 backward compat suite
- Submit PR for Tech Lead review

### 🚨 Blockers / Risks
- None at this moment
- Connection timeout handling (200ms drain) tested and working

### 📊 Metrics
- TASK-14-01: 28/28 AC ✅
- TASK-14-02: 24/24 AC ✅
- TASK-14-05: 20/26 AC (in progress, targeting 26/26 EOD)
- Overall coverage: 82%+ (on track for 80% target)

### 💬 Notes
- Tech Lead approved ITransport design early
- No architectural changes needed after Day 1
- Ready to handoff to Dev C on Monday for TASK-14-03
```

---

## 🆘 Troubleshooting

### "Port 3000 already in use"

```bash
# Find what's using port 3000
lsof -i :3000

# Kill the process (if not needed)
kill -9 <PID>

# Or use different port for testing
MCP_PORT=3001 npm run server
```

### "EPIC-11 tests failing after my changes"

```bash
# First, revert your changes (don't panic!)
git stash

# Run EPIC-11 tests again
npm run test -- src/tests/epic-11-*.test.ts

# If still failing: problem is in EPIC-11, not your code
# If now passing: your changes broke compat

# Get changes back
git stash pop

# Debug your stdio transport for incompatibilities
```

### "JSON-RPC messages not parsing"

```bash
# Check format (must be exactly one line per message)
echo '{"jsonrpc":"2.0","method":"test","params":{},"id":1}'

# NOT:
echo '{
  "jsonrpc": "2.0",
  "method": "test"
}'  # This will fail (multi-line)

# Test with manual echo
echo '{"jsonrpc":"2.0","method":"bootstrap_agent","params":{},"id":1}' | npm run server
```

### "Tests timing out"

```bash
# Increase timeout for slow systems
npm run test -- --testTimeout=10000

# Or check if:
# - Express server is actually starting
# - Readline is waiting for input (blocks tests)
# - Async promises not resolving
```

---

## ✅ Final Completion Checklist (Friday EOD)

Before marking Phase 1 COMPLETE:

- [ ] **All 3 PRs merged** to main (T14-01, T14-02, T14-05)
- [ ] **78 AC total verified** (28+24+26)
- [ ] **Coverage report >80%** (all files)
- [ ] **EPIC-11 tests passing** (zero regressions)
- [ ] **No 'any' type used** (type safety enforced)
- [ ] **All code reviewed** and approved
- [ ] **Implementation summary** drafted (for knowledge transfer)
- [ ] **Tech Lead sign-off** obtained
- [ ] **Ready for Dev C handoff** (transport layer stable)

---

## 📞 Quick Reference Links

**Bookmark these:**

- 🔗 Task Assignment Files:
  - [`TASK-14-01-ASSIGNMENT.md`](devs/dev-a/TASK-14-01/TASK-14-01-ASSIGNMENT.md)
  - [`TASK-14-02-ASSIGNMENT.md`](devs/dev-a/TASK-14-02/TASK-14-02-ASSIGNMENT.md)
  - [`TASK-14-05-ASSIGNMENT.md`](devs/dev-a/TASK-14-05/TASK-14-05-ASSIGNMENT.md)

- 📖 Specs & Standards:
  - [MCP Specification](https://spec.modelcontextprotocol.io/)
  - [JSON-RPC 2.0](https://www.jsonrpc.org/specification)
  - `database/standards/03-agent-system/mcp.standards.md`

- 🔧 Code Locations:
  - `src/mcp/transport/*` (your new files)
  - `src/mcp/transports/HTT PTransport.ts` (extend this)
  - `src/mcp/transports/StdioTransport.ts` (extend this)
  - `src/mcp/index.ts` (integrate factory here)

---

## 🎉 You've Got Everything You Need

- ✅ **Task specs** (28+24+26 AC defined and clear)
- ✅ **Architecture guide** (ITransport interface + factory pattern)
- ✅ **Test strategy** (unit + integration + E2E)
- ✅ **Timeline** (12h per task, fits 1.5 days each)
- ✅ **Support** (Tech Lead + Arch on standby)
- ✅ **Success metrics** (clear AC checkpoints)

**Now go build something amazing.** 🚀

---

**Questions? Escalate to Tech Lead immediately — don't get stuck.**

**Last Updated:** 2026-03-09
**Ready Status:** ✅ READY FOR SPRINT START (2026-03-18 Tue 09:00)
