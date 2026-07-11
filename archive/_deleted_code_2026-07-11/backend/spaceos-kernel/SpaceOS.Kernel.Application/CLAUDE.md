# CLAUDE.md — SpaceOS.Kernel.Application

> Application layer rules. Read root CLAUDE.md first for global rules.
> This layer orchestrates use cases. No EF Core. No HTTP. No infrastructure concerns.

---

## PURPOSE

Translate external intent into domain operations.
Handlers are thin orchestrators — business logic lives in the Domain, not here.

---

## FOLDER STRUCTURE

```
Application/
  {Feature}/                        e.g. Tenants/
    Commands/
      Create{Entity}Command.cs
      Create{Entity}CommandHandler.cs
      Create{Entity}CommandValidator.cs
    Queries/
      Get{Entity}ByIdQuery.cs
      Get{Entity}ByIdQueryHandler.cs
      Get{Entity}ByIdQueryValidator.cs
    Events/
      {Event}Handler.cs
    {Entity}Dto.cs
```

One class per file. File name = class name.

---

## COMMAND HANDLER TEMPLATE

```csharp
// Application/Tenants/Commands/CreateTenantCommandHandler.cs

/// <summary>Handles the creation of a new <see cref="Tenant"/> aggregate.</summary>
internal sealed class CreateTenantCommandHandler
    : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    private readonly ITenantRepository _repository;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateTenantCommandHandler(
        ITenantRepository repository,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    public async Task<Result<TenantDto>> Handle(
        CreateTenantCommand command, CancellationToken ct)
    {
        // 1. Guard — upfront, no try/catch DomainException
        var name = TenantName.From(command.Name); // throws DomainException if invalid

        // 2. Domain operation
        var tenant = Tenant.Create(name);

        // 3. Persist
        await _repository.AddAsync(tenant, ct).ConfigureAwait(false);

        // 4. Dispatch domain events — always last, after successful persist
        var events = tenant.PopDomainEvents();
        await _dispatcher.DispatchAsync(events, ct).ConfigureAwait(false);

        return Result<TenantDto>.Success(new TenantDto(tenant.Id.Value, tenant.Name.Value));
    }
}
```

---

## QUERY HANDLER TEMPLATE

```csharp
// Application/Tenants/Queries/GetTenantByIdQueryHandler.cs

/// <summary>Returns a <see cref="TenantDto"/> by ID.</summary>
internal sealed class GetTenantByIdQueryHandler
    : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    private readonly ITenantRepository _repository;

    public GetTenantByIdQueryHandler(ITenantRepository repository) =>
        _repository = repository;

    public async Task<Result<TenantDto>> Handle(
        GetTenantByIdQuery query, CancellationToken ct)
    {
        var tenant = await _repository
            .GetByIdAsync(TenantId.From(query.Id), ct)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result<TenantDto>.NotFound();

        return Result<TenantDto>.Success(new TenantDto(tenant.Id.Value, tenant.Name.Value));
    }
}
```

---

## VALIDATOR TEMPLATE

```csharp
// Application/Tenants/Commands/CreateTenantCommandValidator.cs

/// <summary>Validates <see cref="CreateTenantCommand"/> input.</summary>
internal sealed class CreateTenantCommandValidator
    : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

- Every command and query has a companion validator — no exceptions
- Validator catches structural/format errors; VO constructor catches domain invariants
- Never duplicate the same rule in both validator and VO — validator is the first gate

---

## MANDATORY CHECKLIST — every mutating handler

- [ ] Returns `Result<T>` or `Result`
- [ ] `ConfigureAwait(false)` on every await
- [ ] `CancellationToken` parameter named `ct`
- [ ] VO construction is the upfront guard (no try/catch DomainException)
- [ ] `PopDomainEvents()` + `DispatchAsync()` at the end, after persist
- [ ] Companion validator exists
- [ ] Companion test file exists
- [ ] XML docs on the handler class and Handle method

---

## COMMAND & QUERY TYPES

```csharp
// ✅ record types for immutability
public sealed record CreateTenantCommand(string Name) : IRequest<Result<TenantDto>>;
public sealed record GetTenantByIdQuery(Guid Id) : IRequest<Result<TenantDto>>;
```

---

## DTO RULES

```csharp
// ✅ Inline mapping in the handler — no static From() factory
public sealed record TenantDto(Guid Id, string Name);

// In handler:
return Result<TenantDto>.Success(new TenantDto(tenant.Id.Value, tenant.Name.Value));
```

No AutoMapper. No reflection-based mapping. No static `From()` factory. Explicit inline construction only.

---

## DOMAIN EVENT HANDLER TEMPLATE

```csharp
// Application/Tenants/Events/TenantCreatedEventHandler.cs

/// <summary>Handles side effects after a Tenant is created.</summary>
internal sealed class TenantCreatedEventHandler
    : INotificationHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger) =>
        _logger = logger;

    public Task Handle(TenantCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("Tenant created: {TenantId}", notification.TenantId);
        return Task.CompletedTask;
    }
}
```

---

## ANTI-PATTERNS

```csharp
// ❌ Business logic in handler — belongs in aggregate or VO
if (command.Name.Length > 100)
    return Result.Error("Name too long.");

// ❌ Catching DomainException — use upfront guard instead
try { var name = TenantName.From(command.Name); }
catch (DomainException ex) { return Result.Error(ex.Message); }

// ❌ AutoMapper or reflection mapping
var dto = _mapper.Map<TenantDto>(tenant);

// ❌ EF Core reference in Application
using Microsoft.EntityFrameworkCore;

// ❌ Raw list query bypassing specification
var all = await _repository.ListAllAsync(ct);
```
