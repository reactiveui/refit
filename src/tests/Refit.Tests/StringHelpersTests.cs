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

    /// <summary>Verifies the in-place span escaper classifies every RFC 3986 unreserved character range: an
    /// upper-case letter, a lower-case letter, a digit, the four punctuation unreserved characters, and a
    /// reserved character (the space) that percent-encodes. The chosen characters straddle each range boundary so
    /// both outcomes of every classifier comparison are exercised, matching <see cref="Uri.EscapeDataString(string)"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AppendUriDataEscapedPreservesUnreservedCharactersAndEscapesReserved()
    {
        // 'M' (upper), 'z' (past 'Z', lower), '~' (past 'z', unreserved punctuation), '0' (below 'A', digit),
        // '-' (below '0'), '.', '_', and ' ' (unreserved on neither side -> percent-encoded).
        const string unreservedAndSpace = "Mz~0-._ ";
        var target = new ValueStringBuilder(BuilderInitialCapacity);
        StringHelpers.AppendUriDataEscaped(ref target, unreservedAndSpace.AsSpan());

        var escaped = target.ToString();
        await Assert.That(escaped).IsEqualTo("Mz~0-._%20");
        await Assert.That(escaped).IsEqualTo(Uri.EscapeDataString(unreservedAndSpace));
    }

    /// <summary>Verifies the in-place span escaper matches <see cref="Uri.EscapeDataString(string)"/> across the whole
    /// printable ASCII range, straddling every RFC 3986 unreserved-character range boundary so both outcomes of each
    /// classifier comparison are exercised.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AppendUriDataEscapedMatchesFrameworkEscaperAcrossPrintableAscii()
    {
        var printableAscii = string.Concat(Enumerable.Range(0x20, 0x7F - 0x20).Select(static c => (char)c));
        var target = new ValueStringBuilder(BuilderInitialCapacity);
        StringHelpers.AppendUriDataEscaped(ref target, printableAscii.AsSpan());

        await Assert.That(target.ToString()).IsEqualTo(Uri.EscapeDataString(printableAscii));
    }
}
