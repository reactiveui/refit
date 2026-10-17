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
    string[] Constraints)
{
    /// <summary>Compares all parsed values, including array contents.</summary>
    /// <param name = "other">The model to compare.</param>
    /// <returns>Whether the models describe the same input.</returns>
    public bool Equals(TypeConstraint other) =>
        EqualityComparer<string>.Default.Equals(
            TypeName,
            other.TypeName)
        && EqualityComparer<string>.Default.Equals(
            DeclaredName,
            other.DeclaredName)
        && EqualityComparer<KnownTypeConstraint>.Default.Equals(
            KnownTypeConstraint,
            other.KnownTypeConstraint)
        && GeneratorModelEquality.SequenceEquals(Constraints, other.Constraints);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TypeName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(DeclaredName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(KnownTypeConstraint));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Constraints));
        return hash;
    }
}
