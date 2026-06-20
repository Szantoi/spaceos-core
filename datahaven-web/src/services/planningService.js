/**
 * Planning Service — Reads planning pipeline state
 *
 * Planning Pipeline stages:
 *   IDEAS: docs/planning/ideas/*.md (raw ideas from scan)
 *   SELECTED: docs/planning/selected/pending.md (selected for debate)
 *   DEBATE: docs/planning/consensus/*.md (in-progress debates)
 *   QUEUE: docs/planning/queue/*.md (ready for Conductor)
 *   ARCHIVE: docs/planning/archive/*.md (processed items)
 */

import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

const PLANNING_BASE = process.env.PLANNING_PATH || '/opt/spaceos/docs/planning';
const LOG_FILE = process.env.PIPELINE_LOG || '/opt/spaceos/logs/dispatcher/pipeline.log';

/**
 * Parse a planning document
 */
async function parseDocument(filePath) {
    try {
        const content = await fs.readFile(filePath, 'utf8');
        const { data: frontmatter, content: body } = matter(content);
        const stats = await fs.stat(filePath);

        return {
            id: frontmatter.id || path.basename(filePath, '.md'),
            title: extractTitle(body) || frontmatter.title || path.basename(filePath, '.md'),
            domain: frontmatter.domain || null,
            segment: frontmatter.segment || null,
            type: frontmatter.type || null,
            priority: frontmatter.priority || 'medium',
            status: frontmatter.status || null,
            created: frontmatter.created || stats.birthtime.toISOString().split('T')[0],
            updated: stats.mtime.toISOString(),
            path: filePath.replace('/opt/spaceos/', ''),
            filename: path.basename(filePath),
            // Debate-specific fields
            proposer_a: frontmatter.proposer_a || null,
            proposer_b: frontmatter.proposer_b || null,
            verdict: frontmatter.verdict || null,
            // Outcome tracking (Plan → Project/Epic)
            outcome: frontmatter.outcome || null,
            outcome_type: frontmatter.outcome_type || null, // 'project' | 'epic' | 'task'
            outcome_path: frontmatter.outcome_path || null, // e.g., 'spaceos/sales/pipeline'
            // Content preview
            preview: body.substring(0, 300).replace(/\n/g, ' ').trim(),
        };
    } catch (err) {
        console.error(`Error parsing ${filePath}:`, err.message);
        return null;
    }
}

/**
 * Extract title from markdown content
 */
function extractTitle(content) {
    const match = content.match(/^#\s+(.+)$/m);
    return match ? match[1].trim() : null;
}

/**
 * List .md files in a directory (non-recursive)
 */
async function listDirectory(dirPath, excludeArchive = true) {
    try {
        const entries = await fs.readdir(dirPath, { withFileTypes: true });
        return entries
            .filter(e => e.isFile() && e.name.endsWith('.md'))
            .filter(e => !excludeArchive || !e.name.includes('archive'))
            .map(e => path.join(dirPath, e.name));
    } catch (err) {
        return [];
    }
}

/**
 * Get all ideas
 */
export async function getIdeas() {
    const ideasPath = path.join(PLANNING_BASE, 'ideas');
    const files = await listDirectory(ideasPath);
    const ideas = await Promise.all(files.map(parseDocument));

    return ideas
        .filter(i => i !== null)
        .sort((a, b) => new Date(b.updated) - new Date(a.updated));
}

/**
 * Get current selection (pending.md)
 */
export async function getSelected() {
    const pendingPath = path.join(PLANNING_BASE, 'selected', 'pending.md');

    try {
        await fs.access(pendingPath);
        const doc = await parseDocument(pendingPath);
        return doc ? [doc] : [];
    } catch {
        // pending.md doesn't exist
        return [];
    }
}

/**
 * Get active debates (consensus in progress)
 */
export async function getDebates() {
    const consensusPath = path.join(PLANNING_BASE, 'consensus');
    const files = await listDirectory(consensusPath);
    const debates = await Promise.all(files.map(parseDocument));

    return debates
        .filter(d => d !== null)
        .sort((a, b) => new Date(b.updated) - new Date(a.updated));
}

/**
 * Get queue items (ready for Conductor)
 */
export async function getQueue() {
    const queuePath = path.join(PLANNING_BASE, 'queue');
    const files = await listDirectory(queuePath);
    const items = await Promise.all(files.map(parseDocument));

    return items
        .filter(i => i !== null)
        .sort((a, b) => new Date(b.updated) - new Date(a.updated));
}

/**
 * Get archived items (limited to recent N)
 */
export async function getArchive(limit = 20) {
    const archivePath = path.join(PLANNING_BASE, 'archive');
    const files = await listDirectory(archivePath, false);
    const items = await Promise.all(files.map(parseDocument));

    return items
        .filter(i => i !== null)
        .sort((a, b) => new Date(b.updated) - new Date(a.updated))
        .slice(0, limit);
}

/**
 * Get full pipeline state
 */
export async function getPipelineState() {
    const [ideas, selected, debates, queue] = await Promise.all([
        getIdeas(),
        getSelected(),
        getDebates(),
        getQueue(),
    ]);

    return {
        stages: {
            ideas: {
                count: ideas.length,
                items: ideas,
            },
            selected: {
                count: selected.length,
                items: selected,
            },
            debate: {
                count: debates.length,
                items: debates,
            },
            queue: {
                count: queue.length,
                items: queue,
            },
        },
        totals: {
            ideas: ideas.length,
            selected: selected.length,
            debate: debates.length,
            queue: queue.length,
        },
        timestamp: new Date().toISOString(),
    };
}

/**
 * Get pipeline log (last N lines)
 */
export async function getPipelineLog(lines = 50) {
    try {
        const { stdout } = await execAsync(`tail -${lines} ${LOG_FILE} 2>/dev/null || echo "No log available"`);

        // Parse log lines into structured format
        const logLines = stdout.trim().split('\n').map(line => {
            const match = line.match(/^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+(.+)$/);
            if (match) {
                return {
                    timestamp: match[1],
                    message: match[2],
                    level: detectLogLevel(match[2]),
                };
            }
            return {
                timestamp: null,
                message: line,
                level: 'info',
            };
        });

        return {
            lines: logLines,
            file: LOG_FILE,
            count: logLines.length,
            timestamp: new Date().toISOString(),
        };
    } catch (err) {
        return {
            lines: [],
            file: LOG_FILE,
            count: 0,
            error: err.message,
            timestamp: new Date().toISOString(),
        };
    }
}

/**
 * Detect log level from message content
 */
function detectLogLevel(message) {
    if (message.includes('HIBA') || message.includes('error') || message.includes('ERROR')) {
        return 'error';
    }
    if (message.includes('WARN') || message.includes('skip')) {
        return 'warning';
    }
    if (message.includes('kész') || message.includes('DONE') || message.includes('OK')) {
        return 'success';
    }
    return 'info';
}

/**
 * Get pipeline metrics
 */
export async function getPipelineMetrics() {
    const state = await getPipelineState();
    const log = await getPipelineLog(100);

    // Count recent errors and successes
    const recentErrors = log.lines.filter(l => l.level === 'error').length;
    const recentSuccess = log.lines.filter(l => l.level === 'success').length;

    // Calculate throughput (items in queue + archive in last 7 days)
    // Simplified: just count queue items

    return {
        pipeline_health: recentErrors > 5 ? 'degraded' : 'healthy',
        stages: state.totals,
        recent_errors: recentErrors,
        recent_successes: recentSuccess,
        timestamp: new Date().toISOString(),
    };
}

/**
 * Get workflow items - combines all stages with outcome tracking
 * Shows the full Plan → Project/Epic pipeline
 */
export async function getWorkflow() {
    const [ideas, selected, debates, queue, archive] = await Promise.all([
        getIdeas(),
        getSelected(),
        getDebates(),
        getQueue(),
        getArchive(50),
    ]);

    // Combine all items with workflow status
    const workflowItems = [];

    // Draft stage: ideas that haven't been selected yet
    for (const item of ideas) {
        workflowItems.push({
            ...item,
            workflowStatus: 'draft',
            workflowStage: 'ideas',
            outcome: null
        });
    }

    // Selected stage
    for (const item of selected) {
        workflowItems.push({
            ...item,
            workflowStatus: 'selected',
            workflowStage: 'selected',
            outcome: null
        });
    }

    // In debate
    for (const item of debates) {
        workflowItems.push({
            ...item,
            workflowStatus: 'in_debate',
            workflowStage: 'debate',
            outcome: null
        });
    }

    // Queue - approved, waiting for implementation
    for (const item of queue) {
        workflowItems.push({
            ...item,
            workflowStatus: 'approved',
            workflowStage: 'queue',
            outcome: null
        });
    }

    // Archive - check for outcome field
    for (const item of archive) {
        workflowItems.push({
            ...item,
            workflowStatus: item.status === 'implemented' ? 'implemented' : 'archived',
            workflowStage: 'archive',
            // outcome populated from frontmatter if exists
            outcome: item.outcome || null,
            outcomeType: item.outcome_type || null,
            outcomePath: item.outcome_path || null
        });
    }

    // Sort by updated (newest first)
    workflowItems.sort((a, b) => new Date(b.updated) - new Date(a.updated));

    // Calculate summary stats
    const stats = {
        draft: ideas.length,
        selected: selected.length,
        in_debate: debates.length,
        approved: queue.length,
        implemented: archive.filter(a => a.status === 'implemented').length,
        archived: archive.filter(a => a.status !== 'implemented').length
    };

    return {
        items: workflowItems,
        stats,
        timestamp: new Date().toISOString()
    };
}

export default {
    getIdeas,
    getSelected,
    getDebates,
    getQueue,
    getArchive,
    getPipelineState,
    getPipelineLog,
    getPipelineMetrics,
    getWorkflow,
};
