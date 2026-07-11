// SpaceOS.Kernel.Tests/Application/GetManifestQueryHandlerTests.cs
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Nodes;
using SpaceOS.Kernel.Application.Nodes.Queries;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetManifestQueryHandler"/>.</summary>
public sealed class GetManifestQueryHandlerTests
{
    private readonly Mock<INodeManifestRepository> _repository = new();
    private readonly GetManifestQueryHandler _handler;

    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private const string ValidServerUrl = "https://node.example.com";

    public GetManifestQueryHandlerTests()
    {
        _handler = new GetManifestQueryHandler(_repository.Object);
    }

    // --- Success path ---

    [Fact]
    public async Task Handle_ExistingManifest_ReturnsSuccessWithDto()
    {
        // Arrange
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), ValidServerUrl);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var query = new GetManifestQuery(ValidTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ValidTenantId, result.Value.TenantId);
        Assert.Equal(ValidServerUrl, result.Value.ServerUrl);
    }

    [Fact]
    public async Task Handle_ExistingManifest_DtoNodeJwtIsNull()
    {
        // Arrange — NodeJwt is only populated on registration, not queries
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), ValidServerUrl);
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var query = new GetManifestQuery(ValidTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result.Value.NodeJwt);
    }

    // --- Not found path ---

    [Fact]
    public async Task Handle_NoManifestForTenant_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);

        var query = new GetManifestQuery(ValidTenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
