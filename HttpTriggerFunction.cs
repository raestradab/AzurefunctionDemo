using AzurefunctionDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzurefunctionDemo;

public class HttpTriggerFunction
{
    private readonly ILogger<HttpTriggerFunction> _logger;

    public HttpTriggerFunction(ILogger<HttpTriggerFunction> logger)
    {
        _logger = logger;
    }

    [Function("HttpTriggerFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var name = req.Query["name"];

        if (req.Method == HttpMethod.Post.Method && string.IsNullOrEmpty(name))
        {
            using var reader = new StreamReader(req.Body);
            name = await reader.ReadToEndAsync();
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        var responseMessage = GreetingService.BuildGreeting(name);

        await response.WriteStringAsync(responseMessage);

        return response;
    }
}
