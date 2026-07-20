// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins the exact relative URI the reflection request builder produces when an object argument binds a path
/// segment and is simultaneously flattened into the query string: the path-bound property is dropped from the query while
/// every remaining property is flattened, and the contract holds on the second call that reuses the cached metadata.</summary>
public sealed class ReflectionObjectPathBindingTests
{
    /// <summary>The exact relative URI the builder must produce: the identifier renders as a path segment and is omitted
    /// from the query, while the remaining properties flatten in declared order.</summary>
    private const string ExpectedPathAndQuery =
        "/users/101/detail?handle=widgets%20%26%20gadgets&Page=3&Tags=1&Tags=2&Tags=3&Inner.Code=abc&Inner.Label=primary";

    /// <summary>The scalar identifier bound to the path segment and omitted from the query.</summary>
    private const int ModelId = 101;

    /// <summary>The page number flattened into the query under its CLR name.</summary>
    private const int ModelPage = 3;

    /// <summary>The multi-expanded tag collection flattened to one repeated key per element.</summary>
    private static readonly int[] _modelTags = [1, 2, 3];

    /// <summary>Verifies the path-bound identifier is dropped from the flattened query and the rest of the object flattens
    /// identically across two builders, the second reusing the cached per-type query-property metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObjectPathBindingDropsBoundPropertyFromFlattenedQuery()
    {
        var request = new ReflectionCachingQueryModel
        {
            Id = ModelId,
            Name = "widgets & gadgets",
            Page = ModelPage,
            Ignored = "secret",
            Tags = _modelTags,
            Inner = new() { Code = "abc", Label = "primary" },
        };

        var first = await new RequestBuilderImplementation<IReflectionObjectPathApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionObjectPathApi.Detail))([request]);
        var second = await new RequestBuilderImplementation<IReflectionObjectPathApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionObjectPathApi.Detail))([request]);

        await Assert.That(first.RequestUri!.PathAndQuery).IsEqualTo(ExpectedPathAndQuery);
        await Assert.That(second.RequestUri!.PathAndQuery).IsEqualTo(ExpectedPathAndQuery);
    }
}
