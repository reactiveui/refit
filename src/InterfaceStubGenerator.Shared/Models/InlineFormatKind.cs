// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies how generated code formats a statically-known value without reflection.</summary>
internal enum InlineFormatKind
{
    /// <summary>No reflection-free rendering is provable; always call the configured URL parameter formatter.</summary>
    FormatterOnly,

    /// <summary>The value is a string and is used as-is.</summary>
    String,

    /// <summary>The value renders via <c>ToString()</c> (bool, char, and non-formattable values), ignoring any format.</summary>
    ToStringOnly,

    /// <summary>The value renders via <c>GeneratedRequestRunner.FormatInvariant</c> with the compile-time format.</summary>
    Formattable,

    /// <summary>The value renders through a generated per-enum switch that resolves member names at compile time.</summary>
    Enum
}
