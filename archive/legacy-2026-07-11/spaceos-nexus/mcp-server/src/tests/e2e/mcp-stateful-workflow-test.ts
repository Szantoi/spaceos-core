/**
 * E2E Test: Stateful Workflow Coordination via MCP tools
 * Project: workflow-state / EPIC-01 / TASK-04
 *
 * Requires: npm run dev (server running on port 3000)
 * Run with: npm run test:e2e:workflow
 */

const BASE = 'http://localhost:3000/mcp';
const PROJECT_ID = `test-project-${Date.now()}`;

type McpToolResult = {
    content: { type: string; text: string }[];
    isError?: boolean;
};

async function callTool(sessionId: string, toolName: string, args: object): Promise<McpToolResult> {
    const response = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json, text/event-stream', // Required by StreamableHTTP transport
            'mcp-session-id': sessionId,
            'x-active-role': 'backend_developer'
        },
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: Math.floor(Math.random() * 10000),
            method: 'tools/call',
            params: { name: toolName, arguments: args },
        }),
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}: ${await response.text()}`);

    // StreamableHTTP always responds in SSE format when Accept includes text/event-stream
    const text = await response.text();
    const dataLine = text.split('\n').find(l => l.startsWith('data:'));
    if (!dataLine) throw new Error(`No data line in SSE response: ${text}`);
    const data: any = JSON.parse(dataLine.slice(5).trim());

    if (data.error) throw new Error(`MCP Error: ${JSON.stringify(data.error)}`);
    return data.result as McpToolResult;
}

async function initSession(): Promise<string> {
    // The first POST to /mcp/http without a session ID creates a new session.
    // The server returns the session ID in the 'mcp-session-id' response header.
    const response = await fetch(`${BASE}/http`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json, text/event-stream',
            'x-active-role': 'backend_developer'
        },
        body: JSON.stringify({
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'workflow-e2e-test', version: '1.0' },
            },
        }),
    });

    // Try header first (standard Streamable HTTP)
    let sessionId = response.headers.get('mcp-session-id');

    // Fallback: some implementations embed it in the response body
    if (!sessionId) {
        const body: any = await response.json();
        sessionId = body?.result?.sessionId ?? body?.sessionId ?? null;
    }

    if (!sessionId) {
        throw new Error(`No mcp-session-id returned from server. Status: ${response.status}`);
    }
    return sessionId;
}

function pass(msg: string) { console.log(`  ✅ PASS: ${msg}`); }
function fail(msg: string) { console.error(`  ❌ FAIL: ${msg}`); process.exitCode = 1; }

async function main() {
    console.log('\n🔬 Workflow State E2E Test — EPIC-01 / TASK-04');
    console.log(`   Project ID: ${PROJECT_ID}\n`);

    const sessionId = await initSession();
    console.log(`   Session: ${sessionId}\n`);

    // ── Scenario 1: get_workflow_state — init project ───────────────────────
    console.log('── Scenario 1: Initialize project state ──');
    const stateResult = await callTool(sessionId, 'get_workflow_state', { project_id: PROJECT_ID });
    const stateText = stateResult.content[0].text;
    if (stateText.includes('initialized') && stateText.includes('BACKLOG_READY')) {
        pass(`Project initialized: ${stateText}`);
    } else {
        fail(`Unexpected state response: ${stateText}`);
    }

    // ── Scenario 2: Invalid action parameter block ──────────────────────────
    console.log('\n── Scenario 2: Invalid action parameter blocked by FSM ──');
    const jumpResult = await callTool(sessionId, 'request_workflow_transition', {
        project_id: PROJECT_ID,
        action: 'invalid_action',
    });
    if (jumpResult.isError === true) {
        pass(`FSM correctly blocked invalid action: ${jumpResult.content[0].text}`);
    } else {
        fail(`Expected FSM violation, but got: ${jumpResult.content[0].text}`);
    }

    // ── Scenario 3: Legitimate transition (BACKLOG_READY → success → IN_DEV)
    console.log('\n── Scenario 3: Legitimate phase transition (success) ──');
    const transResult = await callTool(sessionId, 'request_workflow_transition', {
        project_id: PROJECT_ID,
        action: 'success',
    });
    if (!transResult.isError && transResult.content[0].text.includes('IN_DEV')) {
        pass(`Transition approved: ${transResult.content[0].text}`);
    } else {
        fail(`Transition rejected: ${transResult.content[0].text}`);
    }
    // ── Scenario 4: Retries (3-Strike logic or max_retries reached) ─────────
    console.log('\n── Scenario 4: Retries / Fails (Escalation) ──');

    // Currently we are in IN_DEV (from Scenario 3)
    // IN_DEV max_retries is 3, on_fail is BACKLOG_READY.
    // Fail 1: IN_DEV -> fail -> BACKLOG_READY (Retries: 1)
    const fail1 = await callTool(sessionId, 'request_workflow_transition', { project_id: PROJECT_ID, action: 'fail' });
    if (!fail1.isError && fail1.content[0].text.includes('BACKLOG_READY') && fail1.content[0].text.includes('Retries: 1')) {
        pass(`Fail 1 approved (moved to BACKLOG_READY): ${fail1.content[0].text}`);
    } else fail(`Fail 1 rejected: ${fail1.content[0]?.text}`);

    // Now we are in BACKLOG_READY. max_retries is 1. We already have retry_count 1.
    // Fail 2: BACKLOG_READY -> fail. max_retries is 1, so newRetryCount (2) > max_retries(1).
    // This should immediately ESCALATE.
    const fail2 = await callTool(sessionId, 'request_workflow_transition', { project_id: PROJECT_ID, action: 'fail' });
    if (!fail2.isError && fail2.content[0].text.includes('ESCALATED') && fail2.content[0].text.includes('Retries: 0')) {
        pass(`Fail 2 correctly escalated from BACKLOG_READY: ${fail2.content[0].text}`);
    } else fail(`Fail 2 rejected/incorrect state: ${fail2.content[0].text}`);


    // ── Scenario 5: Verify persisted state ───────────────────────────────────
    console.log('\n── Scenario 5: Verify persisted state after transitions ──');
    const finalState = await callTool(sessionId, 'get_workflow_state', { project_id: PROJECT_ID });
    const finalText = finalState.content[0].text;
    if (finalText.includes('ESCALATED')) {
        pass(`State correctly persisted as ESCALATED: ${finalText}`);
    } else {
        fail(`State not correctly persisted. Got: ${finalText}`);
    }

    const exitCode = process.exitCode ?? 0;
    console.log(`\n${exitCode === 0 ? '🎉 All scenarios passed!' : '💀 Some scenarios FAILED.'}`);
}

main().catch(err => {
    console.error('❌ Test runner error:', err.message);
    process.exit(1);
});
