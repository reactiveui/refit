// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Direct coverage for indexed reflection queries containing explicitly serialized null properties.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class ReflectionIndexedNullQueryTests
{
    /// <summary>The populated value paired with the null value.</summary>
    private const string PopulatedValue = "value";

    /// <summary>The encoded indexed null query pair.</summary>
    private const string EncodedNullPair = "items[0].Value=";

    /// <summary>Verifies null indexed-object properties use the object fallback type and remain in the query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexedNullPropertyUsesObjectFallbackType()
    {
        var values = new[]
        {
            new IndexedItem { Values = [null] },
            new IndexedItem { Value = PopulatedValue },
        };
        var attribute = new QueryAttribute(CollectionFormat.Indexed);
        var untypedEntries = new List<QueryParameterEntry>();
        var typedEntries = new List<QueryParameterEntry>();
        var untyped = new RequestBuilderImplementation(typeof(IReflectionIndexedApi));
        var typed = new RequestBuilderImplementation<IReflectionIndexedApi>();

        untyped.AppendIndexedCollectionParameters(untypedEntries, values, attribute, "items");
        typed.AppendIndexedCollectionParameters(typedEntries, values, attribute, "items");
        var scalarRequest = await typed.BuildRequestFactoryForMethod(nameof(IReflectionIndexedApi.Get))([values]);
        var voidRequest = await typed.BuildRequestFactoryForMethod(nameof(IReflectionIndexedApi.GetVoid))([values]);
        var responseRequest = await typed.BuildRequestFactoryForMethod(nameof(IReflectionIndexedApi.GetResponse))([values]);

        await Assert.That(untypedEntries[0].Key).IsEqualTo("items[0].Value");
        await Assert.That(untypedEntries[0].Value).IsEmpty();
        await Assert.That(untypedEntries[1].Value).IsNull();
        await Assert.That(untypedEntries[2].Value).IsEqualTo(PopulatedValue);
        await Assert.That(typedEntries[0].Key).IsEqualTo("items[0].Value");
        await Assert.That(typedEntries[0].Value).IsEmpty();
        await Assert.That(typedEntries[1].Value).IsNull();
        await Assert.That(typedEntries[2].Value).IsEqualTo(PopulatedValue);
        await Assert.That(scalarRequest.RequestUri!.Query).Contains(EncodedNullPair);
        await Assert.That(voidRequest.RequestUri!.Query).Contains(EncodedNullPair);
        await Assert.That(responseRequest.RequestUri!.Query).Contains(EncodedNullPair);
    }
}
