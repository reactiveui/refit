// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="RefitSettings.ExceptionFactory"/> behavior.</summary>
public class ExceptionFactoryTests
{
    /// <summary>Refit fixture interface used to exercise the exception factory.</summary>
    public interface IMyService
    {
        /// <summary>Sends a GET request that returns a string result.</summary>
        /// <returns>The string response body.</returns>
        [Get("/get-with-result")]
        Task<string> GetWithResult();

        /// <summary>Sends a PUT request that returns no result.</summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Put("/put-without-result")]
        Task PutWithoutResult();
    }

    /// <summary>Verifies that a result-returning call succeeds when the factory always returns null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsNull_WithResult()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ExceptionFactory = _ => Task.FromResult<Exception?>(null)
        };

        _ = handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.NotFound, new StringContent("error-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var result = await fixture.GetWithResult();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("error-result");
    }

    /// <summary>Verifies that a void call succeeds when the factory always returns null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsNull_WithoutResult()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ExceptionFactory = _ => Task.FromResult<Exception?>(null)
        };

        _ = handler
            .Expect(HttpMethod.Put, "http://api/put-without-result")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<IMyService>("http://api", settings);

        await fixture.PutWithoutResult();

        handler.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies that the factory-returned exception is thrown from a result-returning call.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsException_WithResult()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new InvalidOperationException("I like to fail");
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ExceptionFactory = _ => Task.FromResult<Exception?>(exception)
        };

        _ = handler
            .Expect(HttpMethod.Get, "http://api/get-with-result")
            .Respond(HttpStatusCode.OK, new StringContent("success-result"));

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.That(() => (Task)fixture.GetWithResult()).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        handler.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies that the factory-returned exception is thrown from a void call.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsException_WithoutResult()
    {
        var handler = new MockHttpMessageHandler();
        var exception = new InvalidOperationException("I like to fail");
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ExceptionFactory = _ => Task.FromResult<Exception?>(exception)
        };

        _ = handler.Expect(HttpMethod.Put, "http://api/put-without-result").Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IMyService>("http://api", settings);

        var thrownException = await Assert.That(fixture.PutWithoutResult).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        handler.VerifyNoOutstandingExpectation();
    }
}
