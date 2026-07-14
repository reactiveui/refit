// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Tests;

/// <summary>Send-path error, exception, cancellation, and base-address handling for the generated request runtime helper.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>Verifies that void requests apply generated auth headers and honor the exception factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendVoidAsyncAppliesAuthorizationAndThrowsFactoryException()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("accepted")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, RelativeResourcePath)
        {
            Content = new StringContent("body")
        };
        request.Headers.Authorization = new("Bearer");
        var exception = new InvalidOperationException("factory failure");
        var settings = CreateSettings();
        settings.AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>("token");
        settings.ExceptionFactory = _ => new ValueTask<Exception?>(exception);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendVoidAsync(
                    client,
                    request,
                    settings,
                    bufferBody: true,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(thrown).IsSameReferenceAs(exception);
        await Assert.That(handler.AuthorizationParameter).IsEqualTo("token");
    }

    /// <summary>Verifies that void requests require a base address when using generated relative URIs.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendVoidAsyncRequiresBaseAddress()
    {
        using var client = new HttpClient(new CapturingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var exception = await Assert
            .That(
                () => GeneratedRequestRunner.SendVoidAsync(
                    client,
                    request,
                    CreateSettings(),
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception!.Message).IsEqualTo("BaseAddress must be set on the HttpClient instance");
    }

    /// <summary>Verifies that generated response handling uses the configured exception factory for non-wrapper results.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncThrowsExceptionFactoryExceptionForNonApiResponses()
    {
        var exception = new InvalidOperationException("factory failure");
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = CreateSettings();
        settings.ExceptionFactory = _ => new ValueTask<Exception?>(exception);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    settings,
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(thrown).IsSameReferenceAs(exception);
    }

    /// <summary>Verifies that generated response handling wraps transport failures for API response results.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsApiResponseForTransportFailure()
    {
        var handler = new CapturingHandler(
            static (_, _) => throw new HttpRequestException("network failure"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var result = await GeneratedRequestRunner.SendAsync<ApiResponse<string>, string>(
            client,
            request,
            CreateSettings(),
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result!.IsReceived).IsFalse();
        await Assert.That(result.HasRequestError(out var error)).IsTrue();
        await Assert.That(error!.InnerException).IsTypeOf<HttpRequestException>();
    }

    /// <summary>Verifies that generated response handling throws transport failures for non-wrapper results.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncThrowsApiRequestExceptionForTransportFailure()
    {
        var handler = new CapturingHandler(
            static (_, _) => throw new HttpRequestException("network failure"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var exception = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    CreateSettings(),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<ApiRequestException>();

        await Assert.That(exception!.InnerException).IsTypeOf<HttpRequestException>();
    }

    /// <summary>Verifies that API response results carry response factory errors without deserializing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsApiResponseWithResponseException()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(SuccessResultValue)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = CreateSettings(serializer);

        var result = await GeneratedRequestRunner.SendAsync<ApiResponse<GeneratedResult>, GeneratedResult>(
            client,
            request,
            settings,
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result!.IsSuccessStatusCode).IsFalse();
        await Assert.That(result.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(0);
    }

    /// <summary>Verifies that API response deserialization exceptions can be suppressed by the configured factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncApiResponseSuppressesDeserializationExceptionWhenFactoryReturnsNull()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializeException = new FormatException(BadContentMessage)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = CreateSettings(serializer);
        settings.DeserializationExceptionFactory = static (_, _) => new ValueTask<Exception?>((Exception?)null);

        var result = await GeneratedRequestRunner.SendAsync<ApiResponse<GeneratedResult>, GeneratedResult>(
            client,
            request,
            settings,
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result!.IsSuccessful).IsTrue();
        await Assert.That(result.Content).IsNull();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>Verifies that non-wrapper deserialization exceptions can be replaced by the configured factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncThrowsConfiguredDeserializationExceptionForNonApiResponses()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializeException = new FormatException(BadContentMessage)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var replacement = new InvalidOperationException("replacement");
        var settings = CreateSettings(serializer);
        settings.DeserializationExceptionFactory = (_, _) => new ValueTask<Exception?>(replacement);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
                    client,
                    request,
                    settings,
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(thrown).IsSameReferenceAs(replacement);
    }

    /// <summary>Verifies that non-wrapper deserialization exceptions use Refit's default API exception wrapper.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncThrowsDefaultApiExceptionForNonApiResponseDeserializationFailures()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializeException = new FormatException(BadContentMessage)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
                    client,
                    request,
                    CreateSettings(serializer),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<ApiException>();

        await Assert.That(thrown!.Message).IsEqualTo("An error occured deserializing the response.");
        await Assert.That(thrown.InnerException).IsTypeOf<FormatException>();
    }

    /// <summary>Verifies cancellation-triggered deserialization exceptions are rethrown directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncRethrowsCancellationRequestedDuringDeserialization()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializeException = new OperationCanceledException("cancelled")
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("cancelled")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        using var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();

        await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
                    client,
                    request,
                    CreateSettings(serializer),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    tokenSource.Token))
            .ThrowsExactly<OperationCanceledException>();
    }

    /// <summary>Verifies caller-requested cancellation during send is rethrown instead of being wrapped in <see cref="ApiRequestException"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncRethrowsCancellationRequestedDuringSend()
    {
        using var tokenSource = new CancellationTokenSource();
        var handler = new CapturingHandler(
            async (_, _) =>
            {
                await tokenSource.CancelAsync();
                throw new OperationCanceledException(tokenSource.Token);
            });
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    CreateSettings(),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    tokenSource.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>Verifies that generated response calls require a base address for relative requests.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncRequiresBaseAddress()
    {
        using var client = new HttpClient(new CapturingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var exception = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    CreateSettings(),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception!.Message).IsEqualTo("BaseAddress must be set on the HttpClient instance");
    }
}
