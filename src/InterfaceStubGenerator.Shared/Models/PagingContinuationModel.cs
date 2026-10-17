// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes PagingContinuationModel metadata.</summary>
/// <param name="NextSource">Where the continuation is read from.</param>
/// <param name="NextAccess">The member access appended to a page expression for a property source, the header name for a header source, or the member
/// access for the total for a total source.</param>
/// <param name="NextIsUri">Whether a link property holds a <c>Uri</c> rather than text.</param>
/// <param name="Origin">Which origins a followed link may point at.</param>
/// <param name="Origins">The allowed origins when <paramref name="Origin"/> is <see cref="PagingOrigin.Listed"/>.</param>
internal readonly record struct PagingContinuationModel(
    PagingNextSource NextSource,
    string NextAccess,
    bool NextIsUri,
    PagingOrigin Origin,
    string[] Origins)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(PagingContinuationModel other) =>
        EqualityComparer<PagingNextSource>.Default.Equals(
            NextSource,
            other.NextSource)
        && EqualityComparer<string>.Default.Equals(
            NextAccess,
            other.NextAccess)
        && EqualityComparer<bool>.Default.Equals(
            NextIsUri,
            other.NextIsUri)
        && EqualityComparer<PagingOrigin>.Default.Equals(
            Origin,
            other.Origin)
        && GeneratorModelEquality.SequenceEquals(Origins, other.Origins);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(NextSource));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(NextAccess));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(NextIsUri));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Origin));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Origins));
        return hash;
    }
}
