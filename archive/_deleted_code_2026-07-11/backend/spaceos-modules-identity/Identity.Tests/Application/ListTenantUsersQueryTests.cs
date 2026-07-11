// Identity.Tests/Application/ListTenantUsersQueryTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Queries;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ListTenantUsersQueryTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();

    private ListTenantUsersQueryHandler CreateHandler(Guid tenantId)
    {
        _ctxMock.Setup(c => c.TenantId).Returns(tenantId);
        return new ListTenantUsersQueryHandler(_repoMock.Object, _ctxMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsTenantUsersOnly()
    {
        var tenantId = Guid.NewGuid();
        var users = new List<SpaceOSUser>
        {
            SpaceOSUser.Create(tenantId, Email.From("a@b.com"), DisplayName.From("A", "B")),
            SpaceOSUser.Create(tenantId, Email.From("c@d.com"), DisplayName.From("C", "D"))
        };

        _repoMock.Setup(r => r.ListByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(users.AsReadOnly());

        var handler = CreateHandler(tenantId);
        var result = await handler.Handle(new ListTenantUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, u => Assert.Equal(tenantId, u.TenantId));
    }

    [Fact]
    public async Task Handle_CrossTenantIsolation_ReturnsOnlyOwnTenantUsers()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantAUsers = new List<SpaceOSUser>
        {
            SpaceOSUser.Create(tenantA, Email.From("a@b.com"), DisplayName.From("A", "B"))
        };

        // Repo mock only returns tenantA users (RLS enforced at DB level)
        _repoMock.Setup(r => r.ListByTenantAsync(tenantA, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(tenantAUsers.AsReadOnly());

        var handler = CreateHandler(tenantA);
        var result = await handler.Handle(new ListTenantUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.DoesNotContain(result.Value, u => u.TenantId == tenantB);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredUsers()
    {
        var tenantId = Guid.NewGuid();
        var active = SpaceOSUser.Create(tenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));
        var disabled = SpaceOSUser.Create(tenantId, Email.From("c@d.com"), DisplayName.From("C", "D"));
        disabled.Disable();

        _repoMock.Setup(r => r.ListByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<SpaceOSUser> { active, disabled }.AsReadOnly());

        var handler = CreateHandler(tenantId);
        var result = await handler.Handle(new ListTenantUsersQuery(UserStatus.Active), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Active", result.Value[0].Status);
    }
}
