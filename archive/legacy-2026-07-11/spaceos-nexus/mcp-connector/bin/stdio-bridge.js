#!/usr/bin/env node
/**
 * MCP Connector STDIO Bridge
 *
 * Bridges STDIO (Claude Code) to the HTTP MCP Connector.
 * This allows Claude Code to connect to a single MCP server that
 * proxies to multiple backends (knowledge, playwright, context7, etc.)
 *
 * Usage in settings.json:
 * {
 *   "mcpServers": {
 *     "spaceos-connector": {
 *       "command": "node",
 *       "args": ["/opt/spaceos/spaceos-nexus/mcp-connector/bin/stdio-bridge.js"],
 *       "env": {
 *         "MCP_CONNECTOR_HOST": "localhost",
 *         "MCP_CONNECTOR_PORT": "3457",
 *         "MCP_TERMINAL": "root"
 *       }
 *     }
 *   }
 * }
 */

const http = require('http');
const readline = require('readline');

const HOST = process.env.MCP_CONNECTOR_HOST || 'localhost';
const PORT = parseInt(process.env.MCP_CONNECTOR_PORT || '3457', 10);
const TERMINAL = process.env.MCP_TERMINAL || 'root';

// Read JSON-RPC messages from stdin
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

let buffer = '';

process.stdin.on('data', (chunk) => {
  buffer += chunk.toString();

  // Try to parse complete JSON messages
  while (true) {
    const newlineIndex = buffer.indexOf('\n');
    if (newlineIndex === -1) break;

    const line = buffer.slice(0, newlineIndex).trim();
    buffer = buffer.slice(newlineIndex + 1);

    if (line) {
      try {
        const message = JSON.parse(line);
        handleMessage(message);
      } catch (e) {
        // Not valid JSON, might be partial
        console.error(`[stdio-bridge] Parse error: ${e.message}`);
      }
    }
  }
});

function handleMessage(message) {
  const postData = JSON.stringify(message);

  const options = {
    hostname: HOST,
    port: PORT,
    path: '/mcp',
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Content-Length': Buffer.byteLength(postData),
      'X-Terminal': TERMINAL
    }
  };

  const req = http.request(options, (res) => {
    let data = '';

    res.on('data', (chunk) => {
      data += chunk;
    });

    res.on('end', () => {
      try {
        // Write response to stdout
        process.stdout.write(data + '\n');
      } catch (e) {
        const errorResponse = {
          jsonrpc: '2.0',
          error: {
            code: -32603,
            message: `Response error: ${e.message}`
          },
          id: message.id
        };
        process.stdout.write(JSON.stringify(errorResponse) + '\n');
      }
    });
  });

  req.on('error', (e) => {
    const errorResponse = {
      jsonrpc: '2.0',
      error: {
        code: -32603,
        message: `Connection error: ${e.message}`
      },
      id: message.id
    };
    process.stdout.write(JSON.stringify(errorResponse) + '\n');
  });

  req.write(postData);
  req.end();
}

// Handle process signals
process.on('SIGTERM', () => {
  process.exit(0);
});

process.on('SIGINT', () => {
  process.exit(0);
});

// Log startup
console.error(`[stdio-bridge] MCP Connector bridge started`);
console.error(`[stdio-bridge] Connecting to http://${HOST}:${PORT}/mcp`);
console.error(`[stdio-bridge] Terminal: ${TERMINAL}`);
