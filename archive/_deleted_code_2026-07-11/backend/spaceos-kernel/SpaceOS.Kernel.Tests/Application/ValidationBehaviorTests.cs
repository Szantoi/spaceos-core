using Ardalis.Result;
using FluentValidation;
using MediatR;
using Moq;
using SpaceOS.Kernel.Application.Common.Behaviors;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

// Public stubs so FluentValidation and MediatR can resolve them generically.
public sealed record ResultRequest : IRequest<Result>;
public sealed record ResultOfGuidRequest : IRequest<Result<Guid>>;

public class ValidationBehaviorTests
{
    /// <summary>Concrete validator stub that always fails with a preset error — avoids Moq proxy issues with generic type parameters.</summary>
    private sealed class AlwaysFailValidator<T> : AbstractValidator<T>
    {
        public AlwaysFailValidator(string propertyName, string errorMessage)
        {
            RuleFor(x => x)
                .Must(_ => false)
                .WithName(propertyName)
                .WithMessage(errorMessage);
        }
    }

    [Fact]
    public async Task Handle_WhenValidatorFailsAndResponseIsResult_ReturnsInvalidResult()
    {
        // Arrange
        var validator = new AlwaysFailValidator<ResultRequest>("Name", "Name is required");
        var behavior = new ValidationBehavior<ResultRequest, Result>([validator]);
        var next = new Mock<RequestHandlerDelegate<Result>>();

        // Act
        var result = await behavior.Handle(new ResultRequest(), next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidatorFailsAndResponseIsResultOfGuid_ReturnsInvalidResultOfGuid()
    {
        // Arrange
        var validator = new AlwaysFailValidator<ResultOfGuidRequest>("Id", "Id must not be empty");
        var behavior = new ValidationBehavior<ResultOfGuidRequest, Result<Guid>>([validator]);
        var next = new Mock<RequestHandlerDelegate<Result<Guid>>>();

        // Act
        var result = await behavior.Handle(new ResultOfGuidRequest(), next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoValidators_CallsNextAndReturnsItsResult()
    {
        // Arrange
        var expected = Result<Guid>.Success(Guid.NewGuid());
        var behavior = new ValidationBehavior<ResultOfGuidRequest, Result<Guid>>([]);
        var next = new Mock<RequestHandlerDelegate<Result<Guid>>>();
        next.Setup(n => n()).ReturnsAsync(expected);

        // Act
        var result = await behavior.Handle(new ResultOfGuidRequest(), next.Object, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(expected.Value, result.Value);
        next.Verify(n => n(), Times.Once);
    }
}
