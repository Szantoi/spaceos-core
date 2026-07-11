import express, { Express, Request, Response, NextFunction } from "express";
import cors from "cors";
import http from "http";
import { Socket as NetSocket } from "net";
import { BaseTransport, TransportErrorContext, TransportState, TransportConfig, TransportType, TransportInfo } from './ITransport';
import { ErrorDiagnoser } from './ErrorDiagnoser';
import { PluginManager } from '../../plugins/PluginManager';
import { McpContext } from '../middleware/contextMiddleware';
import { ResourceResolutionError, ResourceTemplateRegistry } from '../resources/resourceTemplates';
import { SamplingService } from '../sampling/SamplingService';

export class HTTPTransport extends BaseTransport {
    private app: Express;
    private server: http.Server | null = null;
    private shuttingDown = false;
    private activeConnections = new Set<NetSocket>();
    private shutdownTimeout = 30000; // milliseconds

    // configuration helpers (may be passed via TransportConfig or env)
    private corsOrigin: string;
    private requestTimeout?: number;
    private maxConnections?: number;
    private pluginManager?: PluginManager;
    private resourceRegistry?: ResourceTemplateRegistry;
    private samplingService?: SamplingService;

    /**
     * Exposed for tests to override the shutdown timeout (default 30s).
     */
    public setShutdownTimeout(ms: number): void {
        this.shutdownTimeout = ms;
    }

    constructor(config: TransportConfig) {
        if (config.type !== TransportType.HTTP) {
            throw new Error(`Invalid transport configuration type for HTTPTransport: ${config.type}`);
        }
        super(config);

        // read optional http-specific configuration values
        this.corsOrigin = (config as any).corsOrigin || process.env.MCP_CORS_ORIGIN || '*';
        this.requestTimeout = (config as any).requestTimeout || (process.env.MCP_REQUEST_TIMEOUT ? parseInt(process.env.MCP_REQUEST_TIMEOUT, 10) : undefined);
        this.maxConnections = (config as any).maxConnections || (process.env.MCP_MAX_CONNECTIONS ? parseInt(process.env.MCP_MAX_CONNECTIONS, 10) : undefined);
        this.pluginManager = (config as any).pluginManager || undefined;
        this.resourceRegistry = (config as any).resourceRegistry || undefined;
        this.samplingService = (config as any).samplingService || undefined;

        // create express instance early so tests can inspect
        this.app = express();
        this.setupRoutes();
    }

    /**
     * Allow setting pluginManager after construction (e.g., after PluginManager initialization).
     * Used to decouple transport creation from plugin loading.
     */
    public setPluginManager(pluginManager: PluginManager): void {
        this.pluginManager = pluginManager;
    }

    public setResourceRegistry(resourceRegistry: ResourceTemplateRegistry): void {
        this.resourceRegistry = resourceRegistry;
    }

    public setSamplingService(samplingService: SamplingService): void {
        this.samplingService = samplingService;
    }

    /**
     * Public getter used by tests to inspect the underlying Express app.
     */
    getExpressApp(): Express {
        return this.app;
    }

    /**
     * Expose port for zero‑port binding tests.
     */
    getPort(): number | undefined {
        if (this.server) {
            const addr = this.server.address();
            if (addr && typeof addr === 'object') {
                return addr.port;
            }
        }
        return undefined;
    }

    private setupRoutes(): void {
        // install JSON parser middleware BEFORE any routes
        this.app.use(express.json());

        // install CORS middleware first
        this.app.use(cors({
            origin: this.corsOrigin,
            credentials: true,
        }));

        // middleware to track active connections per request
        this.app.use((req: Request, res: Response, next: NextFunction) => {
            const socket = req.socket;
            this.activeConnections.add(socket);
            console.debug(`[HTTPTransport] connection opened (active=${this.activeConnections.size})`);
            res.on('finish', () => {
                this.activeConnections.delete(socket);
                console.debug(`[HTTPTransport] connection closed (active=${this.activeConnections.size})`);
            });
            next();
        });

        // Health check endpoint (AC-21/AC-22)
        this.app.get('/health', (req: Request, res: Response) => {
            const health = this.getHealthStatus();

            if (health.status === 'SHUTTING_DOWN') {
                return res.status(503).json({
                    status: health.status,
                    activeConnections: health.activeConnections,
                    message: 'Server is shutting down. Do not send new requests.'
                });
            }

            return res.status(200).json({
                status: health.status,
                activeConnections: health.activeConnections,
                uptime: process.uptime()
            });
        });

        this.app.get('/mcp/resources', (req: Request, res: Response) => {
            const resources = this.resourceRegistry ? this.resourceRegistry.listResources() : [];
            return res.status(200).json({ status: 'success', resources });
        });

        this.app.get('/mcp/resources/resolve', async (req: Request, res: Response) => {
            const uri = req.query.uri;

            if (typeof uri !== 'string' || uri.trim().length === 0) {
                return res.status(400).json({
                    status: 'error',
                    error: 'Query parameter "uri" is required',
                    code: 'INVALID_REQUEST'
                });
            }

            if (!this.resourceRegistry) {
                return res.status(503).json({
                    status: 'error',
                    error: 'Resource template registry is not configured',
                    code: 'SERVICE_UNAVAILABLE'
                });
            }

            try {
                const resource = await this.resourceRegistry.resolveUri(uri);
                return res.status(200).json({ status: 'success', resource });
            } catch (error) {
                if (error instanceof ResourceResolutionError) {
                    return res.status(error.statusCode).json({
                        status: 'error',
                        error: error.message,
                        code: error.statusCode === 404 ? 'NOT_FOUND' : 'INVALID_RESOURCE_URI'
                    });
                }

                const message = error instanceof Error ? error.message : 'Unknown resource resolution error';
                return res.status(500).json({ status: 'error', error: message, code: 'INTERNAL_ERROR' });
            }
        });

        this.app.get('/mcp/sampling/pending', (req: Request, res: Response) => {
            if (!this.samplingService) {
                return res.status(200).json({ status: 'success', requests: [] });
            }

            const sessionId = typeof req.query.session_id === 'string' ? req.query.session_id : undefined;
            const requests = this.samplingService.listPending(sessionId);
            return res.status(200).json({ status: 'success', requests, pending: requests });
        });

        this.app.post('/mcp/sampling/respond', (req: Request, res: Response) => {
            if (!this.samplingService) {
                return res.status(503).json({
                    status: 'error',
                    error: 'Sampling service is not configured',
                    code: 'SERVICE_UNAVAILABLE'
                });
            }

            const { request_id, selected } = req.body as { request_id?: string; selected?: string[] };
            if (!request_id || !Array.isArray(selected)) {
                return res.status(400).json({
                    status: 'error',
                    error: 'Fields request_id and selected[] are required',
                    code: 'INVALID_REQUEST'
                });
            }

            const accepted = this.samplingService.resolveSampling(request_id, selected);
            if (!accepted) {
                return res.status(404).json({
                    status: 'error',
                    error: `Sampling request not found: ${request_id}`,
                    code: 'NOT_FOUND'
                });
            }

            return res.status(200).json({ status: 'success', request_id });
        });

        // MCP Tool invocation endpoint (TASK-14-02)
        // POST /mcp/call
        // Body: { tool_name: string, arguments: any }
        // Returns: { status: "success", data: result } or { status: "error", error: string, code?: string }
        this.app.post('/mcp/call', async (req: Request, res: Response) => {
            const { tool_name, arguments: toolArgs } = req.body;

            if (!tool_name || typeof tool_name !== 'string') {
                return res.status(400).json({
                    status: 'error',
                    error: 'Missing or invalid required field: tool_name (string)',
                    code: 'INVALID_REQUEST'
                });
            }

            if (!this.pluginManager) {
                return res.status(503).json({
                    status: 'error',
                    error: 'Plugin system not initialized on this HTTP transport',
                    code: 'SERVICE_UNAVAILABLE'
                });
            }

            try {
                // Create a minimal McpContext for tool invocation
                // In a full implementation, this would include session info, auth, audit data
                const mcpContext: Partial<McpContext> = {
                    session_id: req.headers['x-session-id'] as string || undefined,
                    track: 'standard' as any, // placeholder
                } as Partial<McpContext>;

                if (this.samplingService && mcpContext.session_id) {
                    mcpContext.requestSampling = async (samplingRequest) => {
                        return this.samplingService!.requestSampling(mcpContext.session_id!, samplingRequest);
                    };
                }

                // Invoke the tool via plugin manager
                const result = await this.pluginManager.invokeTool(
                    tool_name,
                    toolArgs || {},
                    mcpContext
                );

                console.debug(`[HTTPTransport.call] Tool "${tool_name}" invoked successfully`);
                return res.status(200).json({
                    status: 'success',
                    data: result
                });

            } catch (error: any) {
                const message = error?.message || 'Unknown error during tool invocation';
                const code = error?.code || error?.name || 'TOOL_ERROR';

                console.error(`[HTTPTransport.call] Tool "${tool_name}" failed:`, error);
                return res.status(500).json({
                    status: 'error',
                    error: message,
                    code,
                    tool: tool_name
                });
            }
        });

        // placeholder endpoint so transport isn't completely useless
        this.app.post('/tool/:name', (req: Request, res: Response) => {
            res.status(200).json({ ok: true, tool: req.params.name });
        });

        // test helper: delay response by specified ms
        if (process.env.NODE_ENV === 'test') {
            this.app.get('/_test/delay/:ms', async (req: Request, res: Response) => {
                const ms = parseInt((req.params.ms as string), 10) || 0;
                await new Promise((r) => setTimeout(r, ms));
                res.json({ delayed: ms });
            });
        }
    }

    private getHealthStatus(): { status: 'HEALTHY' | 'SHUTTING_DOWN'; activeConnections: number } {
        return {
            status: this.shuttingDown ? 'SHUTTING_DOWN' : 'HEALTHY',
            activeConnections: this.activeConnections.size
        };
    }

    async connect(): Promise<void> {
        this.state = TransportState.INITIALIZING;
        return new Promise((resolve, reject) => {
            try {
                const port = this.config.port !== undefined ? this.config.port : 3000;
                const host = this.config.host || 'localhost';
                this.server = this.app.listen(port, host, () => {
                    this.state = TransportState.CONNECTED;
                    // apply optional server properties
                    if (this.requestTimeout && this.server) {
                        this.server.timeout = this.requestTimeout;
                    }
                    if (this.maxConnections && this.server) {
                        this.server.maxConnections = this.maxConnections;
                    }

                    // once listening we can track connections and install shutdown hooks
                    this.setupConnectionTracking();
                    this.setupGracefulShutdown();
                    resolve();
                });
                // handle listen errors (e.g. port in use)
                this.server.on('error', (err) => {
                    this.state = TransportState.ERROR;
                    reject(err);
                });
            } catch (error) {
                this.state = TransportState.ERROR;
                reject(error);
            }
        });
    }

    private setupConnectionTracking(): void {
        if (!this.server) return;
        this.server.on('connection', (socket) => {
            const key = `${socket.remoteAddress}:${socket.remotePort}`;
            this.activeConnections.add(socket);
            console.debug(`[HTTPTransport] socket connected ${key} (active=${this.activeConnections.size})`);
            socket.on('close', () => {
                this.activeConnections.delete(socket);
                console.debug(`[HTTPTransport] socket closed ${key} (active=${this.activeConnections.size})`);
            });
        });
    }

    async disconnect(): Promise<void> {
        if (!this.server) return;
        this.state = TransportState.DISCONNECTING;
        return new Promise((resolve) => {
            this.server!.close(() => {
                this.state = TransportState.DISCONNECTED;
                resolve();
            });
        });
    }

    private setupGracefulShutdown(): void {
        // use arrow so `this` is preserved
        const handler = () => {
            // during testing we don't want to actually exit
            if (process.env.NODE_ENV === 'test') {
                // call shutdown logic but do not exit process
                this.handleShutdown().catch((e) => console.error(e));
            } else {
                this.handleShutdown().then(() => {
                    process.exit(0);
                }).catch(() => process.exit(1));
            }
        };
        process.on('SIGTERM', handler);
        process.on('SIGINT', handler);
    }

    /**
     * Exposed for tests so they can simulate a shutdown without sending a signal.
     */
    async initiateShutdown(): Promise<void> {
        return this.handleShutdown();
    }

    private async handleShutdown(): Promise<void> {
        if (this.shuttingDown) {
            return;
        }
        console.log('[SHUTDOWN] Graceful shutdown initiated');
        this.shuttingDown = true;

        // Give any in-flight connections a brief moment to be established before
        // we stop accepting new ones. This allows clients that already started
        // connecting to complete their handshake without ECONNREFUSED.
        await new Promise((r) => setTimeout(r, 10));

        // stop accepting new connections
        if (this.server) {
            this.server.close();
        }

        const drainStart = Date.now();
        while (this.activeConnections.size > 0 && Date.now() - drainStart < this.shutdownTimeout) {
            await new Promise((r) => setTimeout(r, 100));
        }

        if (this.activeConnections.size > 0) {
            // force close remaining
            for (const sock of Array.from(this.activeConnections)) {
                try {
                    sock.destroy();
                } catch { }
                this.activeConnections.delete(sock);
            }
        }

        await this.disconnect();
        console.log('[SHUTDOWN] Graceful shutdown complete');
    }

    async diagnoseError(error: Error | any): Promise<TransportErrorContext> {
        let statusCode: number | undefined;
        if (error && typeof error === 'object' && 'status' in error) {
            statusCode = error.status;
        } else if (error && typeof error === 'object' && 'statusCode' in error) {
            statusCode = error.statusCode;
        }

        return ErrorDiagnoser.diagnoseHTTPError(error, statusCode);
    }

    getTransportInfo(): TransportInfo {
        const port = this.config.port ?? 3000;
        const host = this.config.host ?? 'localhost';
        return {
            type: TransportType.HTTP,
            endpoint: `http://${host}:${port}`,
            capabilities: ['multiplexing', 'health-check', 'cors', 'json-rpc'],
        };
    }
}
