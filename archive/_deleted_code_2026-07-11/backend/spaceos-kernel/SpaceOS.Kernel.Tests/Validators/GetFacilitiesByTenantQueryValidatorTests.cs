// SpaceOS.Kernel.Tests/Validators/GetFacilitiesByTenantQueryValidatorTests.cs
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Moq;
using SpaceOS.Kernel.Application.Common.Behaviors;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Facilities.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Validators;

/// <summary>Unit tests for pagination validation rules on <see cref="GetFacilitiesByTenantQuery"/>.</summary>
public sealed class GetFacilitiesByTenantQueryValidatorTests
{
    // Concrete validator that mirrors the internal GetFacilitiesByTenantQueryValidator rules.
    private sealed class PageValidator : AbstractValidator<GetFacilitiesByTenantQuery>
    {
        public PageValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    private ValidationBehavior<GetFacilitiesByTenantQuery, Result<PagedList<FacilityDto>>> BuildBehavior()
        => new([new PageValidator()]);

    [Fact]
    public async Task Handle_InvalidPageSize_ReturnsInvalidResult()
    {
        // Arrange — pageSize=101 exceeds the maximum of 100
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FacilityDto>>>>();

        // Act
        var result = await behavior.Handle(
            new GetFacilitiesByTenantQuery(Guid.NewGuid(), Page: 1, PageSize: 101),
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
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FacilityDto>>>>();

        // Act
        var result = await behavior.Handle(
            new GetFacilitiesByTenantQuery(Guid.NewGuid(), Page: 0, PageSize: 20),
            next.Object,
            CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPagingParameters_CallsNext()
    {
        // Arrange — page=2, pageSize=10 are within bounds
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<FacilityDto>>>>();
        next.Setup(n => n()).ReturnsAsync(Result<PagedList<FacilityDto>>.Success(
            new PagedList<FacilityDto>(new List<FacilityDto>().AsReadOnly(), 2, 10, 0)));

        // Act
        var result = await behavior.Handle(
            new GetFacilitiesByTenantQuery(Guid.NewGuid(), Page: 2, PageSize: 10),
            next.Object,
            CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        next.Verify(n => n(), Times.Once);
    }
}
