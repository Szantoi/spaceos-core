// SpaceOS.Kernel.Tests/Application/RegisterNodeCommandHandlerTests.cs
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Nodes;
using SpaceOS.Kernel.Application.Nodes.Commands.RegisterNode;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Modules.Abstractions.Actors;
using SpaceOS.Modules.Abstractions.Sync;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="RegisterNodeCommandHandler"/>.</summary>
public sealed class RegisterNodeCommandHandlerTests
{
    private readonly Mock<INodeManifestRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INodeUrlValidator> _urlValidator = new();
    private readonly Mock<INodeAuthService> _nodeAuthService = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly RegisterNodeCommandHandler _handler;

    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidServerUrl = "https://node.example.com";
    private const string FakeJwt = "eyJhbGciOiJSUzI1NiJ9.test.sig";

    public RegisterNodeCommandHandlerTests()
    {
        _handler = new RegisterNodeCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _urlValidator.Object,
            _nodeAuthService.Object,
            _dispatcher.Object);
    }

    // --- Success path ---

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithDto()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns((string?)null);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);
        _nodeAuthService
            .Setup(s => s.IssueNodeJwtAsync(ValidTenantId, ValidServerUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeJwt);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ValidTenantId, result.Value.TenantId);
        Assert.Equal(ValidServerUrl, result.Value.ServerUrl);
        Assert.Equal(FakeJwt, result.Value.NodeJwt);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsync_Once()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns((string?)null);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);
        _nodeAuthService
            .Setup(s => s.IssueNodeJwtAsync(ValidTenantId, ValidServerUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeJwt);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.AddAsync(It.IsAny<NodeManifest>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_DispatchesDomainEvents_Once()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns((string?)null);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);
        _nodeAuthService
            .Setup(s => s.IssueNodeJwtAsync(ValidTenantId, ValidServerUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeJwt);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- SSRF rejection path ---

    [Fact]
    public async Task Handle_SsrfUrl_ReturnsInvalid()
    {
        // Arrange
        const string rejectionReason = "Private IP ranges are not permitted.";
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns(rejectionReason);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, e => e.ErrorMessage == rejectionReason);
    }

    [Fact]
    public async Task Handle_SsrfUrl_NeverCallsRepository()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns("blocked");

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.AddAsync(It.IsAny<NodeManifest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SsrfUrl_NeverDispatchesDomainEvents()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns("blocked");

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- Duplicate tenant path ---

    [Fact]
    public async Task Handle_ExistingManifestForTenant_ReturnsConflict()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns((string?)null);
        var existing = NodeManifest.Create(TenantId.From(ValidTenantId), ValidServerUrl);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Handle_ExistingManifestForTenant_NeverCallsAddAsync()
    {
        // Arrange
        _urlValidator.Setup(v => v.Validate(ValidServerUrl)).Returns((string?)null);
        var existing = NodeManifest.Create(TenantId.From(ValidTenantId), ValidServerUrl);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new RegisterNodeCommand(ValidTenantId, ValidServerUrl);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.AddAsync(It.IsAny<NodeManifest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
