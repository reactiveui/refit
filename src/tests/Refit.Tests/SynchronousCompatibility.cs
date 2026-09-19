// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;

namespace Refit.Tests;

/// <summary>Keeps synchronous compatibility execution separate from asynchronous setup and assertions.</summary>
internal static class SynchronousCompatibility
{
    /// <summary>Exercises the synchronous validation factory instead of substituting its async counterpart.</summary>
    /// <param name="exception">The buffered HTTP exception to convert.</param>
    /// <returns>The validation exception built by the synchronous API.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValidationApiException CreateValidation(ApiException exception) => ValidationApiException.Create(exception);

    /// <summary>Exercises the byte-array synchronous stream override.</summary>
    /// <param name="stream">The detached pooled stream.</param>
    /// <param name="buffer">The complete destination buffer.</param>
    /// <returns>The number of bytes synchronously read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadArray(Stream stream, byte[] buffer) => stream.Read(buffer, 0, buffer.Length);

    /// <summary>Exercises the synchronous flush override.</summary>
    /// <param name="stream">The detached pooled stream.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Flush(Stream stream) => stream.Flush();
}
