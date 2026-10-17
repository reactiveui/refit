// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace Refit.Testing;

/// <summary>Builds retry-hint responses. Each matched request receives a fresh message, so reusable routes remain replayable.</summary>
public static class RetryReply
{
    /// <summary>Replies with status 429 and Retry-After delta seconds, rounding fractional seconds up.</summary>
    /// <param name="delay">The server's minimum retry interval.</param>
    /// <returns>A retryable response template.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StubResponse After(TimeSpan delay) => After(delay, RetryReplyCompatibility.TooManyRequests);

    /// <summary>Replies with standard Retry-After delta seconds, rounding fractional seconds up.</summary>
    /// <param name="delay">The server's minimum retry interval.</param>
    /// <param name="status">The response status.</param>
    /// <returns>A retryable response template.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The delay is negative.</exception>
    public static StubResponse After(TimeSpan delay, HttpStatusCode status)
    {
        ArgumentOutOfRangeExceptionHelper.ThrowIfLessThan(delay, TimeSpan.Zero);

        return Header("Retry-After", Math.Ceiling(delay.TotalSeconds).ToString(CultureInfo.InvariantCulture), status);
    }

    /// <summary>Replies with status 429 and a standard Retry-After HTTP date.</summary>
    /// <param name="date">The UTC time after which the server permits a retry.</param>
    /// <returns>A retryable response template.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StubResponse After(DateTimeOffset date) => After(date, RetryReplyCompatibility.TooManyRequests);

    /// <summary>Replies with a standard Retry-After HTTP date and an explicit status.</summary>
    /// <param name="date">The UTC time after which the server permits a retry.</param>
    /// <param name="status">The response status.</param>
    /// <returns>A retryable response template.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StubResponse After(DateTimeOffset date, HttpStatusCode status) =>
        Header("Retry-After", date.UtcDateTime.ToString("R", CultureInfo.InvariantCulture), status);

    /// <summary>Replies with status 429 and a provider-specific header or an intentionally malformed hint.</summary>
    /// <param name="name">The retry header name.</param>
    /// <param name="value">The raw header value.</param>
    /// <returns>A response template with a fresh message on each match.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StubResponse Header(string name, string value) => Header(name, value, RetryReplyCompatibility.TooManyRequests);

    /// <summary>Replies with an explicit status and a provider-specific or intentionally malformed hint.</summary>
    /// <param name="name">The retry header name.</param>
    /// <param name="value">The raw header value.</param>
    /// <param name="status">The response status.</param>
    /// <returns>A response template with a fresh message on each match.</returns>
    /// <exception cref="ArgumentException">The header name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">The header value is null.</exception>
    public static StubResponse Header(string name, string value, HttpStatusCode status)
    {
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(name);

        ArgumentExceptionHelper.ThrowIfNull(value);
        return Reply.From(request =>
        {
            var response = new HttpResponseMessage(status);
            _ = response.Headers.TryAddWithoutValidation(name, value);
            return response;
        });
    }
}
