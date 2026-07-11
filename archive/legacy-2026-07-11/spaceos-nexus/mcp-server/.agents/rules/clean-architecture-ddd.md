---
trigger: always_on
description: Strict architectural guardrails based on Clean Architecture and Domain-Driven Design (DDD).
---

# Clean Architecture & DDD Standards

## Layered Constraints
1. **Core (Domain)**:
   - Contains business logic, entities, value objects, and domain services.
   - **NO EXTERNAL DEPENDENCIES**. Must not depend on Infrastructure, API, or external libraries.
   - Defines interfaces (ports) for external concerns.
2. **Infrastructure**:
   - Implements interfaces defined in Core (e.g., Repositories, File System, External APIs).
   - Depends on Core.
3. **API / Entry Points**:
   - Orchestrates use cases and handles external requests.
   - Depends on Core.

## Design Patterns
- **Interface-First**: Always define interfaces in the Core layer before implementation.
- **Dependency Injection**: Use explicit DI (e.g., `IServiceCollection` extensions) to wire up implementations.
- **Domain Integrity**: Protect the domain from technical leakages.
- **Explicit Typing**: Use explicit typing in C#; avoid `var` unless the type is obvious from the right-hand side.
