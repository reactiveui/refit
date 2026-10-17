// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the parsed metadata for PagingModel.</summary>
/// <param name="Kind">The Kind metadata.</param>
/// <param name="PageType">The PageType metadata.</param>
/// <param name="ItemType">The ItemType metadata.</param>
/// <param name="TokenType">The TokenType metadata.</param>
/// <param name="TokenParameter">The TokenParameter metadata.</param>
/// <param name="ItemsAccess">The ItemsAccess metadata.</param>
/// <param name="Continuation">The Continuation metadata.</param>
internal readonly record struct PagingModel(
    PagingKind Kind,
    string PageType,
    string ItemType,
    string TokenType,
    string TokenParameter,
    string ItemsAccess,
    PagingContinuationModel Continuation)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(PagingModel other) =>
        EqualityComparer<PagingKind>.Default.Equals(
            Kind,
            other.Kind)
        && EqualityComparer<string>.Default.Equals(
            PageType,
            other.PageType)
        && EqualityComparer<string>.Default.Equals(
            ItemType,
            other.ItemType)
        && EqualityComparer<string>.Default.Equals(
            TokenType,
            other.TokenType)
        && EqualityComparer<string>.Default.Equals(
            TokenParameter,
            other.TokenParameter)
        && EqualityComparer<string>.Default.Equals(
            ItemsAccess,
            other.ItemsAccess)
        && EqualityComparer<PagingContinuationModel>.Default.Equals(Continuation, other.Continuation);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Kind));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(PageType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ItemType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TokenType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TokenParameter));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ItemsAccess));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Continuation));
        return hash;
    }
}
