// Identity.Tests/Application/GetUserByIdQueryTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Queries;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class GetUserByIdQueryTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();

    private GetUserByIdQueryHandler CreateHandler(Guid tenantId)
    {
        _ctxMock.Setup(c => c.TenantId).Returns(tenantId);
        return new GetUserByIdQueryHandler(_repoMock.Object, _ctxMock.Object);
    }

    [Fact]
    public async Task Handle_OwnTenantUser_ReturnsUser()
    {
        var tenantId = Guid.NewGuid();
        var user = SpaceOSUser.Create(tenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler(tenantId);
        var result = await handler.Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id.Value, result.Value.Id);
    }

    [Fact]
    public async Task Handle_CrossTenantUser_ReturnsForbidden_BOLA()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        // User belongs to tenantB
        var user = SpaceOSUser.Create(tenantB, Email.From("a@b.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        // Current user is from tenantA → BOLA
        var handler = CreateHandler(tenantA);
        var result = await handler.Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        var handler = CreateHandler(Guid.NewGuid());
        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.NotFound, result.Status);
    }
}
