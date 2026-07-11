// SpaceOS.Infrastructure/Auth/NodeAuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SpaceOS.Modules.Abstractions.Sync;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Development implementation of <see cref="INodeAuthService"/> that issues and
/// validates inter-node JWTs signed with the in-process RSA key from
/// <see cref="DevRsaKeyManager"/>.
/// <para><strong>Production deployments</strong> must bind a Key Vault-backed
/// implementation that uses a managed identity for signing.</para>
/// </summary>
internal sealed class NodeAuthService : INodeAuthService
{
    private const string Issuer   = "spaceos-kernel";
    private const string Audience = "spaceos-sip";

    /// <inheritdoc/>
    public Task<string> IssueNodeJwtAsync(Guid tenantId, string nodeUrl, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeUrl);

        var rsa = DevRsaKeyManager.Instance;
        var key = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, tenantId.ToString()),
            new Claim("node_url", nodeUrl),
            new Claim("tier", "standard"),
        };

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(SyncConstants.NodeJwtTtlHours),
            signingCredentials: creds);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    /// <inheritdoc/>
    public Task<bool> ValidateNodeJwtAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(false);

        // Validation uses the public-key portion only.
        var publicRsa = RSA.Create();
        publicRsa.ImportParameters(DevRsaKeyManager.Instance.ExportParameters(includePrivateParameters: false));
        var signingKey = new RsaSecurityKey(publicRsa);

        var handler = new JwtSecurityTokenHandler();
        try
        {
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer              = Issuer,
                ValidAudience            = Audience,
                IssuerSigningKey         = signingKey,
                ValidAlgorithms          = [SecurityAlgorithms.RsaSha256],
                ValidateLifetime         = true,
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
            }, out _);
            return Task.FromResult(true);
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult(false);
        }
    }
}
