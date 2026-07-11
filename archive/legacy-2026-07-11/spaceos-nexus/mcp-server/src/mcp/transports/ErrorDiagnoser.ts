import { TransportType, TransportError, TransportErrorContext } from './ITransport';

export class ErrorDiagnoser {
    // Stdio-specific error diagnosis
    static diagnoseStdioError(error: Error | any): TransportErrorContext {
        const code = this.mapStdioError(error);
        return {
            code,
            message: this.getErrorMessage(code),
            transport: TransportType.STDIO,
            retryable: this.isRetryable(code),
            retryAfterMs: this.getRetryAfterMs(code),
            originalError: error
        };
    }

    // HTTP-specific error diagnosis
    static diagnoseHTTPError(error: Error | any, statusCode?: number): TransportErrorContext {
        const code = this.mapHTTPError(error, statusCode);
        return {
            code,
            message: this.getErrorMessage(code),
            transport: TransportType.HTTP,
            retryable: this.isRetryable(code),
            retryAfterMs: this.getRetryAfterMs(code),
            originalError: error
        };
    }

    private static mapStdioError(error: Error | any): TransportError {
        const msg = error?.message || String(error);
        if (msg.includes("EPIPE")) return TransportError.EPIPE;
        if (msg.includes("EOF")) return TransportError.EOF_UNEXPECTED;
        if (msg.includes("Failed to parse")) return TransportError.INVALID_JSON;
        return TransportError.INTERNAL_ERROR;
    }

    private static mapHTTPError(error: Error | any, statusCode?: number): TransportError {
        if (statusCode === 413) return TransportError.PAYLOAD_TOO_LARGE;
        const msg = error?.message || String(error);
        if (msg.includes("EADDRINUSE")) return TransportError.PORT_IN_USE;
        if (msg.includes("ETIMEDOUT") || msg.includes("timeout")) return TransportError.REQUEST_TIMEOUT;
        if (msg.includes("CERTIFICATE") || msg.includes("SSL") || msg.includes("TLS")) return TransportError.INVALID_CERTIFICATE;
        return TransportError.INTERNAL_ERROR;
    }

    private static isRetryable(code: TransportError): boolean {
        return [
            TransportError.TIMEOUT,
            TransportError.EPIPE,
            TransportError.REQUEST_TIMEOUT
        ].includes(code);
    }

    private static getRetryAfterMs(code: TransportError): number {
        switch (code) {
            case TransportError.REQUEST_TIMEOUT:
            case TransportError.TIMEOUT:
                return 5000; // 5s backoff
            case TransportError.EPIPE:
                return 1000; // 1s backoff
            default:
                return 0;
        }
    }

    private static getErrorMessage(code: TransportError): string {
        // Map error codes to human-readable messages
        const messages: Record<TransportError, string> = {
            [TransportError.CONFIG_INVALID]: "Transport configuration is invalid. Check host/port.",
            [TransportError.CONNECTION_FAILED]: "Connection failed. Check network connectivity.",
            [TransportError.EPIPE]: "Broken pipe. Child process died or connection closed.",
            [TransportError.EOF_UNEXPECTED]: "Unexpected EOF. Process terminated unexpectedly.",
            [TransportError.INVALID_JSON]: "Received malformed JSON input.",
            [TransportError.PORT_IN_USE]: "Port already in use. Check if server is already running.",
            [TransportError.REQUEST_TIMEOUT]: "HTTP request timed out. Increase timeout or check server.",
            [TransportError.PAYLOAD_TOO_LARGE]: "HTTP request payload too large (max 10MB).",
            [TransportError.INVALID_CERTIFICATE]: "TLS certificate validation failed.",
            [TransportError.TIMEOUT]: "General timeout. Retrying...",
            [TransportError.INTERNAL_ERROR]: "Internal transport error."
        };
        return messages[code] || "Unknown error";
    }
}
