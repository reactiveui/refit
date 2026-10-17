// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes MethodAnnotationsModel metadata.</summary>
/// <param name="IsExplicitInterface">A value indicating whether the method is an explicit interface implementation.</param>
/// <param name="RequiresUnreferencedCode">Whether the interface method declares <c>[RequiresUnreferencedCode]</c>, which a reflection-fallback
/// implementation must mirror instead of suppressing IL2026.</param>
/// <param name="RequiresDynamicCode">Whether the interface method declares <c>[RequiresDynamicCode]</c>, which a reflection-fallback implementation must
/// mirror instead of suppressing IL3050.</param>
internal readonly record struct MethodAnnotationsModel(
    bool IsExplicitInterface,
    bool RequiresUnreferencedCode,
    bool RequiresDynamicCode);
