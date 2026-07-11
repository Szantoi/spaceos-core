using FluentValidation;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>
/// FluentValidation rules for <see cref="GetFlowEpicByIdQuery"/>.
/// </summary>
public class GetFlowEpicByIdQueryValidator : AbstractValidator<GetFlowEpicByIdQuery>
{
    /// <summary>Initialises validation rules for <see cref="GetFlowEpicByIdQuery"/>.</summary>
    public GetFlowEpicByIdQueryValidator()
    {
        RuleFor(x => x.FlowEpicId).NotEmpty();
    }
}
