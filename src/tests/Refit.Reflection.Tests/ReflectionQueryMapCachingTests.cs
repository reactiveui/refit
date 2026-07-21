// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins the exact query string the reflection request builder flattens for a combined query object, including on
/// the second call that reuses the cached per-type query-property metadata, and confirms it matches the generator.</summary>
public sealed class ReflectionQueryMapCachingTests
{
    /// <summary>The base address used when building request URIs.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>The exact relative URI both builders must produce for the combined query model.</summary>
    private const string ExpectedPathAndQuery =
        "/list?Id=101&handle=widgets%20%26%20gadgets&Page=3&Tags=1&Tags=2&Tags=3&Inner.Code=abc&Inner.Label=primary";

    /// <summary>The scalar identifier rendered under its CLR name.</summary>
    private const int ModelId = 101;

    /// <summary>The page number rendered under its CLR name.</summary>
    private const int ModelPage = 3;

    /// <summary>The multi-expanded tag collection flattened to one repeated key per element.</summary>
    private static readonly int[] _modelTags = [1, 2, 3];

    /// <summary>Verifies the combined query object flattens to the same exact query string via the generator and via two
    /// separate reflection builders (the second reusing the cached per-type query-property metadata).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CombinedQueryObjectFlattensIdenticallyAcrossBuildersAndCache()
    {
        var query = CreateModel();

        var handler = new TestHttpMessageHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var generated = RestService.ForGenerated<IReflectionCachingQueryApi>(client, new RefitSettings());
        _ = await generated.Flatten(query);
        var generatedUri = handler.RequestMessage!.RequestUri!.PathAndQuery;

        var first = await new RequestBuilderImplementation<IReflectionCachingQueryApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionCachingQueryApi.Flatten))([query]);
        var second = await new RequestBuilderImplementation<IReflectionCachingQueryApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionCachingQueryApi.Flatten))([query]);

        await Assert.That(generatedUri).IsEqualTo(ExpectedPathAndQuery);
        await Assert.That(first.RequestUri!.PathAndQuery).IsEqualTo(ExpectedPathAndQuery);
        await Assert.That(second.RequestUri!.PathAndQuery).IsEqualTo(ExpectedPathAndQuery);
    }

    /// <summary>Builds the canonical combined query model shared by the flattening assertions.</summary>
    /// <returns>A populated query model.</returns>
    private static ReflectionCachingQueryModel CreateModel() =>
        new()
        {
            Id = ModelId,
            Name = "widgets & gadgets",
            Page = ModelPage,
            Ignored = "secret",
            Tags = _modelTags,
            Inner = new() { Code = "abc", Label = "primary" },
        };
}
