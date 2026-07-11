/**
 * InputValidator handles strict regex-based validation for agent bootstrap parameters.
 * Follows OWASP best practices for input sanitization and injection prevention.
 */
export class InputValidator {
    /**
     * Domain regex: lowercase letters and hyphens only.
     * Must start/end with a letter, 1-64 characters.
     */
    private static readonly DOMAIN_PATTERN = /^[a-z](?:[a-z-]{0,62}[a-z])?$/;

    /**
     * Role regex: lowercase letters and underscores only.
     * Must start/end with a letter, 1-64 characters.
     */
    private static readonly ROLE_PATTERN = /^[a-z](?:[a-z_]{0,62}[a-z])?$/;

    /**
     * Validates the domain parameter.
     * @param domain The domain string to validate.
     * @throws Error with code 'invalid_domain' if validation fails.
     */
    public static validateDomain(domain: unknown): void {
        if (typeof domain !== 'string' || domain.length === 0) {
            const error = new Error('Domain is required');
            (error as any).code = 'INVALID_DOMAIN';
            (error as any).reason = 'missing';
            throw error;
        }

        if (domain.length > 64) {
            const error = new Error('Domain exceeds 64 characters');
            (error as any).code = 'INVALID_DOMAIN';
            (error as any).reason = 'too_long';
            throw error;
        }

        if (!this.DOMAIN_PATTERN.test(domain)) {
            const error = new Error('Invalid domain format: lowercase letters and hyphens only');
            (error as any).code = 'INVALID_DOMAIN';
            (error as any).reason = 'invalid_format';
            throw error;
        }
    }

    /**
     * Validates the role parameter.
     * @param role The role string to validate.
     * @throws Error with code 'invalid_role' if validation fails.
     */
    public static validateRole(role: unknown): void {
        if (typeof role !== 'string' || role.length === 0) {
            const error = new Error('Role is required');
            (error as any).code = 'INVALID_ROLE';
            (error as any).reason = 'missing';
            throw error;
        }

        if (role.length > 64) {
            const error = new Error('Role exceeds 64 characters');
            (error as any).code = 'INVALID_ROLE';
            (error as any).reason = 'too_long';
            throw error;
        }

        if (!this.ROLE_PATTERN.test(role)) {
            const error = new Error('Invalid role format: lowercase letters and underscores only');
            (error as any).code = 'INVALID_ROLE';
            (error as any).reason = 'invalid_format';
            throw error;
        }
    }

    /**
     * Combined validation for bootstrap parameters.
     */
    public static validateBootstrapInput(domain: unknown, role: unknown): void {
        this.validateDomain(domain);
        this.validateRole(role);
    }
}
