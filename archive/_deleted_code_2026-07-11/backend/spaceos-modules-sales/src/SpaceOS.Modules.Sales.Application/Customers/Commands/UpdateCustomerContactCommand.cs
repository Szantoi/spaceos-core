using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record UpdateCustomerContactCommand(
    Guid CustomerId, string ContactName, string? ContactEmail, string? ContactPhone)
    : IRequest<Result<CustomerResponse>>;

public sealed class UpdateCustomerContactCommandValidator : AbstractValidator<UpdateCustomerContactCommand>
{
    public UpdateCustomerContactCommandValidator()
    {
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(320).When(x => x.ContactEmail is not null);
        RuleFor(x => x.ContactPhone).MaximumLength(50).When(x => x.ContactPhone is not null);
    }
}

public sealed class UpdateCustomerContactCommandHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<UpdateCustomerContactCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(UpdateCustomerContactCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        Email? email = null;
        if (cmd.ContactEmail is not null)
        {
            var r = Email.From(cmd.ContactEmail);
            if (!r.IsSuccess) return Result.Invalid(r.ValidationErrors.ToArray());
            email = r.Value;
        }

        PhoneNumber? phone = null;
        if (cmd.ContactPhone is not null)
        {
            var r = PhoneNumber.From(cmd.ContactPhone);
            if (!r.IsSuccess) return Result.Invalid(r.ValidationErrors.ToArray());
            phone = r.Value;
        }

        var result = customer.UpdateContact(cmd.ContactName, email, phone, clock);
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
