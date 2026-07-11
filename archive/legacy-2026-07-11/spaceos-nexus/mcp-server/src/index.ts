import 'reflect-metadata';
import express, { Request, Response } from 'express';
import dotenv from 'dotenv';
import { RoleLoader } from './roles/RoleLoader';
import { queryKnowledge, initVectorStore } from './rag/VectorStore';
import { ResourceTracker } from './metadata/ResourceTracker';
import { WorkflowStateTracker } from './metadata/WorkflowStateTracker'; // WORKFLOW-STATE
import { DocumentServer } from './mcp/DocumentServer';
import { RbacFilter } from './mcp/RbacFilter';
import { SessionManager } from './mcp/SessionManager'; // WRITE-LAYER
import { AgentDb } from './mcp/AgentDb'; // EPIC-09 AGENTDB
import { DatabaseConnectionManager } from './metadata/DatabaseConnectionManager'; // EPIC-09 SECURITY
import { createMcpRouter } from './mcp/mcpRouter';
import { AgentDbSeeder } from './mcp/AgentDbSeeder'; // EPIC-17: Domain registry seeder
import { createMcpServerRouter, createPluginManager } from './mcp/mcpServer'; // TASK-14-02: createPluginManager
import { ResourceTemplateRegistry } from './mcp/resources/resourceTemplates'; // TASK-14-06: Dynamic URI resolution
import { SamplingService } from './mcp/sampling/SamplingService'; // TASK-14-09: Sampling & argument completion
import { BootstrapService } from './mcp/BootstrapService'; // TASK-10-01
import { TransportFactory } from './mcp/transports/TransportFactory'; // EPIC-14: Transport Abstraction
import { ITransport, TransportType } from './mcp/transports/ITransport'; // EPIC-14

import { ChatGoogleGenerativeAI } from '@langchain/google-genai';
import { StringOutputParser } from '@langchain/core/output_parsers';
import { ChatPromptTemplate, SystemMessagePromptTemplate, HumanMessagePromptTemplate } from '@langchain/core/prompts';
import * as path from 'path';
import { GuardrailService } from './roles/GuardrailService';
import { buildKnowledgeBaseIndex } from './rag/indexKnowledgeBase'; // RAG

dotenv.config();

const app = express();
const port = process.env.PORT || 3000;

app.use(express.json());

// ─── MCP Tool Surface ────────────────────────────────────────────────────────
// Must be registered AFTER app.use(express.json()) but BEFORE other routes


// __dirname = .../JoineryTech.McpServer/src
// Repo root   = ../  = JoineryTech.McpServer/
// DB root     = ../database = JoineryTech.McpServer/database
const workspaceRoot = path.join(__dirname, '..'); // JoineryTech.McpServer/
const databaseRoot = path.join(__dirname, '..', 'database'); // JoineryTech.McpServer/database
const roleLoader = new RoleLoader(databaseRoot);

const guardrailService = new GuardrailService(databaseRoot);

// Resource tracker (SQLite) for role-related files
const resourceTracker = new ResourceTracker();
// Workflow state tracker (SQLite) — WORKFLOW-STATE (rollback: delete these 2 lines + the import above)
const workflowTracker = new WorkflowStateTracker();
// Session manager (SQLite) — WRITE-LAYER (rollback: delete these 2 lines + the imports above)
const sessionManager = new SessionManager();

// EPIC-09 AgentDb service initialization (manages both EPIC-08 + EPIC-09 schemas)
// Uses dual-pool connection manager for security: admin (RW) + agent (RO)
const metadataDbPath = path.join(databaseRoot, 'metadata.db');
const connectionManager = new DatabaseConnectionManager(metadataDbPath);
const agentDb = new AgentDb(connectionManager);

// Initialize RBAC filter
const rbacFilter = new RbacFilter(agentDb);

try {
    agentDb.initSchema();
    console.log('[EPIC-09] ✅ AgentDb initialized with dual-pool security: ' + metadataDbPath);
} catch (error) {
    console.error('[EPIC-09] ❌ FATAL: AgentDb initialization failed:', error);
    process.exit(1);
}

// EPIC-17: Seed domain registry from filesystem (idempotent, runs on every start)
new AgentDbSeeder(agentDb).seedDomains(path.join(databaseRoot, 'roles'));


// Initialize MCP Document Server
// Pass resourceTracker so submitArtifact() shares the same DB instance (WRITE-LAYER)
// EPIC-16: Pass agentDb for DB-first reads (getRole, getWorkflow, getTemplate, getCore)
const documentServer = new DocumentServer(databaseRoot, workspaceRoot, resourceTracker, agentDb);

// TASK-10-01: Initialize BootstrapService for bootstrap_agent MCP tool
// Depends on agentDb + sessionManager (already initialized above)
const bootstrapService = new BootstrapService(agentDb, sessionManager);
console.log('[TASK-10-01] ✅ BootstrapService initialized');

// TASK-14-02: Initialize PluginManager for HTTPTransport tool routing
// Creates system context with all dependencies and loads plugins
const pluginManager = createPluginManager(agentDb, sessionManager, rbacFilter, workflowTracker, guardrailService);
console.log('[TASK-14-02] ✅ PluginManager initialized');

// TASK-14-06: Initialize ResourceTemplateRegistry for dynamic resource URI resolution
// Resolves resource:// URIs (role/{domain}/{role}, workflow/{type}, etc.) to file content
const resourceRegistry = new ResourceTemplateRegistry();
resourceRegistry.registerDefaults(databaseRoot, workspaceRoot);
console.log('[TASK-14-06] ✅ ResourceTemplateRegistry initialized');

// TASK-14-09: Initialize SamplingService for option-based argument clarification
// Shared queue: HTTPTransport exposes /mcp/sampling/pending + /mcp/sampling/resolve
const samplingService = new SamplingService();
console.log('[TASK-14-09] ✅ SamplingService initialized');

// ─── EPIC-14 PHASE 1: Transport Abstraction Layer ──────────────────────────
// Select transport based on environment variable (stdio | http)
const mcpTransportType = (process.env.MCP_TRANSPORT || 'stdio') as unknown as TransportType;
let transport: ITransport | null = null;

async function initTransport(): Promise<void> {
    try {
        const transportConfig = {
            type: mcpTransportType,
            port: process.env.MCP_PORT ? parseInt(process.env.MCP_PORT, 10) : 3000,
            host: process.env.MCP_HOST || 'localhost',
            pluginManager,       // TASK-14-02: Pass pluginManager for /mcp/call endpoint
            resourceRegistry,    // TASK-14-06: Pass resourceRegistry for resource:// URI resolution
            samplingService      // TASK-14-09: Pass samplingService for /mcp/sampling endpoints
        };
        transport = TransportFactory.create(transportConfig);
        console.log(`[EPIC-14] ✅ Transport instance created: ${mcpTransportType} (port=${transportConfig.port}, host=${transportConfig.host})`);

        // establish connection/listener
        await transport.connect();
        console.log(`[EPIC-14] ✅ Transport connected (${mcpTransportType})`);
    } catch (error: any) {
        console.error(`[EPIC-14] ❌ Transport initialization failed:`, error.message);
        process.exit(1);
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// GRACEFUL SHUTDOWN — Close database connections + transport on SIGTERM/SIGINT
// ─────────────────────────────────────────────────────────────────────────────

const shutdown = async () => {
    console.log('[Shutdown] Initiating graceful shutdown...');

    // EPIC-14: Graceful transport shutdown (disconnect clients, drain connections)
    if (transport) {
        try {
            console.log('[Shutdown] Closing MCP transport connections...');
            // HTTPTransport provides a more sophisticated shutdown handler that drains
            // active connections.  We detect and invoke it if available so the generic
            // disconnect() call doesn’t immediately abort in‑flight requests.
            if ((transport as any).initiateShutdown && typeof (transport as any).initiateShutdown === 'function') {
                await (transport as any).initiateShutdown();
            } else {
                await transport.disconnect();
            }
            console.log('[Shutdown] ✅ MCP transport closed gracefully');
        } catch (error: any) {
            console.error('[Shutdown] ⚠️ Error closing MCP transport:', error.message);
        }
    }

    // EPIC-11, TASK-10-01: Close database connections
    try {
        agentDb.close?.();
        sessionManager?.close?.();
        console.log('[Shutdown] ✅ All database connections closed');
    } catch (error) {
        console.error('[Shutdown] ❌ Error closing databases:', error);
    }

    process.exit(0);
};

process.on('SIGTERM', () => shutdown().catch(err => {
    console.error('[Shutdown] Fatal error during SIGTERM:', err);
    process.exit(1);
}));
process.on('SIGINT', () => shutdown().catch(err => {
    console.error('[Shutdown] Fatal error during SIGINT:', err);
    process.exit(1);
}));

// Utility LLM initialization
const getLLM = () => {
    if (!process.env.GOOGLE_API_KEY) {
        throw new Error('GOOGLE_API_KEY environment variable is missing.');
    }

    return new ChatGoogleGenerativeAI({
        model: 'gemini-2.5-flash', // Default model, can be parameterized
        maxOutputTokens: 2048,
        temperature: 0.2, // Keep temperature low for deterministic role adherence
        maxRetries: 1, // Prevent indefinite hanging during rate limitation
    });
};

/**
 * Endpoint to execute a task using a specific role (hat).
 * POST /api/execute
 * {
 *   "domain": "engineering",
 *   "role": "backend_developer",
 *   "task": "Please analyze the following bug..."
 * }
 */
// REST API document router (human-readable, browser-friendly)
// WRITE-LAYER: pass resourceTracker + sessionManager for artifact/submit and session endpoints
// EPIC-09: pass agentDb for role/schema/runbook queries
app.use('/mcp', createMcpRouter(documentServer, resourceTracker, workflowTracker, sessionManager, agentDb));

// MCP protocol router (SSE + Streamable HTTP — for Copilot Chat / MCP clients)
// SSE endpoint:            GET  http://localhost:3000/mcp/sse
// SSE message endpoint:    POST http://localhost:3000/mcp/sse-message
// Streamable HTTP:         POST http://localhost:3000/mcp/http
// Build the MCP router once so we can mount it on whichever app is appropriate
const mcpRouter = createMcpServerRouter(
    documentServer,
    workflowTracker,
    rbacFilter,
    sessionManager,
    resourceTracker,
    agentDb,
    bootstrapService,
    guardrailService
); // TASK-10-01 + WORKFLOW-STATE + RBAC + WRITE-LAYER + EPIC-09 + TASK-14-06

// Start transport and HTTP routing (must happen before app.listen)
(async () => {
    await initTransport();

    // If we're using HTTP transport, mount the router on the transport's express
    // app since that is the channel the agent will hit. Otherwise fall back to
    // our local `app` instance (used for /api endpoints).
    if (transport && mcpTransportType === TransportType.HTTP &&
        typeof (transport as any).getExpressApp === 'function') {
        const transportApp = (transport as any).getExpressApp();
        transportApp.use('/mcp', mcpRouter);
        console.log('[EPIC-14] ✅ MCP router mounted on HTTP transport app');
    } else {
        app.use('/mcp', mcpRouter);
    }

    // ─── Knowledge Search (RAG) ──────────────────────────────────────────────────
    /**
     * POST /api/knowledge/search
     * { "query": "...", "domain"?: "engineering", "topK"?: 5 }
     */
    app.post('/api/knowledge/search', async (req: Request, res: Response): Promise<void> => {
        try {
            const { query, domain, topK } = req.body;
            if (!query) {
                res.status(400).json({ error: 'Missing required field: query' });
                return;
            }
            const chunks = await queryKnowledge(query, topK ?? 5, domain);
            res.json({ query, domain, topK: topK ?? 5, results: chunks });
        } catch (e: any) {
            res.status(500).json({ error: e.message });
        }
    });

    // ─── Resource endpoints ──────────────────────────────────────────────────────
    // Resource endpoints
    app.post('/api/resource', (req: Request, res: Response) => {
        const { project, user, type, relative_path, hash } = req.body;
        if (!project || !user || !type || !relative_path) {
            res.status(400).json({ error: 'Missing required fields: project, user, type, relative_path' });
            return;
        }
        const record = resourceTracker.addOrUpdate({ project, user, type, relative_path, hash });
        res.json(record);
    });

    app.post('/api/resource/archive', (req: Request, res: Response) => {
        const { id } = req.body;
        if (!id) {
            res.status(400).json({ error: 'Missing id' });
            return;
        }
        resourceTracker.archive(id);
        res.json({ ok: true });
    });

    app.post('/api/resource/move', (req: Request, res: Response) => {
        const { id, newPath } = req.body;
        if (!id || !newPath) {
            res.status(400).json({ error: 'Missing id or newPath' });
            return;
        }
        resourceTracker.move(id, newPath);
        res.json({ ok: true });
    });

    app.get('/api/resource', (req: Request, res: Response) => {
        const project = req.query.project as string | undefined;
        const list = resourceTracker.list(project);
        res.json(list);
    });

    // simple admin dashboard for resources
    app.get('/admin/resources', (req: Request, res: Response) => {
        const list = resourceTracker.list();
        let html = `<!doctype html><html><head><meta charset="utf-8"><title>Resources</title></head><body><h1>Tracked Resources</h1><table border="1" cellpadding="4"><tr><th>ID</th><th>Project</th><th>User</th><th>Type</th><th>Path</th><th>Status</th><th>Created</th><th>Modified</th><th>Archived</th></tr>`;
        for (const r of list) {
            html += `<tr><td>${r.id}</td><td>${r.project}</td><td>${r.user}</td><td>${r.type}</td><td>${r.relative_path}</td><td>${r.status}</td><td>${r.created_at}</td><td>${r.modified_at || ''}</td><td>${r.archived_at || ''}</td></tr>`;
        }
        html += `</table></body></html>`;
        res.send(html);
    });

    app.post('/api/execute', async (req: Request, res: Response): Promise<void> => {
        try {
            const { domain, role, task, project_id } = req.body;

            if (!domain || !role || !task) {
                res.status(400).json({ error: 'Missing required parameters: domain, role, task' });
                return;
            }

            let retryCount = 0;
            if (project_id) {
                const state = workflowTracker.getState(project_id);
                if (state) {
                    // retryCount = state.retry_count; // retry_count no longer exists in SessionState
                    console.log(`   [FSM] Project ${project_id} is at state ${state.state}`);
                }
            }

            // 1. Load the Role definition (The "Hat")
            const roleSchema = roleLoader.loadRole(domain, role);
            const roleSystemPrompt = roleLoader.generateSystemPrompt(roleSchema, domain, retryCount);

            // 1b. Retrieve relevant domain knowledge from the vector store (RAG)
            const relevantChunks = await queryKnowledge(task, 5, domain);
            const knowledgeSection = relevantChunks.length > 0
                ? `\n\n<RELEVANT_STANDARDS>\n${relevantChunks.join('\n\n---\n\n')}\n</RELEVANT_STANDARDS>\n`
                : '';
            const systemPromptText = roleSystemPrompt + knowledgeSection;

            console.log(`   [RAG] Retrieved ${relevantChunks.length} knowledge chunks for domain: ${domain}`);

            // 2. Setup LLM Chain with the defined Role + injected knowledge
            const llm = getLLM();

            const prompt = ChatPromptTemplate.fromMessages([
                SystemMessagePromptTemplate.fromTemplate(systemPromptText),
                HumanMessagePromptTemplate.fromTemplate("{task}")
            ]);

            const outputParser = new StringOutputParser();
            const chain = prompt.pipe(llm).pipe(outputParser);

            // 3. Execute the Task
            console.log(`Executing task as [${domain} / ${roleSchema.role}]...`);
            const response = await chain.invoke({
                task: task
            });

            // 4. Guardrail Interceptor (Evaluator)
            console.log(`   [Guardrail] Initiating compliance check...`);
            try {
                const evaluation = await guardrailService.checkCompliance(domain, role, task, response);

                if (evaluation.verdict === 'FAIL') {
                    console.warn(`   [Guardrail] ❌ Role Violation Blocked! Reason: ${evaluation.reasoning}`);
                    res.status(403).json({
                        error: 'Role Violation: The agent attempted an action outside its permitted scope.',
                        reasoning: evaluation.reasoning,
                        internal_agent_response: response // Optional: Can omit in strict environments
                    });
                    return;
                } else {
                    console.log(`   [Guardrail] ✅ Response passed compliance check.`);
                }
            } catch (guardrailError: any) {
                console.error(`   [Guardrail] Critical error during evaluation:`, guardrailError.message);
                // Defaulting to fail closed on error for safety
                res.status(500).json({
                    error: 'Internal Guardrail Error: Validating the response failed.',
                    details: guardrailError.message
                });
                return;
            }

            res.json({
                role: roleSchema.role,
                domain: domain,
                system_prompt_used: systemPromptText,
                result: response
            });
        } catch (error: any) {
            console.error('Execution Error:', error.message);
            res.status(500).json({ error: error.message });
        }
    });

    // Pre-warm vector store connection at startup
    initVectorStore()
        .then(async () => {
            console.log('🔍 [RAG] Vector store ready.');
            // Automatically build/refresh index at startup for MVP/Memory mode
            await buildKnowledgeBaseIndex();
        })
        .catch(err => console.warn('⚠️ [RAG] Vector store init failed at startup:', err.message));

    app.listen(port, () => {
        console.log(`Agent Server running on port ${port}`);
        console.log(`Ready to load roles from: ${databaseRoot}/roles`);
    });
})();