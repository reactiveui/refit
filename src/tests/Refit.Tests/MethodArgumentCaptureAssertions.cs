// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit.Tests;

/// <summary>Shared assertions over the <see cref="HttpRequestMessageOptions.MethodArguments"/> request option so the
/// reflection and source-generated request paths are verified to capture identical argument arrays.</summary>
internal static class MethodArgumentCaptureAssertions
{
    /// <summary>Asserts the request captured exactly the given argument values, in declared order.</summary>
    /// <param name="request">The built request message to inspect.</param>
    /// <param name="expectedArguments">The expected argument values, in declared parameter order.</param>
    /// <returns>A task that represents the asynchronous assertion.</returns>
    public static async Task AssertCapturedAsync(HttpRequestMessage request, params object?[] expectedArguments)
    {
        var captured = GetCapturedArguments(request);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!).IsCollectionEqualTo(expectedArguments);
    }

    /// <summary>Asserts the request did not capture the method-arguments option.</summary>
    /// <param name="request">The built request message to inspect.</param>
    /// <returns>A task that represents the asynchronous assertion.</returns>
    public static async Task AssertAbsentAsync(HttpRequestMessage request) =>
        await Assert.That(GetCapturedArguments(request)).IsNull();

    /// <summary>Reads the captured argument array from the request options, or null when it was not captured.</summary>
    /// <param name="request">The request message to inspect.</param>
    /// <returns>The captured argument values, or null.</returns>
    private static object?[]? GetCapturedArguments(HttpRequestMessage request) =>
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<object?[]>(HttpRequestMessageOptions.MethodArguments),
            out var value)
            ? value
            : null;
}
