// Identity.Tests/Infrastructure/CrossTenantAccessTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Queries;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Infrastructure;

/// <summary>
/// Verifies cross-tenant isolation invariants via mock repository.
/// Real RLS enforcement is exercised by integration tests against a live PostgreSQL instance.
/// </summary>
public sealed class CrossTenantAccessTests
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private SpaceOSUser MakeUser(Guid tenantId, string email) =>
        SpaceOSUser.Create(tenantId, Email.From(email), DisplayName.From("A", "B"));

    [Fact]
    public async Task ListTenantUsers_TenantAToken_ReturnsOnlyTenantAUsers()
    {
        var userA1 = MakeUser(_tenantA, "a1@example.com");
        var userA2 = MakeUser(_tenantA, "a2@example.com");

        var repoMock = new Mock<ISpaceOSUserRepository>();
        repoMock.Setup(r => r.ListByTenantAsync(_tenantA, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SpaceOSUser> { userA1, userA2 }.AsReadOnly());

        var ctxMock = new Mock<ICurrentUserContext>();
        ctxMock.Setup(c => c.TenantId).Returns(_tenantA);

        var handler = new ListTenantUsersQueryHandler(repoMock.Object, ctxMock.Object);
        var result = await handler.Handle(new ListTenantUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, u => Assert.Equal(_tenantA, u.TenantId));
        Assert.DoesNotContain(result.Value, u => u.TenantId == _tenantB);
    }

    [Fact]
    public async Task GetUserById_TenantAToken_TenantBUserId_ReturnsForbidden()
    {
        var userB = MakeUser(_tenantB, "b@example.com");

        var repoMock = new Mock<ISpaceOSUserRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userB);

        var ctxMock = new Mock<ICurrentUserContext>();
        ctxMock.Setup(c => c.TenantId).Returns(_tenantA); // TenantA caller

        var handler = new GetUserByIdQueryHandler(repoMock.Object, ctxMock.Object);
        var result = await handler.Handle(new GetUserByIdQuery(userB.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task ListTenantUsers_EmptyTenant_ReturnsEmptyList()
    {
        var repoMock = new Mock<ISpaceOSUserRepository>();
        repoMock.Setup(r => r.ListByTenantAsync(_tenantA, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SpaceOSUser>().AsReadOnly());

        var ctxMock = new Mock<ICurrentUserContext>();
        ctxMock.Setup(c => c.TenantId).Returns(_tenantA);

        var handler = new ListTenantUsersQueryHandler(repoMock.Object, ctxMock.Object);
        var result = await handler.Handle(new ListTenantUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void SpaceOSUserRepository_DeleteNotExposed_InvariantCheck()
    {
        // The ISpaceOSUserRepository interface does NOT expose Delete —
        // enforcing the identity_app role constraint at the interface level.
        var methods = typeof(ISpaceOSUserRepository).GetMethods();
        var deleteMethod = methods.FirstOrDefault(m =>
            m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("Remove", StringComparison.OrdinalIgnoreCase));

        Assert.Null(deleteMethod);
    }
}
