---
name: webapp-testing
description: Toolkit for interacting with and testing local web applications using Playwright. Supports verifying frontend functionality, debugging UI behavior, capturing browser screenshots, and viewing browser logs.
---

# Web Application Testing

To test local web applications, write native Python Playwright scripts.

**Helper Scripts Available**:
- `scripts/with_server.py` - Manages server lifecycle (supports multiple servers)

## Decision Tree
1. Is it static HTML? Read directly -> Write Playwright.
2. Is it dynamic? Use `with_server.py` if not running -> Reconnaissance (navigate, screenshot, identify selectors) -> Action.

## Example: Using with_server.py
```bash
python scripts/with_server.py --server "npm run dev" --port 5173 -- python your_automation.py
```

## Best Practices
- Use `sync_playwright()` for synchronous scripts.
- Always close the browser.
- Wait for `networkidle` before inspecting dynamic apps.
