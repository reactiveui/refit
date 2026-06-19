// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes the constraints applied to a generic type parameter.</summary>
/// <param name="TypeName">The fully qualified type parameter name.</param>
/// <param name="DeclaredName">The declared name of the type parameter.</param>
/// <param name="KnownTypeConstraint">The well-known constraints applied to the type parameter.</param>
/// <param name="Constraints">The additional textual constraints applied to the type parameter.</param>
internal readonly record struct TypeConstraint(
    string TypeName,
    string DeclaredName,
    KnownTypeConstraint KnownTypeConstraint,
    ImmutableEquatableArray<string> Constraints);
