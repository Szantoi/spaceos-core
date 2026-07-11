using Ardalis.Result;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Kernel.Application.Internal.Dtos;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Application.Internal.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public sealed class GetTenantActorQueryHandlerTests
{
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid TargetId    = Guid.NewGuid();

    private readonly Mock<ITenantRepository>          _tenants   = new();
    private readonly Mock<IB2BHandshakeVerifier>      _handshake = new();
    private readonly Mock<IInternalAccessAuditWriter> _audit     = new();

    private GetTenantActorQueryHandler CreateSut() =>
        new(_tenants.Object, _handshake.Object, _audit.Object,
            NullLogger<GetTenantActorQueryHandler>.Instance);

    private static Tenant MakeTenant(string name, TenantType type) =>
        Tenant.Register(name, tenantType: type);

    [Fact]
    public async Task Handle_ExistingTenant_NoHandshake_ReturnsSuccessWithFalseFlag()
    {
        // Arrange
        var tenant = MakeTenant("Partner Kft.", TenantType.Manufacturer);
        _tenants.Setup(r => r.GetByIdAsync(TenantId.From(TargetId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(false);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "Found", default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasVerifiedHandshakeWithRequester);
        Assert.Equal("Manufacturer", result.Value.TenantType);
        _audit.Verify(a => a.RecordAsync(RequesterId, TargetId, "Found", default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingTenant_WithHandshake_ReturnsTrueFlag()
    {
        // Arrange
        var tenant = MakeTenant("Partner Kft.", TenantType.PanelCutter);
        _tenants.Setup(r => r.GetByIdAsync(TenantId.From(TargetId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(true);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "Found", default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasVerifiedHandshakeWithRequester);
        Assert.Equal("PanelCutter", result.Value.TenantType);
    }

    [Fact]
    public async Task Handle_NonExistentTarget_ReturnsNotFound()
    {
        // Arrange
        _tenants.Setup(r => r.GetByIdAsync(TenantId.From(TargetId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "NotFound", default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        _handshake.Verify(
            h => h.HasVerifiedHandshakeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default),
            Times.Never);
        _audit.Verify(a => a.RecordAsync(RequesterId, TargetId, "NotFound", default), Times.Once);
    }

    [Fact]
    public async Task Handle_AuditWriteThrows_ResponseStillReturned()
    {
        // Arrange
        var tenant = MakeTenant("Stable Kft.", TenantType.Manufacturer);
        _tenants.Setup(r => r.GetByIdAsync(TenantId.From(TargetId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(false);
        _audit.Setup(a => a.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        // Act
        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TenantActorResponse_ContainsNoPiiFields()
    {
        // Arrange
        var forbiddenNames = new[]
        {
            "email", "phone", "address", "contact", "tax",
            "billing", "vat", "iban", "zip", "city", "street"
        };

        // Act
        var properties = typeof(TenantActorResponse)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant());

        // Assert
        foreach (var forbidden in forbiddenNames)
            Assert.DoesNotContain(properties, p => p.Contains(forbidden));
    }
}
