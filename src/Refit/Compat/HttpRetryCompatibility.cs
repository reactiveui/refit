// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Refit;

/// <summary>Adapts retry operations to the HTTP, timer and random APIs available on each target.</summary>
internal static class HttpRetryCompatibility
{
    /// <summary>The rate-limit status missing from older framework enum definitions.</summary>
    internal const HttpStatusCode TooManyRequests =
#if NET8_0_OR_GREATER
        HttpStatusCode.TooManyRequests;
#else
        (HttpStatusCode)429;
#endif

#if !NET6_0_OR_GREATER
    /// <summary>The exponential backoff multiplier.</summary>
    private const int BackoffMultiplier = 2;
#endif

    /// <summary>Waits using the supplied clock and cancellation token.</summary>
    /// <param name="delay">The wait interval.</param>
    /// <param name="timeProvider">The operation clock.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <returns>A task representing the wait.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task DelayAsync(TimeSpan delay, TimeProvider timeProvider, CancellationToken cancellationToken) =>
#if NET8_0_OR_GREATER
        Task.Delay(delay, timeProvider, cancellationToken);
#else
        timeProvider.Delay(delay, cancellationToken);
#endif

    /// <summary>Copies content into a buffer that checks the operation token on every write.</summary>
    /// <param name="content">The caller-owned content.</param>
    /// <param name="buffer">The bounded destination.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <returns>A task representing the copy.</returns>
    internal static async Task CopyContentAsync(HttpContent content, Stream buffer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
#else
        // Legacy HttpContent cannot cancel a serializer while it is waiting between writes.
        await content.CopyToAsync(buffer).ConfigureAwait(false);
#endif
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Copies request metadata without transferring content ownership.</summary>
    /// <param name="request">The original request.</param>
    /// <returns>A fresh request with the original metadata.</returns>
    internal static HttpRequestMessage CloneRequestMetadata(HttpRequestMessage request)
    {
        var copy = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
        {
            _ = copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

#if NET5_0_OR_GREATER
        copy.VersionPolicy = request.VersionPolicy;
        foreach (var option in request.Options)
        {
            copy.Options.Set(new(option.Key), option.Value);
        }
#else
        foreach (var property in request.Properties)
        {
            copy.Properties[property.Key] = property.Value;
        }
#endif
        return copy;
    }

    /// <summary>Reads a singleton header without splitting commas in HTTP dates.</summary>
    /// <param name="response">The response.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The trimmed header value, or null.</returns>
    internal static string? GetSingleHeader(HttpResponseMessage response, string name)
    {
#if NET6_0_OR_GREATER
        if (!response.Headers.NonValidated.TryGetValues(name, out var values))
        {
            return null;
        }

        return values.Count == 1 ? values.ToString().Trim() : null;
#else
        if (!response.Headers.TryGetValues(name, out var values))
        {
            return null;
        }

        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return null;
        }

        var value = enumerator.Current;
        return enumerator.MoveNext() ? null : value.Trim();
#endif
    }

    /// <summary>Produces a uniformly distributed sample from zero inclusive to one exclusive.</summary>
    /// <returns>The jitter sample.</returns>
    internal static double NextJitterSample()
    {
#if NET6_0_OR_GREATER
        return RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;
#else
        using var generator = RandomNumberGenerator.Create();
        var bytes = new byte[sizeof(uint)];
        generator.GetBytes(bytes);
        return BitConverter.ToUInt32(bytes, 0) / ((double)uint.MaxValue + 1);
#endif
    }

    /// <summary>Scales the initial backoff by a power of two, allowing overflow to be capped by the caller.</summary>
    /// <param name="milliseconds">The initial backoff.</param>
    /// <param name="attempt">The zero-based retry attempt.</param>
    /// <returns>The exponential interval in milliseconds.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ScaleBackoff(double milliseconds, int attempt) =>
#if NET6_0_OR_GREATER
        Math.ScaleB(milliseconds, attempt);
#else
        milliseconds * Math.Pow(BackoffMultiplier, attempt);
#endif
}
