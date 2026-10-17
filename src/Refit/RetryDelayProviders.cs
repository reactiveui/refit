// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Reads server retry hints independently of request execution. Invalid or missing hints return null.</summary>
public static class RetryDelayProviders
{
    /// <summary>The latest representable Unix timestamp in seconds.</summary>
    private const long MaxUnixTimeSeconds = 253_402_300_799L;

    /// <summary>The UTC ISO timestamp encoding accepted for reset headers.</summary>
    private const string IsoUtcTimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    /// <summary>The ISO timestamp encoding requiring an explicit UTC offset.</summary>
    private const string IsoOffsetTimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz";

    /// <summary>Reads standard Retry-After delta seconds or an HTTP date.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="now">The current UTC time used for absolute dates.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The server interval, or null if no valid hint exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan? RetryAfter(HttpResponseMessage response, DateTimeOffset now, CancellationToken cancellationToken) =>
        ReadHeader(response, "Retry-After", RetryHeaderFormat.RetryAfter, now, cancellationToken);

    /// <summary>Prefers Azure millisecond hints, then the standard Retry-After header.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="now">The current UTC time used for absolute dates.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The first valid server interval, or null.</returns>
    public static TimeSpan? Azure(HttpResponseMessage response, DateTimeOffset now, CancellationToken cancellationToken) =>
        ReadHeader(response, "x-ms-retry-after-ms", RetryHeaderFormat.Milliseconds, now, cancellationToken)
        ?? ReadHeader(response, "retry-after-ms", RetryHeaderFormat.Milliseconds, now, cancellationToken)
        ?? RetryAfter(response, now, cancellationToken);

    /// <summary>Reads the AWS x-amz-retry-after millisecond hint, then standard Retry-After.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="now">The current UTC time used for absolute dates.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The first valid server interval, or null. AWS services do not all emit a hint.</returns>
    public static TimeSpan? Aws(HttpResponseMessage response, DateTimeOffset now, CancellationToken cancellationToken) =>
        ReadHeader(response, "x-amz-retry-after", RetryHeaderFormat.Milliseconds, now, cancellationToken)
        ?? RetryAfter(response, now, cancellationToken);

    /// <summary>Reads GitHub Retry-After, or the primary rate-limit reset only when the remaining quota is zero.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="now">The current UTC time used for reset timestamps.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The server interval, or null. A secondary limit without a header cannot be identified from headers alone.</returns>
    public static TimeSpan? GitHub(HttpResponseMessage response, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var delay = RetryAfter(response, now, cancellationToken);
        if (delay.HasValue)
        {
            return delay;
        }

        return response.StatusCode is HttpStatusCode.Forbidden or HttpRetryCompatibility.TooManyRequests
               && HasZeroRemaining(response)
            ? ReadHeader(response, "x-ratelimit-reset", RetryHeaderFormat.UnixTimeSeconds, now, cancellationToken)
            : null;
    }

    /// <summary>Reads Jira Retry-After or, on throttled responses, its ISO 8601 reset timestamp.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="now">The current UTC time used for reset timestamps.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The first valid server interval, or null.</returns>
    public static TimeSpan? Jira(HttpResponseMessage response, DateTimeOffset now, CancellationToken cancellationToken) =>
        RetryAfter(response, now, cancellationToken)
        ?? (response.StatusCode == HttpRetryCompatibility.TooManyRequests
            ? ReadHeader(response, "X-RateLimit-Reset", RetryHeaderFormat.Iso8601Date, now, cancellationToken)
            : null);

    /// <summary>Reads a custom header, rejecting ambiguous multiple values and invalid or overflowing numbers.</summary>
    /// <param name="response">The server response.</param>
    /// <param name="headerName">The response header name.</param>
    /// <param name="format">The interval encoding.</param>
    /// <param name="now">The current UTC time used for absolute timestamps.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The interval, with past dates mapped to zero, or null.</returns>
    /// <exception cref="ArgumentNullException">The response is null.</exception>
    /// <exception cref="ArgumentException">The header name is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The header format is not defined.</exception>
    public static TimeSpan? ReadHeader(
        HttpResponseMessage response,
        string headerName,
        RetryHeaderFormat format,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentExceptionHelper.ThrowIfNull(response);
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(headerName);

        if (format is < RetryHeaderFormat.Seconds or > RetryHeaderFormat.Iso8601Date)
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var value = GetSingleHeader(response, headerName);
        return value is null ? null : ParseValue(value, format, now);
    }

    /// <summary>Converts a validated singleton header to an interval.</summary>
    /// <param name="value">The raw header value.</param>
    /// <param name="format">The encoding.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The parsed interval, or null.</returns>
    private static TimeSpan? ParseValue(string value, RetryHeaderFormat format, DateTimeOffset now)
    {
        if (format == RetryHeaderFormat.RetryAfter)
        {
            return RetryConditionHeaderValue.TryParse(value, out var condition)
                ? condition.Delta ?? Until(condition.Date.GetValueOrDefault(), now)
                : null;
        }

        if (format == RetryHeaderFormat.Iso8601Date)
        {
            var utc = value.Length > 0 && value[^1] == 'Z';
            return DateTimeOffset.TryParseExact(
                value,
                utc ? IsoUtcTimestampFormat : IsoOffsetTimestampFormat,
                CultureInfo.InvariantCulture,
                utc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.None,
                out var date)
                ? Until(date, now)
                : null;
        }

        return ParseNumber(value, format, now);
    }

    /// <summary>Reads relative numeric intervals or Unix timestamps.</summary>
    /// <param name="value">The raw number.</param>
    /// <param name="format">The numeric encoding.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The interval, or null for invalid or overflowing numbers.</returns>
    private static TimeSpan? ParseNumber(string value, RetryHeaderFormat format, DateTimeOffset now)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        if (format == RetryHeaderFormat.UnixTimeSeconds)
        {
            return number <= MaxUnixTimeSeconds ? Until(DateTimeOffset.FromUnixTimeSeconds(number), now) : null;
        }

        var ticksPerUnit = format == RetryHeaderFormat.Milliseconds ? TimeSpan.TicksPerMillisecond : TimeSpan.TicksPerSecond;
        return number <= TimeSpan.MaxValue.Ticks / ticksPerUnit ? TimeSpan.FromTicks(number * ticksPerUnit) : null;
    }

    /// <summary>Reads a singleton header without treating commas in HTTP dates as separators.</summary>
    /// <param name="response">The response.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The trimmed single value, or null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? GetSingleHeader(HttpResponseMessage response, string name) =>
        HttpRetryCompatibility.GetSingleHeader(response, name);

    /// <summary>Checks GitHub's primary quota marker.</summary>
    /// <param name="response">The response.</param>
    /// <returns>Whether the remaining quota is zero.</returns>
    private static bool HasZeroRemaining(HttpResponseMessage response) =>
        GetSingleHeader(response, "x-ratelimit-remaining") == "0";

    /// <summary>Converts an absolute deadline to a nonnegative delay.</summary>
    /// <param name="date">The deadline.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The remaining interval.</returns>
    private static TimeSpan Until(DateTimeOffset date, DateTimeOffset now) =>
        date > now ? date - now : TimeSpan.Zero;
}
