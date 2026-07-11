import { defineConfig } from '@playwright/test';

/**
 * Playwright Configuration — API E2E Testing
 *
 * API-only mode: no browser required.
 * Tests target the running agent-system dev server on port 3000.
 *
 * Usage:
 *   npx playwright test
 *   npx playwright test --reporter=list
 */
export default defineConfig({
    testDir: './src/tests/e2e',
    testMatch: '**/*.test.ts',
    timeout: 15_000,
    retries: 1,
    reporter: [['list'], ['json', { outputFile: 'playwright-results.json' }]],
    use: {
        baseURL: 'http://127.0.0.1:3000',
        // No browser needed — all tests use the `request` API context
        extraHTTPHeaders: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
        },
    },
});
