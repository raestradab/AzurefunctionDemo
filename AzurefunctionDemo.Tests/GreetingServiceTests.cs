using AzurefunctionDemo.Services;
using Xunit;

namespace AzurefunctionDemo.Tests;

public class GreetingServiceTests
{
    [Fact]
    public void BuildGreeting_WithNullName_ReturnsDefaultMessage()
    {
        var result = GreetingService.BuildGreeting(null);

        Assert.Equal(
            "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
            result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGreeting_WithEmptyOrWhitespaceName_ReturnsDefaultMessage(string name)
    {
        var result = GreetingService.BuildGreeting(name);

        Assert.StartsWith("This HTTP triggered function executed successfully.", result);
    }

    [Fact]
    public void BuildGreeting_WithName_ReturnsPersonalizedMessage()
    {
        var result = GreetingService.BuildGreeting("Rafael");

        Assert.Equal("Hello4, Rafael. This HTTP triggered function executed successfully.", result);
    }

    [Fact]
    public void BuildGreeting_WithNamePaddedWithWhitespace_PreservesRawNameInMessage()
    {
        var result = GreetingService.BuildGreeting(" Rafael ");

        Assert.Equal("Hello4,  Rafael . This HTTP triggered function executed successfully.", result);
    }
}
