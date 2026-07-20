// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins how the reflection request builder classifies parameters that carry <see cref="PropertyAttribute"/> and/or
/// <see cref="QueryAttribute"/>: a property-only parameter is kept out of the query string, while a parameter that is both a
/// property and a query parameter still contributes to the query. Both classifications are also written as request options.</summary>
public sealed class ReflectionPropertyQueryParameterTests
{
    /// <summary>Verifies a property-only parameter is excluded from the query while a property+query parameter is retained,
    /// and both are stored as request options.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PropertyOnlyParameterExcludedWhilePropertyQueryParameterRetained()
    {
        var request = await new RequestBuilderImplementation<IReflectionPropertyQueryApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionPropertyQueryApi.Search))(["hello", "trace-42", "blue"]);

        await Assert.That(request.RequestUri!.PathAndQuery).IsEqualTo("/search?q=hello&tag=blue");

        await Assert.That(request.Options.TryGetValue(new HttpRequestOptionsKey<string>("trace-id"), out var trace)).IsTrue();
        await Assert.That(trace).IsEqualTo("trace-42");
        await Assert.That(request.Options.TryGetValue(new HttpRequestOptionsKey<string>("tag-prop"), out var tag)).IsTrue();
        await Assert.That(tag).IsEqualTo("blue");
    }
}
