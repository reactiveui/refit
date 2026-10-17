// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the APIs and syntax supported by the consumer's target.</summary>
/// <param name="SupportsSpanEscape">Whether the target exposes <c>Uri.EscapeDataString(ReadOnlySpan&lt;char&gt;)</c> (net9+), enabling the escaping span
/// fast path.</param>
/// <param name="SupportsNullable">Whether the compilation supports nullable reference types.</param>
/// <param name="SupportsStaticLambdas">Whether the compilation supports the <c>static</c> lambda modifier (C# 9).</param>
/// <param name="SupportsCollectionExpressions">Whether the compilation supports collection expressions (C# 12), letting the path builder receive its
/// replacements as a stack-allocatable <c>[...]</c> span instead of an array.</param>
internal readonly record struct GenerationCapabilitiesModel(
    bool SupportsSpanEscape,
    bool SupportsNullable,
    bool SupportsStaticLambdas,
    bool SupportsCollectionExpressions);
