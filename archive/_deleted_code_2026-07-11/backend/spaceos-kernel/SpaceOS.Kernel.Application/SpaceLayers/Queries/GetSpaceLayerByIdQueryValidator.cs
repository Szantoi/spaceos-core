using FluentValidation;

namespace SpaceOS.Kernel.Application.SpaceLayers.Queries;

public class GetSpaceLayerByIdQueryValidator : AbstractValidator<GetSpaceLayerByIdQuery>
{
    public GetSpaceLayerByIdQueryValidator()
    {
        RuleFor(x => x.SpaceLayerId)
            .NotEmpty().WithMessage("SpaceLayerId is required.");
    }
}
