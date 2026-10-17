// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Carries alias and type classification caches for an interface parsing pass.</summary>
/// <param name="ExternAliases">The per-interface collector recording the extern aliases used while qualifying its types.</param>
/// <param name="AssemblyAliasCache">A pass-wide cache mapping an assembly symbol to its resolved extern alias (or null), shared across every interface so
/// the extern-alias metadata-reference lookup runs once per assembly, not per type node.</param>
/// <param name="QualifiedTypeCache">A pass-wide cache mapping a type symbol to its fully-qualified display string for the common non-extern-aliased case,
/// shared across every interface so <c>ToDisplayString</c> renders each distinct type once per pass rather than once per occurrence. The aliased path is
/// never cached because it records used aliases as a side effect.</param>
/// <param name="FormattableClassificationCache">A pass-wide cache mapping a value type symbol to whether it implements <c>IFormattable</c> and
/// <c>ISpanFormattable</c>, shared across every interface so the interface walk (which materializes a fresh public <c>AllInterfaces</c> array on each
/// access) runs once per distinct type rather than once per parameter occurrence.</param>
internal readonly record struct GenerationCachesModel(
    HashSet<string> ExternAliases,
    Dictionary<ISymbol, string?> AssemblyAliasCache,
    Dictionary<ISymbol, string> QualifiedTypeCache,
    Dictionary<ISymbol, (bool Formattable, bool SpanFormattable)> FormattableClassificationCache);
