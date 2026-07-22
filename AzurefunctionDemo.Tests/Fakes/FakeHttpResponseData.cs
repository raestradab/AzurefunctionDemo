using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzurefunctionDemo.Tests.Fakes;

public class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    {
    }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; } = new();

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies { get; } = null!;
}
