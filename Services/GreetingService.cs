namespace AzurefunctionDemo.Services;

public class GreetingService
{
    public static string BuildGreeting(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            : $"Hello4, {name}. This HTTP triggered function executed successfully.";
    }
}
