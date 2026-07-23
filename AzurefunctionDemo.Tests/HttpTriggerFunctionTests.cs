using System.Text;
using AzurefunctionDemo.Tests.Fakes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AzurefunctionDemo.Tests;

public class HttpTriggerFunctionTests
{
    private readonly HttpTriggerFunction _sut = new(NullLogger<HttpTriggerFunction>.Instance);

    private static FunctionContext CreateFunctionContext() => Mock.Of<FunctionContext>();

    private static FakeHttpRequestData CreateRequest(string url, string method = "GET", string? body = null)
    {
        var context = CreateFunctionContext();
        var bodyStream = body is null ? null : new MemoryStream(Encoding.UTF8.GetBytes(body));
        return new FakeHttpRequestData(context, new Uri(url), method, bodyStream);
    }

    [Fact]
    public async Task Run_WithNameInQueryString_ReturnsPersonalizedGreeting()
    {
        var request = CreateRequest("http://localhost/api/HttpTriggerFunction?name=Rafael");

        var response = await _sut.Run(request);

        var body = await ReadBodyAsync(response);
        Assert.Equal("Hello14, Rafael. This HTTP triggered function executed successfully.", body);
    }

    [Fact]
    public async Task Run_WithoutName_ReturnsDefaultGreeting()
    {
        var request = CreateRequest("http://localhost/api/HttpTriggerFunction");

        var response = await _sut.Run(request);

        var body = await ReadBodyAsync(response);
        Assert.StartsWith("This HTTP triggered function executed successfully.", body);
    }

    [Fact]
    public async Task Run_PostWithNameInBody_ReturnsPersonalizedGreeting()
    {
        var request = CreateRequest("http://localhost/api/HttpTriggerFunction", method: "POST", body: "Rafael");

        var response = await _sut.Run(request);

        var body = await ReadBodyAsync(response);
        Assert.Equal("Hello14, Rafael. This HTTP triggered function executed successfully.", body);
    }

    [Fact]
    public async Task Run_PostWithNameInQueryString_PrefersQueryOverBody()
    {
        var request = CreateRequest("http://localhost/api/HttpTriggerFunction?name=FromQuery", method: "POST", body: "FromBody");

        var response = await _sut.Run(request);

        var body = await ReadBodyAsync(response);
        Assert.Equal("Hello14, FromQuery. This HTTP triggered function executed successfully.", body);
    }

    [Fact]
    public async Task Run_SetsContentTypeHeaderAndOkStatus()
    {
        var request = CreateRequest("http://localhost/api/HttpTriggerFunction?name=Rafael");

        var response = await _sut.Run(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, h => h.Key == "Content-Type" && h.Value.Contains("text/plain; charset=utf-8"));
    }

    private static async Task<string> ReadBodyAsync(Microsoft.Azure.Functions.Worker.Http.HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }
}
