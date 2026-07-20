// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Drives real requests through the reflection request builder for the attribute shapes on
/// <see cref="IReflectionParseShapeApi"/> and pins the resulting URI, headers, request options, JSON body and multipart
/// parts. These end-to-end assertions guard the whole per-call build against regressions in the constructor's
/// per-parameter attribute classification.</summary>
public sealed class ReflectionParseShapeRequestTests
{
    /// <summary>The serialized JSON body produced for the body payload fixture.</summary>
    private const string SerializedBody = "{\"name\":\"x\"}";

    /// <summary>The nested-model identifier flattened into the query on the nested-path request.</summary>
    private const int NestedModelId = 5;

    /// <summary>The sample byte-array multipart payload.</summary>
    private static readonly byte[] _payloadBytes = [1, 2, 3];

    /// <summary>The sample bytes backing the stream multipart part.</summary>
    private static readonly byte[] _streamBytes = [9, 9, 9];

    /// <summary>Every distinctly attributed parameter reaches its destination on the built request: header, header
    /// collection, authorization header, request-property option and scalar query, alongside the static headers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DenseAttributesRouteEachParameterToItsDestination()
    {
        var headerCollection = new Dictionary<string, string> { ["X-Extra"] = "extra" };
        var output = await new RequestBuilderImplementation<IReflectionParseShapeApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionParseShapeApi.DenseAttributes))(
                ["key123", headerCollection, "tok", "trace-1", "filtered"]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/dense?filter=filtered");
        await Assert.That(output.Headers.GetValues("X-Api-Key")).IsCollectionEqualTo(["key123"]);
        await Assert.That(output.Headers.Authorization!.ToString()).IsEqualTo("Bearer tok");
        await Assert.That(output.Headers.GetValues("X-Extra")).IsCollectionEqualTo(["extra"]);
        await Assert.That(output.Headers.GetValues("X-Interface")).IsCollectionEqualTo(["iface"]);
        await Assert.That(output.Headers.GetValues("X-Method")).IsCollectionEqualTo(["dense"]);

        var found = output.Options.TryGetValue(new HttpRequestOptionsKey<object>("trace-id"), out var traceId);
        await Assert.That(found).IsTrue();
        await Assert.That(traceId).IsEqualTo("trace-1");
    }

    /// <summary>A nested-object path chain renders the bound value as a path segment while the remaining property flattens
    /// into the query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NestedObjectPathRendersChainAndFlattensRemainder()
    {
        var model = new ReflectionParseShapeModel { Id = NestedModelId, Inner = new() { Code = "abc" } };

        var output = await new RequestBuilderImplementation<IReflectionParseShapeApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionParseShapeApi.NestedPath))([model]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/orgs/abc/audit?Id=5");
    }

    /// <summary>An implicit body argument is serialized as the JSON request content on a POST.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SerializedBodyPostsJsonContent()
    {
        var handler = await new RequestBuilderImplementation<IReflectionParseShapeApi>()
            .RunRequest(nameof(IReflectionParseShapeApi.SerializedBody))([new BodyPayload { Name = "x" }]);

        await Assert.That(handler.RequestMessage!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.SendContent).IsEqualTo(SerializedBody);
    }

    /// <summary>A multipart upload emits one form-data part per argument, each under its parameter name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadBuildsOnePartPerArgument()
    {
        await using var stream = new MemoryStream(_streamBytes);
        var handler = await new RequestBuilderImplementation<IReflectionParseShapeApi>()
            .RunRequest(nameof(IReflectionParseShapeApi.Upload))(["hello", _payloadBytes, stream]);

        // The captured request content is disposed by the HttpClient after sending, so assert against the multipart
        // body the handler read before disposal: it carries one content-disposition part per argument.
        await Assert.That(handler.RequestMessage!.Content).IsTypeOf<MultipartFormDataContent>();
        var body = handler.SendContent!;
        await Assert.That(body).Contains("name=title");
        await Assert.That(body).Contains("name=payload");
        await Assert.That(body).Contains("name=content");
        await Assert.That(body).Contains("hello");
    }

    /// <summary>A <c>[Url]</c> parameter supplies the complete absolute request URI, bypassing the base address.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AbsoluteUrlParameterSuppliesTheRequestUri()
    {
        var output = await new RequestBuilderImplementation<IReflectionParseShapeApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionParseShapeApi.AbsoluteUrl))(
                ["https://other.test/path?x=1"]);

        await Assert.That(output.RequestUri!.AbsoluteUri).IsEqualTo("https://other.test/path?x=1");
    }
}
