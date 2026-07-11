// SpaceOS.Kernel.Tests/StageRegistry/Handlers/AdvanceFlowEpicStageCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.StageRegistry.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry.Handlers;

/// <summary>Unit tests for <see cref="AdvanceFlowEpicStageCommandHandler"/>.</summary>
public sealed class AdvanceFlowEpicStageCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _epicRepository = new();
    private readonly Mock<IStageChainTemplateRepository> _chainRepository = new();
    private readonly Mock<IStageChainValidator> _validator = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly AdvanceFlowEpicStageCommandHandler _handler;

    private static readonly TenantId TestTenantId = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId TestFacilityId = FacilityId.From(Guid.NewGuid());

    private static FlowEpic BuildEpicWithChain(Guid chainId)
    {
        var epic = FlowEpic.Create("Test Epic", TestFacilityId, TestTenantId);
        epic.AssignChain(chainId, "stage_a");
        epic.PopDomainEvents();
        return epic;
    }

    private static StageChainTemplate BuildTemplateWithStep(Guid chainId)
    {
        // We cannot set Id directly; create and use as-is since chain lookup is by Id from epic
        var template = StageChainTemplate.Create(TestTenantId.Value, "Standard Chain");
        var definition = StageDefinition.Register(TestTenantId.Value, "stage_b", "Stage B", "http://127.0.0.1:5001");
        template.AddStep(definition, 1);
        template.PopDomainEvents();
        return template;
    }

    public AdvanceFlowEpicStageCommandHandlerTests()
    {
        _handler = new AdvanceFlowEpicStageCommandHandler(
            _epicRepository.Object,
            _chainRepository.Object,
            _validator.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ReturnsSuccess()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        var template = BuildTemplateWithStep(chainId);

        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        // ValidateAdvance does not throw — happy path
        _validator
            .Setup(v => v.ValidateAdvance(It.IsAny<FlowEpic>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StageChainStep>>()));

        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidTransition_CallsUpdateAsync_Once()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        var template = BuildTemplateWithStep(chainId);

        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _validator
            .Setup(v => v.ValidateAdvance(It.IsAny<FlowEpic>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StageChainStep>>()));

        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _epicRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<FlowEpic>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTransition_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        var template = BuildTemplateWithStep(chainId);

        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _validator
            .Setup(v => v.ValidateAdvance(It.IsAny<FlowEpic>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StageChainStep>>()));

        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTransition_DispatchesDomainEvents_Once()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        var template = BuildTemplateWithStep(chainId);

        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _validator
            .Setup(v => v.ValidateAdvance(It.IsAny<FlowEpic>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StageChainStep>>()));

        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(d =>
            d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicNotFound_ReturnsNotFound()
    {
        // Arrange
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);
        var command = new AdvanceFlowEpicStageCommand(Guid.NewGuid(), "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_EpicHasNoChainAssigned_ReturnsError()
    {
        // Arrange
        var epic = FlowEpic.Create("Test Epic", TestFacilityId, TestTenantId); // no chain assigned
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Handle_ChainNotFound_ReturnsNotFound()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageChainTemplate?)null);
        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidatorThrowsDomainException_PropagatesException()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var epic = BuildEpicWithChain(chainId);
        var template = BuildTemplateWithStep(chainId);

        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _validator
            .Setup(v => v.ValidateAdvance(It.IsAny<FlowEpic>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<StageChainStep>>()))
            .Throws(new DomainException("Transition not allowed."));

        var command = new AdvanceFlowEpicStageCommand(epic.Id.Value, "invalid_stage");

        // Act + Assert
        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
