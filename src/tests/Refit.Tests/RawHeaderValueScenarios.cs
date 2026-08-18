// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Raw-header-preservation arrange-act-assert bodies shared by the reflection request builder and the
/// source-generated request runner. Each scenario takes the path's header-apply delegate so both real code paths run
/// the same assertions while the body lives in one place.</summary>
internal static class RawHeaderValueScenarios
{
    /// <summary>A header whose value the framework parser would reformat if it ever materialized it.</summary>
    private const string RawHeaderName = "Accept";

    /// <summary>A media type with no space after the parameter separator, which the parser would reinsert.</summary>
    private const string RawHeaderValue = "application/vnd.api.json;version=3.4.1";

    /// <summary>A second header, applied after the raw one, whose application must not disturb it.</summary>
    private const string TrailingHeaderName = "X-Trace";

    /// <summary>The value of the second header.</summary>
    private const string TrailingHeaderValue = "abc";

    /// <summary>Applying a later header leaves an earlier verbatim value byte for byte as it was supplied.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// The existence check that precedes each apply used to enumerate the collection, which forces the framework to
    /// parse every value already stored and write the parsed form back. A method declaring one header never showed it;
    /// two or more rewrote the first on the wire.
    /// </remarks>
    internal static async Task KeepsEarlierRawValueWhenALaterHeaderIsApplied(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, RawHeaderName, RawHeaderValue, validateHeaders: false);
        applyHeader(request, TrailingHeaderName, TrailingHeaderValue, validateHeaders: false);

        // NonValidated reports what is actually stored, so it distinguishes the supplied value from the parsed one.
        var stored = request.Headers.NonValidated.TryGetValues(RawHeaderName, out var values);

        await Assert.That(stored).IsTrue();
        await Assert.That(values.ToString()).IsEqualTo(RawHeaderValue);
    }

    /// <summary>Replacing a header leaves the other headers already applied untouched.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task KeepsEarlierRawValueWhenALaterHeaderIsReplaced(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, RawHeaderName, RawHeaderValue, validateHeaders: false);
        applyHeader(request, TrailingHeaderName, TrailingHeaderValue, validateHeaders: false);
        applyHeader(request, TrailingHeaderName, "def", validateHeaders: false);

        var stored = request.Headers.NonValidated.TryGetValues(RawHeaderName, out var values);

        await Assert.That(stored).IsTrue();
        await Assert.That(values.ToString()).IsEqualTo(RawHeaderValue);
        await Assert.That(request.Headers.GetValues(TrailingHeaderName)).IsEquivalentTo(["def"]);
    }
}
