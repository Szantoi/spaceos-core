using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.FinalizeGyartasilap;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class FinalizeGyartasilapHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();

    private readonly Mock<IGyartasilapRepository> _repository = new();

    private FinalizeGyartasilapCommandHandler CreateHandler() =>
        new(_repository.Object);

    private static Gyartasilap CreateDraftGyartasilap()
        => Gyartasilap.Create(TenantId, OrderId, null, "L1").Value;

    [Fact]
    public async Task Handle_FromDraft_Succeeds()
    {
        // Arrange
        var gyartasilap = CreateDraftGyartasilap();
        _repository.Setup(r => r.GetByIdAsync(gyartasilap.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gyartasilap);
        _repository.Setup(r => r.UpdateAsync(gyartasilap, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new FinalizeGyartasilapCommand(TenantId, gyartasilap.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        gyartasilap.Status.Should().Be(GyartasilapStatus.Finalized);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gyartasilap?)null);

        var command = new FinalizeGyartasilapCommand(TenantId, missingId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_AlreadyFinalized_ReturnsFail()
    {
        // Arrange
        var gyartasilap = CreateDraftGyartasilap();
        gyartasilap.Finalize(); // already finalized

        _repository.Setup(r => r.GetByIdAsync(gyartasilap.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gyartasilap);

        var command = new FinalizeGyartasilapCommand(TenantId, gyartasilap.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSuccess_CallsUpdateRepository()
    {
        // Arrange
        var gyartasilap = CreateDraftGyartasilap();
        _repository.Setup(r => r.GetByIdAsync(gyartasilap.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gyartasilap);
        _repository.Setup(r => r.UpdateAsync(gyartasilap, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new FinalizeGyartasilapCommand(TenantId, gyartasilap.Id);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.UpdateAsync(gyartasilap, It.IsAny<CancellationToken>()), Times.Once);
    }
}
