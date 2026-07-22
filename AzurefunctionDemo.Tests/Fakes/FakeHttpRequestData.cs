using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzurefunctionDemo.Tests.Fakes;

/// <summary>
/// Minimal in-memory HttpRequestData for unit testing isolated-worker functions,
/// since the SDK ships no test doubles for this abstract class.
/// </summary>
public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext, Uri url, string method = "GET", Stream? body = null)
        : base(functionContext)
    {
        Url = url;
        Method = method;
        Body = body ?? new MemoryStream();
    }

    public override Stream Body { get; }

    public override HttpHeadersCollection Headers { get; } = new();

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = new List<IHttpCookie>();

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities { get; } = new List<ClaimsIdentity>();

    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
}
