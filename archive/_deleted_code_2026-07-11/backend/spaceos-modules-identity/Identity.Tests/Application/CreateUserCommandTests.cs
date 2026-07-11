// Identity.Tests/Application/CreateUserCommandTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Commands;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class CreateUserCommandTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<IKcSyncOutboxRepository> _outboxMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CreateUserCommandHandler CreateHandler()
    {
        _ctxMock.Setup(c => c.TenantId).Returns(_tenantId);
        return new CreateUserCommandHandler(_repoMock.Object, _outboxMock.Object, _ctxMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCreatedUser()
    {
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), _tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserCommand("user@example.com", "Kovács", "János"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Email);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SpaceOSUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxMock.Verify(o => o.InsertAsync(It.IsAny<KcSyncOutboxEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflict()
    {
        var existing = SpaceOSUser.Create(_tenantId, Email.From("user@example.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), _tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserCommand("user@example.com", "Kovács", "János"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsInvalid()
    {
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), _tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateUserCommand("not-an-email", "Kovács", "János"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_OutboxInserted_WithCorrectOperation()
    {
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), _tenantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SpaceOSUser?)null);

        KcSyncOutboxEntry? captured = null;
        _outboxMock.Setup(o => o.InsertAsync(It.IsAny<KcSyncOutboxEntry>(), It.IsAny<CancellationToken>()))
                   .Callback<KcSyncOutboxEntry, CancellationToken>((e, _) => captured = e)
                   .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(new CreateUserCommand("user@example.com", "Kovács", "János"), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(KcSyncOperation.CreateUser, captured!.Operation);
        Assert.Equal(_tenantId, captured.TenantId);
    }
}
