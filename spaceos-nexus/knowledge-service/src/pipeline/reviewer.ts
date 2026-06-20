// reviewer.ts - TypeScript equivalent of reviewer.sh
// Dual Haiku review for DONE messages with Anthropic SDK

import Anthropic from '@anthropic-ai/sdk';
import { promises as fs } from 'fs';
import * as path from 'path';
import * as yaml from 'js-yaml';
import { SPACEOS_ROOT, log, telegram } from './common';

// ── Types ───────────────────────────────────────────────────────────────────

interface ReviewConfig {
  reviewer: {
    model_a: string;
    model_b: string;
    parallel: boolean;
    require_both: boolean;
  };
  timing: {
    review_timeout: number;
    file_wait: number;
  };
  verdict: {
    approve_keywords: string[];
    reject_keywords: string[];
  };
  paths: {
    prompt_template: string;
    context_file: string;
    log_dir: string;
    review_dir: string;
  };
  reject_inbox: {
    priority: string;
    model_fallback: string;
  };
  notifications: {
    on_approve: boolean;
    on_reject: boolean;
    on_error: boolean;
  };
}

interface ReviewResult {
  verdict: 'APPROVE' | 'REJECT' | 'ERROR' | 'UNKNOWN';
  feedback: string;
  rawResponse: string;
}

interface DualReviewResult {
  approved: boolean;
  reviewA: ReviewResult;
  reviewB: ReviewResult;
  terminal: string;
  doneFile: string;
  doneBase: string;
}

// ── Configuration Loading ───────────────────────────────────────────────────

async function loadConfig(): Promise<ReviewConfig> {
  const configPath = path.join(SPACEOS_ROOT, 'scripts/reviewer-config.yaml');
  const content = await fs.readFile(configPath, 'utf-8');
  return yaml.load(content) as ReviewConfig;
}

async function loadPromptTemplate(config: ReviewConfig): Promise<string> {
  const templatePath = path.join(SPACEOS_ROOT, config.paths.prompt_template);
  return fs.readFile(templatePath, 'utf-8');
}

async function loadContext(config: ReviewConfig): Promise<string> {
  const contextPath = path.join(SPACEOS_ROOT, config.paths.context_file);
  try {
    return await fs.readFile(contextPath, 'utf-8');
  } catch {
    return '';
  }
}

// ── Parse DONE file metadata ────────────────────────────────────────────────

function extractTerminal(donePath: string): string {
  const match = donePath.match(/mailbox\/([^/]+)\//);
  return match ? match[1] : 'unknown';
}

function extractRef(content: string): string {
  const match = content.match(/^ref:\s*(.+)$/m);
  return match ? match[1].trim() : '';
}

function extractModel(content: string): string {
  const match = content.match(/^model:\s*(.+)$/m);
  return match ? match[1].trim() : '';
}

// ── Find original inbox file ────────────────────────────────────────────────

async function findInboxFile(terminal: string, msgRef: string): Promise<{ path: string; content: string } | null> {
  if (!msgRef || msgRef === '—') return null;

  const inboxDir = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxDir);
    for (const file of files) {
      if (!file.endsWith('.md')) continue;
      const filePath = path.join(inboxDir, file);
      const content = await fs.readFile(filePath, 'utf-8');
      if (content.includes(`id: ${msgRef}`)) {
        return { path: filePath, content };
      }
    }
  } catch {
    // Inbox doesn't exist
  }

  return null;
}

// ── Build review prompt ─────────────────────────────────────────────────────

function buildPrompt(
  template: string,
  context: string,
  inboxPath: string,
  inboxContent: string,
  donePath: string,
  doneContent: string
): string {
  return template
    .replace('{{CONTEXT}}', context)
    .replace('{{INBOX_PATH}}', inboxPath)
    .replace('{{INBOX_CONTENT}}', inboxContent || '(nem található)')
    .replace('{{DONE_PATH}}', donePath)
    .replace('{{DONE_CONTENT}}', doneContent);
}

// ── Single review execution ─────────────────────────────────────────────────

async function runSingleReview(
  client: Anthropic,
  model: string,
  prompt: string,
  timeoutMs: number
): Promise<ReviewResult> {
  // Model name mapping (use latest models)
  const modelMap: Record<string, string> = {
    'haiku': 'claude-haiku-4-20250514',
    'sonnet': 'claude-sonnet-4-20250514',
    'opus': 'claude-opus-4-20250514',
  };

  const fullModel = modelMap[model] || model;

  try {
    const response = await Promise.race([
      client.messages.create({
        model: fullModel,
        max_tokens: 1024,
        messages: [{
          role: 'user',
          content: prompt
        }]
      }),
      new Promise<never>((_, reject) =>
        setTimeout(() => reject(new Error('Timeout')), timeoutMs)
      )
    ]);

    const textContent = response.content.find(c => c.type === 'text');
    const rawResponse = textContent ? textContent.text : '';

    return parseReviewResponse(rawResponse);
  } catch (error) {
    const errorMsg = error instanceof Error ? error.message : 'Unknown error';
    return {
      verdict: 'ERROR',
      feedback: `Review hiba: ${errorMsg}`,
      rawResponse: ''
    };
  }
}

// ── Parse review response ───────────────────────────────────────────────────

function parseReviewResponse(response: string): ReviewResult {
  // Extract verdict
  const verdictMatch = response.match(/VERDICT:\s*(\w+)/i);
  const rawVerdict = verdictMatch ? verdictMatch[1].toUpperCase() : 'UNKNOWN';

  // Normalize verdict
  const approveKeywords = ['APPROVE', 'APPROVED', 'JÓVÁHAGYVA'];
  const rejectKeywords = ['REJECT', 'REJECTED', 'VISSZADOBVA'];

  let verdict: 'APPROVE' | 'REJECT' | 'ERROR' | 'UNKNOWN' = 'UNKNOWN';
  if (approveKeywords.includes(rawVerdict)) {
    verdict = 'APPROVE';
  } else if (rejectKeywords.includes(rawVerdict)) {
    verdict = 'REJECT';
  }

  // Extract feedback
  const feedbackMatch = response.match(/FEEDBACK:\s*([\s\S]*?)(?:$|---)/i);
  const feedback = feedbackMatch ? feedbackMatch[1].trim() : '';

  return { verdict, feedback, rawResponse: response };
}

// ── Dual review execution ───────────────────────────────────────────────────

export async function runDualReview(donePath: string): Promise<DualReviewResult> {
  const config = await loadConfig();
  const template = await loadPromptTemplate(config);
  const context = await loadContext(config);

  // Read DONE file
  const doneContent = await fs.readFile(donePath, 'utf-8');
  const terminal = extractTerminal(donePath);
  const doneBase = path.basename(donePath, '.md');
  const msgRef = extractRef(doneContent);

  // Find original inbox
  const inbox = await findInboxFile(terminal, msgRef);
  const inboxPath = inbox?.path || '(nem található)';
  const inboxContent = inbox?.content || '';

  // Build prompt
  const prompt = buildPrompt(template, context, inboxPath, inboxContent, donePath, doneContent);

  // Initialize Anthropic client
  const client = new Anthropic({
    apiKey: process.env.ANTHROPIC_API_KEY
  });

  const timeoutMs = config.timing.review_timeout * 1000;

  await log(`[Reviewer] Starting dual review: ${doneBase} (A: ${config.reviewer.model_a}, B: ${config.reviewer.model_b})`);

  // Run reviews in parallel
  const [reviewA, reviewB] = await Promise.all([
    runSingleReview(client, config.reviewer.model_a, prompt, timeoutMs),
    runSingleReview(client, config.reviewer.model_b, prompt, timeoutMs),
  ]);

  // Save review results
  const reviewDir = path.join(SPACEOS_ROOT, config.paths.review_dir);
  await fs.mkdir(reviewDir, { recursive: true });
  await fs.writeFile(
    path.join(reviewDir, `${doneBase}_reviewer_a.txt`),
    reviewA.rawResponse
  );
  await fs.writeFile(
    path.join(reviewDir, `${doneBase}_reviewer_b.txt`),
    reviewB.rawResponse
  );

  // Determine approval
  let approved = false;
  if (config.reviewer.require_both) {
    approved = reviewA.verdict === 'APPROVE' && reviewB.verdict === 'APPROVE';
  } else {
    approved = reviewA.verdict === 'APPROVE' || reviewB.verdict === 'APPROVE';
  }

  await log(`[Reviewer] Results: A=${reviewA.verdict}, B=${reviewB.verdict} → ${approved ? 'APPROVED' : 'REJECTED'}`);

  return {
    approved,
    reviewA,
    reviewB,
    terminal,
    doneFile: donePath,
    doneBase
  };
}

// ── Create reject inbox message ─────────────────────────────────────────────

export async function createRejectInbox(result: DualReviewResult): Promise<string> {
  const config = await loadConfig();

  const inboxDir = path.join(SPACEOS_ROOT, 'docs/mailbox', result.terminal, 'inbox');
  await fs.mkdir(inboxDir, { recursive: true });

  // Find next message number
  let lastNum = 0;
  try {
    const files = await fs.readdir(inboxDir);
    for (const file of files) {
      const match = file.match(/_(\d{3})_/);
      if (match) {
        const num = parseInt(match[1], 10);
        if (num > lastNum) lastNum = num;
      }
    }
  } catch { /* empty dir */ }

  const nextNum = String(lastNum + 1).padStart(3, '0');
  const date = new Date().toISOString().split('T')[0];
  const filename = `${date}_${nextNum}_review-reject-${result.doneBase}.md`;
  const filePath = path.join(inboxDir, filename);

  // Get original model from inbox
  const msgRef = extractRef(await fs.readFile(result.doneFile, 'utf-8'));
  const inbox = await findInboxFile(result.terminal, msgRef);
  const origModel = inbox ? extractModel(inbox.content) : config.reject_inbox.model_fallback;

  const content = `---
id: MSG-${result.terminal.toUpperCase()}-${nextNum}-REVIEW-REJECT
from: reviewer
to: ${result.terminal}
type: task
priority: ${config.reject_inbox.priority}
status: UNREAD
model: ${origModel || config.reject_inbox.model_fallback}
ref: ${result.doneBase}
created: ${date}
---

# Review visszadobás: ${result.doneBase}

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: ${result.reviewA.verdict}

${result.reviewA.feedback || '(nincs feedback)'}

## Reviewer-B verdict: ${result.reviewB.verdict}

${result.reviewB.feedback || '(nincs feedback)'}

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
`;

  await fs.writeFile(filePath, content);

  // Telegram notification
  if (config.notifications.on_reject) {
    await telegram(`⚠️ *${result.terminal.toUpperCase()} DONE visszadobva*\nA: ${result.reviewA.verdict}\nB: ${result.reviewB.verdict}\n\`${result.doneBase}\``);
  }

  return filePath;
}

// ── Main review handler (called by watchDone) ───────────────────────────────

export async function handleDoneReview(donePath: string): Promise<{ approved: boolean; resultPath?: string }> {
  try {
    const result = await runDualReview(donePath);

    if (result.approved) {
      const config = await loadConfig();
      if (config.notifications.on_approve) {
        await telegram(`✅ *${result.terminal.toUpperCase()} DONE elfogadva*\n\`${result.doneBase}\``);
      }
      return { approved: true };
    } else {
      const rejectPath = await createRejectInbox(result);
      return { approved: false, resultPath: rejectPath };
    }
  } catch (error) {
    const errorMsg = error instanceof Error ? error.message : 'Unknown error';
    await log(`[Reviewer] Error: ${errorMsg}`);
    await telegram(`❌ *Reviewer hiba*\n${errorMsg}`);
    return { approved: false };
  }
}

// ── Standalone execution ────────────────────────────────────────────────────

if (require.main === module) {
  const donePath = process.argv[2];
  if (!donePath) {
    console.error('Usage: npx ts-node reviewer.ts <done_file_path>');
    process.exit(1);
  }

  handleDoneReview(donePath).then(result => {
    console.log(`Review result: ${result.approved ? 'APPROVED' : 'REJECTED'}`);
    if (result.resultPath) {
      console.log(`Reject inbox: ${result.resultPath}`);
    }
    process.exit(result.approved ? 0 : 1);
  });
}
