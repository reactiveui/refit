// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the interface's members metadata.</summary>
/// <param name="Constraints">The generic type constraints of the interface.</param>
/// <param name="MemberNames">The names of the interface members.</param>
/// <param name="Properties">The interface properties implemented by the generated stub.</param>
/// <param name="NonRefitMethods">The non-Refit methods declared on the interface.</param>
/// <param name="RefitMethods">The Refit methods declared on the interface.</param>
/// <param name="DerivedRefitMethods">The Refit methods inherited from base interfaces.</param>
internal sealed record InterfaceMembersModel(
    TypeConstraint[] Constraints,
    string[] MemberNames,
    InterfacePropertyModel[] Properties,
    MethodModel[] NonRefitMethods,
    MethodModel[] RefitMethods,
    MethodModel[] DerivedRefitMethods)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(InterfaceMembersModel? other) =>
        other is not null
        && GeneratorModelEquality.SequenceEquals(
            Constraints,
            other.Constraints)
        && GeneratorModelEquality.SequenceEquals(
            MemberNames,
            other.MemberNames)
        && GeneratorModelEquality.SequenceEquals(
            Properties,
            other.Properties)
        && GeneratorModelEquality.SequenceEquals(
            NonRefitMethods,
            other.NonRefitMethods)
        && GeneratorModelEquality.SequenceEquals(
            RefitMethods,
            other.RefitMethods)
        && GeneratorModelEquality.SequenceEquals(DerivedRefitMethods, other.DerivedRefitMethods);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Constraints));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(MemberNames));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Properties));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(NonRefitMethods));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(RefitMethods));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(DerivedRefitMethods));
        return hash;
    }
}
