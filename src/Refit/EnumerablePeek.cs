// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Describes the cardinality observed when peeking a sequence.</summary>
internal enum EnumerablePeek
{
    /// <summary>The sequence contained no elements.</summary>
    Empty,

    /// <summary>The sequence contained exactly one element.</summary>
    Single,

    /// <summary>The sequence contained more than one element.</summary>
    Many
}
