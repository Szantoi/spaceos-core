// terminalReviewer.ts - Architect + Librarian based review
// Replaces Anthropic API calls with Claude Code terminal sessions
//
// Architecture:
//   - Architect (haiku): "Does implementation match the plan?"
//   - Librarian (haiku): "Is it consistent with history?"
//
// Both run in parallel, results saved to MEMORY
// No ANTHROPIC_API_KEY needed - uses existing terminal infrastructure

import { exec, execSync } from 'child_process';
import { promisify } from 'util';
import { promises as fs } from 'fs';
import * as path from 'path';
import { SPACEOS_ROOT, log, telegram } from './common';
import { sha256File, sha256String } from './hashUtils';
import {
  appendReviewDecision,
  generateReviewId,
  type ReviewDecision
} from './reviewLog';
import * as terminalsConfig from '../config/terminals';

const execAsync = promisify(exec);

// ── Types ───────────────────────────────────────────────────────────────────

export interface TerminalReviewResult {
  terminal: string;
  verdict: 'APPROVE' | 'REJECT' | 'ERROR';
  feedback: string;
  reviewedAt: string;
  sessionOutput?: string;
}

export interface DualTerminalReviewResult {
  approved: boolean;
  architectReview: TerminalReviewResult;
  librarianReview: TerminalReviewResult;
  terminal: string;
  doneFile: string;
  doneBase: string;
  reviewId: string;
}

// ── Config ──────────────────────────────────────────────────────────────────

const TMUX_SOCKET = terminalsConfig.getTmuxSocket();
const REVIEW_TIMEOUT_MS = 120000; // 2 minutes max per review
const MODEL = 'haiku'; // Fast, cheap, sufficient for review

// ── Review Levels ───────────────────────────────────────────────────────────
//
// Review levels determine how strictly a DONE message is reviewed:
//
// none     - No review needed (info messages, acknowledgments)
// light    - Single reviewer (Librarian only - quick consistency check)
// standard - Dual review (Architect + Librarian) - default
// strict   - Dual review + both must APPROVE + detailed feedback required
//
// Set via frontmatter: review_level: none | light | standard | strict

export type ReviewLevel = 'none' | 'light' | 'standard' | 'strict';

// Message types that don't need review
const NO_REVIEW_TYPES = ['info', 'ack', 'acknowledgment', 'status', 'heartbeat'];

// ── Extract review configuration from DONE content ──────────────────────────

function extractReviewLevel(content: string): ReviewLevel {
  const match = content.match(/^review_level:\s*(.+)$/m);
  if (match) {
    const level = match[1].trim().toLowerCase();
    if (['none', 'light', 'standard', 'strict'].includes(level)) {
      return level as ReviewLevel;
    }
  }
  return 'standard'; // default
}

function extractMessageType(content: string): string {
  const match = content.match(/^type:\s*(.+)$/m);
  return match ? match[1].trim().toLowerCase() : 'done';
}

function shouldSkipReview(content: string): { skip: boolean; reason?: string } {
  const msgType = extractMessageType(content);
  const reviewLevel = extractReviewLevel(content);

  // Skip review for info/ack type messages
  if (NO_REVIEW_TYPES.includes(msgType)) {
    return { skip: true, reason: `Message type '${msgType}' does not require review` };
  }

  // Skip if explicitly set to none
  if (reviewLevel === 'none') {
    return { skip: true, reason: 'review_level: none' };
  }

  return { skip: false };
}

// ── Helpers ─────────────────────────────────────────────────────────────────

function extractTerminal(donePath: string): string {
  const newMatch = donePath.match(/terminals\/([^/]+)\//);
  if (newMatch) return newMatch[1];
  const legacyMatch = donePath.match(/mailbox\/([^/]+)\//);
  return legacyMatch ? legacyMatch[1] : 'unknown';
}

function extractRef(content: string): string {
  const match = content.match(/^ref:\s*(.+)$/m);
  return match ? match[1].trim() : '';
}

async function findInboxFile(terminal: string, msgRef: string): Promise<{ path: string; content: string } | null> {
  if (!msgRef || msgRef === '—') return null;

  const inboxPaths = [
    path.join(SPACEOS_ROOT, 'terminals', terminal, 'inbox'),
    path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox'),
  ];

  for (const inboxDir of inboxPaths) {
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
  }
  return null;
}

// ── Check if terminal is busy ───────────────────────────────────────────────

async function isTerminalBusy(terminal: string): Promise<boolean> {
  const sessionName = `spaceos-${terminal}`;

  try {
    // Check if session exists
    await execAsync(`tmux -S ${TMUX_SOCKET} has-session -t ${sessionName}`);

    // Session exists - check if it's actively processing
    const pane = execSync(`tmux -S ${TMUX_SOCKET} capture-pane -t ${sessionName} -p`, {
      timeout: 3000,
      encoding: 'utf-8'
    });

    // If pane shows active processing indicators, it's busy
    const busyIndicators = [
      'Thinking...',
      'Running tool',
      '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏', // Spinner chars
    ];

    return busyIndicators.some(indicator => pane.includes(indicator));
  } catch {
    // Session doesn't exist = not busy
    return false;
  }
}

// ── Wait for terminal to be available ───────────────────────────────────────

async function waitForTerminal(terminal: string, maxWaitMs: number = 60000): Promise<boolean> {
  const startTime = Date.now();

  while (Date.now() - startTime < maxWaitMs) {
    if (!await isTerminalBusy(terminal)) {
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, 5000)); // Check every 5s
  }

  return false;
}

// ── Run single terminal review ──────────────────────────────────────────────

async function runTerminalReview(
  reviewerTerminal: 'architect' | 'librarian',
  donePath: string,
  doneContent: string,
  inboxPath: string | null,
  inboxContent: string | null
): Promise<TerminalReviewResult> {
  const sessionName = `spaceos-${reviewerTerminal}`;
  const reviewStarted = new Date().toISOString();

  // Build review prompt based on reviewer role
  const prompt = reviewerTerminal === 'architect'
    ? buildArchitectPrompt(donePath, doneContent, inboxPath, inboxContent)
    : buildLibrarianPrompt(donePath, doneContent, inboxPath, inboxContent);

  try {
    // Wait for terminal to be available
    const available = await waitForTerminal(reviewerTerminal, 30000);
    if (!available) {
      return {
        terminal: reviewerTerminal,
        verdict: 'ERROR',
        feedback: `${reviewerTerminal} terminal busy, could not start review`,
        reviewedAt: reviewStarted,
      };
    }

    // Check if session exists, if not create it
    try {
      await execAsync(`tmux -S ${TMUX_SOCKET} has-session -t ${sessionName}`);
    } catch {
      // Session doesn't exist - create it
      const workdir = path.join(SPACEOS_ROOT, 'terminals', reviewerTerminal);
      await execAsync(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c "${workdir}"`);
      await new Promise(resolve => setTimeout(resolve, 1000));
      await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "claude --model ${MODEL}" Enter`);
      await new Promise(resolve => setTimeout(resolve, 3000));
    }

    // Send review prompt
    const safePrompt = prompt.replace(/'/g, "'\\''").replace(/\n/g, ' ');
    await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} -l '${safePrompt}'`);
    await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} Enter`);

    // Wait for response with timeout
    const result = await waitForReviewResponse(sessionName, REVIEW_TIMEOUT_MS);

    return {
      terminal: reviewerTerminal,
      verdict: result.verdict,
      feedback: result.feedback,
      reviewedAt: reviewStarted,
      sessionOutput: result.rawOutput,
    };
  } catch (error) {
    const errorMsg = error instanceof Error ? error.message : String(error);
    await log(`[TerminalReviewer] ${reviewerTerminal} review error: ${errorMsg}`);

    return {
      terminal: reviewerTerminal,
      verdict: 'ERROR',
      feedback: `Review error: ${errorMsg}`,
      reviewedAt: reviewStarted,
    };
  }
}

// ── Build Architect review prompt ───────────────────────────────────────────

function buildArchitectPrompt(
  donePath: string,
  doneContent: string,
  inboxPath: string | null,
  inboxContent: string | null
): string {
  return `[REVIEW REQUEST - Architect]

Te az Architect terminál vagy. Egy DONE üzenetet kell review-znod.
Kérdés: A MEGVALÓSÍTÁS MEGFELEL-E A TERVNEK?

## Eredeti feladat (inbox)
${inboxPath ? `Fájl: ${inboxPath}` : '(nem található)'}
${inboxContent ? `\n${inboxContent.substring(0, 2000)}` : ''}

## DONE üzenet
Fájl: ${donePath}
${doneContent.substring(0, 3000)}

## Ellenőrizd:
1. A spec-ben kért funkcionalitás megvalósult?
2. API contract változás dokumentált?
3. Breaking change van? Ha igen, indokolt?
4. Architekturális minták betartva?

## Válasz formátum (KÖTELEZŐ):
VERDICT: APPROVE vagy REJECT
FEEDBACK: [1-3 mondat indoklás]

Csak ezt a formátumot használd, semmi mást!`;
}

// ── Build Librarian review prompt ───────────────────────────────────────────

function buildLibrarianPrompt(
  donePath: string,
  doneContent: string,
  inboxPath: string | null,
  inboxContent: string | null
): string {
  return `[REVIEW REQUEST - Librarian]

Te a Librarian terminál vagy. Egy DONE üzenetet kell review-znod.
Kérdés: KONZISZTENS-E A KORÁBBI MEGOLDÁSOKKAL?

## Eredeti feladat (inbox)
${inboxPath ? `Fájl: ${inboxPath}` : '(nem található)'}
${inboxContent ? `\n${inboxContent.substring(0, 2000)}` : ''}

## DONE üzenet
Fájl: ${donePath}
${doneContent.substring(0, 3000)}

## Ellenőrizd:
1. Hasonló feladat volt korábban? Ha igen, konzisztens a megoldás?
2. A knowledge base-ben dokumentált pattern-eket követi?
3. Volt hasonló hiba korábban amit most is elkövethetett?
4. Dokumentáció/comment elegendő?

## Válasz formátum (KÖTELEZŐ):
VERDICT: APPROVE vagy REJECT
FEEDBACK: [1-3 mondat indoklás]

Csak ezt a formátumot használd, semmi mást!`;
}

// ── Wait for review response ────────────────────────────────────────────────

async function waitForReviewResponse(
  sessionName: string,
  timeoutMs: number
): Promise<{ verdict: 'APPROVE' | 'REJECT' | 'ERROR'; feedback: string; rawOutput: string }> {
  const startTime = Date.now();
  let lastOutput = '';

  while (Date.now() - startTime < timeoutMs) {
    await new Promise(resolve => setTimeout(resolve, 2000));

    try {
      const pane = execSync(`tmux -S ${TMUX_SOCKET} capture-pane -t ${sessionName} -p -S -100`, {
        timeout: 5000,
        encoding: 'utf-8'
      });

      // Check if response is complete (no spinner, has VERDICT)
      const hasVerdict = /VERDICT:\s*(APPROVE|REJECT)/i.test(pane);
      const isProcessing = ['Thinking...', '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'].some(s => pane.includes(s));

      if (hasVerdict && !isProcessing) {
        return parseReviewOutput(pane);
      }

      lastOutput = pane;
    } catch {
      // Capture failed, continue waiting
    }
  }

  // Timeout - try to parse whatever we have
  if (lastOutput) {
    const parsed = parseReviewOutput(lastOutput);
    if (parsed.verdict !== 'ERROR') {
      return parsed;
    }
  }

  return {
    verdict: 'ERROR',
    feedback: 'Review timeout - no response received',
    rawOutput: lastOutput,
  };
}

// ── Parse review output ─────────────────────────────────────────────────────

function parseReviewOutput(output: string): { verdict: 'APPROVE' | 'REJECT' | 'ERROR'; feedback: string; rawOutput: string } {
  const verdictMatch = output.match(/VERDICT:\s*(APPROVE|REJECT)/i);
  const feedbackMatch = output.match(/FEEDBACK:\s*(.+?)(?:\n|$)/i);

  if (!verdictMatch) {
    return {
      verdict: 'ERROR',
      feedback: 'Could not parse verdict from response',
      rawOutput: output,
    };
  }

  return {
    verdict: verdictMatch[1].toUpperCase() as 'APPROVE' | 'REJECT',
    feedback: feedbackMatch ? feedbackMatch[1].trim() : '(no feedback)',
    rawOutput: output,
  };
}

// ── Light review (Librarian only) ───────────────────────────────────────────

async function runLightReview(donePath: string): Promise<DualTerminalReviewResult> {
  const reviewId = generateReviewId();
  const doneContent = await fs.readFile(donePath, 'utf-8');
  const terminal = extractTerminal(donePath);
  const doneBase = path.basename(donePath, '.md');
  const msgRef = extractRef(doneContent);

  const inbox = await findInboxFile(terminal, msgRef);
  const inboxPath = inbox?.path || null;
  const inboxContent = inbox?.content || null;

  await log(`[TerminalReviewer] Starting LIGHT review: ${doneBase} (Librarian only, model: ${MODEL})`);

  // Only Librarian reviews
  const librarianReview = await runTerminalReview('librarian', donePath, doneContent, inboxPath, inboxContent);

  // Auto-approve architect for light review
  const architectReview: TerminalReviewResult = {
    terminal: 'architect',
    verdict: 'APPROVE',
    feedback: '(skipped - light review)',
    reviewedAt: new Date().toISOString(),
  };

  const approved = librarianReview.verdict === 'APPROVE';

  await log(`[TerminalReviewer] Light review result: Librarian=${librarianReview.verdict} → ${approved ? 'APPROVED' : 'REJECTED'}`);

  // Save to review log
  const inboxHash = inbox ? await sha256File(inbox.path) : 'sha256:unknown';
  const doneHash = await sha256File(donePath);

  await appendReviewDecision({
    timestamp: new Date().toISOString(),
    review_id: reviewId,
    inbox_file: inboxPath || '(unknown)',
    inbox_hash: inboxHash,
    done_file: donePath,
    done_hash: doneHash,
    task_type: 'TERMINAL_REVIEW_LIGHT',
    review_attempt: 1,
    reviewer_a: {
      model: 'skipped',
      verdict: 'APPROVE',
      feedback_hash: 'sha256:skipped',
    },
    reviewer_b: {
      model: `${MODEL}-librarian`,
      verdict: librarianReview.verdict,
      feedback_hash: sha256String(librarianReview.feedback),
    },
    final_verdict: approved ? 'APPROVED' : 'REJECTED',
  });

  await saveReviewToMemory(terminal, doneBase, architectReview, librarianReview, approved);

  await telegram(`${approved ? '✅' : '⚠️'} *${terminal.toUpperCase()} Light Review*
Librarian: ${librarianReview.verdict}
File: \`${doneBase}\``);

  return {
    approved,
    architectReview,
    librarianReview,
    terminal,
    doneFile: donePath,
    doneBase,
    reviewId,
  };
}

// ── Main dual terminal review ───────────────────────────────────────────────

export async function runDualTerminalReview(donePath: string, level: ReviewLevel = 'standard'): Promise<DualTerminalReviewResult> {
  const reviewId = generateReviewId();
  const doneContent = await fs.readFile(donePath, 'utf-8');
  const terminal = extractTerminal(donePath);
  const doneBase = path.basename(donePath, '.md');
  const msgRef = extractRef(doneContent);

  // Find original inbox
  const inbox = await findInboxFile(terminal, msgRef);
  const inboxPath = inbox?.path || null;
  const inboxContent = inbox?.content || null;

  await log(`[TerminalReviewer] Starting ${level.toUpperCase()} review: ${doneBase} (Architect + Librarian, model: ${MODEL})`);

  // Run both reviews in parallel
  const [architectReview, librarianReview] = await Promise.all([
    runTerminalReview('architect', donePath, doneContent, inboxPath, inboxContent),
    runTerminalReview('librarian', donePath, doneContent, inboxPath, inboxContent),
  ]);

  // Determine final verdict based on review level
  let approved: boolean;
  if (level === 'strict') {
    // Strict: both must APPROVE AND provide substantial feedback
    const hasSubstantialFeedback =
      architectReview.feedback.length > 20 &&
      librarianReview.feedback.length > 20 &&
      architectReview.feedback !== '(no feedback)' &&
      librarianReview.feedback !== '(no feedback)';

    approved = architectReview.verdict === 'APPROVE' &&
               librarianReview.verdict === 'APPROVE' &&
               hasSubstantialFeedback;

    if (!hasSubstantialFeedback && architectReview.verdict === 'APPROVE' && librarianReview.verdict === 'APPROVE') {
      await log(`[TerminalReviewer] STRICT review: Both approved but feedback insufficient, requiring re-review`);
    }
  } else {
    // Standard: both must APPROVE
    approved = architectReview.verdict === 'APPROVE' && librarianReview.verdict === 'APPROVE';
  }

  await log(`[TerminalReviewer] Results: Architect=${architectReview.verdict}, Librarian=${librarianReview.verdict} → ${approved ? 'APPROVED' : 'REJECTED'}`);

  // Save to review log
  const inboxHash = inbox ? await sha256File(inbox.path) : 'sha256:unknown';
  const doneHash = await sha256File(donePath);

  await appendReviewDecision({
    timestamp: new Date().toISOString(),
    review_id: reviewId,
    inbox_file: inboxPath || '(unknown)',
    inbox_hash: inboxHash,
    done_file: donePath,
    done_hash: doneHash,
    task_type: 'TERMINAL_REVIEW',
    review_attempt: 1,
    reviewer_a: {
      model: `${MODEL}-architect`,
      verdict: architectReview.verdict,
      feedback_hash: sha256String(architectReview.feedback),
    },
    reviewer_b: {
      model: `${MODEL}-librarian`,
      verdict: librarianReview.verdict,
      feedback_hash: sha256String(librarianReview.feedback),
    },
    final_verdict: approved ? 'APPROVED' : 'REJECTED',
  });

  // Save review details to MEMORY
  await saveReviewToMemory(terminal, doneBase, architectReview, librarianReview, approved);

  // Telegram notification
  const emoji = approved ? '✅' : '⚠️';
  await telegram(`${emoji} *${terminal.toUpperCase()} Terminal Review*
Architect: ${architectReview.verdict}
Librarian: ${librarianReview.verdict}
File: \`${doneBase}\``);

  return {
    approved,
    architectReview,
    librarianReview,
    terminal,
    doneFile: donePath,
    doneBase,
    reviewId,
  };
}

// ── Save review to MEMORY ───────────────────────────────────────────────────

async function saveReviewToMemory(
  terminal: string,
  doneBase: string,
  architectReview: TerminalReviewResult,
  librarianReview: TerminalReviewResult,
  approved: boolean
): Promise<void> {
  const date = new Date().toISOString().split('T')[0];

  // Save to Architect MEMORY
  const architectMemoryPath = path.join(SPACEOS_ROOT, 'terminals/architect/MEMORY.md');
  const architectEntry = `
## ${date} Review: ${doneBase}
- **Terminal:** ${terminal}
- **Verdict:** ${architectReview.verdict}
- **Feedback:** ${architectReview.feedback}
`;

  try {
    await fs.appendFile(architectMemoryPath, architectEntry);
  } catch {
    // Memory file might not exist
  }

  // Save to Librarian PROCESSED_LOG
  const librarianLogPath = path.join(SPACEOS_ROOT, 'terminals/librarian/PROCESSED_LOG.md');
  const librarianEntry = `
## ${date} Review: ${doneBase}
- **Terminal:** ${terminal}
- **Verdict:** ${librarianReview.verdict}
- **Feedback:** ${librarianReview.feedback}
- **Final:** ${approved ? 'APPROVED' : 'REJECTED'}
`;

  try {
    await fs.appendFile(librarianLogPath, librarianEntry);
  } catch {
    // Log file might not exist
  }
}

// ── Create reject inbox (reuse from reviewer.ts pattern) ───────────────────

export async function createTerminalRejectInbox(result: DualTerminalReviewResult): Promise<string> {
  const inboxDir = path.join(SPACEOS_ROOT, 'terminals', result.terminal, 'inbox');
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
  const filename = `${date}_${nextNum}_terminal-review-reject-${result.doneBase}.md`;
  const filePath = path.join(inboxDir, filename);

  const content = `---
id: MSG-${result.terminal.toUpperCase()}-${nextNum}-REVIEW-REJECT
from: terminal-reviewer
to: ${result.terminal}
type: task
priority: high
status: UNREAD
model: sonnet
ref: ${result.doneBase}
review_id: ${result.reviewId}
created: ${date}
---

# Terminal Review visszadobás: ${result.doneBase}

Az Architect és Librarian terminal review **nem fogadta el** a DONE-t.

## Architect verdict: ${result.architectReview.verdict}

${result.architectReview.feedback}

## Librarian verdict: ${result.librarianReview.verdict}

${result.librarianReview.feedback}

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
`;

  await fs.writeFile(filePath, content);
  return filePath;
}

// ── Export for watchDone integration ────────────────────────────────────────

export interface TerminalReviewHandlerResult {
  approved: boolean;
  resultPath?: string;
  skipped?: boolean;
  skipReason?: string;
  reviewLevel?: ReviewLevel;
}

export async function handleTerminalReview(donePath: string): Promise<TerminalReviewHandlerResult> {
  try {
    // Read content to determine review strategy
    const doneContent = await fs.readFile(donePath, 'utf-8');
    const doneBase = path.basename(donePath, '.md');
    const terminal = extractTerminal(donePath);

    // Check if review should be skipped
    const skipCheck = shouldSkipReview(doneContent);
    if (skipCheck.skip) {
      await log(`[TerminalReviewer] SKIP: ${doneBase} - ${skipCheck.reason}`);
      await telegram(`⏭️ *${terminal.toUpperCase()} Review Skipped*
Reason: ${skipCheck.reason}
File: \`${doneBase}\``);

      return {
        approved: true,  // Auto-approve skipped reviews
        skipped: true,
        skipReason: skipCheck.reason,
      };
    }

    // Determine review level
    const reviewLevel = extractReviewLevel(doneContent);
    await log(`[TerminalReviewer] Review level: ${reviewLevel} for ${doneBase}`);

    // Route to appropriate review function
    let result: DualTerminalReviewResult;

    switch (reviewLevel) {
      case 'light':
        result = await runLightReview(donePath);
        break;

      case 'strict':
        result = await runDualTerminalReview(donePath, 'strict');
        break;

      case 'standard':
      default:
        result = await runDualTerminalReview(donePath, 'standard');
        break;
    }

    if (result.approved) {
      return { approved: true, reviewLevel };
    } else {
      const rejectPath = await createTerminalRejectInbox(result);
      return { approved: false, resultPath: rejectPath, reviewLevel };
    }
  } catch (error) {
    const errorMsg = error instanceof Error ? error.message : String(error);
    await log(`[TerminalReviewer] Error: ${errorMsg}`);
    return { approved: false };
  }
}
