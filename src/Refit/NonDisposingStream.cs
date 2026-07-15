// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>A <see cref="DelegatingStream"/> that never disposes the stream it wraps.</summary>
/// <remarks>
/// Refit disposes the <see cref="System.Net.Http.HttpRequestMessage"/> after every send, which disposes its
/// content and therefore any stream that content holds. A caller-supplied request-body stream is owned by the
/// caller, not Refit, so it is wrapped here: disposing the content disposes this wrapper without ever reaching
/// the caller's stream. Streams that Refit opens itself (for example <see cref="FileInfoPart"/>) are wrapped in a
/// plain owning <c>StreamContent</c> instead, so Refit still closes them.
/// </remarks>
/// <param name="innerStream">The caller-owned stream to wrap without taking ownership.</param>
internal sealed class NonDisposingStream(Stream innerStream)
    : DelegatingStream(innerStream, ownsInnerStream: false);
