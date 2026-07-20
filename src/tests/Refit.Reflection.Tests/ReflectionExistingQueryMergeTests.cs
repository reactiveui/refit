// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins how the reflection request builder merges an absolute <c>[Url]</c>'s existing query string with an
/// appended <c>[Query]</c> parameter: existing entries come first, values are URL-decoded (<c>+</c> to space, percent
/// escapes), duplicate keys are comma-joined, and blank or valueless keys are dropped.</summary>
public sealed class ReflectionExistingQueryMergeTests
{
    /// <summary>Verifies an existing single query parameter is preserved before the appended one.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ExistingParameterPrecedesAppended() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?existing=1",
            "https://cdn.example.com/f?existing=1&token=abc");

    /// <summary>Verifies duplicate keys in the existing query are comma-joined (then percent-escaped).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task DuplicateExistingKeysAreCommaJoined() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?a=1&a=2",
            "https://cdn.example.com/f?a=1%2C2&token=abc");

    /// <summary>Verifies existing query values are URL-decoded: <c>+</c> becomes a space, percent escapes are decoded.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ExistingValuesAreUrlDecoded() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?x=a+b&y=%41",
            "https://cdn.example.com/f?x=a%20b&y=A&token=abc");

    /// <summary>Verifies a blank key and a valueless key in the existing query are dropped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task BlankAndValuelessKeysAreDropped() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?=v&k",
            "https://cdn.example.com/f?token=abc");

    /// <summary>Verifies duplicate keys are joined while an interleaved distinct key keeps its first-seen position.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InterleavedDuplicateKeysJoinInFirstSeenOrder() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?dup=1&other=z&dup=2",
            "https://cdn.example.com/f?dup=1%2C2&other=z&token=abc");

    /// <summary>Verifies keys that differ only by case are treated as duplicates and joined under the first casing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task CaseDifferingKeysAreJoinedUnderFirstCasing() =>
        AssertMergeAsync(
            "https://cdn.example.com/f?Mix=1&mix=2&MIX=3",
            "https://cdn.example.com/f?Mix=1%2C2%2C3&token=abc");

    /// <summary>Verifies an absolute URL with no existing query still receives the appended parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task NoExistingQueryStillAppends() =>
        AssertMergeAsync(
            "https://cdn.example.com/f",
            "https://cdn.example.com/f?token=abc");

    /// <summary>Builds a request for the absolute URL with an appended token and asserts the merged absolute URI.</summary>
    /// <param name="url">The absolute URL, possibly already carrying a query string.</param>
    /// <param name="expectedAbsoluteUri">The expected merged absolute URI.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static async Task AssertMergeAsync(string url, string expectedAbsoluteUri)
    {
        var reflected = await new RequestBuilderImplementation<IReflectionAbsoluteUrlApi>()
            .BuildRequestFactoryForMethod(nameof(IReflectionAbsoluteUrlApi.GetAbsoluteWithQuery))([url, "abc"]);

        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(expectedAbsoluteUri);
    }
}
