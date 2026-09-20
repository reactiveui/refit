// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes how a method that returns <c>PagedEnumerable</c> reads items and the continuation from each page.</summary>
/// <param name="Kind">Whether a token parameter or a link identifies the next page.</param>
/// <param name="PageType">The fully qualified page type.</param>
/// <param name="ItemType">The fully qualified item type.</param>
/// <param name="TokenType">The fully qualified type of the token parameter, or an empty string for a link.</param>
/// <param name="TokenParameter">The name of the token parameter, or an empty string for a link.</param>
/// <param name="ItemsAccess">The member access appended to a page expression to read the items.</param>
/// <param name="NextSource">Where the continuation is read from.</param>
/// <param name="NextAccess">The member access appended to a page expression for a property source, the header name for a
/// header source, or the member access for the total for a total source.</param>
/// <param name="NextIsUri">Whether a link property holds a <c>Uri</c> rather than text.</param>
/// <param name="Origin">Which origins a followed link may point at.</param>
/// <param name="Origins">The allowed origins when <paramref name="Origin"/> is <see cref="PagingOrigin.Listed"/>.</param>
internal readonly record struct PagingModel(
    PagingKind Kind,
    string PageType,
    string ItemType,
    string TokenType,
    string TokenParameter,
    string ItemsAccess,
    PagingNextSource NextSource,
    string NextAccess,
    bool NextIsUri,
    PagingOrigin Origin,
    ImmutableEquatableArray<string> Origins);
