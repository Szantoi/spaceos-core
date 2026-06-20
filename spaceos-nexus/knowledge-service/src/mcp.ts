/**
 * MCP (Model Context Protocol) HTTP Transport Implementation
 *
 * Implements the MCP protocol for Claude Code integration.
 * Tools: 23 tools across 7 categories (knowledge, mailbox, identity, skills, terminal-docs, terminal-status, system)
 */

import { Router, Request, Response } from 'express';
import { searchKnowledge, getDocumentCount, usingChroma } from './vectorStore';
import { embeddingBackend } from './embeddings';
import { listInbox, sendMessage, submitDone, getTaskStatus } from './mailbox';
import {
  getIdentity,
  listTerminals,
  readMemory,
  writeMemory,
  appendMemory,
  getCapabilities,
  TERMINALS,
} from './identity';
import {
  registerWorking,
  registerIdle,
  shouldWakeUp,
  getAllStatus,
  getStatus,
} from './terminalStatus';
import {
  listSkills,
  getSkill,
  getWorkflow,
  getTerminalSetup,
  getProjectContext,
  listTerminalDocs,
  getTerminalDocs,
  getTerminalsIndex,
} from './skills';

const router = Router();

// MCP Protocol Version
const MCP_VERSION = '2024-11-05';

// Bearer token for authentication (optional)
const AUTH_TOKEN = process.env.MCP_AUTH_TOKEN || '';

// ─── Authentication Middleware ──────────────────────────────────────────────

function authenticate(req: Request, res: Response, next: () => void) {
  if (!AUTH_TOKEN) {
    // No auth configured, allow all
    return next();
  }

  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    res.status(401).json({
      jsonrpc: '2.0',
      error: { code: -32001, message: 'Unauthorized: Bearer token required' },
      id: null,
    });
    return;
  }

  const token = authHeader.substring(7);
  if (token !== AUTH_TOKEN) {
    res.status(403).json({
      jsonrpc: '2.0',
      error: { code: -32002, message: 'Forbidden: Invalid token' },
      id: null,
    });
    return;
  }

  next();
}

// ─── Tool Definitions ───────────────────────────────────────────────────────

const TOOLS = [
  // Knowledge tools
  {
    name: 'search_knowledge',
    description: 'Search the SpaceOS knowledge base using semantic search. Returns relevant documentation chunks.',
    inputSchema: {
      type: 'object',
      properties: {
        query: {
          type: 'string',
          description: 'The search query (semantic search)',
        },
        limit: {
          type: 'number',
          description: 'Maximum number of results (default: 5, max: 20)',
        },
      },
      required: ['query'],
    },
  },

  // Mailbox tools
  {
    name: 'list_inbox',
    description: 'List inbox messages for a terminal. Returns YAML frontmatter metadata and content.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
        status: {
          type: 'string',
          enum: ['UNREAD', 'READ', 'all'],
          description: 'Filter by status (default: all)',
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'send_message',
    description: 'Send a new inbox message to a terminal. Creates a markdown file with YAML frontmatter.',
    inputSchema: {
      type: 'object',
      properties: {
        to: {
          type: 'string',
          description: 'Target terminal name',
        },
        type: {
          type: 'string',
          enum: ['task', 'question', 'info', 'blocked'],
          description: 'Message type',
        },
        content: {
          type: 'string',
          description: 'Message content (markdown)',
        },
        priority: {
          type: 'string',
          enum: ['critical', 'high', 'medium', 'low'],
          description: 'Message priority',
        },
        model: {
          type: 'string',
          enum: ['opus', 'sonnet', 'haiku'],
          description: 'Suggested model for processing',
        },
        ref: {
          type: 'string',
          description: 'Reference to related message ID (optional)',
        },
      },
      required: ['to', 'type', 'content', 'priority'],
    },
  },
  {
    name: 'submit_done',
    description: 'Submit a DONE message to the outbox. Used when a task is completed.',
    inputSchema: {
      type: 'object',
      properties: {
        from: {
          type: 'string',
          description: 'Terminal name submitting the DONE',
        },
        task_id: {
          type: 'string',
          description: 'Original task message ID (e.g., MSG-KERNEL-042)',
        },
        summary: {
          type: 'string',
          description: 'Summary of what was done',
        },
        files_changed: {
          type: 'array',
          items: { type: 'string' },
          description: 'List of files that were changed',
        },
      },
      required: ['from', 'task_id', 'summary', 'files_changed'],
    },
  },

  // Task tools
  {
    name: 'get_task_status',
    description: 'Get status of tasks from docs/tasks/. Returns task metadata and status.',
    inputSchema: {
      type: 'object',
      properties: {
        task_id: {
          type: 'string',
          description: 'Specific task ID to query (optional, returns all if omitted)',
        },
      },
    },
  },

  // Identity tools
  {
    name: 'get_identity',
    description: 'Get terminal identity including CLAUDE.md instructions and memory file. Use this to understand what a terminal does and its current state.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'list_terminals',
    description: 'List all SpaceOS terminals with their paths and status (has CLAUDE.md, has memory).',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // Memory tools
  {
    name: 'read_memory',
    description: 'Read the memory file for a specific terminal. Memory contains session state, learned patterns, and context.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'write_memory',
    description: 'Write/replace the entire memory file for a terminal. Use with caution - this overwrites existing memory.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: 'Terminal name',
        },
        content: {
          type: 'string',
          description: 'Full memory content (markdown format)',
        },
      },
      required: ['terminal', 'content'],
    },
  },
  {
    name: 'append_memory',
    description: 'Append new content to terminal memory. Safer than write_memory - preserves existing content.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: 'Terminal name',
        },
        content: {
          type: 'string',
          description: 'Content to append (markdown format)',
        },
      },
      required: ['terminal', 'content'],
    },
  },

  // System tools
  {
    name: 'get_capabilities',
    description: 'List all available MCP capabilities/tools, optionally filtered by category.',
    inputSchema: {
      type: 'object',
      properties: {
        category: {
          type: 'string',
          enum: ['knowledge', 'mailbox', 'identity', 'tasks', 'system'],
          description: 'Filter by category (optional)',
        },
      },
    },
  },
  {
    name: 'get_service_status',
    description: 'Get the health status of the SpaceOS Knowledge Service.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // Skill tools
  {
    name: 'list_skills',
    description: 'List all SpaceOS skills with their descriptions. Skills define terminal behaviors and workflows.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'get_skill',
    description: 'Get the full content of a specific skill including SKILL.md and reference files.',
    inputSchema: {
      type: 'object',
      properties: {
        skill_name: {
          type: 'string',
          description: 'Skill name (e.g., spaceos-terminal, spaceos-root, spaceos-conductor)',
        },
      },
      required: ['skill_name'],
    },
  },
  {
    name: 'get_workflow',
    description: 'Get the full SpaceOS WORKFLOW.md - defines the pipeline, mailbox system, and terminal architecture.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'get_terminal_setup',
    description: 'Get complete setup instructions for a terminal: CLAUDE.md, relevant skill, workflow excerpt, and MCP config.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'get_project_context',
    description: 'Get SpaceOS project context: vision, knowledge index, and codebase status.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // Terminal docs tools
  {
    name: 'list_terminal_docs',
    description: 'List all terminal documentation folders with their README status, port, and type.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'get_terminal_docs',
    description: 'Get terminal documentation README - quick reference for session startup, commands, and workflow.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'get_terminals_index',
    description: 'Get the main terminals INDEX.md - architecture overview, terminal list, MCP config.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // Terminal status tools (wake-on-inbox support)
  {
    name: 'register_working',
    description: 'Register terminal as working on a task. Prevents wake-up notifications while busy.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
        task_id: {
          type: 'string',
          description: 'Optional task ID being worked on',
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'register_idle',
    description: 'Register terminal as idle (finished task). Allows wake-up notifications.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: `Terminal name: ${TERMINALS.join(', ')}`,
        },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'get_terminal_status',
    description: 'Get the working/idle status of a terminal or all terminals.',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: {
          type: 'string',
          description: 'Terminal name (optional - omit for all terminals)',
        },
      },
    },
  },
];

// ─── Tool Handlers ──────────────────────────────────────────────────────────

async function handleToolCall(
  name: string,
  args: Record<string, unknown>
): Promise<{ content: Array<{ type: string; text: string }> }> {
  try {
    switch (name) {
      // Knowledge tools
      case 'search_knowledge': {
        const query = String(args.query || '');
        const limit = Math.min(Number(args.limit) || 5, 20);
        const results = await searchKnowledge(query, limit);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ query, limit, count: results.length, results }, null, 2),
            },
          ],
        };
      }

      // Mailbox tools
      case 'list_inbox': {
        const terminal = String(args.terminal || '');
        const status = (args.status as 'UNREAD' | 'READ' | 'all') || 'all';
        const messages = await listInbox(terminal, status);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ terminal, status, count: messages.length, messages }, null, 2),
            },
          ],
        };
      }

      case 'send_message': {
        const result = await sendMessage({
          to: String(args.to),
          type: String(args.type) as 'task' | 'question' | 'done' | 'blocked',
          content: String(args.content),
          priority: String(args.priority) as 'critical' | 'high' | 'medium' | 'low',
          model: args.model ? String(args.model) as 'haiku' | 'sonnet' | 'opus' : undefined,
          ref: args.ref ? String(args.ref) : undefined,
        });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ success: true, ...result }, null, 2),
            },
          ],
        };
      }

      case 'submit_done': {
        const result = await submitDone({
          from: String(args.from),
          task_id: String(args.task_id),
          summary: String(args.summary),
          files_changed: args.files_changed as string[],
        });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ success: true, ...result }, null, 2),
            },
          ],
        };
      }

      // Task tools
      case 'get_task_status': {
        const taskId = args.task_id ? String(args.task_id) : undefined;
        const tasks = await getTaskStatus(taskId);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ count: tasks.length, tasks }, null, 2),
            },
          ],
        };
      }

      // Identity tools
      case 'get_identity': {
        const terminal = String(args.terminal || '');
        const identity = await getIdentity(terminal);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(identity, null, 2),
            },
          ],
        };
      }

      case 'list_terminals': {
        const terminals = await listTerminals();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ count: terminals.length, terminals }, null, 2),
            },
          ],
        };
      }

      // Memory tools
      case 'read_memory': {
        const terminal = String(args.terminal || '');
        const memory = await readMemory(terminal);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ terminal, hasMemory: memory !== null, memory }, null, 2),
            },
          ],
        };
      }

      case 'write_memory': {
        const terminal = String(args.terminal || '');
        const content = String(args.content || '');
        const result = await writeMemory(terminal, content);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ terminal, ...result }, null, 2),
            },
          ],
        };
      }

      case 'append_memory': {
        const terminal = String(args.terminal || '');
        const content = String(args.content || '');
        const result = await appendMemory(terminal, content);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ terminal, ...result }, null, 2),
            },
          ],
        };
      }

      // System tools
      case 'get_capabilities': {
        const category = args.category ? String(args.category) : undefined;
        const capabilities = getCapabilities(category);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                count: capabilities.length,
                filter: category || 'all',
                capabilities,
              }, null, 2),
            },
          ],
        };
      }

      case 'get_service_status': {
        const count = await getDocumentCount();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                status: 'ok',
                vectorBackend: usingChroma() ? 'chromadb' : 'memory',
                embeddingBackend: embeddingBackend(),
                documents: count,
                terminals: TERMINALS.length,
                tools: TOOLS.length,
              }, null, 2),
            },
          ],
        };
      }

      // Skill tools
      case 'list_skills': {
        const skills = await listSkills();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ count: skills.length, skills }, null, 2),
            },
          ],
        };
      }

      case 'get_skill': {
        const skillName = String(args.skill_name || '');
        const skill = await getSkill(skillName);
        if (!skill) {
          return {
            content: [
              {
                type: 'text',
                text: JSON.stringify({ error: `Skill not found: ${skillName}` }, null, 2),
              },
            ],
          };
        }
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(skill, null, 2),
            },
          ],
        };
      }

      case 'get_workflow': {
        const workflow = await getWorkflow();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ hasWorkflow: workflow !== null, workflow }, null, 2),
            },
          ],
        };
      }

      case 'get_terminal_setup': {
        const terminal = String(args.terminal || '');
        const setup = await getTerminalSetup(terminal);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                terminal,
                hasClaudeMd: setup.claudeMd !== null,
                hasSkill: setup.skill !== null,
                ...setup,
              }, null, 2),
            },
          ],
        };
      }

      case 'get_project_context': {
        const context = await getProjectContext();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                hasVision: context.vision !== null,
                hasKnowledgeIndex: context.knowledgeIndex !== null,
                hasCodebaseStatus: context.codebaseStatus !== null,
                ...context,
              }, null, 2),
            },
          ],
        };
      }

      // Terminal docs tools
      case 'list_terminal_docs': {
        const docs = await listTerminalDocs();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ count: docs.length, terminals: docs }, null, 2),
            },
          ],
        };
      }

      case 'get_terminal_docs': {
        const terminal = String(args.terminal || '');
        const docs = await getTerminalDocs(terminal);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                terminal: docs.name,
                hasReadme: docs.readme !== null,
                ...docs,
              }, null, 2),
            },
          ],
        };
      }

      case 'get_terminals_index': {
        const index = await getTerminalsIndex();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({ hasIndex: index !== null, index }, null, 2),
            },
          ],
        };
      }

      // Terminal status tools
      case 'register_working': {
        const terminal = String(args.terminal || '');
        const taskId = args.task_id ? String(args.task_id) : undefined;
        registerWorking(terminal, taskId);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                success: true,
                terminal,
                state: 'working',
                taskId,
                message: `Terminal ${terminal} registered as WORKING`,
              }, null, 2),
            },
          ],
        };
      }

      case 'register_idle': {
        const terminal = String(args.terminal || '');
        registerIdle(terminal);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                success: true,
                terminal,
                state: 'idle',
                message: `Terminal ${terminal} registered as IDLE`,
              }, null, 2),
            },
          ],
        };
      }

      case 'get_terminal_status': {
        const terminal = args.terminal ? String(args.terminal) : undefined;
        if (terminal) {
          const status = getStatus(terminal);
          return {
            content: [
              {
                type: 'text',
                text: JSON.stringify({
                  terminal,
                  status: status || { state: 'idle', lastActivity: null },
                  shouldWakeUp: shouldWakeUp(terminal),
                }, null, 2),
              },
            ],
          };
        } else {
          const allStatus = getAllStatus();
          return {
            content: [
              {
                type: 'text',
                text: JSON.stringify({ terminals: allStatus }, null, 2),
              },
            ],
          };
        }
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({ error: msg }, null, 2),
        },
      ],
    };
  }
}

// ─── MCP JSON-RPC Handler ───────────────────────────────────────────────────

router.post('/', authenticate, async (req: Request, res: Response) => {
  const { jsonrpc, method, params, id } = req.body;

  if (jsonrpc !== '2.0') {
    res.status(400).json({
      jsonrpc: '2.0',
      error: { code: -32600, message: 'Invalid Request: jsonrpc must be "2.0"' },
      id: id || null,
    });
    return;
  }

  try {
    switch (method) {
      case 'initialize': {
        res.json({
          jsonrpc: '2.0',
          result: {
            protocolVersion: MCP_VERSION,
            serverInfo: {
              name: 'spaceos-knowledge-service',
              version: '1.4.0',
            },
            capabilities: {
              tools: {},
            },
          },
          id,
        });
        break;
      }

      case 'tools/list': {
        res.json({
          jsonrpc: '2.0',
          result: {
            tools: TOOLS,
          },
          id,
        });
        break;
      }

      case 'tools/call': {
        const { name, arguments: args } = params || {};
        if (!name) {
          res.status(400).json({
            jsonrpc: '2.0',
            error: { code: -32602, message: 'Invalid params: name is required' },
            id,
          });
          return;
        }

        const result = await handleToolCall(name, args || {});
        res.json({
          jsonrpc: '2.0',
          result,
          id,
        });
        break;
      }

      case 'notifications/initialized': {
        // Client notification, no response needed
        res.status(204).send();
        break;
      }

      default: {
        res.status(400).json({
          jsonrpc: '2.0',
          error: { code: -32601, message: `Method not found: ${method}` },
          id,
        });
      }
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({
      jsonrpc: '2.0',
      error: { code: -32603, message: msg },
      id,
    });
  }
});

// ─── MCP Info Endpoint (GET) ────────────────────────────────────────────────

router.get('/', (_req: Request, res: Response) => {
  res.json({
    name: 'spaceos-knowledge-service',
    version: '1.3.0',
    protocol: MCP_VERSION,
    description: 'SpaceOS Knowledge Service MCP Server - RAG search, mailbox, identity, memory, skills, workflow, terminal setup, terminal docs',
    tools: TOOLS.map(t => t.name),
    toolCount: TOOLS.length,
    terminals: TERMINALS,
    documentation: 'https://nexus.joinerytech.hu',
  });
});

export default router;
