// Identity.Tests/Application/DisableUserCommandTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Commands;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class DisableUserCommandTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private DisableUserCommandHandler CreateHandler()
    {
        _ctxMock.Setup(c => c.TenantId).Returns(_tenantId);
        return new DisableUserCommandHandler(_repoMock.Object, _ctxMock.Object);
    }

    private SpaceOSUser MakeUser() =>
        SpaceOSUser.Create(_tenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));

    [Fact]
    public async Task Handle_ActiveUser_DisablesSuccessfully()
    {
        var user = MakeUser();
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new DisableUserCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Disabled, user.Status);
        _repoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyDisabledUser_ReturnsError()
    {
        var user = MakeUser();
        user.Disable();
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new DisableUserCommand(user.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
        Assert.Contains("already_disabled", result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DisableUserCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_CrossTenantUser_ReturnsForbidden()
    {
        var otherTenantId = Guid.NewGuid();
        var user = SpaceOSUser.Create(otherTenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new DisableUserCommand(user.Id.Value), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.Forbidden, result.Status);
    }
}
