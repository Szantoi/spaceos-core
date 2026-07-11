import { z } from 'zod';
import { McpContext } from '../middleware/contextMiddleware';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';

/**
 * Messaging Plugin — SQLite Message Queue integration for MCP
 *
 * Enables daemon-to-daemon communication through the Datahaven message queue.
 * Connects to the SQLite queue at DATAHAVEN_HOME/messages.db
 */

interface Message {
    id: number;
    from_daemon: string;
    to_daemon: string;
    msg_type: string;
    priority: string;
    subject: string | null;
    payload: any;
    correlation_id: string;
    status: string;
    created_at: string;
}

interface QueueStats {
    total: number;
    by_status: Record<string, number>;
    pending_by_daemon: Record<string, number>;
}

@Plugin({
    name: "messaging",
    version: "1.0.0",
    dependencies: []
})
export class MessagingPlugin extends BasePlugin {
    private dbPath: string;

    constructor(context: any) {
        super(context);
        const datahavenHome = process.env.DATAHAVEN_HOME || '/opt/datahaven';
        this.dbPath = `${datahavenHome}/messages.db`;
    }

    /**
     * Execute Python messaging command and return result
     */
    private async execMessaging(code: string): Promise<any> {
        const { spawn } = await import('child_process');

        return new Promise((resolve, reject) => {
            const pythonCode = `
import sys
import json
sys.path.insert(0, '${process.env.DATAHAVEN_CORE || '/home/gabor/datahaven-core'}')
from messaging.queue import MessageQueue

mq = MessageQueue('${this.dbPath}')
result = ${code}
print(json.dumps(result, default=str))
`;
            const proc = spawn('python3', ['-c', pythonCode]);
            let stdout = '';
            let stderr = '';

            proc.stdout.on('data', (data) => { stdout += data.toString(); });
            proc.stderr.on('data', (data) => { stderr += data.toString(); });

            proc.on('close', (exitCode) => {
                if (exitCode !== 0) {
                    reject(new Error(`Python error: ${stderr}`));
                } else {
                    try {
                        resolve(JSON.parse(stdout.trim()));
                    } catch {
                        resolve(stdout.trim());
                    }
                }
            });

            proc.on('error', reject);
        });
    }

    @Tool({
        name: "mq_send",
        description: "Send a message to another daemon via the message queue",
        schema: z.object({
            to_daemon: z.string().describe("Target daemon ID (e.g., 'kernel', 'architect')"),
            msg_type: z.enum(["task", "done", "blocked", "query", "response", "event"]),
            subject: z.string().optional(),
            payload: z.record(z.any()).optional(),
            priority: z.enum(["critical", "high", "medium", "low"]).default("medium"),
            ttl_seconds: z.number().optional().describe("Time-to-live in seconds")
        })
    })
    async mqSend(args: {
        to_daemon: string;
        msg_type: string;
        subject?: string;
        payload?: Record<string, any>;
        priority?: string;
        ttl_seconds?: number;
    }, context: McpContext) {
        const fromDaemon = context.role || 'mcp-agent';
        const payloadJson = args.payload ? JSON.stringify(args.payload).replace(/'/g, "\\'") : 'None';
        const subjectStr = args.subject ? `'${args.subject.replace(/'/g, "\\'")}'` : 'None';
        const ttlStr = args.ttl_seconds ? args.ttl_seconds.toString() : 'None';

        try {
            const msgId = await this.execMessaging(`
mq.send(
    from_daemon='${fromDaemon}',
    to_daemon='${args.to_daemon}',
    msg_type='${args.msg_type}',
    subject=${subjectStr},
    payload=${payloadJson !== 'None' ? payloadJson : 'None'},
    priority='${args.priority || 'medium'}',
    ttl_seconds=${ttlStr}
)
`);
            return {
                success: true,
                data: {
                    message_id: msgId,
                    from: fromDaemon,
                    to: args.to_daemon,
                    type: args.msg_type
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_receive",
        description: "Receive pending messages for this daemon",
        schema: z.object({
            limit: z.number().default(10),
            msg_types: z.array(z.string()).optional().describe("Filter by message types"),
            peek: z.boolean().default(false).describe("If true, don't mark as delivered")
        })
    })
    async mqReceive(args: {
        limit?: number;
        msg_types?: string[];
        peek?: boolean;
    }, context: McpContext) {
        const daemonId = context.role || 'mcp-agent';
        const limit = args.limit || 10;

        try {
            let messages: Message[];
            if (args.peek) {
                messages = await this.execMessaging(`mq.peek('${daemonId}', ${limit})`);
            } else {
                const typesFilter = args.msg_types ? JSON.stringify(args.msg_types) : 'None';
                messages = await this.execMessaging(`mq.receive('${daemonId}', ${limit}, ${typesFilter})`);
            }

            return {
                success: true,
                data: {
                    daemon: daemonId,
                    count: messages.length,
                    messages: messages
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_ack",
        description: "Acknowledge a message as processed",
        schema: z.object({
            message_id: z.number(),
            nack: z.boolean().default(false).describe("Negative acknowledge (failed)"),
            requeue: z.boolean().default(true).describe("Requeue on nack")
        })
    })
    async mqAck(args: {
        message_id: number;
        nack?: boolean;
        requeue?: boolean;
    }, _context: McpContext) {
        try {
            let result: boolean;
            if (args.nack) {
                result = await this.execMessaging(`mq.nack(${args.message_id}, requeue=${args.requeue ? 'True' : 'False'})`);
            } else {
                result = await this.execMessaging(`mq.ack(${args.message_id})`);
            }

            return {
                success: true,
                data: {
                    message_id: args.message_id,
                    action: args.nack ? (args.requeue ? 'requeued' : 'failed') : 'processed',
                    result
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_query",
        description: "Send a query and wait for response from another daemon",
        schema: z.object({
            to_daemon: z.string(),
            question: z.string(),
            payload: z.record(z.any()).optional(),
            timeout_seconds: z.number().default(60)
        })
    })
    async mqQuery(args: {
        to_daemon: string;
        question: string;
        payload?: Record<string, any>;
        timeout_seconds?: number;
    }, context: McpContext) {
        const fromDaemon = context.role || 'mcp-agent';
        const payloadJson = args.payload ? JSON.stringify(args.payload).replace(/'/g, "\\'") : 'None';
        const timeout = args.timeout_seconds || 60;

        try {
            // Send query
            const correlationId = await this.execMessaging(`
mq.query('${fromDaemon}', '${args.to_daemon}', '${args.question.replace(/'/g, "\\'")}', ${payloadJson !== 'None' ? payloadJson : 'None'}, ${timeout})
`);

            // Wait for response (with shorter timeout for MCP context)
            const waitTimeout = Math.min(timeout, 30); // Cap at 30s for MCP
            const response = await this.execMessaging(`mq.wait_response('${correlationId}', ${waitTimeout})`);

            if (response) {
                return {
                    success: true,
                    data: {
                        correlation_id: correlationId,
                        response: response
                    }
                };
            } else {
                return {
                    success: false,
                    error: { code: 408, message: 'Query timeout - no response received' }
                };
            }
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_reply",
        description: "Reply to a received message",
        schema: z.object({
            original_message_id: z.number(),
            original_from: z.string(),
            original_correlation_id: z.string(),
            payload: z.record(z.any()).optional(),
            subject: z.string().optional()
        })
    })
    async mqReply(args: {
        original_message_id: number;
        original_from: string;
        original_correlation_id: string;
        payload?: Record<string, any>;
        subject?: string;
    }, context: McpContext) {
        const fromDaemon = context.role || 'mcp-agent';
        const payloadJson = args.payload ? JSON.stringify(args.payload).replace(/'/g, "\\'") : '{}';
        const subjectStr = args.subject ? `'${args.subject.replace(/'/g, "\\'")}'` : 'None';

        try {
            const msgId = await this.execMessaging(`
mq.send(
    from_daemon='${fromDaemon}',
    to_daemon='${args.original_from}',
    msg_type='response',
    subject=${subjectStr},
    payload=${payloadJson},
    correlation_id='${args.original_correlation_id}',
    ref_msg_id=${args.original_message_id}
)
`);
            return {
                success: true,
                data: {
                    message_id: msgId,
                    reply_to: args.original_from,
                    correlation_id: args.original_correlation_id
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_stats",
        description: "Get message queue statistics",
        schema: z.object({})
    })
    async mqStats(_args: {}, _context: McpContext) {
        try {
            const stats: QueueStats = await this.execMessaging('mq.stats()');
            return {
                success: true,
                data: stats
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_register_daemon",
        description: "Register this agent as a daemon in the message queue",
        schema: z.object({
            daemon_id: z.string().optional().describe("Daemon ID (defaults to role)"),
            name: z.string(),
            description: z.string().optional(),
            capabilities: z.array(z.string()).optional()
        })
    })
    async mqRegisterDaemon(args: {
        daemon_id?: string;
        name: string;
        description?: string;
        capabilities?: string[];
    }, context: McpContext) {
        const daemonId = args.daemon_id || context.role || 'mcp-agent';
        const descStr = args.description ? `'${args.description.replace(/'/g, "\\'")}'` : 'None';
        const capsStr = args.capabilities ? JSON.stringify(args.capabilities) : 'None';

        try {
            await this.execMessaging(`
mq.register_daemon('${daemonId}', '${args.name.replace(/'/g, "\\'")}', ${descStr}, ${capsStr})
`);
            return {
                success: true,
                data: {
                    daemon_id: daemonId,
                    registered: true
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_heartbeat",
        description: "Send heartbeat to keep daemon status online",
        schema: z.object({
            status: z.enum(["online", "busy", "offline"]).default("online")
        })
    })
    async mqHeartbeat(args: { status?: string }, context: McpContext) {
        const daemonId = context.role || 'mcp-agent';
        const status = args.status || 'online';

        try {
            await this.execMessaging(`mq.heartbeat('${daemonId}', '${status}')`);
            return {
                success: true,
                data: {
                    daemon_id: daemonId,
                    status,
                    timestamp: new Date().toISOString()
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }

    @Tool({
        name: "mq_list_daemons",
        description: "List all registered daemons",
        schema: z.object({
            online_only: z.boolean().default(false)
        })
    })
    async mqListDaemons(args: { online_only?: boolean }, _context: McpContext) {
        try {
            const daemons = await this.execMessaging(`mq.list_daemons(online_only=${args.online_only ? 'True' : 'False'})`);
            return {
                success: true,
                data: {
                    count: daemons.length,
                    daemons
                }
            };
        } catch (error) {
            return {
                success: false,
                error: { code: 500, message: error instanceof Error ? error.message : 'Unknown error' }
            };
        }
    }
}

export default MessagingPlugin;
