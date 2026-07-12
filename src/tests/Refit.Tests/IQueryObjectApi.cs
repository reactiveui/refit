// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose query objects are flattened inline by the source generator.</summary>
public interface IQueryObjectApi
{
    /// <summary>Flattens an explicitly marked query object.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/obj")]
    Task<string> FlattenObject([Query] SealedQueryObject query);

    /// <summary>Flattens a query object with no <c>[Query]</c> attribute on a body-less method.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/implied")]
    Task<string> FlattenImplied(SealedQueryObject query);

    /// <summary>Flattens a query object whose properties carry their own prefixes.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/prefixed")]
    Task<string> FlattenPrefixed(PrefixedSealedQueryObject query);

    /// <summary>Flattens a value-typed query object.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/struct")]
    Task<string> FlattenStruct(StructQueryObject query);

    /// <summary>Flattens a value-typed query object under a parameter-level prefix.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/paramprefix")]
    Task<string> FlattenParameterPrefix([Query(".", "root")] StructQueryObject query);

    /// <summary>Flattens a base-typed query object, whose declared properties are the only ones emitted.</summary>
    /// <param name="query">The query object, which may be a derived instance.</param>
    /// <returns>The response body.</returns>
    [Get("/declared")]
    Task<string> FlattenDeclared(BaseRecord query);

    /// <summary>Flattens a query object whose keys resolve through the serializer-aware naming precedence.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/json")]
    Task<string> FlattenJsonNamed([Query] JsonNamedQueryObject query);

    /// <summary>Flattens a query object whose properties are collections of simple elements.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/coll")]
    Task<string> FlattenCollections([Query] CollectionPropertyQueryObject query);

    /// <summary>Flattens a query object with a nested object property under a dotted key.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/nested")]
    Task<string> FlattenNested([Query] NestedQueryObject query);

    /// <summary>Expands a dictionary into one query pair per entry.</summary>
    /// <param name="query">The dictionary.</param>
    /// <returns>The response body.</returns>
    [Get("/dict")]
    Task<string> ExpandDictionary(IDictionary<string, string> query);

    /// <summary>Expands a dictionary whose keys are enums rendered through their <c>EnumMember</c> value.</summary>
    /// <param name="query">The dictionary.</param>
    /// <returns>The response body.</returns>
    [Get("/dict/enum")]
    Task<string> ExpandEnumKeyedDictionary(Dictionary<QuerySort, int> query);

    /// <summary>Expands a dictionary under a parameter-level prefix.</summary>
    /// <param name="query">The dictionary.</param>
    /// <returns>The response body.</returns>
    [Get("/dict/prefixed")]
    Task<string> ExpandPrefixedDictionary([Query(".", "root")] IDictionary<string, string> query);
}
