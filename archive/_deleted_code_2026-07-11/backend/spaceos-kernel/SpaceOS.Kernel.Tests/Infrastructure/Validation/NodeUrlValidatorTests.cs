// SpaceOS.Kernel.Tests/Infrastructure/Validation/NodeUrlValidatorTests.cs
using SpaceOS.Infrastructure.Validation;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure.Validation;

/// <summary>
/// SSRF attack vector tests for <see cref="NodeUrlValidator"/>.
/// Covers all private/loopback/link-local IPv4/IPv6 ranges, HTTP rejection,
/// port restrictions, and internal DNS suffix rejection.
/// </summary>
public sealed class NodeUrlValidatorTests
{
    private readonly NodeUrlValidator _validator = new();

    // --- Valid URLs ---

    [Theory]
    [InlineData("https://node.example.com")]
    [InlineData("https://node.example.com:443")]
    [InlineData("https://node.example.com:8443")]
    [InlineData("https://93.184.216.34")]
    [InlineData("https://93.184.216.34:8443")]
    public void Validate_ValidPublicHttpsUrl_ReturnsNull(string url)
    {
        var result = _validator.Validate(url);
        Assert.Null(result);
    }

    // --- HTTP rejection (must be HTTPS) ---

    [Fact]
    public void Validate_HttpScheme_RejectsNonHttps()
    {
        var result = _validator.Validate("http://node.example.com");
        Assert.NotNull(result);
        Assert.Contains("HTTPS", result, StringComparison.OrdinalIgnoreCase);
    }

    // --- Port restrictions ---

    [Theory]
    [InlineData("https://node.example.com:8080")]
    [InlineData("https://node.example.com:3000")]
    [InlineData("https://node.example.com:22")]
    public void Validate_NonAllowedPort_Rejects(string url)
    {
        var result = _validator.Validate(url);
        Assert.NotNull(result);
        Assert.Contains("port", result, StringComparison.OrdinalIgnoreCase);
    }

    // --- IPv4 private ranges ---

    [Fact]
    public void Validate_Ipv4_Loopback_127001_Rejects()
    {
        var result = _validator.Validate("https://127.0.0.1");
        Assert.NotNull(result);
        Assert.Contains("private", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_Ipv4_Loopback_127255255255_Rejects()
    {
        var result = _validator.Validate("https://127.255.255.255");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_Private_10x_Rejects()
    {
        var result = _validator.Validate("https://10.0.0.1");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_Private_172_16_Rejects()
    {
        var result = _validator.Validate("https://172.16.0.1");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_Private_172_31_Rejects()
    {
        var result = _validator.Validate("https://172.31.255.255");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_Private_192168_Rejects()
    {
        var result = _validator.Validate("https://192.168.1.1");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_LinkLocal_169254_Rejects()
    {
        var result = _validator.Validate("https://169.254.169.254");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4_ThisNetwork_0000_Rejects()
    {
        var result = _validator.Validate("https://0.0.0.0");
        Assert.NotNull(result);
    }

    // --- IPv6 ranges ---

    [Fact]
    public void Validate_Ipv6_Loopback_Rejects()
    {
        var result = _validator.Validate("https://[::1]");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv6_LinkLocal_fe80_Rejects()
    {
        var result = _validator.Validate("https://[fe80::1]");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv6_UniqueLocal_fc00_Rejects()
    {
        var result = _validator.Validate("https://[fc00::1]");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv6_UniqueLocal_fd00_Rejects()
    {
        var result = _validator.Validate("https://[fd00::1]");
        Assert.NotNull(result);
    }

    // --- IPv4-mapped IPv6 bypass ---

    [Fact]
    public void Validate_Ipv4MappedIpv6_PrivateRange_Rejects()
    {
        // ::ffff:192.168.1.1 — attempts to bypass IPv4 private check via IPv6 mapping
        var result = _validator.Validate("https://[::ffff:192.168.1.1]");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_Ipv4MappedIpv6_Loopback_Rejects()
    {
        // ::ffff:127.0.0.1 — attempts to bypass loopback check via IPv6 mapping
        var result = _validator.Validate("https://[::ffff:127.0.0.1]");
        Assert.NotNull(result);
    }

    // --- DNS suffix rejection ---

    [Fact]
    public void Validate_Localhost_Rejects()
    {
        var result = _validator.Validate("https://localhost");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_DotLocal_Suffix_Rejects()
    {
        var result = _validator.Validate("https://myserver.local");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_DotInternal_Suffix_Rejects()
    {
        var result = _validator.Validate("https://backend.internal");
        Assert.NotNull(result);
    }

    // --- Empty / malformed ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNull_Rejects(string? url)
    {
        var result = _validator.Validate(url!);
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_RelativeUrl_Rejects()
    {
        var result = _validator.Validate("/api/nodes");
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_InvalidUri_Rejects()
    {
        var result = _validator.Validate("not-a-url");
        Assert.NotNull(result);
    }
}
