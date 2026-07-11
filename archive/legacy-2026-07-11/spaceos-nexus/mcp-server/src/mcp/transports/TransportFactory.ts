import { ITransport, TransportConfig, TransportType } from './ITransport';
import { StdioTransport } from './StdioTransport';
import { HTTPTransport } from './HTTPTransport';

export class ConfigurationError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ConfigurationError';
    }
}

export class TransportFactory {
    static validate(config: TransportConfig): { valid: boolean; errors: string[] } {
        const errors: string[] = [];

        // Validate transport type
        if (!Object.values(TransportType).includes(config.type as TransportType)) {
            errors.push(`Invalid transport type: ${config.type}. Must be one of: ${Object.values(TransportType).join(", ")}`);
        }

        // Validate HTTP-specific config
        if (config.type === TransportType.HTTP) {
            // port 0 is permitted for ephemeral binding (OS chooses a free port)
            if (config.port !== undefined && config.port !== 0 && (config.port < 1 || config.port > 65535)) {
                errors.push(`Invalid port: ${config.port}. Must be between 1-65535 (0 allowed for ephemeral)`);
            }
            if (config.host !== undefined && config.host.length === 0) {
                errors.push(`Invalid host: empty string`);
            }
        }

        return { valid: errors.length === 0, errors };
    }

    static create(config: TransportConfig): ITransport {
        const { valid, errors } = this.validate(config);
        if (!valid) {
            throw new ConfigurationError(`Invalid transport configuration: ${errors.join("; ")}`);
        }

        switch (config.type) {
            case TransportType.STDIO:
                return new StdioTransport(config);
            case TransportType.HTTP:
                const httpConfig = {
                    ...config,
                    port: config.port !== undefined ? config.port : 3000,
                    host: config.host !== undefined ? config.host : "localhost"
                };
                return new HTTPTransport(httpConfig);
            default:
                throw new ConfigurationError(`Unsupported transport type: ${config.type}`);
        }
    }
}
