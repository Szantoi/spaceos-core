// SpaceOS.Kernel.Application/Nodes/Queries/GetManifestQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Nodes.Queries;

/// <summary>Validates <see cref="GetManifestQuery"/> input before the handler executes.</summary>
internal sealed class GetManifestQueryValidator : AbstractValidator<GetManifestQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetManifestQuery"/>.</summary>
    public GetManifestQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();
    }
}
