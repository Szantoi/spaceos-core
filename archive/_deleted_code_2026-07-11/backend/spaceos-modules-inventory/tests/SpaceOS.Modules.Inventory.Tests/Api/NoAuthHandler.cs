using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SpaceOS.Modules.Inventory.Tests.Api;

/// <summary>Authentication handler that always fails — used to test 401 scenarios.</summary>
public class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Fail("No authentication"));
}
