// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Names sample inputs whose values matter to the rendered output.</summary>
internal static class SampleValues
{
    /// <summary>The integer rendered as 012 by the D3 format.</summary>
    internal const int Count = 12;

    /// <summary>The integer used in a route and typed request option.</summary>
    internal const int Identifier = 42;

    /// <summary>The query's row-count input.</summary>
    internal const int Rows = 25;

    /// <summary>The integer element added to a multi-valued query.</summary>
    internal const int Element = 7;

    /// <summary>The final value in the short streaming payload.</summary>
    internal const int LastElement = 2;

    /// <summary>The end-exclusive offset of the id placeholder.</summary>
    internal const int PlaceholderEnd = 11;

    /// <summary>The start offset of the id placeholder.</summary>
    internal const int PlaceholderStart = 7;

    /// <summary>The timeout used for a local handler that responds immediately.</summary>
    internal const int TimeoutMilliseconds = 5000;

    /// <summary>The length and expected sum of the short demonstration payloads.</summary>
    internal const int PayloadLength = 3;

    /// <summary>The integers reused by JSON Lines and collection formatting.</summary>
    internal static readonly ImmutableArray<int> Items = [1, LastElement];

    /// <summary>Gets the caller-stream contents without exposing a shared mutable array.</summary>
    internal static ReadOnlySpan<byte> StreamBytes => [1, LastElement, PayloadLength];
}
