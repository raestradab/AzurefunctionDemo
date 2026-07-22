using AzurefunctionDemo.Services;
using Xunit;

namespace AzurefunctionDemo.Tests;

public class GreetingServiceTests
{
    private readonly GreetingService _sut = new();

    [Fact]
    public void BuildGreeting_WithNullName_ReturnsDefaultMessage()
    {
        var result = _sut.BuildGreeting(null);

        Assert.Equal(
            "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
            result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGreeting_WithEmptyOrWhitespaceName_ReturnsDefaultMessage(string name)
    {
        var result = _sut.BuildGreeting(name);

        Assert.StartsWith("This HTTP triggered function executed successfully.", result);
    }

    [Fact]
    public void BuildGreeting_WithName_ReturnsPersonalizedMessage()
    {
        var result = _sut.BuildGreeting("Rafael");

        Assert.Equal("Hello2, Rafael. This HTTP triggered function executed successfully.", result);
    }

    [Fact]
    public void BuildGreeting_WithNamePaddedWithWhitespace_PreservesRawNameInMessage()
    {
        var result = _sut.BuildGreeting(" Rafael ");

        Assert.Equal("Hello2,  Rafael . This HTTP triggered function executed successfully.", result);
    }
}
