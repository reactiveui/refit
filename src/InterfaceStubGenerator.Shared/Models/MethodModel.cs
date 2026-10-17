// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the parsed metadata for MethodModel.</summary>
/// <param name="Name">The Name metadata.</param>
/// <param name="ReturnType">The ReturnType metadata.</param>
/// <param name="ContainingType">The ContainingType metadata.</param>
/// <param name="DeclaredMethod">The DeclaredMethod metadata.</param>
/// <param name="ReturnTypeMetadata">The ReturnTypeMetadata metadata.</param>
/// <param name="Request">The Request metadata.</param>
/// <param name="Parameters">The Parameters metadata.</param>
/// <param name="Constraints">The Constraints metadata.</param>
/// <param name="Annotations">The Annotations metadata.</param>
internal readonly record struct MethodModel(
    string Name,
    string ReturnType,
    string ContainingType,
    string DeclaredMethod,
    ReturnTypeInfo ReturnTypeMetadata,
    RequestModel Request,
    ParameterModel[] Parameters,
    TypeConstraint[] Constraints,
    MethodAnnotationsModel Annotations)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(MethodModel other) =>
        EqualityComparer<string>.Default.Equals(
            Name,
            other.Name)
        && EqualityComparer<string>.Default.Equals(
            ReturnType,
            other.ReturnType)
        && EqualityComparer<string>.Default.Equals(
            ContainingType,
            other.ContainingType)
        && EqualityComparer<string>.Default.Equals(
            DeclaredMethod,
            other.DeclaredMethod)
        && EqualityComparer<ReturnTypeInfo>.Default.Equals(
            ReturnTypeMetadata,
            other.ReturnTypeMetadata)
        && EqualityComparer<RequestModel>.Default.Equals(
            Request,
            other.Request)
        && GeneratorModelEquality.SequenceEquals(
            Parameters,
            other.Parameters)
        && GeneratorModelEquality.SequenceEquals(
            Constraints,
            other.Constraints)
        && EqualityComparer<MethodAnnotationsModel>.Default.Equals(Annotations, other.Annotations);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Name));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ReturnType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ContainingType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(DeclaredMethod));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ReturnTypeMetadata));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Request));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Parameters));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Constraints));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Annotations));
        return hash;
    }
}
