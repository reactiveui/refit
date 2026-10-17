// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the static headers and parameter bindings of a request.</summary>
/// <param name="StaticHeaders">The static headers parsed from inherited interfaces, the declaring interface, and the method.</param>
/// <param name="Parameters">The parsed request parameter bindings.</param>
internal readonly record struct RequestBindingsModel(
    HeaderModel[] StaticHeaders,
    RequestParameterModel[] Parameters)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(RequestBindingsModel other) =>
        GeneratorModelEquality.SequenceEquals(
            StaticHeaders,
            other.StaticHeaders)
        && GeneratorModelEquality.SequenceEquals(Parameters, other.Parameters);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(StaticHeaders));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Parameters));
        return hash;
    }
}
