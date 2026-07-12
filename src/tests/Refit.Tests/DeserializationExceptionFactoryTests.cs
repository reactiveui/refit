// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Http;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="RefitSettings.DeserializationExceptionFactory"/> behavior.</summary>
public class DeserializationExceptionFactoryTests
{
    /// <summary>The base address for the test service.</summary>
    private const string BaseUrl = "http://api";

    /// <summary>The stub endpoint URL that returns a deserialized result.</summary>
    private const string GetWithResultUrl = "http://api/get-with-result";

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
        const int intContent = 123;
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent($"{intContent}") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl);

        var result = await fixture.GetWithResult();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo(intContent);
    }

    /// <summary>Verifies an unsuccessful deserialization throws when no exception factory is configured.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoDeserializationExceptionFactory_WithUnsuccessfulDeserialization()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent("non-int-result") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl);

        var thrownException = await Assert.That(fixture.GetWithResult).ThrowsExactly<ApiException>();
        await Assert.That(thrownException!.Message).IsEqualTo("An error occured deserializing the response.");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a factory returning null leaves a successful deserialization unaffected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsNull_WithSuccessfulDeserialization()
    {
        const int intContent = 123;
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent($"{intContent}") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl, new RefitSettings
        {
            DeserializationExceptionFactory = static (_, _) => new ValueTask<Exception?>((Exception?)null)
        });

        var result = await fixture.GetWithResult();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo(intContent);
    }

    /// <summary>Verifies a factory returning null suppresses the deserialization exception, yielding the default value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsNull_WithUnsuccessfulDeserialization()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent("non-int-result") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl, new RefitSettings
        {
            DeserializationExceptionFactory = static (_, _) => new ValueTask<Exception?>((Exception?)null)
        });

        var result = await fixture.GetWithResult();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo(default);
    }

    /// <summary>Verifies a factory returning an exception causes that exception to be thrown on unsuccessful deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsException_WithUnsuccessfulDeserialization()
    {
        var exception = new InvalidOperationException("Unsuccessful Deserialization Exception");
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent("non-int-result") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl, new RefitSettings
        {
            DeserializationExceptionFactory = (_, _) => new ValueTask<Exception?>(exception)
        });

        var thrownException = await Assert.That(fixture.GetWithResult).ThrowsExactly<InvalidOperationException>();
        await Assert.That(thrownException).IsEqualTo(exception);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a configured exception factory does not affect a successful deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvideFactoryWhichReturnsException_WithSuccessfulDeserialization()
    {
        var exception = new InvalidOperationException("Unsuccessful Deserialization Exception");
        const int intContent = 123;
        var handler = new StubHttp
        {
            {
                Route.Get(GetWithResultUrl),
                new StubResponse { Status = HttpStatusCode.OK, Content = new StringContent($"{intContent}") }
            },
        };
        var fixture = handler.CreateClient<IMyService>(BaseUrl, new RefitSettings
        {
            DeserializationExceptionFactory = (_, _) => new ValueTask<Exception?>(exception)
        });

        var result = await fixture.GetWithResult();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo(intContent);
    }
}
