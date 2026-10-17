// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes a Refit interface and its generated implementation.</summary>
/// <param name="FileName">The generated file name.</param>
/// <param name="Names">The generated identifiers and display names.</param>
/// <param name="Options">The consumer syntax and generation options.</param>
/// <param name="Members">The members implemented by the stub.</param>
/// <param name="Nullability">The interface's nullable context.</param>
/// <param name="DisposeMethod">Whether the stub implements disposal.</param>
/// <param name="ExternAliases">The aliases required by the interface's types.</param>
internal sealed record InterfaceModel(
    string FileName,
    InterfaceNamesModel Names,
    InterfaceOptionsModel Options,
    InterfaceMembersModel Members,
    Nullability Nullability,
    bool DisposeMethod,
    string[] ExternAliases)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(InterfaceModel? other) =>
        other is not null
        && FileName == other.FileName
        && Names == other.Names
        && Options == other.Options
        && Members == other.Members
        && Nullability == other.Nullability
        && DisposeMethod == other.DisposeMethod
        && GeneratorModelEquality.SequenceEquals(ExternAliases, other.ExternAliases);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(FileName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Names));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Options));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Members));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Nullability));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(DisposeMethod));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(ExternAliases));
        return hash;
    }
}
