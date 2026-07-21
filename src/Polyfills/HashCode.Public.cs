// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK
namespace System;

/// <summary>Provides the object hash-code contract for the .NET Framework polyfill.</summary>
internal ref partial struct HashCode
{
    /// <inheritdoc/>
    public override readonly int GetHashCode() => ToHashCode();
}
#endif
