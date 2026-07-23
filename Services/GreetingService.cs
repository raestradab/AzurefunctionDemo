namespace AzurefunctionDemo.Services;

public static class GreetingService
{
    public static string BuildGreeting(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.4"
            : $"Hello13, {name}. This HTTP triggered function executed successfully.";
    }
}
