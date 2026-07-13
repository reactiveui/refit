// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the slice-escaping helper of <see cref="StringHelpers"/>.</summary>
public sealed class StringHelpersTests
{
    /// <summary>The start index of the slice escaped by the fixture.</summary>
    private const int SliceStart = 2;

    /// <summary>The length of the slice escaped by the fixture.</summary>
    private const int SliceLength = 3;

    /// <summary>The initial capacity of the value string builder under test.</summary>
    private const int BuilderInitialCapacity = 16;

    /// <summary>Verifies the slice overload escapes only the requested span of the value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EscapeDataStringEscapesRequestedSlice()
    {
        var result = StringHelpers.EscapeDataString("ab c/d", SliceStart, SliceLength);

        await Assert.That(result).IsEqualTo("%20c%2F");
    }

    /// <summary>Verifies the in-place span escaper defers a non-ASCII value to the framework escaper, matching
    /// <see cref="Uri.EscapeDataString(string)"/>'s UTF-8 percent-encoding.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AppendUriDataEscapedEscapesNonAsciiValue()
    {
        var target = new ValueStringBuilder(BuilderInitialCapacity);
        StringHelpers.AppendUriDataEscaped(ref target, "café".AsSpan());

        // ToString returns the rented buffer to the pool.
        await Assert.That(target.ToString()).IsEqualTo("caf%C3%A9");
    }
}
