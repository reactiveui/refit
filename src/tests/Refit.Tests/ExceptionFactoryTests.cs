// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="RefitSettings.ExceptionFactory"/> behavior.</summary>
public class ExceptionFactoryTests
{
    /// <summary>The base address used for the fixture service.</summary>
    private const string BaseAddress = "http://api";

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
        var handler = new StubHttp
        {
            {
                Route.Get("http://api/get-with-result"),
                new StubResponse { Status = HttpStatusCode.NotFound, Content = new StringContent("error-result") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseAddress, new RefitSettings
        {
            ExceptionFactory = static _ => Task.FromResult<Exception?>(null)
        });

        var result = await fixture.GetWithResult();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("error-result");
    }

    /// <summary>Verifies that a void call succeeds when the factory always returns null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsNull_WithoutResult()
    {
        var handler = new StubHttp
        {
            {
                Route.Put("http://api/put-without-result"),
                Reply.Status(HttpStatusCode.NotFound)
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseAddress, new RefitSettings
        {
            ExceptionFactory = static _ => Task.FromResult<Exception?>(null)
        });

        await fixture.PutWithoutResult();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies that the factory-returned exception is thrown from a result-returning call.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsException_WithResult()
    {
        var exception = new InvalidOperationException("I like to fail");
        var handler = new StubHttp
        {
            {
                Route.Get("http://api/get-with-result"),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent("success-result") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseAddress, new RefitSettings
        {
            ExceptionFactory = _ => Task.FromResult<Exception?>(exception)
        });

        var thrownException = await Assert.That(() => (Task)fixture.GetWithResult()).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies that the factory-returned exception is thrown from a void call.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichAlwaysReturnsException_WithoutResult()
    {
        var exception = new InvalidOperationException("I like to fail");
        var handler = new StubHttp
        {
            {
                Route.Put("http://api/put-without-result"),
                Reply.Status(HttpStatusCode.OK)
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseAddress, new RefitSettings
        {
            ExceptionFactory = _ => Task.FromResult<Exception?>(exception)
        });

        var thrownException = await Assert.That(fixture.PutWithoutResult).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        await handler.VerifyAllCalledAsync();
    }
}
