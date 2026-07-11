// SpaceOS.Infrastructure/Validation/NodeUrlValidator.cs
using System.Net;
using System.Net.Sockets;
using SpaceOS.Modules.Abstractions.Actors;

namespace SpaceOS.Infrastructure.Validation;

/// <summary>
/// SSRF-prevention implementation of <see cref="INodeUrlValidator"/>.
/// Rejects any URL that is not absolute HTTPS on port 443 or 8443, or that
/// resolves to a private / loopback address.
/// </summary>
/// <remarks>
/// <para>
/// <strong>DNS TOCTOU warning:</strong> This validator checks the literal host string
/// at registration time. It does NOT resolve DNS names to IP addresses. A hostname that
/// passes this check could resolve to a private IP at connection time (DNS rebinding
/// attack). Callers that open outbound connections MUST perform a separate DNS
/// resolution check immediately before connecting, then verify the resolved IP against
/// <see cref="IsPrivateIPv4"/> or the IPv6 private-range checks applied here.
/// </para>
/// <para>
/// This limitation is by design: async DNS resolution cannot be performed inside a
/// synchronous validation method without introducing deadlock risk. The caller
/// (federation execution layer) is responsible for the runtime address check.
/// </para>
/// </remarks>
internal sealed class NodeUrlValidator : INodeUrlValidator
{
    /// <inheritdoc/>
    public string? Validate(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            return "Server URL is required.";

        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
            return "Server URL must be a valid absolute URI.";

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return "Server URL must use HTTPS.";

        // Uri.Port returns -1 when no explicit port is specified; -1 implies the default
        // port for the scheme, which for HTTPS is 443.
        var port = uri.Port == -1 ? 443 : uri.Port;
        if (port != 443 && port != 8443)
            return $"Server URL port must be 443 or 8443, got {port}.";

        if (IsPrivateHost(uri.Host))
            return "Server URL must not resolve to a private or loopback address.";

        return null;
    }

    private static bool IsPrivateHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var ip))
        {
            // DNS name — reject obvious internal aliases.
            // See DNS TOCTOU warning in class XML doc for runtime validation requirement.
            return host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsPrivateIPv4(ip.GetAddressBytes());
        }

        // IPv6 branch
        // Check for IPv4-mapped IPv6 addresses (::ffff:0:0/96) first.
        // Without this check an attacker can bypass IPv4 private-range blocks by
        // supplying e.g. "::ffff:192.168.1.1" which maps to the private 192.168.1.1.
        if (ip.IsIPv4MappedToIPv6)
        {
            var mappedIpv4 = ip.MapToIPv4();
            return IsPrivateIPv4(mappedIpv4.GetAddressBytes());
        }

        var bytes = ip.GetAddressBytes();

        // ::1 — IPv6 loopback
        if (ip.Equals(IPAddress.IPv6Loopback)) return true;
        // fe80::/10 — link-local
        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return true;
        // fc00::/7 — unique-local (fc00:: and fd00::)
        if (bytes[0] == 0xfc || bytes[0] == 0xfd) return true;

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="bytes"/> represents a private,
    /// loopback, link-local, or reserved IPv4 address.
    /// </summary>
    /// <param name="bytes">A 4-element byte array in network byte order.</param>
    internal static bool IsPrivateIPv4(byte[] bytes)
    {
        // 127.0.0.0/8 — loopback
        if (bytes[0] == 127) return true;
        // 10.0.0.0/8 — private
        if (bytes[0] == 10) return true;
        // 172.16.0.0/12 — private
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        // 192.168.0.0/16 — private
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 169.254.0.0/16 — link-local
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        // 0.0.0.0/8 — this network
        if (bytes[0] == 0) return true;

        return false;
    }
}
