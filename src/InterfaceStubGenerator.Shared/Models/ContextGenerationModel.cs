// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Model describing the shared generation context for a set of Refit interfaces.</summary>
/// <param name="RefitInternalNamespace">The namespace used for Refit internal generated types.</param>
/// <param name="PreserveAttributeDisplayName">The display name of the preserve attribute.</param>
/// <param name="GeneratedClassName">The assembly-scoped name of the generated implementation container type.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
/// <param name="Interfaces">The interfaces to generate implementations for.</param>
internal sealed record ContextGenerationModel(
    string RefitInternalNamespace,
    string PreserveAttributeDisplayName,
    string GeneratedClassName,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers,
    InterfaceModel[] Interfaces)
{
    /// <summary>Compares all parsed values, including array contents.</summary>
    /// <param name = "other">The model to compare.</param>
    /// <returns>Whether the models describe the same input.</returns>
    public bool Equals(ContextGenerationModel? other) =>
        other is not null
        && EqualityComparer<string>.Default.Equals(
            RefitInternalNamespace,
            other.RefitInternalNamespace)
        && EqualityComparer<string>.Default.Equals(
            PreserveAttributeDisplayName,
            other.PreserveAttributeDisplayName)
        && EqualityComparer<string>.Default.Equals(
            GeneratedClassName,
            other.GeneratedClassName)
        && EqualityComparer<bool>.Default.Equals(
            GeneratedRequestBuilding,
            other.GeneratedRequestBuilding)
        && EqualityComparer<bool>.Default.Equals(
            EmitGeneratedCodeMarkers,
            other.EmitGeneratedCodeMarkers)
        && GeneratorModelEquality.SequenceEquals(Interfaces, other.Interfaces);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(RefitInternalNamespace));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(PreserveAttributeDisplayName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(GeneratedClassName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(GeneratedRequestBuilding));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(EmitGeneratedCodeMarkers));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Interfaces));
        return hash;
    }
}
