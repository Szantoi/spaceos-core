// Identity.Tests/Application/UpdateUserProfileCommandTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Commands;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class UpdateUserProfileCommandTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private UpdateUserProfileCommandHandler CreateHandler()
    {
        _ctxMock.Setup(c => c.TenantId).Returns(_tenantId);
        return new UpdateUserProfileCommandHandler(_repoMock.Object, _ctxMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedUser()
    {
        var user = SpaceOSUser.Create(_tenantId, Email.From("a@b.com"), DisplayName.From("Old", "Name"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserProfileCommand(user.Id.Value, "New", "Name"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value.FirstName);
        _repoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserProfileCommand(Guid.NewGuid(), "A", "B"), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_CrossTenantUser_ReturnsForbidden()
    {
        var user = SpaceOSUser.Create(Guid.NewGuid(), Email.From("a@b.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserProfileCommand(user.Id.Value, "A", "B"), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.Forbidden, result.Status);
    }
}
