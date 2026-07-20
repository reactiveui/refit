// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Verifies request-building paths of the reflection request builder that only the reflection path reaches:
/// authorization parameters, synchronous and streaming body serialization, buffered bodies, round-tripping null path
/// values and header parsing from base interfaces.</summary>
public sealed class ReflectionRequestBuildingTests
{
    /// <summary>The base address used when building request URIs.</summary>
    private const string BaseUrl = "http://api/";

    /// <summary>The serialized JSON body shared by the body-serialization fixtures.</summary>
    private const string SerializedBody = "{\"name\":\"x\"}";

    /// <summary>Verifies an authorization parameter contributes the Authorization header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AuthorizeParameterAddsAuthorizationHeader()
    {
        var fixture = new RequestBuilderImplementation<IAuthorizeHeaderApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IAuthorizeHeaderApi.Get));

        var output = await factory(["abc"]);

        await Assert.That(output.Headers.Authorization!.ToString()).IsEqualTo("Bearer abc");
    }

    /// <summary>Verifies a buffered body-serialization mode serializes the body synchronously.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferedSerializationModeSerializesBodySynchronously()
    {
        var settings = new RefitSettings { RequestBodySerialization = RequestBodySerializationMode.Buffered };
        var fixture = new RequestBuilderImplementation<IBodySerializationApi>(settings);

        var handler = await fixture.RunRequest(nameof(IBodySerializationApi.Post))([new BodyPayload { Name = "x" }]);

        await Assert.That(handler.SendContent).IsEqualTo(SerializedBody);
    }

    /// <summary>Verifies a streamed body-serialization mode serializes the body as streaming content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamedSerializationModeSerializesBodyAsStream()
    {
        var settings = new RefitSettings { RequestBodySerialization = RequestBodySerializationMode.Streamed };
        var fixture = new RequestBuilderImplementation<IBodySerializationApi>(settings);

        var handler = await fixture.RunRequest(nameof(IBodySerializationApi.Post))([new BodyPayload { Name = "x" }]);

        await Assert.That(handler.SendContent).IsEqualTo(SerializedBody);
    }

    /// <summary>Verifies a buffered body attribute assigns the serialized content directly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferedBodyAttributeAssignsSerializedContentDirectly()
    {
        var fixture = new RequestBuilderImplementation<IBufferedBodyApi>();

        var handler = await fixture.RunRequest(nameof(IBufferedBodyApi.Post))([new BodyPayload { Name = "x" }]);

        await Assert.That(handler.SendContent).IsEqualTo(SerializedBody);
    }

    /// <summary>Verifies a round-tripping path parameter formats a null value as an empty segment.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTrippingPathParameterFormatsNullAsEmpty()
    {
        var fixture = new RequestBuilderImplementation<IRoundTrippingNullString>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IRoundTrippingNullString.GetValue));

        var output = await factory([null!]);

        var uri = new Uri(new(BaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/");
    }

    /// <summary>Verifies headers declared on a base interface are applied and blank entries are ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeadersFromBaseInterfaceAreAppliedAndBlankEntriesIgnored()
    {
        var fixture = new RequestBuilderImplementation<IInheritedHeadersApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IInheritedHeadersApi.Get));

        var output = await factory([]);

        await Assert.That(output.Headers.GetValues("X-Base")).IsCollectionEqualTo(["base"]);
    }

    /// <summary>Verifies RFC 3986 resolution splits a route template's inline query string from its path and merges it
    /// with the dynamic query parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Rfc3986ResolutionMergesInlineTemplateQueryWithQueryParameters()
    {
        const int pageNumber = 3;
        var settings = new RefitSettings { UrlResolution = UrlResolutionMode.Rfc3986 };
        var fixture = new RequestBuilderImplementation<IRfcUrlResolutionApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IRfcUrlResolutionApi.GetValuesWithQuery));

        var output = await factory([pageNumber]);

        // The reflection builder assigns a relative URI ("values?active=true&page=3") that the HttpClient resolves
        // against the base address once the request is sent, so the captured request carries the merged absolute URI.
        await Assert.That(output.RequestUri!.AbsoluteUri).IsEqualTo("http://api/values?active=true&page=3");
    }
}
