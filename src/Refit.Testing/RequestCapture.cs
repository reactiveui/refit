// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Testing;

/// <summary>Controls how a <see cref="StubHttp"/> captures request bodies. Assign one to <see cref="StubHttp.RequestCapture"/>.</summary>
/// <remarks>
/// <para>
/// <see cref="Full"/> is the default. The handler reads the whole body before matching, so body matchers and
/// <see cref="StubHttp.LastRequestBodyAsync{T}"/> work, but a streaming upload is buffered before any reply code runs.
/// </para>
/// <para>
/// <see cref="None"/> and <see cref="Bounded(int)"/> leave the body unread. The reply code (for example a responder passed
/// to <c>Reply.From</c>) consumes it at its own pace. <see cref="Bounded(int)"/> also records up to a byte limit as the body is read, for typed
/// inspection afterwards. Routes with <see cref="RouteMatcher.Body"/> or <see cref="RouteMatcher.FormData"/> need
/// <see cref="Full"/>, because matching would otherwise consume the body.
/// </para>
/// </remarks>
[System.Diagnostics.DebuggerDisplay("Enabled = {IsEnabled}, MaxBytes = {MaxBytes}")]
public sealed class RequestCapture
{
    /// <summary>Initializes a new instance of the <see cref="RequestCapture"/> class.</summary>
    /// <param name="isEnabled">Whether bodies are recorded.</param>
    /// <param name="maxBytes">The recording limit, or <see langword="null"/> to buffer the whole body up front.</param>
    private RequestCapture(bool isEnabled, int? maxBytes)
    {
        IsEnabled = isEnabled;
        MaxBytes = maxBytes;
    }

    /// <summary>Gets the default policy: read and buffer every request body before matching.</summary>
    public static RequestCapture Full { get; } = new(true, null);

    /// <summary>Gets a policy that never reads or records request bodies.</summary>
    public static RequestCapture None { get; } = new(false, null);

    /// <summary>Gets a value indicating whether request bodies are recorded for typed inspection.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets the recording limit in bytes for <see cref="Bounded(int)"/>, or <see langword="null"/> for <see cref="Full"/> and <see cref="None"/>.</summary>
    public int? MaxBytes { get; }

    /// <summary>Gets a value indicating whether the body is buffered before matching, as <see cref="Full"/> does.</summary>
    internal bool BuffersBody => IsEnabled && MaxBytes is null;

    /// <summary>
    /// Creates a policy that leaves the body for the reply code to read and records at most <paramref name="maxBytes"/>
    /// bytes as it is read. A body larger than the limit is marked truncated rather than buffered.
    /// </summary>
    /// <param name="maxBytes">The most bytes to record per request.</param>
    /// <returns>A bounded capture policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxBytes"/> is negative.</exception>
    public static RequestCapture Bounded(int maxBytes)
    {
        ArgumentOutOfRangeExceptionHelper.ThrowIfNegative(maxBytes);
        return new(true, maxBytes);
    }
}
