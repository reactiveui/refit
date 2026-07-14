// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Applies a sanitized header name and value to an <see cref="HttpRequestMessage"/>, choosing the request or
/// content header collection and optionally validating the value. Shared by the source-generated request runtime and
/// the reflection request builder so both paths behave identically.</summary>
internal static class HttpHeaderApplier
{
    /// <summary>Applies a sanitized header value to the request or its content, validating it when requested.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="name">The sanitized header name.</param>
    /// <param name="value">The sanitized header value.</param>
    /// <param name="validateHeaders">Whether the value is validated (<c>Add</c>) or added verbatim (<c>TryAddWithoutValidation</c>).</param>
    /// <exception cref="FormatException">Validation is enabled and the value is malformed for the header's parser.</exception>
    internal static void Apply(HttpRequestMessage request, string name, string value, bool validateHeaders)
    {
        if (validateHeaders)
        {
            // Add validates the value against the header's parser, surfacing malformed values as FormatException.
            if (TryAddValidated(request.Headers, name, value) || request.Content is null)
            {
                return;
            }

            request.Content.Headers.Add(name, value);
            return;
        }

        var added = request.Headers.TryAddWithoutValidation(name, value);

        // Don't even bother trying to add the header as a content header
        // if we just added it to the other collection.
        if (added || request.Content is null)
        {
            return;
        }

        _ = request.Content.Headers.TryAddWithoutValidation(name, value);
    }

    /// <summary>Adds a header with framework validation, reporting whether it belongs to this collection.</summary>
    /// <param name="headers">The header collection to add to.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns><see langword="true"/> when the header was stored; <see langword="false"/> when it belongs on the
    /// content collection instead, mirroring the <c>false</c> return of <c>TryAddWithoutValidation</c>.</returns>
    /// <exception cref="FormatException">The value is malformed for this header's parser.</exception>
    private static bool TryAddValidated(System.Net.Http.Headers.HttpHeaders headers, string name, string value)
    {
        try
        {
            headers.Add(name, value);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The header name belongs on the request's content headers, not this collection; a malformed value would
            // instead surface as FormatException, which is allowed to propagate.
            return false;
        }
    }
}
