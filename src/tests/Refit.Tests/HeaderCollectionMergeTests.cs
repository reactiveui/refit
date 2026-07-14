// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Exercises how a header-collection parameter merges into the pending header dictionary.</summary>
public sealed class HeaderCollectionMergeTests
{
    /// <summary>A header-collection-only method adds its entries to a freshly created header dictionary.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HeaderCollectionOnlyMethodCreatesHeaderDictionary()
    {
        var headers = new Dictionary<string, string>
        {
            { "key1", "val1" },
            { "key2", "val2" }
        };

        var fixture = new RequestBuilderImplementation<IHeaderCollectionMergeApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IHeaderCollectionMergeApi.PostWithOnlyHeaderCollection));
        var output = await factory([headers]);

        await Assert.That(output.Headers.Contains("key1")).IsTrue();
        await Assert.That(output.Headers.GetValues("key1").First()).IsEqualTo("val1");
        await Assert.That(output.Headers.Contains("key2")).IsTrue();
        await Assert.That(output.Headers.GetValues("key2").First()).IsEqualTo("val2");
    }

    /// <summary>A static header already present is merged with, not replaced by, the header collection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HeaderCollectionMergesIntoExistingHeaderDictionary()
    {
        var headers = new Dictionary<string, string>
        {
            { "key1", "val1" }
        };

        var fixture = new RequestBuilderImplementation<IHeaderCollectionMergeApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IHeaderCollectionMergeApi.PostWithStaticHeaderAndCollection));
        var output = await factory([headers]);

        await Assert.That(output.Headers.Contains("X-Static")).IsTrue();
        await Assert.That(output.Headers.GetValues("X-Static").First()).IsEqualTo("static-value");
        await Assert.That(output.Headers.Contains("key1")).IsTrue();
        await Assert.That(output.Headers.GetValues("key1").First()).IsEqualTo("val1");
    }
}
