// SpaceOS.Kernel.Tests/AuditLog/GetAuditEventsQueryValidatorTests.cs

using Ardalis.Result;
using FluentValidation;
using MediatR;
using Moq;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Common.Behaviors;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>Unit tests for validation rules on <see cref="GetAuditEventsQuery"/>.</summary>
public sealed class GetAuditEventsQueryValidatorTests
{
    // Concrete validator that mirrors the internal GetAuditEventsQueryValidator rules.
    private sealed class AuditQueryValidator : AbstractValidator<GetAuditEventsQuery>
    {
        public AuditQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");

            RuleFor(x => x)
                .Must(q => q.From is null || q.To is null || q.From <= q.To)
                .WithName(nameof(GetAuditEventsQuery.From))
                .WithMessage("From must be before or equal to To.");
        }
    }

    private ValidationBehavior<GetAuditEventsQuery, Result<PagedList<AuditEventDto>>> BuildBehavior()
        => new([new AuditQueryValidator()]);

    private static GetAuditEventsQuery ValidQuery() => new(
        TenantId: Guid.NewGuid(),
        EventType: null,
        From: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        To: new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
        Page: 1,
        PageSize: 20);

    [Fact]
    public async Task GetAuditEventsQueryValidator_ValidQuery_Passes()
    {
        // Arrange
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<AuditEventDto>>>>();
        next.Setup(n => n()).ReturnsAsync(Result<PagedList<AuditEventDto>>.Success(
            new PagedList<AuditEventDto>(new List<AuditEventDto>().AsReadOnly(), 1, 20, 0)));

        // Act
        var result = await behavior.Handle(ValidQuery(), next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        next.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task GetAuditEventsQueryValidator_InvalidDateRange_Fails()
    {
        // Arrange — from is after to
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<AuditEventDto>>>>();
        var query = new GetAuditEventsQuery(
            TenantId: Guid.NewGuid(),
            EventType: null,
            From: new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Page: 1,
            PageSize: 20);

        // Act
        var result = await behavior.Handle(query, next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, e =>
            e.ErrorMessage.Contains("From must be before or equal to To"));
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task GetAuditEventsQueryValidator_NullTenantId_Passes()
    {
        // Arrange
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<AuditEventDto>>>>();
        next.Setup(n => n()).ReturnsAsync(Result.Success(
            new PagedList<AuditEventDto>(new List<AuditEventDto>().AsReadOnly(), 1, 20, 0)));
        var query = new GetAuditEventsQuery(
            TenantId: null,
            EventType: null,
            From: null,
            To: null,
            Page: 1,
            PageSize: 20);

        // Act
        var result = await behavior.Handle(query, next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        next.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task GetAuditEventsQueryValidator_PageZero_Fails()
    {
        // Arrange
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<AuditEventDto>>>>();
        var query = new GetAuditEventsQuery(
            TenantId: Guid.NewGuid(),
            EventType: null,
            From: null,
            To: null,
            Page: 0,
            PageSize: 20);

        // Act
        var result = await behavior.Handle(query, next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task GetAuditEventsQueryValidator_PageSizeExceeds100_Fails()
    {
        // Arrange
        var behavior = BuildBehavior();
        var next = new Mock<RequestHandlerDelegate<Result<PagedList<AuditEventDto>>>>();
        var query = new GetAuditEventsQuery(
            TenantId: Guid.NewGuid(),
            EventType: null,
            From: null,
            To: null,
            Page: 1,
            PageSize: 101);

        // Act
        var result = await behavior.Handle(query, next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }
}
