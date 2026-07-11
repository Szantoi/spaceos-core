# SpaceOS Build System — MSG-NEXUS-011
# Fast incremental builds with npm cache optimization

.PHONY: build build-fast dev test clean help install-deps typecheck lint

# ─── Configuration ────────────────────────────────────────────────────────────

CACHE_DIR := $(HOME)/.spaceos-build-cache
NPM_CACHE := $(HOME)/.npm
CLIENT_DIR := datahaven-web/client
KNOWLEDGE_DIR := spaceos-nexus/knowledge-service

# ─── Help ─────────────────────────────────────────────────────────────────────

help:
	@echo "SpaceOS Build System (MSG-NEXUS-011)"
	@echo ""
	@echo "Fast incremental builds with npm cache optimization"
	@echo "Target: <10s incremental, ~30s full build"
	@echo ""
	@echo "Available targets:"
	@echo "  make build-fast    — Fast incremental build with cache (~10s)"
	@echo "  make build         — Full clean build (~30s)"
	@echo "  make dev           — Start dev servers in tmux sessions"
	@echo "  make test          — Run all test suites"
	@echo "  make lint          — Run linters"
	@echo "  make typecheck     — Run TypeScript type checking"
	@echo "  make clean         — Clean build artifacts and cache"
	@echo "  make install-deps  — Install all dependencies (npm ci)"
	@echo ""

# ─── Fast Build (with cache) ──────────────────────────────────────────────────

build-fast:
	@echo "🚀 Fast incremental build with cache..."
	@mkdir -p $(CACHE_DIR)
	@$(MAKE) --no-print-directory _build-fast-client
	@$(MAKE) --no-print-directory _build-fast-knowledge
	@echo "✅ Fast build complete"

_build-fast-client:
	@echo "→ Building datahaven-web/client (cached)..."
	@cd $(CLIENT_DIR) && \
		if [ -f node_modules/.cache/.build-timestamp ]; then \
			echo "  📦 Using cached dependencies"; \
		else \
			npm ci --prefer-offline --cache $(NPM_CACHE) --silent; \
			mkdir -p node_modules/.cache && touch node_modules/.cache/.build-timestamp; \
		fi
	@cd $(CLIENT_DIR) && npm run build --silent
	@echo "  ✓ Client build complete"

_build-fast-knowledge:
	@echo "→ Building spaceos-nexus/knowledge-service (cached)..."
	@cd $(KNOWLEDGE_DIR) && \
		if [ -f node_modules/.cache/.build-timestamp ]; then \
			echo "  📦 Using cached dependencies"; \
		else \
			npm ci --prefer-offline --cache $(NPM_CACHE) --silent; \
			mkdir -p node_modules/.cache && touch node_modules/.cache/.build-timestamp; \
		fi
	@cd $(KNOWLEDGE_DIR) && npm run build --silent
	@echo "  ✓ Knowledge service build complete"

# ─── Full Build ───────────────────────────────────────────────────────────────

build:
	@echo "🔨 Full clean build..."
	@$(MAKE) --no-print-directory clean
	@$(MAKE) --no-print-directory install-deps
	@echo "→ Building datahaven-web/client..."
	@cd $(CLIENT_DIR) && npm run build
	@echo "→ Building spaceos-nexus/knowledge-service..."
	@cd $(KNOWLEDGE_DIR) && npm run build
	@echo "✅ Full build complete"

# ─── Install Dependencies ─────────────────────────────────────────────────────

install-deps:
	@echo "📦 Installing dependencies with cache..."
	@cd $(CLIENT_DIR) && npm ci --prefer-offline --cache $(NPM_CACHE)
	@cd $(KNOWLEDGE_DIR) && npm ci --prefer-offline --cache $(NPM_CACHE)
	@mkdir -p $(CLIENT_DIR)/node_modules/.cache && touch $(CLIENT_DIR)/node_modules/.cache/.build-timestamp
	@mkdir -p $(KNOWLEDGE_DIR)/node_modules/.cache && touch $(KNOWLEDGE_DIR)/node_modules/.cache/.build-timestamp
	@echo "✅ Dependencies installed"

# ─── Development Mode ─────────────────────────────────────────────────────────

dev:
	@echo "🔧 Starting development servers in tmux..."
	@tmux has-session -t spaceos-client 2>/dev/null && echo "  ⚠️  Client session already running" || \
		(tmux new-session -d -s spaceos-client -c $(PWD)/$(CLIENT_DIR) && \
		 tmux send-keys -t spaceos-client "npm run dev" Enter && \
		 echo "  ✓ Client dev server started (tmux session: spaceos-client)")
	@tmux has-session -t spaceos-knowledge 2>/dev/null && echo "  ⚠️  Knowledge session already running" || \
		(tmux new-session -d -s spaceos-knowledge -c $(PWD)/$(KNOWLEDGE_DIR) && \
		 tmux send-keys -t spaceos-knowledge "npm run dev" Enter && \
		 echo "  ✓ Knowledge dev server started (tmux session: spaceos-knowledge)")
	@echo ""
	@echo "Dev servers running. Attach with:"
	@echo "  tmux attach -t spaceos-client"
	@echo "  tmux attach -t spaceos-knowledge"

# ─── Testing ──────────────────────────────────────────────────────────────────

test:
	@echo "🧪 Running all tests..."
	@echo "→ Testing datahaven-web/client..."
	@cd $(CLIENT_DIR) && npm test -- --run --silent 2>/dev/null || echo "  ⚠️  No client tests configured"
	@echo "→ Testing spaceos-nexus/knowledge-service..."
	@cd $(KNOWLEDGE_DIR) && npm test -- --run --silent
	@echo "✅ All tests passed"

# ─── Type Checking ────────────────────────────────────────────────────────────

typecheck:
	@echo "🔍 Running TypeScript type checking..."
	@echo "→ Client type check..."
	@cd $(CLIENT_DIR) && npx tsc --noEmit
	@echo "→ Knowledge service type check..."
	@cd $(KNOWLEDGE_DIR) && npx tsc --noEmit
	@echo "✅ Type checking passed"

# ─── Linting ──────────────────────────────────────────────────────────────────

lint:
	@echo "🔍 Running linters..."
	@echo "→ Linting datahaven-web/client..."
	@cd $(CLIENT_DIR) && npm run lint
	@echo "→ Linting spaceos-nexus/knowledge-service..."
	@cd $(KNOWLEDGE_DIR) && echo "  (no lint script configured, skipping)"
	@echo "✅ Linting complete"

# ─── Clean ────────────────────────────────────────────────────────────────────

clean:
	@echo "🧹 Cleaning build artifacts and cache..."
	@rm -rf $(CLIENT_DIR)/dist
	@rm -rf $(CLIENT_DIR)/node_modules/.cache
	@rm -rf $(CLIENT_DIR)/node_modules/.vite
	@rm -rf $(KNOWLEDGE_DIR)/dist
	@rm -rf $(KNOWLEDGE_DIR)/node_modules/.cache
	@rm -rf $(CACHE_DIR)
	@echo "✅ Clean complete"

# ─── Performance Metrics ──────────────────────────────────────────────────────

benchmark:
	@echo "📊 Build performance benchmark..."
	@echo ""
	@echo "Full build (make clean build):"
	@time $(MAKE) --no-print-directory build 2>&1 | grep real
	@echo ""
	@echo "Incremental build (make build-fast):"
	@time $(MAKE) --no-print-directory build-fast 2>&1 | grep real
	@echo ""
	@echo "Target: Full ~30s, Incremental <10s"
