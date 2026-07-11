/**
 * Config Loader - Loads and validates config.yaml
 */

import fs from 'fs';
import path from 'path';
import yaml from 'yaml';
import { ConnectorConfig } from '../types';

/**
 * Load configuration from YAML file
 */
export function loadConfig(configPath: string): ConnectorConfig {
  const absolutePath = path.resolve(configPath);

  if (!fs.existsSync(absolutePath)) {
    throw new Error(`Config file not found: ${absolutePath}`);
  }

  const fileContents = fs.readFileSync(absolutePath, 'utf8');

  // Replace environment variables ${VAR_NAME}
  const interpolated = fileContents.replace(/\$\{(\w+)\}/g, (_, varName) => {
    return process.env[varName] || '';
  });

  const config = yaml.parse(interpolated) as ConnectorConfig;

  // Validate required sections
  if (!config.backends) {
    throw new Error('Missing required config section: backends');
  }
  if (!config.routing) {
    throw new Error('Missing required config section: routing');
  }
  if (!config.permissions) {
    throw new Error('Missing required config section: permissions');
  }
  if (!config.audit) {
    throw new Error('Missing required config section: audit');
  }

  return config;
}

/**
 * Validate configuration for consistency
 */
export function validateConfig(config: ConnectorConfig): void {
  const errors: string[] = [];

  // Validate: all routing targets exist in backends
  for (const [tool, backend] of Object.entries(config.routing)) {
    if (!config.backends[backend]) {
      errors.push(`Routing: tool '${tool}' targets non-existent backend '${backend}'`);
    }
  }

  // Validate: all permission tools exist in routing (or use wildcard)
  for (const [terminal, perms] of Object.entries(config.permissions)) {
    if (Array.isArray(perms)) {
      // Array format: ["*"] or ["tool1", "tool2"]
      for (const tool of perms) {
        if (tool === '*') continue;
        if (!config.routing[tool]) {
          errors.push(`Permissions: terminal '${terminal}' references non-existent tool '${tool}'`);
        }
      }
    } else if (perms.tools) {
      // Object format: { tools: [...] }
      for (const tool of perms.tools) {
        if (tool === '*') continue;
        if (!config.routing[tool]) {
          errors.push(`Permissions: terminal '${terminal}' references non-existent tool '${tool}'`);
        }
      }
    }
  }

  // Validate backend configs
  for (const [name, backend] of Object.entries(config.backends)) {
    if (backend.type === 'http' && !backend.url) {
      errors.push(`Backend '${name}': HTTP type requires 'url' field`);
    }
    if (backend.type === 'stdio' && !backend.command) {
      errors.push(`Backend '${name}': STDIO type requires 'command' field`);
    }
  }

  if (errors.length > 0) {
    throw new Error(`Config validation failed:\n  - ${errors.join('\n  - ')}`);
  }
}

/**
 * Get default performance config
 */
export function getDefaultPerformance(): Required<ConnectorConfig>['performance'] {
  return {
    request_timeout: 30000,
    max_concurrent_requests: 100,
    circuit_breaker: {
      enabled: true,
      failure_threshold: 5,
      timeout: 60000,
      half_open_after: 30000,
    },
  };
}
