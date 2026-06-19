// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="RefitSettings.DeserializationExceptionFactory"/> behavior.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class DeserializationExceptionFactoryTests
{
    /// <summary>Refit fixture interface returning a deserialized integer result.</summary>
    public interface IMyService
    {
        /// <summary>Gets the integer result from the test endpoint.</summary>
        /// <returns>The deserialized integer result.</returns>
        [Get("/get-with-result")]
        Task<int> GetWithResult();
    }

    /// <summary>Verifies a successful deserialization succeeds when no exception factory is configured.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoDeserializationExceptionFactory_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
        };

        const int intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo(intContent);
    }

    /// <summary>Verifies an unsuccessful deserialization throws when no exception factory is configured.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoDeserializationExceptionFactory_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.That(fixture.GetWithResult).ThrowsExactly<ApiException>();
        await Assert.That(thrownException!.Message).IsEqualTo("An error occured deserializing the response.");

        handler.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a factory returning null leaves a successful deserialization unaffected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsNull_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(null)
        };

        const int intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo(intContent);
    }

    /// <summary>Verifies a factory returning null suppresses the deserialization exception, yielding the default value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsNull_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(null)
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Verifies a factory returning an exception causes that exception to be thrown on unsuccessful deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsException_WithUnsuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new InvalidOperationException("Unsuccessful Deserialization Exception");
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(exception)
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("non-int-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.That(fixture.GetWithResult).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        handler.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a configured exception factory does not affect a successful deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsException_WithSuccessfulDeserialization()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new InvalidOperationException("Unsuccessful Deserialization Exception");
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(exception)
        };

        const int intContent = 123;
        handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent($"{intContent}"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo(intContent);
    }
}
