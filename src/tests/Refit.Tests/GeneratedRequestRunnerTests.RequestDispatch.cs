// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
namespace Refit.Tests;

/// <summary>Request header, request-option and relative-URI assembly, and successful request dispatch with response materialization, for the generated request runtime helper.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>The response body returned by the observable dispatch fixtures.</summary>
    private const string ObservedResponseContent = "observed";

    /// <summary>Verifies that generated header assignment replaces, removes, and sanitizes values.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesRemovesAndSanitizesHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        GeneratedRequestRunner.SetHeader(request, TestHeaderName, "first", validateHeaders: false);
        GeneratedRequestRunner.SetHeader(request, TestHeaderName, "second\r\nvalue", validateHeaders: false);

        await Assert.That(request.Headers.GetValues(TestHeaderName)).IsCollectionEqualTo(["secondvalue"]);

        GeneratedRequestRunner.SetHeader(request, TestHeaderName, null, validateHeaders: false);

        await Assert.That(request.Headers.Contains(TestHeaderName)).IsFalse();
    }

    /// <summary>Verifies that content headers create placeholder content for methods that can carry bodies.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderUsesContentHeadersForContentHeaderNames()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RelativeResourcePath);

        GeneratedRequestRunner.SetHeader(request, "Content-Language", ContentLanguageValue, validateHeaders: false);

        await Assert.That(request.Content).IsNotNull();
        await Assert.That(request.Content!.Headers.ContentLanguage).IsCollectionEqualTo([ContentLanguageValue]);
    }

    /// <summary>Verifies that generated header assignment removes existing content headers before adding replacements.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesExistingContentHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RelativeResourcePath)
        {
            Content = new StringContent("body")
        };
        request.Content.Headers.ContentLanguage.Add(ContentLanguageValue);

        GeneratedRequestRunner.SetHeader(request, "Content-Language", "fr-FR", validateHeaders: false);

        await Assert.That(request.Content.Headers.ContentLanguage).IsCollectionEqualTo(["fr-FR"]);
    }

    /// <summary>Verifies that generated header assignment sends a malformed value verbatim when validation is disabled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithoutValidationSendsMalformedValueVerbatim() =>
        HeaderValidationScenarios.SendsMalformedValueVerbatimWithoutValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that generated header assignment throws for a malformed value when validation is enabled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithValidationThrowsForMalformedValue() =>
        HeaderValidationScenarios.ThrowsForMalformedValueWithValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that generated header assignment stores a well-formed value when validation is enabled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithValidationAddsWellFormedRequestHeader() =>
        HeaderValidationScenarios.AddsWellFormedRequestHeaderWithValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that a validated content header falls back to the content collection when misused on the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithValidationFallsBackToContentHeader() =>
        HeaderValidationScenarios.FallsBackToContentHeaderWithValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that a validated content header is dropped when there is no content collection to receive it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithValidationDropsContentHeaderWithoutContent() =>
        HeaderValidationScenarios.DropsContentHeaderWithoutContentWithValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that without validation a content header is dropped when there is no content collection to receive it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task SetHeaderWithoutValidationDropsContentHeaderWithoutContent() =>
        HeaderValidationScenarios.DropsContentHeaderWithoutContentWithoutValidation(GeneratedRequestRunner.SetHeader, RelativeResourcePath);

    /// <summary>Verifies that header collections are optional and replace earlier values by key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddHeaderCollectionIgnoresNullAndReplacesExistingHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var headers = new Dictionary<string, string>
        {
            [FirstHeaderName] = "one",
            ["X-Second"] = "two"
        };

        GeneratedRequestRunner.SetHeader(request, FirstHeaderName, "original", validateHeaders: false);
        GeneratedRequestRunner.AddHeaderCollection(request, null, validateHeaders: false);
        GeneratedRequestRunner.AddHeaderCollection(request, headers, validateHeaders: false);

        await Assert.That(request.Headers.GetValues(FirstHeaderName)).IsCollectionEqualTo(["one"]);
        await Assert.That(request.Headers.GetValues("X-Second")).IsCollectionEqualTo(["two"]);
    }

    /// <summary>Verifies that generated request properties use typed request options where available.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRequestPropertyStoresTypedRequestOption()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        GeneratedRequestRunner.AddRequestProperty<int>(request, "number", RequestPropertyValue);

#if NET6_0_OR_GREATER
        await Assert.That(request.Options.TryGetValue(new HttpRequestOptionsKey<int>("number"), out var value))
            .IsTrue();
        await Assert.That(value).IsEqualTo(RequestPropertyValue);
#else
        await Assert.That(request.Properties["number"]).IsEqualTo(RequestPropertyValue);
#endif
    }

    /// <summary>Verifies that configured request options and interface type metadata are applied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddConfiguredRequestOptionsAddsConfiguredValuesAndInterfaceType()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = new RefitSettings(new RecordingContentSerializer())
        {
            HttpRequestMessageOptions = new()
            {
                ["configured"] = ConfiguredOptionValue
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
        await Assert.That(configuredValue).IsEqualTo(ConfiguredOptionValue);
        await Assert.That(
                request.Options.TryGetValue(
                    new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType),
                    out var interfaceType))
            .IsTrue();
        await Assert.That(interfaceType).IsEqualTo(typeof(GeneratedRequestRunnerTests));
#else
        await Assert.That(request.Properties["configured"]).IsEqualTo(ConfiguredOptionValue);
        await Assert.That(request.Properties[HttpRequestMessageOptions.InterfaceType])
            .IsEqualTo(typeof(GeneratedRequestRunnerTests));
#endif
    }

    /// <summary>Verifies that string response paths avoid the serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsStringContentWithoutSerializer()
    {
        var serializer = new RecordingContentSerializer();
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("response")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
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
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("buffered")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, RelativeResourcePath)
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
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = CreateSettings();
        var exceptionFactoryCalled = false;
        settings.ExceptionFactory = _ =>
        {
            exceptionFactoryCalled = true;
            return new ValueTask<Exception?>(new InvalidOperationException("should not run"));
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
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

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
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("streamed-response")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

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
            DeserializedValue = new GeneratedResult(DeserializedResultValue)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":42}")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var result = await GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(new(DeserializedResultValue));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies that empty serialized responses return the default value without deserializing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsDefaultForEmptySerializedContent()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(DeserializedResultValue)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([])
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

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

    /// <summary>Verifies that API response results carry deserialized content on success.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncReturnsSuccessfulApiResponseWithDeserializedContent()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(SuccessResultValue)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\":123}")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var result = await GeneratedRequestRunner.SendAsync<ApiResponse<GeneratedResult>, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result!.IsSuccessful).IsTrue();
        await Assert.That(result.Content).IsEqualTo(new(SuccessResultValue));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies that best-effort response buffering failures do not prevent serializer deserialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncIgnoresResponseBufferingFailuresBeforeDeserializing()
    {
        var serializer = new RecordingContentSerializer
        {
            DeserializedValue = new GeneratedResult(BufferedResultValue)
        };
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ThrowingLoadContent()
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var result = await GeneratedRequestRunner.SendAsync<GeneratedResult, GeneratedResult>(
            client,
            request,
            CreateSettings(serializer),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(new(BufferedResultValue));
        await Assert.That(serializer.DeserializeCallCount).IsEqualTo(1);
    }

    /// <summary>Verifies BuildRelativeUri throws when the legacy mode has no base address.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriThrowsWhenBaseAddressMissing()
    {
        using var client = new HttpClient();

        await Assert
            .That(() => GeneratedRequestRunner.BuildRelativeUri(client, "/x", UrlResolutionMode.RefitLegacy))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies BuildRelativeUri prepends the base path under legacy resolution.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriPrependsBasePathInLegacyMode()
    {
        using var client = new HttpClient { BaseAddress = new("http://foo/api/") };

        var uri = GeneratedRequestRunner.BuildRelativeUri(client, "/x", UrlResolutionMode.RefitLegacy);

        await Assert.That(uri.OriginalString).IsEqualTo("/api/x");
    }

    /// <summary>Verifies BuildRelativeUri emits the relative path verbatim under RFC 3986 resolution.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriEmitsRelativePathInRfcMode()
    {
        using var client = new HttpClient();

        var uri = GeneratedRequestRunner.BuildRelativeUri(client, "x", UrlResolutionMode.Rfc3986);

        await Assert.That(uri.OriginalString).IsEqualTo("x");
    }

    /// <summary>Verifies the query-format overload also emits the relative path verbatim under RFC 3986 resolution,
    /// where the escaping format is irrelevant.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriWithQueryFormatEmitsRelativePathInRfcMode()
    {
        using var client = new HttpClient();

        var uri = GeneratedRequestRunner.BuildRelativeUri(client, "x", UrlResolutionMode.Rfc3986, UriFormat.UriEscaped);

        await Assert.That(uri.OriginalString).IsEqualTo("x");
    }

    /// <summary>Verifies the query-format overload prepends the trimmed base path under legacy resolution.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriWithQueryFormatPrependsBasePathInLegacyMode()
    {
        using var client = new HttpClient { BaseAddress = new("http://foo/api/") };

        var uri = GeneratedRequestRunner.BuildRelativeUri(client, "/x", UrlResolutionMode.RefitLegacy, UriFormat.UriEscaped);

        await Assert.That(uri.OriginalString).IsEqualTo("/api/x");
    }

    /// <summary>Verifies the query-format overload uses an empty base path for a root base address under legacy resolution.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriWithQueryFormatUsesEmptyBasePathForRootBaseAddress()
    {
        using var client = new HttpClient { BaseAddress = new("http://foo/") };

        var uri = GeneratedRequestRunner.BuildRelativeUri(client, "/x", UrlResolutionMode.RefitLegacy, UriFormat.UriEscaped);

        await Assert.That(uri.OriginalString).IsEqualTo("/x");
    }

    /// <summary>Verifies the query-format overload throws when the legacy mode has no base address.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRelativeUriWithQueryFormatThrowsWhenBaseAddressMissing()
    {
        using var client = new HttpClient();

        await Assert
            .That(() => GeneratedRequestRunner.BuildRelativeUri(client, "/x", UrlResolutionMode.RefitLegacy, UriFormat.UriEscaped))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a cold observable links the method's cancellation token with the per-subscription token when
    /// both can cancel, and still yields the response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendObservableLinksMethodAndSubscriptionCancellationTokens()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ObservedResponseContent)
                }));
        using var client = CreateClient(handler);
        using var methodTokenSource = new CancellationTokenSource();

        var observable = GeneratedRequestRunner.SendObservable<string, string>(
            client,
            static () => new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath),
            CreateSettings(),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            methodTokenSource.Token);

        var result = await ObservableTestHelpers.Await(observable);

        await Assert.That(result).IsEqualTo(ObservedResponseContent);
    }

    /// <summary>Verifies a cold observable skips the linked cancellation source when the method's token cannot cancel,
    /// running against the per-subscription token alone, and still yields the response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendObservableSkipsLinkedSourceWhenMethodTokenCannotCancel()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ObservedResponseContent)
                }));
        using var client = CreateClient(handler);

        var observable = GeneratedRequestRunner.SendObservable<string, string>(
            client,
            static () => new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath),
            CreateSettings(),
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);

        var result = await ObservableTestHelpers.Await(observable);

        await Assert.That(result).IsEqualTo(ObservedResponseContent);
    }

    /// <summary>Verifies EnsureResponseContent substitutes empty content when the response has none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EnsureResponseContentSubstitutesEmptyWhenNull()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };

        var content = RequestExecutionHelpers.EnsureResponseContent(response);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies EnsureResponseContent returns the existing content untouched.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EnsureResponseContentReturnsExistingContent()
    {
        var original = new StringContent("hi");
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = original };

        var content = RequestExecutionHelpers.EnsureResponseContent(response);

        await Assert.That(ReferenceEquals(content, original)).IsTrue();
    }
}
