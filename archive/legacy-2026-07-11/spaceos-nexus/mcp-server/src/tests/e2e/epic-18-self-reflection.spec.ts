import { test, expect } from '@playwright/test';

const BASE = 'http://127.0.0.1:3000/mcp';

async function initSession(request: any): Promise<string> {
  const response = await request.post(`${BASE}/http`, {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json, text/event-stream',
      'x-active-role': 'backend_developer',
    },
    data: {
      jsonrpc: '2.0',
      id: 1,
      method: 'initialize',
      params: {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: { name: 'self-reflection-e2e', version: '1.0' },
      },
    },
  });

  let sessionId = response.headers()['mcp-session-id'] as string | undefined;
  if (!sessionId) {
    const body = await response.json();
    sessionId = body?.result?.sessionId ?? body?.sessionId;
  }
  if (!sessionId) throw new Error('No session id returned from init');
  return sessionId;
}

type McpResponse = any;

async function callTool(request: any, sessionId: string, toolName: string, args: object): Promise<McpResponse> {
  const response = await request.post(`${BASE}/http`, {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json, text/event-stream',
      'mcp-session-id': sessionId,
      'x-active-role': 'backend_developer',
    },
    data: {
      jsonrpc: '2.0',
      id: Math.floor(Math.random() * 100000),
      method: 'tools/call',
      params: { name: toolName, arguments: args },
    },
  });

  if (!response.ok()) {
    throw new Error(`HTTP ${response.status()} (${toolName})`);
  }

  const text = await response.text();
  const line = text.split('\n').find((l: string) => l.startsWith('data:'));
  if (!line) throw new Error(`No data line in SSE response: ${text}`);

  const data = JSON.parse(line.slice(5).trim());
  if (data.error) throw new Error(`MCP tool error: ${JSON.stringify(data.error)}`);

  return data.result;
}

test('EPIC-18: self-reflection cycle (session1 -> session2) works end-to-end', async ({ request }) => {
  const session1 = await initSession(request);
  const session2 = await initSession(request);

  // Session 1: save episode and generate highlights
  const save1 = await callTool(request, session1, 'save_episode', {
    agent_id: 'agent-1',
    episode_data: {
      thought_process: 'Review deployment checklist and validate CI pipeline.',
      actions: ['run_checks', 'validate_pipeline'],
      outcome: 'Deployment checklist reviewed and confirmed',
      reasoning: 'Prevent regressions by following checklist',
    },
  });
  expect(save1.status).toBe('success');

  const gen1 = await callTool(request, session1, 'generate_episode_highlights', {
    session_id: session1,
    ai_model: 'test-model',
    ai_tokens_used: 123,
  });
  expect(gen1.success).toBe(true);
  const highlightIdSession1 = gen1.data?.highlight_id;
  expect(typeof highlightIdSession1).toBe('string');

  // Session 2: create episode with overlapping keyword 'checklist'
  const save2 = await callTool(request, session2, 'save_episode', {
    agent_id: 'agent-2',
    episode_data: {
      thought_process: 'Run checklist again after updates.',
      actions: ['review_checklist', 'update_docs'],
      outcome: 'Checklist updated and rerun successfully',
      reasoning: 'Reusing previous deployment checklist findings',
    },
  });
  expect(save2.status).toBe('success');

  await callTool(request, session2, 'generate_episode_highlights', {
    session_id: session2,
    ai_model: 'test-model',
    ai_tokens_used: 42,
  });

  // Session 2 reflection should retrieve prior highlight by keyword match
  const reflect = await callTool(request, session2, 'reflect_session', {
    session_id: session2,
    include_prior_highlights: true,
    limit: 5,
  });

  expect(reflect.success).toBe(true);
  const prior = reflect.data?.prior_highlights;
  expect(Array.isArray(prior)).toBe(true);
  const found = prior.find((h: any) => h.highlight_id === highlightIdSession1);
  expect(found).toBeDefined();
  expect(found?.retrieval_reason).toBe('keyword-match');

  // Semantic (Chroma) search should also be available via query_memory
  const queryRes = await callTool(request, session2, 'query_memory', {
    agent_id: 'agent-2',
    query: 'deployment checklist',
    limit: 5,
  });

  expect(queryRes.status).toBe('success');
  expect(Array.isArray(queryRes.episodes)).toBe(true);
  expect(queryRes.episodes.length).toBeGreaterThan(0);
});
