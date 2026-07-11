// Identity.Infrastructure/Keycloak/IdentityProviderException.cs

namespace Identity.Infrastructure.Keycloak;

public sealed class IdentityProviderException : Exception
{
    public int? StatusCode { get; }

    public IdentityProviderException(string message) : base(message) { }

    public IdentityProviderException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public IdentityProviderException(string message, Exception inner) : base(message, inner) { }
}
