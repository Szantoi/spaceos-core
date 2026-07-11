# **SpaceOS Kernel Development Rules**

1. **Strict Clean Architecture**: The use of Microsoft.EntityFrameworkCore, Microsoft.AspNetCore.\*, or System.Text.Json is strictly forbidden within the Kernel (Domain and Application layers). The Kernel must remain purely infrastructure-agnostic and focus on business logic.  
2. **Pure Domain**: The Domain layer must have zero dependencies on external layers or third-party libraries. It should only contain business logic, entities, value objects, and domain exceptions.  
3. **Value Objects & Immutability**:  
   * Use C\# 14 public readonly record struct for all identifiers (IDs) and complex types.  
   * Ensure structural equality and immutability.  
   * Implement validation within the constructor; throw a DomainException if rules are violated.  
4. **TDD (Test-Driven Development) Flow**:  
   * Every feature implementation **must** begin with a failing unit test.  
   * All tests must reside in the SpaceOS.Kernel.Tests project.  
   * Follow the Red-Green-Refactor cycle.  
5. **Encapsulation & State Management**:  
   * No public setters for properties.  
   * Entity states must only be modified through explicit, descriptive methods (e.g., Rename(...), TransitionTo(...)).  
   * Protect business invariants at all times.  
6. **Coding Standards**:  
   * Use C\# 14 features (e.g., primary constructors where appropriate).  
   * Follow PascalCase for classes and methods, camelCase for private fields and parameters.  
   * Use XML documentation for public API surface within the Domain.