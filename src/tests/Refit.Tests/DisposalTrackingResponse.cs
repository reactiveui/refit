// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>A response that records whether it has been disposed.</summary>
/// <param name="statusCode">The response status.</param>
internal sealed class DisposalTrackingResponse(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
{
    /// <summary>Gets a value indicating whether the response was disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
