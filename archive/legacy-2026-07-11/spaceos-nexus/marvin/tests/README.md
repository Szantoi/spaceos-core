# SpaceOS Marvin — Test Suite

Unit and integration tests for Marvin agent infrastructure.

## Test Coverage

### Phase 2: Planning Pipeline
- Config loading (planning config.yaml)
- Marvin Tasks (scan, select, debate) — *requires OPENAI_API_KEY*

### Phase 3: Reviewer + Nightwatch
- **WorkflowStateTracker** (15 tests) ✅
  - Session lifecycle (STARTED → IN_PROGRESS → DONE)
  - Stuck detection logic
  - Multi-session tracking
  - Edge cases
- **Config Loading** (14 tests) ✅
  - reviewer-config.yaml parsing
  - nightwatch-config.yaml parsing
  - planning config.yaml parsing
  - Validation logic
- **Utility Functions** (37 tests) ✅
  - Frontmatter parsing (10 tests)
  - Verdict extraction (15 tests)
  - File path utilities (5 tests)
  - Timestamp utilities (5 tests)
  - Message construction (2 tests)
- **Nightwatch Scheduler** (20 tests) ✅ NEW
  - Config loading (2 tests)
  - Tmux session management (8 tests)
  - Priority session checks (2 tests)
  - DONE message detection (2 tests)
  - Stuck session detection (2 tests)
  - UNREAD inbox detection (2 tests)
  - Full cycle integration (2 tests)

## Running Tests

### All tests
```bash
source venv/bin/activate
pytest
```

### Specific test file
```bash
pytest tests/test_workflow_state_tracker.py -v
```

### Specific test class
```bash
pytest tests/test_workflow_state_tracker.py::TestSessionLifecycle -v
```

### Test markers
```bash
# Unit tests only (fast, no external dependencies)
pytest -m unit

# Integration tests
pytest -m integration

# Skip tests requiring API key
pytest -m "not requires_api_key"
```

## Test Results (2026-06-18)

```
============================= test session starts ==============================
platform linux -- Python 3.13.5, pytest-9.1.0
collected 86 items

tests/test_config_loading.py ..............                              [ 16%]
tests/test_nightwatch_scheduler.py ....................                  [ 39%]
tests/test_utils.py .....................................                [ 82%]
tests/test_workflow_state_tracker.py ...............                     [100%]

======================= 86 passed, 69 warnings in 2.78s ========================
```

**Status:** ✅ All tests passing (86/86)

**Runtime:** ~2.5-3.0s (includes async tests)

**Warnings:** 0 ✅ (Python 3.12 datetime deprecation FIXED in Session 6)

## Test Structure

```
tests/
├── __init__.py
├── README.md                        — This file
├── test_workflow_state_tracker.py   — SQLite state tracker tests (15 tests)
├── test_config_loading.py           — YAML config parsing tests (14 tests)
├── test_utils.py                    — Utility functions tests (37 tests)
├── test_nightwatch_scheduler.py     — Nightwatch scheduler mock tests (20 tests)
└── conftest.py                      — Shared fixtures (future)
```

## Writing New Tests

### Test file naming
- `test_*.py` — Test modules
- `Test*` — Test classes
- `test_*` — Test functions

### Fixtures
```python
import pytest

@pytest.fixture
def tracker():
    """Create temporary tracker for testing"""
    # Setup
    tracker = WorkflowStateTracker(db_path=":memory:")
    yield tracker
    # Teardown (if needed)
```

### Markers
```python
@pytest.mark.unit
def test_fast_operation():
    """Unit test (no external dependencies)"""
    pass

@pytest.mark.integration
def test_with_database():
    """Integration test (requires database)"""
    pass

@pytest.mark.requires_api_key
def test_marvin_agent():
    """Test requiring OPENAI_API_KEY"""
    pass
```

## Future Tests (Phase 3.1)

- [x] test_nightwatch_scheduler.py (mock tests) ✅ COMPLETE (20 tests)
- [ ] test_reviewer_task.py (verdict parsing, inbox generation) — requires OPENAI_API_KEY
- [x] test_utils.py (frontmatter parser, verdict extractor) ✅ COMPLETE (37 tests)
- [ ] test_integration_e2e.py (full workflow) — requires OPENAI_API_KEY

## CI/CD Integration

```yaml
# .github/workflows/test.yml (example)
name: Test Suite
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-python@v2
        with:
          python-version: '3.13'
      - run: pip install -r requirements.txt
      - run: pytest
```

## References

- pytest docs: https://docs.pytest.org/
- PHASE3_PREP_SUMMARY.md — Phase 3.0 implementation details
- pytest.ini — Test configuration
