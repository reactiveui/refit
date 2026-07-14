// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Header-validation arrange-act-assert bodies shared by the reflection request builder and the
/// source-generated request runner. Each scenario takes the path's header-apply delegate so both real code paths run
/// the same assertions while the body lives in one place.</summary>
internal static class HeaderValidationScenarios
{
    /// <summary>A strictly-parsed header used by the header-validation scenarios.</summary>
    private const string ValidatedHeaderName = "If-Modified-Since";

    /// <summary>A value that fails the <see cref="ValidatedHeaderName"/> parser but is accepted verbatim without validation.</summary>
    private const string MalformedHeaderValue = "not a date";

    /// <summary>A value the <see cref="ValidatedHeaderName"/> parser accepts, covering the validated success path.</summary>
    private const string ValidatedHeaderValue = "Wed, 21 Oct 2015 07:28:00 GMT";

    /// <summary>A content header whose name is misused on the request header collection, forcing the content fallback.</summary>
    private const string ContentHeaderName = "Content-Language";

    /// <summary>A value the <see cref="ContentHeaderName"/> parser accepts.</summary>
    private const string ContentHeaderValue = "en-US";

    /// <summary>Adding a header verbatim (validation disabled) keeps a value the framework parser would reject.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task SendsMalformedValueVerbatimWithoutValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, ValidatedHeaderName, MalformedHeaderValue, validateHeaders: false);

        await Assert.That(request.Headers.GetValues(ValidatedHeaderName)).IsEquivalentTo([MalformedHeaderValue]);
    }

    /// <summary>Enabling validation surfaces a malformed header value as a <see cref="FormatException"/>.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task ThrowsForMalformedValueWithValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await Assert.That(() => applyHeader(request, ValidatedHeaderName, MalformedHeaderValue, validateHeaders: true))
            .Throws<FormatException>();
    }

    /// <summary>Enabling validation stores a well-formed value on the request header collection.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task AddsWellFormedRequestHeaderWithValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, ValidatedHeaderName, ValidatedHeaderValue, validateHeaders: true);

        await Assert.That(request.Headers.GetValues(ValidatedHeaderName)).IsEquivalentTo([ValidatedHeaderValue]);
    }

    /// <summary>Enabling validation applies a content header to the request's content collection when it is misused on
    /// the request headers.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task FallsBackToContentHeaderWithValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent("body")
        };

        applyHeader(request, ContentHeaderName, ContentHeaderValue, validateHeaders: true);

        await Assert.That(request.Content!.Headers.ContentLanguage).IsEquivalentTo([ContentHeaderValue]);
        await Assert.That(request.Headers.Any()).IsFalse();
    }

    /// <summary>Enabling validation drops a content header when there is no content collection to receive it.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task DropsContentHeaderWithoutContentWithValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, ContentHeaderName, ContentHeaderValue, validateHeaders: true);

        await Assert.That(request.Content).IsNull();
        await Assert.That(request.Headers.Any()).IsFalse();
    }

    /// <summary>Without validation a content header is dropped when there is no content collection to receive it.</summary>
    /// <param name="applyHeader">The header-apply path under test.</param>
    /// <param name="requestUri">The request URI matching the path's addressing convention.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task DropsContentHeaderWithoutContentWithoutValidation(ApplyHeader applyHeader, string requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        applyHeader(request, ContentHeaderName, ContentHeaderValue, validateHeaders: false);

        await Assert.That(request.Content).IsNull();
        await Assert.That(request.Headers.Any()).IsFalse();
    }
}
