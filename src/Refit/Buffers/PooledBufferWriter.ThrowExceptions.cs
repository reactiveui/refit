// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;

namespace Refit.Buffers;

/// <summary>A buffer writer that rents its backing storage from a shared array pool.</summary>
internal sealed partial class PooledBufferWriter
{
    /// <summary>Creates an <see cref="ArgumentOutOfRangeException"/> for a method that received a negative "count" parameter.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ArgumentOutOfRangeException CreateArgumentOutOfRangeExceptionForNegativeCount() =>
        new("count", "The count can't be < 0");

    /// <summary>Creates an <see cref="ArgumentOutOfRangeException"/> for a method that received a negative "offset" parameter.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ArgumentOutOfRangeException CreateArgumentOutOfRangeExceptionForNegativeOffset() =>
        new("offset", "The offset can't be < 0");

    /// <summary>Creates an <see cref="ArgumentOutOfRangeException"/> for when <see cref="Advance"/> advances too far.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ArgumentOutOfRangeException CreateArgumentOutOfRangeExceptionForAdvancedTooFar() =>
        new("count", "Advanced too far");

    /// <summary>Creates an <see cref="ArgumentException"/> for when the end of a <see cref="PooledMemoryStream"/> has been exceeded.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ArgumentException CreateArgumentOutOfRangeExceptionForEndOfStreamReached() =>
        new("The end of the stream has been exceeded");

    /// <summary>Creates an <see cref="ObjectDisposedException"/> for when a <see cref="PooledMemoryStream"/> method is called on a disposed instance.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ObjectDisposedException CreateObjectDisposedException() =>
        new("The stream in use has alreadybeen disposed");

    /// <summary>Creates a <see cref="NotSupportedException"/> for when an operation in <see cref="PooledMemoryStream"/> is not supported.</summary>
    /// <returns>The exception to throw.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static NotSupportedException CreateNotSupportedException() =>
        new("The stream doesn't support the requested operation");
}
