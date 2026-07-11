# Transport Layer API

## Supported Transports

### Stdio Transport (Default)
- Uses stdin/stdout streams
- Error codes: `CONFIG_INVALID`, `CONNECTION_FAILED`, `EPIPE`, `EOF_UNEXPECTED`
- Use case: Child process, testing, simple deployments

### HTTP Transport (New)
- HTTP server with graceful shutdown
- Supports binding to port `0` (ephemeral) which is useful for tests and dynamic allocation
- Error codes: `PORT_IN_USE`, `REQUEST_TIMEOUT`, `PAYLOAD_TOO_LARGE`, `INVALID_CERTIFICATE`
- Use case: Production deployments, load balancer integration, scalability

## Error Handling Example

```typescript
import { ErrorDiagnoser } from './ErrorDiagnoser';
import { ITransport } from './ITransport';

async function executeWithTransport(transport: ITransport) {
  try {
    // connect to the transport, send messages, etc.
    await transport.connect();
  } catch (error) {
    const context = await transport.diagnoseError(error);

    if (context.retryable) {
      // Implement retry logic
      console.log(`[${context.code}] Retryable error. Waiting ${context.retryAfterMs}ms...`);
    } else {
      // Non-retryable error, log and escalate
      console.error(`[${context.code}] ${context.message}`);
    }
  }
}
```
