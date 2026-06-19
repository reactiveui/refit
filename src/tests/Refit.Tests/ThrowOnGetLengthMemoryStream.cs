// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace Refit.Tests;

/// <summary>A <see cref="MemoryStream"/> that can be toggled to behave like a non-seekable stream whose length is unavailable.</summary>
public sealed class ThrowOnGetLengthMemoryStream : MemoryStream
{
    /// <summary>Gets or sets a value indicating whether the stream reports as seekable and exposes its length.</summary>
    public bool CanGetLength { get; set; }

    /// <inheritdoc/>
    public override bool CanSeek => CanGetLength;

    /// <inheritdoc/>
    public override long Length => CanGetLength ? base.Length : throw new NotSupportedException();
}
