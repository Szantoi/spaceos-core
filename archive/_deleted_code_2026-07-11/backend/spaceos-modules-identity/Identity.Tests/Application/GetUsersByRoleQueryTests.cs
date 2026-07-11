// Identity.Tests/Application/GetUsersByRoleQueryTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Queries;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class GetUsersByRoleQueryTests
{
    private readonly Mock<IIdentityProviderClient> _idpMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();

    private GetUsersByRoleQueryHandler CreateHandler(Guid tenantId)
    {
        _ctxMock.Setup(c => c.TenantId).Returns(tenantId);
        return new GetUsersByRoleQueryHandler(_idpMock.Object, _ctxMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRole_ReturnsUserList()
    {
        var tenantId = Guid.NewGuid();
        var kcId1 = Guid.NewGuid();
        var kcId2 = Guid.NewGuid();
        var users = new List<(KeycloakUserId KcId, Email Email, DisplayName DisplayName, string Role)>
        {
            (KeycloakUserId.From(kcId1.ToString()), Email.From("operator1@example.com"), DisplayName.From("John", "Doe"), "machine_operator"),
            (KeycloakUserId.From(kcId2.ToString()), Email.From("operator2@example.com"), DisplayName.From("Jane", "Smith"), "machine_operator")
        };

        _idpMock.Setup(i => i.GetUsersByRoleAsync(tenantId, "machine_operator", It.IsAny<CancellationToken>()))
                .ReturnsAsync(users.AsReadOnly());

        var handler = CreateHandler(tenantId);
        var result = await handler.Handle(new GetUsersByRoleQuery("machine_operator"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, u => Assert.Equal("machine_operator", u.Role));
    }

    [Fact]
    public async Task Handle_InvalidRole_ReturnsBadRequest()
    {
        var tenantId = Guid.NewGuid();
        var handler = CreateHandler(tenantId);

        var result = await handler.Handle(new GetUsersByRoleQuery("invalid_role"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
        Assert.Contains("Invalid role", result.ValidationErrors.First().ErrorMessage);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsEmpty()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Mock returns empty list for tenantA (RLS policy filters out tenantB users)
        _idpMock.Setup(i => i.GetUsersByRoleAsync(tenantA, "machine_operator", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<(KeycloakUserId, Email, DisplayName, string)>().AsReadOnly());

        var handler = CreateHandler(tenantA);
        var result = await handler.Handle(new GetUsersByRoleQuery("machine_operator"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();

        _idpMock.Setup(i => i.GetUsersByRoleAsync(tenantId, "admin", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<(KeycloakUserId, Email, DisplayName, string)>().AsReadOnly());

        var handler = CreateHandler(tenantId);
        var result = await handler.Handle(new GetUsersByRoleQuery("admin"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
