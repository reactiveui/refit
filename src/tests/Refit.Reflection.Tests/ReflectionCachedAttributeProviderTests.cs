// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins that formatting which reads a parameter's <c>[Query(Format = ...)]</c> attribute stays correct when the
/// reflection request builder reuses its cached attribute provider across separate builder instances.</summary>
public sealed class ReflectionCachedAttributeProviderTests
{
    /// <summary>The base address used when building request URIs.</summary>
    private const string BaseUrl = "http://api/";

    /// <summary>The identifier formatted through the parameter's <c>[Query(Format = "0.0")]</c> attribute.</summary>
    private const int SampleId = 6;

    /// <summary>Verifies a parameter's query format attribute is applied on both a cold builder (materializing the cached
    /// attribute provider) and a second builder (reusing it).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryFormatAttributeAppliesAcrossCachedProviderReuse()
    {
        var first = await new RequestBuilderImplementation<IReflectionQueryFormatApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionQueryFormatApi.FetchSomeStuffWithQueryFormat))([SampleId]);
        var second = await new RequestBuilderImplementation<IReflectionQueryFormatApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionQueryFormatApi.FetchSomeStuffWithQueryFormat))([SampleId]);

        await Assert.That(new Uri(new(BaseUrl), first.RequestUri!).PathAndQuery).IsEqualTo("/foo/bar/6.0");
        await Assert.That(new Uri(new(BaseUrl), second.RequestUri!).PathAndQuery).IsEqualTo("/foo/bar/6.0");
    }

    /// <summary>Verifies the provider materializes the inherited <see cref="QueryAttribute"/> lookup once and reuses it,
    /// while every other lookup (a non-inherited read, a different attribute type, and the untyped/IsDefined members)
    /// delegates straight to the wrapped provider.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CachesInheritedQueryLookupAndDelegatesEverythingElse()
    {
        var inner = typeof(FormattedModel).GetProperty(nameof(FormattedModel.Amount))!;
        var provider = new CachedAttributeProvider(inner);

        // Inherited QueryAttribute read: the first materializes the cache, the second returns the same array.
        var firstQuery = provider.GetCustomAttributes(typeof(QueryAttribute), true);
        var secondQuery = provider.GetCustomAttributes(typeof(QueryAttribute), true);
        await Assert.That(firstQuery.Length).IsEqualTo(1);
        await Assert.That(ReferenceEquals(firstQuery, secondQuery)).IsTrue();

        // A non-inherited read short-circuits the cache and delegates to the wrapped provider.
        await Assert.That(provider.GetCustomAttributes(typeof(QueryAttribute), false).Length).IsEqualTo(1);

        // A different attribute type is never cached; it delegates to the wrapped provider.
        await Assert.That(provider.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length).IsEqualTo(0);

        // The untyped and IsDefined members delegate straight through.
        await Assert.That(provider.GetCustomAttributes(true).Length).IsEqualTo(1);
        await Assert.That(provider.IsDefined(typeof(QueryAttribute), true)).IsTrue();
    }

    /// <summary>A model whose property carries a single <see cref="QueryAttribute"/> for the cached-provider assertions.</summary>
    private sealed class FormattedModel
    {
        /// <summary>Gets or sets a value formatted through a query attribute.</summary>
        [Query(Format = "0.0")]
        public int Amount { get; set; }
    }
}
