// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Applies a per-call timeout, in milliseconds, to a Refit interface method.</summary>
/// <remarks>
/// The timeout is layered on top of the request's effective cancellation token: when it elapses the request is
/// canceled, surfacing as an <see cref="OperationCanceledException"/> (typically a
/// <see cref="System.Threading.Tasks.TaskCanceledException"/>), consistent with how
/// <see cref="HttpClient.Timeout"/> reports a lapsed deadline. It composes with, and is independent
/// of, <see cref="HttpClient.Timeout"/> and any handler-based timeout: whichever fires first cancels
/// the call. A value that is not positive is ignored, leaving the request without a per-call deadline.
/// </remarks>
/// <param name="milliseconds">The timeout, in milliseconds. Must be positive to take effect.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TimeoutAttribute(int milliseconds) : Attribute
{
    /// <summary>Gets the timeout, in milliseconds, applied to the decorated method's request.</summary>
    public int Milliseconds { get; } = milliseconds;
}
