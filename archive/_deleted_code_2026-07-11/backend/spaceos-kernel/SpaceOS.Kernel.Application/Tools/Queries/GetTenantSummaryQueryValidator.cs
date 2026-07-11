// SpaceOS.Kernel.Application/Tools/Queries/GetTenantSummaryQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Validates <see cref="GetTenantSummaryQuery"/> input.</summary>
internal sealed class GetTenantSummaryQueryValidator : AbstractValidator<GetTenantSummaryQuery>
{
    /// <summary>Initialises the validation rules.</summary>
    public GetTenantSummaryQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId must be a non-empty GUID.");
    }
}
