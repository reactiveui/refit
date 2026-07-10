// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for the reflection request builder's query and header helpers.</summary>
public partial class RequestBuilderTests
{
    /// <summary>The header name used by the header-replacement tests.</summary>
    private const string ReplacedHeaderName = "X-Refit-Test";

    /// <summary>The original header value written before a replacement.</summary>
    private const string OriginalHeaderValue = "first";

    /// <summary>The header value written by a replacement.</summary>
    private const string ReplacementHeaderValue = "second";

    /// <summary>The dictionary key the test formatter renders as blank.</summary>
    private const string BlankDictionaryKey = "blank";

    /// <summary>The query value the test formatter renders as null.</summary>
    private const string NullFormattedQueryValue = "nulled";

    /// <summary>Skips dictionary entries whose value is null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryDictionarySkipsNullValues()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithObjectDictionary));

        var output = await factory([new Dictionary<string, object?> { ["skipped"] = null, ["kept"] = "yes" }]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?kept=yes");
    }

    /// <summary>Expands a dictionary value that is itself a complex object into nested query keys.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryDictionaryExpandsComplexValuesIntoNestedKeys()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithObjectDictionary));

        var output = await factory([new Dictionary<string, object?> { ["outer"] = new NestedQueryValue { Inner = "v" } }]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?outer.Inner=v");
    }

    /// <summary>Skips dictionary entries whose formatted key is blank.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryDictionarySkipsBlankFormattedKeys()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new BlankKeyUrlParameterFormatter() };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithObjectDictionary));

        var output = await factory([new Dictionary<string, object?> { [BlankDictionaryKey] = "v" }]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo");
    }

    /// <summary>Omits a query parameter whose formatted value is null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParameterIsOmittedWhenTheFormattedValueIsNull()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new NullValueUrlParameterFormatter() };
        var scoped = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var scopedFactory = scoped.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.UnescapedQueryParams));

        var output = await scopedFactory([NullFormattedQueryValue]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query");
    }

    /// <summary>Emits a null property when it opts in via Query(SerializeNull = true).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryPropertyIsEmittedWhenSerializeNullIsSet()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithSerializationObject));

        var output = await factory([new QuerySerializationObject()]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?SerializedNull=");
    }

    /// <summary>Omits a formatted query property when the formatter renders it as null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryPropertyIsOmittedWhenTheFormatterReturnsNull()
    {
        var settings = new RefitSettings
        {
            FormUrlEncodedParameterFormatter = new NullFormUrlEncodedParameterFormatter(),
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithSerializationObject));

        var output = await factory([new QuerySerializationObject { Formatted = "value" }]);

        // SerializedNull still opts in; only the formatted property is dropped.
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?SerializedNull=");
    }

    /// <summary>Serializes a string body through the obsolete JSON serialization method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObsoleteJsonBodySerializationMethodStillSerializes()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.PostWithObsoleteJsonBody));

        var output = await factory(["raw"]);

        // The legacy value serializes rather than sending the string as raw text.
        await Assert.That(output.SendContent).IsEqualTo("\"raw\"");
    }

    /// <summary>Sends a string body through the url-encoded serialization method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UrlEncodedBodyAcceptsAStringPayload()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.PostSomeUrlEncodedStuff));

        var output = await factory([1, "a b"]);

        await Assert.That(output.SendContent).IsEqualTo("a%20b");
    }

    /// <summary>Substitutes an empty path segment when the formatter renders an object property as null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PathBoundObjectPropertyUsesEmptyWhenTheFormatterReturnsNull()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new AlwaysNullUrlParameterFormatter() };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithOptionalParametersPathBoundObject));

        var output = await factory([new PathBoundObject { SomeProperty = 1 }, null!, null!, null!]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.AbsolutePath).IsEqualTo("/api/");
    }

    /// <summary>Substitutes empty segments when the formatter renders round-tripping path sections as null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RoundTrippingPathSectionsUseEmptyWhenTheFormatterReturnsNull()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new AlwaysNullUrlParameterFormatter() };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuffWithRoundTrippingParam));

        var output = await factory(["a/b", 1]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.AbsolutePath).IsEqualTo("/foo/bar///");
    }

    /// <summary>Falls back to the derived file name when a multipart item supplies an empty one.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipartItemWithAnExplicitNameAndEmptyFileNameUsesTheDerivedFileName()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.Blob_Post_Byte));

        var output = await factory(["dir/file", new ByteArrayPart([1, 2], string.Empty, name: "custom")]);

        await Assert.That(output.SendContent).Contains("custom");
    }

    /// <summary>Leaves the body unserialized when the serialization method is not a declared enum value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UndeclaredBodySerializationMethodLeavesTheBodyUnserialized()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithUndeclaredBodySerializationMethod));

        var output = await factory(["raw"]);

        // The body is dropped; what remains is the empty placeholder content Refit attaches for header carrying.
        await Assert.That(output.Content!.Headers.ContentLength).IsEqualTo(0);
    }

    /// <summary>Treats a non-generic enumerable as a query map because its element type is unknown.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DoNotConvertToQueryMapIsFalseForANonGenericEnumerable()
    {
        var nonGenericSequence = new ArrayList { 1, 2 };

        await Assert.That(RequestBuilderImplementation.DoNotConvertToQueryMap(nonGenericSequence)).IsFalse();
    }

    /// <summary>Emits a scalar directly rather than expanding it into a query map.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DoNotConvertToQueryMapIsTrueForAScalar() =>
        await Assert.That(RequestBuilderImplementation.DoNotConvertToQueryMap("scalar")).IsTrue();

    /// <summary>Replaces an existing request header rather than duplicating it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesAnExistingRequestHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrlWithSlash);
        _ = request.Headers.TryAddWithoutValidation(ReplacedHeaderName, OriginalHeaderValue);

        RequestBuilderImplementation.SetHeader(request, ReplacedHeaderName, ReplacementHeaderValue);

        await Assert.That(request.Headers.GetValues(ReplacedHeaderName)).IsEquivalentTo([ReplacementHeaderValue]);
    }

    /// <summary>Removes an existing request header when a null value is supplied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderRemovesAnExistingRequestHeaderWhenTheValueIsNull()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrlWithSlash);
        _ = request.Headers.TryAddWithoutValidation(ReplacedHeaderName, OriginalHeaderValue);

        RequestBuilderImplementation.SetHeader(request, ReplacedHeaderName, null);

        await Assert.That(request.Headers.Contains(ReplacedHeaderName)).IsFalse();
    }

    /// <summary>Replaces a header that was set on the request content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SetHeaderReplacesAnExistingContentHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrlWithSlash);
        using var content = new StringContent("body");
        _ = content.Headers.TryAddWithoutValidation(ReplacedHeaderName, OriginalHeaderValue);
        request.Content = content;

        RequestBuilderImplementation.SetHeader(request, ReplacedHeaderName, ReplacementHeaderValue);

        await Assert.That(request.Headers.GetValues(ReplacedHeaderName)).IsEquivalentTo([ReplacementHeaderValue]);
        await Assert.That(request.Content.Headers.Contains(ReplacedHeaderName)).IsFalse();
    }

    /// <summary>A complex query value expanded into nested query-string keys.</summary>
    private sealed class NestedQueryValue
    {
        /// <summary>Gets or sets the inner value.</summary>
        public string? Inner { get; set; }
    }

    /// <summary>A URL parameter formatter that renders every dictionary key as blank.</summary>
    private sealed class BlankKeyUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        /// <inheritdoc/>
        public override string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) =>
            value is string text && text == BlankDictionaryKey
                ? " "
                : base.Format(value, attributeProvider, type);
    }

    /// <summary>A URL parameter formatter that renders a sentinel value as null.</summary>
    private sealed class NullValueUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        /// <inheritdoc/>
        public override string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) =>
            value is string text && text == NullFormattedQueryValue
                ? null
                : base.Format(value, attributeProvider, type);
    }

    /// <summary>A form-url-encoded formatter that renders every formatted value as null.</summary>
    private sealed class NullFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
    {
        /// <inheritdoc/>
        public string? Format(object? value, string? formatString) => null;
    }

    /// <summary>A URL parameter formatter that renders every value as null.</summary>
    private sealed class AlwaysNullUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        /// <inheritdoc/>
        public override string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) => null;
    }
}
