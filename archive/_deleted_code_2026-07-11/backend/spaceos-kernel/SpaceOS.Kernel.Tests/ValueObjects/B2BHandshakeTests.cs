// SpaceOS.Kernel.Tests/ValueObjects/B2BHandshakeTests.cs

using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

/// <summary>Unit tests for <see cref="B2BHandshake"/> value object invariants and Sprint C extensions.</summary>
public sealed class B2BHandshakeTests
{
    private static readonly TenantId GuestTenant = TenantId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Constructor_WithRequiredArgs_SetsGuestTenantId()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Equal(GuestTenant, handshake.GuestTenantId);
    }

    [Fact]
    public void Constructor_WithRequiredArgs_SetsDelegatedOn()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Equal(Now, handshake.DelegatedOn);
    }

    [Fact]
    public void Constructor_WithRequiredArgsOnly_InitiatorAnchorJsonIsNull()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Null(handshake.InitiatorAnchorJson);
    }

    [Fact]
    public void Constructor_WithRequiredArgsOnly_ResponsibleAnchorJsonIsNull()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Null(handshake.ResponsibleAnchorJson);
    }

    [Fact]
    public void Constructor_WithRequiredArgsOnly_VisibilityScopeIsNull()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Null(handshake.VisibilityScope);
    }

    [Fact]
    public void Constructor_WithRequiredArgsOnly_ContractHashIsNull()
    {
        // Act
        var handshake = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Null(handshake.ContractHash);
    }

    [Fact]
    public void InitWith_InitiatorAnchorJson_SetsValue()
    {
        // Arrange
        const string anchorJson = "{\"nodeId\":\"abc\"}";

        // Act
        var handshake = new B2BHandshake(GuestTenant, Now)
        {
            InitiatorAnchorJson = anchorJson
        };

        // Assert
        Assert.Equal(anchorJson, handshake.InitiatorAnchorJson);
    }

    [Fact]
    public void InitWith_ResponsibleAnchorJson_SetsValue()
    {
        // Arrange
        const string anchorJson = "{\"nodeId\":\"xyz\"}";

        // Act
        var handshake = new B2BHandshake(GuestTenant, Now)
        {
            ResponsibleAnchorJson = anchorJson
        };

        // Assert
        Assert.Equal(anchorJson, handshake.ResponsibleAnchorJson);
    }

    [Fact]
    public void InitWith_VisibilityScope_SetsValue()
    {
        // Arrange
        const string scope = "Public";

        // Act
        var handshake = new B2BHandshake(GuestTenant, Now)
        {
            VisibilityScope = scope
        };

        // Assert
        Assert.Equal(scope, handshake.VisibilityScope);
    }

    [Fact]
    public void InitWith_ContractHash_SetsValue()
    {
        // Arrange
        const string hash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";

        // Act
        var handshake = new B2BHandshake(GuestTenant, Now)
        {
            ContractHash = hash
        };

        // Assert
        Assert.Equal(hash, handshake.ContractHash);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var handshake1 = new B2BHandshake(GuestTenant, Now);
        var handshake2 = new B2BHandshake(GuestTenant, Now);

        // Assert
        Assert.Equal(handshake1, handshake2);
    }

    [Fact]
    public void RecordEquality_DifferentGuestTenant_AreNotEqual()
    {
        // Arrange
        var handshake1 = new B2BHandshake(GuestTenant, Now);
        var handshake2 = new B2BHandshake(TenantId.New(), Now);

        // Assert
        Assert.NotEqual(handshake1, handshake2);
    }

    [Fact]
    public void RecordEquality_WithAndWithoutAnchorJson_AreNotEqual()
    {
        // Arrange
        var base_ = new B2BHandshake(GuestTenant, Now);
        var withAnchor = new B2BHandshake(GuestTenant, Now)
        {
            InitiatorAnchorJson = "{\"nodeId\":\"abc\"}"
        };

        // Assert
        Assert.NotEqual(base_, withAnchor);
    }
}
