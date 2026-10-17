// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.CompilerServices;

namespace Refit.Testing.Tests;

/// <summary>Tests the legacy content serialization hook and capture stream capabilities.</summary>
public sealed class CapturingContentTests
{
    /// <summary>Verifies the legacy serialization hook records the body without owning its copy target.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SerializeToStreamAsync_LegacyHook_RecordsBodyAndLeavesTargetOpen(CancellationToken cancellationToken)
    {
        const string Body = "legacy payload";
        using var inner = new StringContent(Body);
        var capture = new CapturedRequestBody(null, null);
        using var content = new CapturingContent(inner, capture);
        await using var target = new MemoryStream();

        // Modern HttpContent dispatches to the cancellation-aware hook even for CopyToAsync(Stream).
        await SerializeToStreamAsync(content, target, null);

        await Assert.That(capture.GetText()).IsEqualTo(Body);
        await Assert.That(System.Text.Encoding.UTF8.GetString(target.ToArray())).IsEqualTo(Body);
        await target.WriteAsync("!"u8.ToArray(), cancellationToken);
        await Assert.That(target.CanWrite).IsTrue();
    }

    /// <summary>Verifies capture streams never expose seeking even for a seekable underlying stream.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CaptureStream_SeekableInner_ReportsNonSeekable()
    {
        await using var inner = new MemoryStream();
        await using var stream = new CapturingContent.CaptureStream(inner, new CapturedRequestBody(null, null), ownsInner: false);

        await Assert.That(inner.CanSeek).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
    }

    /// <summary>Invokes the non-cancellation HttpContent serialization hook.</summary>
    /// <param name="content">The capturing content.</param>
    /// <param name="stream">The caller-owned copy target.</param>
    /// <param name="context">The transport context.</param>
    /// <returns>The serialization task.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SerializeToStreamAsync")]
    private static extern Task SerializeToStreamAsync(CapturingContent content, Stream stream, TransportContext? context);
}
