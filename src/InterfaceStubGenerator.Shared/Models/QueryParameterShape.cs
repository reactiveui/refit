// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies how a query-bound parameter renders into the query string.</summary>
internal enum QueryParameterShape
{
    /// <summary>A single <c>key=value</c> pair.</summary>
    Scalar,

    /// <summary>A collection expanded per the effective <c>CollectionFormat</c>.</summary>
    Collection,

    /// <summary>A single valueless flag (<c>?name</c>) from <c>[QueryName]</c>.</summary>
    Flag,

    /// <summary>A collection of valueless flags, one per element.</summary>
    FlagCollection,

    /// <summary>An object whose public readable properties are flattened into individual query pairs.</summary>
    Object,

    /// <summary>A dictionary whose entries become one query pair each, keyed by the formatted dictionary key.</summary>
    Dictionary,

    /// <summary>A value flattened by a user-supplied <c>IQueryConverter&lt;T&gt;</c> named with <c>[QueryConverter]</c>.</summary>
    Converter,

    /// <summary>A collection of objects whose properties are flattened with an indexed key prefix:
    /// <c>key[0].Prop=val&amp;key[1].Prop=val</c>, matching <c>CollectionFormat.Indexed</c>.</summary>
    IndexedCollection
}
