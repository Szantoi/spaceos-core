// Identity.Tests/Infrastructure/UserCacheServiceTests.cs

using Identity.Application.Common.DTOs;
using Identity.Infrastructure.Cache;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace Identity.Tests.Infrastructure;

public sealed class UserCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _muxMock = new();
    private readonly Mock<IDatabase> _dbMock = new();

    private UserCacheService CreateService()
    {
        _muxMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(_dbMock.Object);
        return new UserCacheService(_muxMock.Object, NullLogger<UserCacheService>.Instance);
    }

    [Fact]
    public async Task GetCachedUsersAsync_CacheHit_ReturnsList()
    {
        var tenantId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new(Guid.NewGuid(), tenantId, "a@b.com", "A", "B", "Active", "Synced")
        };
        var json = JsonSerializer.Serialize(users);

        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)json);

        var service = CreateService();
        var result = await service.GetCachedUsersAsync(tenantId);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("a@b.com", result[0].Email);
    }

    [Fact]
    public async Task GetCachedUsersAsync_CacheMiss_ReturnsNull()
    {
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);

        var service = CreateService();
        var result = await service.GetCachedUsersAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SetCachedUsersAsync_CallsStringSet()
    {
        var tenantId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new(Guid.NewGuid(), tenantId, "a@b.com", "A", "B", "Active", "Synced")
        };

        _dbMock.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
                It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        var service = CreateService();
        await service.SetCachedUsersAsync(tenantId, users.AsReadOnly());

        _dbMock.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(tenantId.ToString())),
            It.IsAny<RedisValue>(),
            It.Is<TimeSpan?>(t => t == TimeSpan.FromSeconds(30)),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_DeletesKey()
    {
        var tenantId = Guid.NewGuid();
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        var service = CreateService();
        await service.InvalidateAsync(tenantId);

        _dbMock.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(tenantId.ToString())),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetCachedUsersAsync_RedisUnavailable_ReturnsNull()
    {
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        var service = CreateService();
        var result = await service.GetCachedUsersAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
