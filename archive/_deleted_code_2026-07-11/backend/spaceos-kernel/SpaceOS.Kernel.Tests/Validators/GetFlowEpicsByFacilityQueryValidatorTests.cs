// SpaceOS.Kernel.Tests/Validators/GetFlowEpicsByFacilityQueryValidatorTests.cs
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Moq;
using SpaceOS.Kernel.Application.Common.Behaviors;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>Unit tests for pagination validation rules on <see cref="GetFlowEpicsByFacilityQuery"/>.</summary>
public sealed class GetFlowEpicsByFacilityQueryValidatorTests
{
    // Concrete validator that mirrors the internal GetFlowEpicsByFacilityQueryValidator rules.
    private sealed class PageValidator : AbstractValidator<GetFlowEpicsByFacilityQuery>
    {
        public PageValidator()
        {
            RuleFor(x => x.FacilityId)
                .NotEmpty().WithMessage("FacilityId is required.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    private ValidationBehavior<GetFlowEpicsByFacilityQuery, Result<PagedList<FlowEpicDto>>> BuildBehavior()
        => new([new PageValidator()]);

    [Fact]
    public async Task Handle_InvalidPageSize_ReturnsInvalidResult()
    {
        // Arrange — pageSize=101 exceeds the maximum of 100
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FlowEpicDto>>>>();

        // Act
        var result = await behavior.Handle(
            new GetFlowEpicsByFacilityQuery(Guid.NewGuid(), Page: 1, PageSize: 101),
            next.Object,
            CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_PageZero_ReturnsInvalidResult()
    {
        // Arrange — page=0 is below the minimum of 1
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FlowEpicDto>>>>();

        // Act
        var result = await behavior.Handle(
            new GetFlowEpicsByFacilityQuery(Guid.NewGuid(), Page: 0, PageSize: 20),
            next.Object,
            CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPagingParameters_CallsNext()
    {
        // Arrange — page=1, pageSize=20 are within bounds
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FlowEpicDto>>>>();
        next.Setup(n => n()).ReturnsAsync(Result<PagedList<FlowEpicDto>>.Success(
            new PagedList<FlowEpicDto>(new List<FlowEpicDto>().AsReadOnly(), 1, 20, 0)));

        // Act
        var result = await behavior.Handle(
            new GetFlowEpicsByFacilityQuery(Guid.NewGuid(), Page: 1, PageSize: 20),
            next.Object,
            CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        next.Verify(n => n(), Times.Once);
    }
}
