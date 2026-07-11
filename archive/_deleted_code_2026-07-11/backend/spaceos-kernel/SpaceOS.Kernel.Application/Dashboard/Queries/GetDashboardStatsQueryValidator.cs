// SpaceOS.Kernel.Application/Dashboard/Queries/GetDashboardStatsQueryValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Dashboard.Queries;

/// <summary>
/// Validates <see cref="GetDashboardStatsQuery"/>.
/// The query carries no parameters; this validator satisfies the pipeline contract.
/// </summary>
internal sealed class GetDashboardStatsQueryValidator : AbstractValidator<GetDashboardStatsQuery>
{
    /// <summary>Initialises <see cref="GetDashboardStatsQueryValidator"/> with no rules (parameterless query).</summary>
    public GetDashboardStatsQueryValidator() { }
}
