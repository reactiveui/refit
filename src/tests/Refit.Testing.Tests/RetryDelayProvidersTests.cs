// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>Checks server retry intervals without real-time waiting.</summary>
public class RetryDelayProvidersTests
{
    /// <summary>The standard retry header.</summary>
    private const string RetryAfterHeader = "Retry-After";

    /// <summary>The current time relative to the Unix epoch in seconds.</summary>
    private const int CurrentSeconds = 10;

    /// <summary>The Azure retry interval in milliseconds.</summary>
    private const int AzureMilliseconds = 1500;

    /// <summary>The ambiguous duplicate intervals.</summary>
    private static readonly string[] DuplicateHints = ["1", "2"];

    /// <summary>Verifies standard delta seconds, dates, and invalid values.</summary>
    /// <param name="value">The response header.</param>
    /// <param name="seconds">The expected delay, or null for an invalid header.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("12", 12)]
    [Arguments("0", 0)]
    [Arguments("-1", null)]
    [Arguments("1.5", null)]
    [Arguments("garbage", null)]
    [Arguments("999999999999999999999999", null)]
    [Arguments("Thu, 01 Jan 1970 00:00:20 GMT", 10)]
    [Arguments("Thu, 01 Jan 1970 00:00:00 GMT", 0)]
    public async Task RetryAfter_ParsesValidIntervals(string value, int? seconds, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, value);

        var delay = RetryDelayProviders.RetryAfter(response, DateTimeOffset.UnixEpoch.AddSeconds(CurrentSeconds), cancellationToken);

        await Assert.That(delay?.TotalSeconds).IsEqualTo(seconds is null ? null : (double?)seconds);
    }

    /// <summary>Verifies Azure millisecond hints take priority over standard hints.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Azure_PrefersMilliseconds(CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        _ = response.Headers.TryAddWithoutValidation("x-ms-retry-after-ms", "1500");
        _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, "10");

        var delay = RetryDelayProviders.Azure(response, DateTimeOffset.UnixEpoch, cancellationToken);

        await Assert.That(delay).IsEqualTo(TimeSpan.FromMilliseconds(AzureMilliseconds));
    }

    /// <summary>Verifies configurable header encodings reject invalid and overflowing values.</summary>
    /// <param name="format">The header encoding.</param>
    /// <param name="value">The raw value.</param>
    /// <param name="milliseconds">The expected interval, or null.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(RetryHeaderFormat.Seconds, " 2 ", 2000L)]
    [Arguments(RetryHeaderFormat.Milliseconds, "1500", 1500L)]
    [Arguments(RetryHeaderFormat.Milliseconds, "0", 0L)]
    [Arguments(RetryHeaderFormat.Milliseconds, "-2", null)]
    [Arguments(RetryHeaderFormat.Milliseconds, "1.5", null)]
    [Arguments(RetryHeaderFormat.Seconds, "9223372036854775807", null)]
    [Arguments(RetryHeaderFormat.Milliseconds, "9223372036854775807", null)]
    [Arguments(RetryHeaderFormat.UnixTimeSeconds, "20", 10_000L)]
    [Arguments(RetryHeaderFormat.UnixTimeSeconds, "0", 0L)]
    [Arguments(RetryHeaderFormat.UnixTimeSeconds, "253402300800", null)]
    [Arguments(RetryHeaderFormat.UnixTimeSeconds, "253402300799", 253_402_300_789_000L)]
    [Arguments(RetryHeaderFormat.Iso8601Date, "1970-01-01T00:00:20Z", 10_000L)]
    [Arguments(RetryHeaderFormat.Iso8601Date, "1970-01-01T01:00:20+01:00", 10_000L)]
    [Arguments(RetryHeaderFormat.Iso8601Date, "1970-01-01T00:00:00.000Z", 0L)]
    [Arguments(RetryHeaderFormat.Iso8601Date, "1970-01-01T00:00:20", null)]
    [Arguments(RetryHeaderFormat.Iso8601Date, "tomorrow", null)]
    public async Task ReadHeader_ValidatesEncoding(RetryHeaderFormat format, string value, long? milliseconds, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage();
        _ = response.Headers.TryAddWithoutValidation("X-Retry", value);

        var delay = RetryDelayProviders.ReadHeader(response, "X-Retry", format, DateTimeOffset.UnixEpoch.AddSeconds(CurrentSeconds), cancellationToken);

        await Assert.That(delay?.TotalMilliseconds).IsEqualTo(milliseconds is null ? null : (double?)milliseconds);
    }

    /// <summary>Verifies missing, empty and duplicate headers do not supply a retry interval.</summary>
    /// <param name="kind">The header shape to reject.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("missing")]
    [Arguments("empty")]
    [Arguments("duplicates")]
    [Arguments("combined")]
    public async Task ReadHeader_RejectsAmbiguousHints(string kind, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage();
        if (kind == "empty")
        {
            _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, []);
        }
        else if (kind == "duplicates")
        {
            _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, DuplicateHints);
        }
        else if (kind == "combined")
        {
            _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, "1, 2");
        }

        await Assert.That(RetryDelayProviders.RetryAfter(response, DateTimeOffset.UnixEpoch, cancellationToken)).IsNull();
    }

    /// <summary>Verifies providers fall back to standard Retry-After.</summary>
    /// <param name="provider">The adapter name.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Azure")]
    [Arguments("Aws")]
    [Arguments("GitHub")]
    [Arguments("Jira")]
    public async Task Providers_FallBackToStandardHeader(string provider, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        _ = response.Headers.TryAddWithoutValidation(RetryAfterHeader, "1");

        var reader = GetProvider(provider);

        await Assert.That(reader(response, DateTimeOffset.UnixEpoch, cancellationToken)).IsEqualTo(TimeSpan.FromSeconds(1));
    }

    /// <summary>Verifies provider-specific headers and absent fallback hints.</summary>
    /// <param name="provider">The adapter name.</param>
    /// <param name="header">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <param name="milliseconds">The expected delay, or null.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Azure", "retry-after-ms", "1500", 1500)]
    [Arguments("Azure", "x-ms-retry-after-ms", "bad", null)]
    [Arguments("Aws", "x-amz-retry-after", "1500", 1500)]
    [Arguments("Aws", "x-amz-retry-after", "bad", null)]
    [Arguments("Jira", "X-RateLimit-Reset", "1970-01-01T00:00:20Z", 10_000)]
    [Arguments("Jira", "Other", "1", null)]
    public async Task Providers_ReadTheirOwnFormats(string provider, string header, string value, int? milliseconds, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        _ = response.Headers.TryAddWithoutValidation(header, value);

        var reader = GetProvider(provider);
        var delay = reader(response, DateTimeOffset.UnixEpoch.AddSeconds(CurrentSeconds), cancellationToken);

        await Assert.That(delay?.TotalMilliseconds).IsEqualTo(milliseconds is null ? null : (double?)milliseconds);
    }

    /// <summary>Verifies quota timestamps do not turn ordinary successful or forbidden responses into throttling hints.</summary>
    /// <param name="status">The HTTP status.</param>
    /// <param name="remaining">The quota marker.</param>
    /// <param name="hasDelay">Whether the reset timestamp should apply.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(HttpStatusCode.Forbidden, "0", true)]
    [Arguments(HttpStatusCode.TooManyRequests, "0", true)]
    [Arguments(HttpStatusCode.Forbidden, "1", false)]
    [Arguments(HttpStatusCode.Forbidden, null, false)]
    [Arguments(HttpStatusCode.OK, "0", false)]
    public async Task GitHub_RequiresExhaustedQuota(HttpStatusCode status, string? remaining, bool hasDelay, CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(status);
        _ = response.Headers.TryAddWithoutValidation("x-ratelimit-reset", "20");
        if (remaining is not null)
        {
            _ = response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", remaining);
        }

        var delay = RetryDelayProviders.GitHub(response, DateTimeOffset.UnixEpoch.AddSeconds(CurrentSeconds), cancellationToken);

        await Assert.That(delay.HasValue).IsEqualTo(hasDelay);
    }

    /// <summary>Verifies Jira ignores reset metadata outside throttled responses.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Jira_IgnoresUnthrottledReset(CancellationToken cancellationToken)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        _ = response.Headers.TryAddWithoutValidation("X-RateLimit-Reset", "1970-01-01T00:00:20Z");

        await Assert.That(RetryDelayProviders.Jira(response, DateTimeOffset.UnixEpoch, cancellationToken)).IsNull();
    }

    /// <summary>Verifies header readers guard arguments and honor cancellation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadHeader_GuardsArgumentsAndCancellation()
    {
        using var response = new HttpResponseMessage();
        using (Assert.Multiple())
        {
            await Assert.That(static () => RetryDelayProviders.ReadHeader(null!, "X", RetryHeaderFormat.Seconds, DateTimeOffset.UnixEpoch, CancellationToken.None)).Throws<ArgumentNullException>();
            await Assert.That(() => RetryDelayProviders.ReadHeader(response, string.Empty, RetryHeaderFormat.Seconds, DateTimeOffset.UnixEpoch, CancellationToken.None)).Throws<ArgumentException>();
            await Assert.That(() => RetryDelayProviders.ReadHeader(response, "X", (RetryHeaderFormat)(-1), DateTimeOffset.UnixEpoch, CancellationToken.None)).Throws<ArgumentOutOfRangeException>();
            await Assert.That(() => RetryDelayProviders.ReadHeader(response, "X", RetryHeaderFormat.Seconds, DateTimeOffset.UnixEpoch, new(true))).Throws<OperationCanceledException>();
        }
    }

    /// <summary>Selects the provider adapter for a data-driven test.</summary>
    /// <param name="name">The provider name.</param>
    /// <returns>The delay reader.</returns>
    /// <exception cref="ArgumentException">The provider name is not recognized.</exception>
    private static Func<HttpResponseMessage, DateTimeOffset, CancellationToken, TimeSpan?> GetProvider(string name) =>
        name switch
        {
            "Azure" => RetryDelayProviders.Azure,
            "Aws" => RetryDelayProviders.Aws,
            "GitHub" => RetryDelayProviders.GitHub,
            "Jira" => RetryDelayProviders.Jira,
            _ => throw new ArgumentException("Unknown provider.", nameof(name)),
        };
}
