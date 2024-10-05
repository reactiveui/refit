using System.Net;
using System.Net.Http;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests;

public class DeserializationExceptionFactoryTests
{
    public interface IMyService
    {
        [Get("/get-with-result")]
        Task<int> GetWithResult();
    }

    [Fact]
    public async Task NoDeserializationExceptionFactory_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
        };

        var intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        Assert.Equal(intContent, result);
    }

    [Fact]
    public async Task NoDeserializationExceptionFactory_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.ThrowsAsync<ApiException>(() => fixture.GetWithResult());
        Assert.Equal("An error occured deserializing the response.", thrownException.Message);

        handler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ProvideFactoryWhichReturnsNull_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception>(null)
        };

        var intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        Assert.Equal(intContent, result);
    }

    [Fact]
    public async Task ProvideFactoryWhichReturnsNull_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception>(null)
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        Assert.Equal(default, result);
    }

    [Fact]
    public async Task ProvideFactoryWhichReturnsException_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new Exception("Unsuccessful Deserialization Exception");
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception>(exception)
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.ThrowsAsync<Exception>(() => fixture.GetWithResult());
        Assert.Equal(exception, thrownException);

        handler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ProvideFactoryWhichReturnsException_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new Exception("Unsuccessful Deserialization Exception");
        var settings = new RefitSettings()
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception>(exception)
        };

        var intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        Assert.Equal(intContent, result);
    }
}
