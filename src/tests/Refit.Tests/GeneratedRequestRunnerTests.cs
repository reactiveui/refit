// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Refit.Tests;

/// <summary>Tests for the generated request runtime helper.</summary>
public class GeneratedRequestRunnerTests
{
    /// <summary>Verifies that already-created HTTP content is reused directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentReusesHttpContent()
    {
        var content = new StringContent("body");
        var settings = CreateSettings();

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            content,
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies that stream bodies become stream content without serializer involvement.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesStreamContentForStreamBodies()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("stream-body"));
        var settings = CreateSettings();

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            stream,
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(result).IsTypeOf<StreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("stream-body");
    }

    /// <summary>Verifies that default string bodies are sent as literal string content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesLiteralStringForDefaultStringBodies()
    {
        var serializer = new RecordingContentSerializer();
        var settings = CreateSettings(serializer);

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "literal",
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("literal");
        await Assert.That(serializer.SerializeCallCount).IsEqualTo(0);
    }

    /// <summary>Verifies that serialized body modes use the configured content serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesSerializerForSerializedBodyModes()
    {
        var serializer = new RecordingContentSerializer();
        var settings = CreateSettings(serializer);

        var defaultContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            42,
            BodySerializationMethod.Default,
            streamBody: false);
        var serializedContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "serialized",
            BodySerializationMethod.Serialized,
            streamBody: false);
#pragma warning disable CS0618 // Generated request building must keep accepting legacy compiled BodySerializationMethod.Json callers.
        var legacyJsonContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "legacy-json",
            BodySerializationMethod.Json,
            streamBody: false);
#pragma warning restore CS0618

        await Assert.That(await defaultContent.ReadAsStringAsync()).IsEqualTo("serialized:42");
        await Assert.That(await serializedContent.ReadAsStringAsync()).IsEqualTo("serialized:serialized");
        await Assert.That(await legacyJsonContent.ReadAsStringAsync()).IsEqualTo("serialized:legacy-json");
        await Assert.That(serializer.SerializeCallCount).IsEqualTo(3);
    }

    /// <summary>Verifies that streaming serialized bodies are copied through push-stream content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentWrapsSerializedContentForStreamingBodies()
    {
        var settings = CreateSettings(new RecordingContentSerializer());

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "streamed",
            BodySerializationMethod.Serialized,
            streamBody: true);

        await Assert.That(result).IsTypeOf<PushStreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("serialized:streamed");
    }

    /// <summary>Verifies that unsupported body serialization modes are rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentRejectsUnsupportedBodySerializationMode()
    {
        var settings = CreateSettings();

        await Assert
            .That(
                () => GeneratedRequestRunner.CreateBodyContent(
                    settings,
                    new { Value = 42 },
                    BodySerializationMethod.UrlEncoded,
                    streamBody: false))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies that generated header assignment replaces, removes, and sanitizes values.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesRemovesAndSanitizesHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        GeneratedRequestRunner.SetHeader(request, "X-Test", "first");
        GeneratedRequestRunner.SetHeader(request, "X-Test", "second\r\nvalue");

        await Assert.That(request.Headers.GetValues("X-Test")).IsCollectionEqualTo(["secondvalue"]);

        GeneratedRequestRunner.SetHeader(request, "X-Test", null);

        await Assert.That(request.Headers.Contains("X-Test")).IsFalse();
    }

    /// <summary>Verifies that content headers create placeholder content for methods that can carry bodies.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderUsesContentHeadersForContentHeaderNames()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource");

        GeneratedRequestRunner.SetHeader(request, "Content-Language", "en-US");

        await Assert.That(request.Content).IsNotNull();
        await Assert.That(request.Content!.Headers.ContentLanguage).IsCollectionEqualTo(["en-US"]);
    }

    /// <summary>Verifies that generated header assignment removes existing content headers before adding replacements.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesExistingContentHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent("body")
        };
        request.Content.Headers.ContentLanguage.Add("en-US");

        GeneratedRequestRunner.SetHeader(request, "Content-Language", "fr-FR");

        await Assert.That(request.Content.Headers.ContentLanguage).IsCollectionEqualTo(["fr-FR"]);
    }

    /// <summary>Verifies that header collections are optional and replace earlier values by key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddHeaderCollectionIgnoresNullAndReplacesExistingHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var headers = new Dictionary<string, string>
        {
            ["X-First"] = "one",
            ["X-Second"] = "two"
        };

        GeneratedRequestRunner.SetHeader(request, "X-First", "original");
        GeneratedRequestRunner.AddHeaderCollection(request, null);
        GeneratedRequestRunner.AddHeaderCollection(request, headers);

        await Assert.That(request.Headers.GetValues("X-First")).IsCollectionEqualTo(["one"]);
        await Assert.That(request.Headers.GetValues("X-Second")).IsCollectionEqualTo(["two"]);
    }

    /// <summary>Verifies that generated request properties use typed request options where available.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRequestPropertyStoresTypedRequestOption()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        GeneratedRequestRunner.AddRequestProperty<int>(request, "number", 42);

#if NET6_0_OR_GREATER
        await Assert.That(request.Options.TryGetValue(new HttpRequestOptionsKey<int>("number"), out var value))
            .IsTrue();
        await Assert.That(value).IsEqualTo(42);
#else
        await Assert.That(request.Properties["number"]).IsEqualTo(42);
#endif
    }

    /// <summary>Verifies that configured request options and interface type metadata are applied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddConfiguredRequestOptionsAddsConfiguredValuesAndInterfaceType()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var settings = new RefitSettings(new RecordingContentSerializer())
        {
            HttpRequestMessageOptions = new()
            {
                ["configured"] = 42
            }
        };

        GeneratedRequestRunner.AddConfiguredRequestOptions(
            request,
            settings,
            typeof(GeneratedRequestRunnerTests));

#if NET6_0_OR_GREATER
        await Assert.That(
                request.Options.TryGetValue(
                    new HttpRequestOptionsKey<object>("configured"),
                    out var configuredValue))
            .IsTrue();
        await Assert.That(configuredValue).IsEqualTo(42);
        await Assert.That(
                request.Options.TryGetValue(
                    new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType),
                    out var interfaceType))
            .IsTrue();
        await Assert.That(interfaceType).IsEqualTo(typeof(GeneratedRequestRunnerTests));
#else
        await Assert.That(request.Properties["configured"]).IsEqualTo(42);
        await Assert.That(request.Properties[HttpRequestMessageOptions.InterfaceType])
            .IsEqualTo(typeof(GeneratedRequestRunnerTests));
#endif
    }

    /// <summary>Verifies that void requests apply generated auth headers and honor the exception factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendVoidAsyncAppliesAuthorizationAndThrowsFactoryException()
    {
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("accepted")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent("body")
        };
        request.Headers.Authorization = new("Bearer");
        var exception = new InvalidOperationException("factory failure");
        var settings = CreateSettings();
        settings.AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("token");
        settings.ExceptionFactory = _ => Task.FromResult<Exception?>(exception);

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
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

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

    /// <summary>Verifies that string response paths avoid the serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsStringContentWithoutSerializer()
    {
        var serializer = new RecordingContentSerializer();
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("response")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var settings = CreateSettings(serializer);

        var result = await GeneratedRequestRunner.SendAsync<string, string>(
            client,
            request,
            settings,
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo("response");
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(0);
    }

    /// <summary>Verifies that generated response calls buffer request content when requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncBuffersRequestContentWhenRequested()
    {
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("buffered")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent("request-body")
        };

        var result = await GeneratedRequestRunner.SendAsync<string, string>(
            client,
            request,
            CreateSettings(),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: true,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo("buffered");
    }

    /// <summary>Verifies that HTTP response messages can be returned without running the exception factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage(
        "Reliability",
        "CA2025:Ensure tasks using IDisposable instances complete before the instances are disposed",
        Justification = "The test awaits generated SendAsync before response disposal and verifies this exact response is returned.")]
    public async Task SendAsyncReturnsHttpResponseMessageWithoutExceptionFactory()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server-error")
        };
        var handler = new CapturingHandler((_, _) => Task.FromResult(response));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var settings = CreateSettings();
        var exceptionFactoryCalled = false;
        settings.ExceptionFactory = _ =>
        {
            exceptionFactoryCalled = true;
            return Task.FromResult<Exception?>(new InvalidOperationException("should not run"));
        };

        var result = await GeneratedRequestRunner.SendAsync<HttpResponseMessage, HttpResponseMessage>(
            client,
            request,
            settings,
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(response);
        await Assert.That(exceptionFactoryCalled).IsFalse();
    }

    /// <summary>Verifies that HTTP content can be returned directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsHttpContentDirectly()
    {
        var responseContent = new StringContent("content");
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = responseContent
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<HttpContent, HttpContent>(
            client,
            request,
            CreateSettings(),
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(responseContent);
    }

    /// <summary>Verifies that stream responses are read from response content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsResponseStream()
    {
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("streamed-response")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<Stream, Stream>(
            client,
            request,
            CreateSettings(),
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        using var reader = new StreamReader(result!);
        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo("streamed-response");
    }

    /// <summary>Verifies that serialized responses use the configured content serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncDeserializesSerializedContent()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(42)
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":42}")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(new(42));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies that empty serialized responses return the default value without deserializing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsDefaultForEmptySerializedContent()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(42)
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([])
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(0);
    }

    /// <summary>Verifies that generated response handling uses the configured exception factory for non-wrapper results.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncThrowsExceptionFactoryExceptionForNonApiResponses()
    {
        var exception = new InvalidOperationException("factory failure");
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var settings = CreateSettings();
        settings.ExceptionFactory = _ => Task.FromResult<Exception?>(exception);

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
            (_, _) => throw new HttpRequestException("network failure"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

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
            (_, _) => throw new HttpRequestException("network failure"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

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

    /// <summary>Verifies that API response results carry deserialized content on success.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsSuccessfulApiResponseWithDeserializedContent()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(123)
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":123}")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<ApiResponse<GeneratedResult>, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result!.IsSuccessful).IsTrue();
        await Assert.That(result.Content).IsEqualTo(new(123));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies that API response results carry response factory errors without deserializing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsApiResponseWithResponseException()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(123)
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
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
            DeserializeException = new FormatException("bad content")
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var settings = CreateSettings(serializer);
        settings.DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(null);

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
            DeserializeException = new FormatException("bad content")
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        var replacement = new InvalidOperationException("replacement");
        var settings = CreateSettings(serializer);
        settings.DeserializationExceptionFactory = (_, _) => Task.FromResult<Exception?>(replacement);

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
            DeserializeException = new FormatException("bad content")
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("bad")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

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
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("cancelled")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
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

    /// <summary>Verifies that best-effort response buffering failures do not prevent serializer deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncIgnoresResponseBufferingFailuresBeforeDeserializing()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(321)
        };
        var handler = new CapturingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ThrowingLoadContent()
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

        var result = await GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(new(321));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies that generated response calls require a base address for relative requests.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncRequiresBaseAddress()
    {
        using var client = new HttpClient(new CapturingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");

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

    /// <summary>Creates settings backed by the test serializer.</summary>
    /// <param name="serializer">The serializer to assign, or null for a recording serializer.</param>
    /// <returns>The configured settings.</returns>
    private static RefitSettings CreateSettings(IHttpContentSerializer? serializer = null) =>
        new(serializer ?? new RecordingContentSerializer());

    /// <summary>Creates an HTTP client that can send generated relative request URIs.</summary>
    /// <param name="handler">The handler that will receive generated requests.</param>
    /// <returns>The configured client.</returns>
    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler)
        {
            BaseAddress = new("https://api.example")
        };

    /// <summary>Captures request details sent by generated response helpers.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The send delegate used by this handler.</summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        /// <summary>Initializes a new instance of the <see cref="CapturingHandler"/> class.</summary>
        public CapturingHandler()
            : this(
                (_, _) => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(string.Empty)
                    }))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CapturingHandler"/> class.</summary>
        /// <param name="sendAsync">The send delegate to invoke.</param>
        public CapturingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
            _sendAsync = sendAsync;

        /// <summary>Gets the authorization parameter captured from the sent request.</summary>
        public string? AuthorizationParameter { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return SendAndAttachRequestAsync(request, cancellationToken);
        }

        /// <summary>Runs the send delegate and mirrors HttpClientHandler by attaching the request to the response.</summary>
        /// <param name="request">The sent request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response with a request message.</returns>
        private async Task<HttpResponseMessage> SendAndAttachRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await _sendAsync(request, cancellationToken).ConfigureAwait(false);
            response.RequestMessage ??= request;
            return response;
        }
    }

    /// <summary>Records serializer usage and returns configured test values.</summary>
    private sealed class RecordingContentSerializer : IHttpContentSerializer
    {
        /// <summary>Gets the number of serialization calls.</summary>
        public int SerializeCallCount { get; private set; }

        /// <summary>Gets the number of deserialization calls.</summary>
        public int DeserializeCallCount { get; private set; }

        /// <summary>Gets the value returned from deserialization.</summary>
        public object? DeserializedValue { get; init; }

        /// <summary>Gets the exception thrown from deserialization.</summary>
        public Exception? DeserializeException { get; init; }

        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item)
        {
            SerializeCallCount++;
            return new StringContent($"serialized:{item}");
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "The method implements Refit's published serializer interface.")]
        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            DeserializeCallCount++;
            if (DeserializeException is not null)
            {
                throw DeserializeException;
            }

            return Task.FromResult((T?)DeserializedValue);
        }

        /// <inheritdoc />
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
            propertyInfo.Name;
    }

    /// <summary>Content that fails when buffering attempts to serialize it into memory.</summary>
    private sealed class ThrowingLoadContent : HttpContent
    {
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            throw new InvalidOperationException("buffering failed");

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 1;
            return true;
        }
    }

    /// <summary>Simple deserialized response model for generated runtime tests.</summary>
    /// <param name="Value">The model value.</param>
    private sealed record GeneratedResult(int Value);
}
