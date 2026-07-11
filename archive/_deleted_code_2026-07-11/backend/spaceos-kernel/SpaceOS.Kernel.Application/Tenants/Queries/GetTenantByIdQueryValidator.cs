using FluentValidation;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

public class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");
    }
}
