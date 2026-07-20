// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests pinning the byte output of the catch-all path escaper under the pristine default formatter.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>Verifies each slash-separated section is percent-encoded independently under the default formatter,
    /// with the separators preserved and only the default-formatter fast path exercised.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RoundTripEscapePathEscapesEachSectionUnderDefaultFormatter()
    {
        var settings = new RefitSettings();

        var result = GeneratedRequestRunner.RoundTripEscapePath(
            "aa a/bb-b/c%d/e",
            settings,
            typeof(string),
            typeof(string));

        await Assert.That(result).IsEqualTo("aa%20a/bb-b/c%25d/e");
    }

    /// <summary>Verifies a null catch-all value escapes the default-formatter rendering of a null value (the empty
    /// string), exercising the null-value branch of the escaper.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RoundTripEscapePathRendersNullValueAsEmptyUnderDefaultFormatter()
    {
        var settings = new RefitSettings();

        var result = GeneratedRequestRunner.RoundTripEscapePath(
            null,
            settings,
            typeof(string),
            typeof(string));

        await Assert.That(result).IsEqualTo(string.Empty);
    }
}
