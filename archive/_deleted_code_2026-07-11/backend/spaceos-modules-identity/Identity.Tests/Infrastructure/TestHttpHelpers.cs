// Identity.Tests/Infrastructure/TestHttpHelpers.cs

namespace Identity.Tests.Infrastructure;

internal sealed class DelegatingHandlerStub : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
