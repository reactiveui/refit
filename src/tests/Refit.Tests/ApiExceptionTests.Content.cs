// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
namespace Refit.Tests;

/// <summary>Content deserialization, truncation, and error-body reading tests for <see cref="ApiExceptionTests"/>.</summary>
public sealed partial class ApiExceptionTests
{
    /// <summary>Verifies the synchronous GetContentAs deserializes the buffered error body (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Concurrency", "PSH1313:Call the async overload from an async method", Justification = "This test intentionally covers the synchronous content helper.")]
    public async Task SyncGetContentAsDeserializesContent()
    {
        using var response = CreateErrorResponse(ValueJson);
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var model = exception.GetContentAs<ResponseModel>();

        await Assert.That(model!.Value).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies the synchronous GetContentAs returns default when there is no body (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Concurrency", "PSH1313:Call the async overload from an async method", Justification = "This test intentionally covers the synchronous content helper.")]
    public async Task SyncGetContentAsReturnsDefaultWhenNoContent()
    {
        using var response = CreateErrorResponse(" ");
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.GetContentAs<ResponseModel>()).IsNull();
    }

    /// <summary>Verifies TryGetContentAs returns the value inside an exception filter (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncTryGetContentAsReturnsTrueAndValue()
    {
        using var response = CreateErrorResponse(ValueJson);
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var ok = exception.TryGetContentAs<ResponseModel>(out var model);

        await Assert.That(ok).IsTrue();
        await Assert.That(model!.Value).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies TryGetContentAs returns false for missing or malformed content (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncTryGetContentAsReturnsFalseForMissingOrInvalidContent()
    {
        using var empty = CreateErrorResponse(" ");
        using var invalid = CreateErrorResponse("not json");
        var emptyException = await ApiException.Create(empty.RequestMessage!, HttpMethod.Get, empty, new());
        var invalidException = await ApiException.Create(invalid.RequestMessage!, HttpMethod.Get, invalid, new());

        await Assert.That(emptyException.TryGetContentAs<ResponseModel>(out var missing)).IsFalse();
        await Assert.That(missing).IsNull();
        await Assert.That(invalidException.TryGetContentAs<ResponseModel>(out var broken)).IsFalse();
        await Assert.That(broken).IsNull();
    }

    /// <summary>Verifies the synchronous helpers behave when the serializer lacks sync support (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncGetContentAsWithoutSyncSerializerSupport()
    {
        using var response = CreateErrorResponse(ValueJson);
        var settings = new RefitSettings(new AsyncOnlySerializer());
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.TryGetContentAs<ResponseModel>(out _)).IsFalse();
        await Assert.That(exception.GetContentAs<ResponseModel>)
            .ThrowsExactly<NotSupportedException>();
    }

    /// <summary>Verifies the error body is truncated to the configured maximum length.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthTruncatesErrorBody()
    {
        using var response = CreateErrorResponse(new string('a', LongBodyLength));
        var settings = new RefitSettings { MaxExceptionContentLength = MaxContentLength };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.Content!.Length).IsEqualTo(MaxContentLength);
    }

    /// <summary>Verifies an unset maximum length leaves the error body intact.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthUnsetReadsFullBody()
    {
        using var response = CreateErrorResponse(new string('a', LongBodyLength));

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.Content!.Length).IsEqualTo(LongBodyLength);
    }

    /// <summary>Verifies a zero maximum length yields an empty error body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthZeroReturnsEmptyContent()
    {
        const int bodyLength = 100;
        using var response = CreateErrorResponse(new string('a', bodyLength));
        var settings = new RefitSettings { MaxExceptionContentLength = 0 };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.Content).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies a maximum length larger than the body reads the entire body and stops at end of stream.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthLargerThanBodyReadsEntireBody()
    {
        const int bodyLength = 10;
        using var response = CreateErrorResponse(new string('a', bodyLength));
        var settings = new RefitSettings { MaxExceptionContentLength = LongBodyLength };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.Content!.Length).IsEqualTo(bodyLength);
    }

    /// <summary>Verifies the error body is still read when the response carries no content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateReadsContentWhenContentTypeMissing()
    {
        using var response = CreateErrorResponse("{\"Value\":1}");
        response.Content!.Headers.ContentType = null;

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception).IsTypeOf<ApiException>();
        await Assert.That(exception.Content).IsEqualTo("{\"Value\":1}");
    }

    /// <summary>Verifies the error body is read when the content read genuinely suspends asynchronously.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateReadsContentThatCompletesAsynchronously()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new(HttpMethod.Get, ExampleUri),
            Content = new AsyncReadContent("{\"Value\":7}")
        };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.Content).IsEqualTo("{\"Value\":7}");
    }

    /// <summary>Content whose read genuinely suspends, forcing the asynchronous completion path when the exception reads the body.</summary>
    private sealed class AsyncReadContent : HttpContent
    {
        /// <summary>The UTF-8 payload bytes.</summary>
        private readonly byte[] _payload;

        /// <summary>Initializes a new instance of the <see cref="AsyncReadContent"/> class.</summary>
        /// <param name="payload">The string payload.</param>
        public AsyncReadContent(string payload) => _payload = System.Text.Encoding.UTF8.GetBytes(payload);

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await Task.Yield();
            await stream.WriteAsync(_payload.AsMemory()).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _payload.Length;
            return true;
        }
    }

    /// <summary>A serializer that only supports asynchronous deserialization (no sync capability).</summary>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Mirrors the explicit type parameters of the wrapped IHttpContentSerializer contract.")]
    private sealed class AsyncOnlySerializer : IHttpContentSerializer
    {
        /// <summary>The wrapped serializer that does the real work.</summary>
        private readonly SystemTextJsonContentSerializer _inner = new();

        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) => _inner.ToHttpContent(item);

        /// <inheritdoc />
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            _inner.FromHttpContentAsync<T>(content, cancellationToken);

        /// <inheritdoc />
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) =>
            _inner.GetFieldNameForProperty(propertyInfo);
    }
}
