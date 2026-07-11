export enum TransportType {
    STDIO = "stdio",
    HTTP = "http"
}

export enum TransportState {
    INITIALIZING = "INITIALIZING",
    CONNECTED = "CONNECTED",
    DISCONNECTING = "DISCONNECTING",
    DISCONNECTED = "DISCONNECTED",
    ERROR = "ERROR"
}

export enum TransportError {
    // Generic (both transports)
    CONFIG_INVALID = "CONFIG_INVALID",
    CONNECTION_FAILED = "CONNECTION_FAILED",
    TIMEOUT = "TIMEOUT",
    INTERNAL_ERROR = "INTERNAL_ERROR",

    // Stdio-specific
    EPIPE = "EPIPE",                            // broken pipe
    EOF_UNEXPECTED = "EOF_UNEXPECTED",          // premature EOF
    INVALID_JSON = "INVALID_JSON",              // received malformed JSON

    // HTTP-specific
    PORT_IN_USE = "PORT_IN_USE",                // port bind failure
    INVALID_CERTIFICATE = "INVALID_CERTIFICATE", // TLS error
    REQUEST_TIMEOUT = "REQUEST_TIMEOUT",        // HTTP request timeout
    PAYLOAD_TOO_LARGE = "PAYLOAD_TOO_LARGE"     // HTTP 413
}

export interface TransportErrorContext {
    code: TransportError;
    message: string;
    transport: TransportType;
    retryable: boolean;
    retryAfterMs?: number;
    originalError?: Error;
}

export interface TransportConfig {
    type: TransportType;
    port?: number;
    host?: string;
    [key: string]: any;
}

export interface TransportInfo {
    type: TransportType;
    endpoint?: string;
    capabilities: string[];
}

export interface ITransport {
    connect(): Promise<void>;
    disconnect(): Promise<void>;
    getState(): TransportState;
    isConnected(): boolean;
    getConfig(): TransportConfig;
    diagnoseError(error: Error | any): Promise<TransportErrorContext>;
    getTransportInfo(): TransportInfo;
}

export abstract class BaseTransport implements ITransport {
    protected state: TransportState = TransportState.INITIALIZING;
    protected config: TransportConfig;

    constructor(config: TransportConfig) {
        this.config = config;
    }

    getState(): TransportState {
        return this.state;
    }

    isConnected(): boolean {
        return this.state === TransportState.CONNECTED;
    }

    getConfig(): TransportConfig {
        return this.config;
    }

    abstract connect(): Promise<void>;
    abstract disconnect(): Promise<void>;
    abstract diagnoseError(error: Error | any): Promise<TransportErrorContext>;
    abstract getTransportInfo(): TransportInfo;
}
