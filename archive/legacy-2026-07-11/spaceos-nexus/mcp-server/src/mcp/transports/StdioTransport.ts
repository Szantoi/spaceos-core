import { BaseTransport, TransportErrorContext, TransportState, TransportConfig, TransportType, TransportError, TransportInfo } from './ITransport';
import { ErrorDiagnoser } from './ErrorDiagnoser';
import { createInterface, Interface as ReadlineInterface } from 'readline';
import { ResourceContent, ResourceTemplateRegistry } from '../resources/resourceTemplates';
import { SamplingRequest, SamplingResult, SamplingService } from '../sampling/SamplingService';

/**
 * StdioTransport — Standard Input/Output MCP transport
 *
 * Implements:
 * - Line-based JSON protocol (one JSON object per line)
 * - Bidirectional communication via stdio or provided streams
 * - Error recovery from malformed input
 * - Graceful EOF handling
 */
export class StdioTransport extends BaseTransport {
    private rl: ReadlineInterface;
    private messageCallbacks: Map<string, (data: any) => void> = new Map();
    private errorCallbacks: ((error: TransportErrorContext) => void)[] = [];

    private inputStream: NodeJS.ReadableStream;
    private outputStream: NodeJS.WritableStream;
    private resourceRegistry?: ResourceTemplateRegistry;
    private samplingService?: SamplingService;

    constructor(
        config: TransportConfig,
        inputStream?: NodeJS.ReadableStream,
        outputStream?: NodeJS.WritableStream
    ) {
        if (config.type !== TransportType.STDIO) {
            throw new Error(`Invalid transport configuration type for StdioTransport: ${config.type}`);
        }
        super(config);
        this.inputStream = inputStream || process.stdin;
        this.outputStream = outputStream || process.stdout;
        // TASK-14-06: Accept registry/sampling from config (mirrors HTTPTransport pattern)
        this.resourceRegistry = (config as any).resourceRegistry || undefined;
        this.samplingService = (config as any).samplingService || undefined;
        this.rl = createInterface({
            input: this.inputStream,
            output: undefined,
            terminal: false
        });
        this.setupHandlers();
    }

    private setupHandlers(): void {
        this.rl.on('line', (line: string) => {
            try {
                const data = JSON.parse(line);
                if (data.id && this.messageCallbacks.has(data.id)) {
                    const handler = this.messageCallbacks.get(data.id)!;
                    handler(data);
                    this.messageCallbacks.delete(data.id);
                }
            } catch (error) {
                this.notifyError({
                    code: TransportError.INVALID_JSON,
                    message: `Failed to parse line: ${line}`,
                    transport: TransportType.STDIO,
                    retryable: false,
                    originalError: error as Error
                });
            }
        });

        this.rl.on('close', () => {
            this.state = TransportState.DISCONNECTED;
            // do not exit process in tests, just update state
        });
    }

    async connect(): Promise<void> {
        this.state = TransportState.CONNECTED;
    }

    async disconnect(): Promise<void> {
        this.state = TransportState.DISCONNECTING;
        this.rl.close();
        this.state = TransportState.DISCONNECTED;
    }

    public setResourceRegistry(resourceRegistry: ResourceTemplateRegistry): void {
        this.resourceRegistry = resourceRegistry;
    }

    public setSamplingService(samplingService: SamplingService): void {
        this.samplingService = samplingService;
    }

    public listResources(): Array<{ uriPattern: string; description: string }> {
        return this.resourceRegistry ? this.resourceRegistry.listResources() : [];
    }

    public async resolveResource(uri: string): Promise<ResourceContent> {
        if (!this.resourceRegistry) {
            throw new Error('Resource registry not configured');
        }
        return this.resourceRegistry.resolveUri(uri);
    }

    public async requestSampling(sessionId: string, request: SamplingRequest): Promise<SamplingResult> {
        if (!this.samplingService) {
            return {
                requestId: 'sampling-service-unavailable',
                selected: [],
                error: 'Sampling service not configured',
                needsClarification: true
            };
        }

        return this.samplingService.requestSampling(sessionId, request);
    }

    async send(data: any): Promise<void> {
        const line = JSON.stringify(data) + '\n';
        return new Promise((resolve, reject) => {
            this.outputStream.write(line, 'utf8', (err) => {
                if (err) reject(err);
                else resolve();
            });
        });
    }

    receive(id: string, handler: (data: any) => void): void {
        this.messageCallbacks.set(id, handler);
    }

    onError(handler: (error: TransportErrorContext) => void): void {
        this.errorCallbacks.push(handler);
    }

    getState(): TransportState {
        return this.state;
    }

    getTransportInfo(): TransportInfo {
        return {
            type: TransportType.STDIO,
            endpoint: 'stdio://embedded',
            capabilities: ['json-rpc-2.0', 'streaming']
        };
    }

    private notifyError(error: TransportErrorContext): void {
        this.errorCallbacks.forEach(cb => cb(error));
    }

    async diagnoseError(error: Error | any): Promise<TransportErrorContext> {
        return ErrorDiagnoser.diagnoseStdioError(error);
    }
}
