using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record CreateCustomerCommand(
    CustomerType Type,
    string DisplayName,
    string ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes) : IRequest<Result<CustomerResponse>>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(320).When(x => x.ContactEmail is not null);
        RuleFor(x => x.ContactPhone).MaximumLength(50).When(x => x.ContactPhone is not null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}

public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customers,
    IQuotaGuard quotaGuard,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var quota = await quotaGuard.EnsureCanCreateAsync(tenantContext.TenantId, QuotaScope.Customer, ct)
            .ConfigureAwait(false);
        if (!quota.IsSuccess) return Result.Forbidden();

        Email? email = null;
        if (cmd.ContactEmail is not null)
        {
            var emailResult = Email.From(cmd.ContactEmail);
            if (!emailResult.IsSuccess) return Result.Invalid(emailResult.ValidationErrors.ToArray());
            email = emailResult.Value;
        }

        PhoneNumber? phone = null;
        if (cmd.ContactPhone is not null)
        {
            var phoneResult = PhoneNumber.From(cmd.ContactPhone);
            if (!phoneResult.IsSuccess) return Result.Invalid(phoneResult.ValidationErrors.ToArray());
            phone = phoneResult.Value;
        }

        var result = Customer.Create(
            tenantContext.TenantId, cmd.Type, cmd.DisplayName, cmd.ContactName,
            email, phone, tenantContext.ActorSub, clock);

        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        var customer = result.Value;
        await customers.AddAsync(customer, ct).ConfigureAwait(false);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success(MapToResponse(customer));
    }

    internal static CustomerResponse MapToResponse(Customer c) => new(
        c.Id, c.TenantId, c.Type, c.DisplayName, c.CompanyTaxNumber,
        c.ContactName, c.ContactEmail?.Value, c.ContactPhone?.Value,
        c.BillingAddress is null ? null : new AddressDto(c.BillingAddress.Street, c.BillingAddress.City, c.BillingAddress.ZipCode, c.BillingAddress.Country),
        c.ShippingAddress is null ? null : new AddressDto(c.ShippingAddress.Street, c.ShippingAddress.City, c.ShippingAddress.ZipCode, c.ShippingAddress.Country),
        c.Status, c.LinkedTenantId, c.LinkedAt, c.LinkStatus, c.LinkVerifiedAt,
        c.Notes, c.IsArchived, c.CreatedAt, c.CreatedBy, c.UpdatedAt);
}
