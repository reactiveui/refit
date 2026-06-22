// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface exercising a range of query-string edge cases.</summary>
public interface IQueryApi
{
    /// <summary>Gets with an empty query string.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?")]
    Task EmptyQuery();

    /// <summary>Gets with a whitespace-only query string.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?     ")]
    Task WhiteSpaceQuery();

    /// <summary>Gets with a query string whose key is empty.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?=value")]
    Task EmptyQueryKey();

    /// <summary>Gets with a query string whose value is empty.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?key=")]
    Task EmptyQueryValue();

    /// <summary>Gets with a query string whose key and value are both empty.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?=")]
    Task EmptyQueryKeyAndValue();

    /// <summary>Gets with a query string containing unescaped characters.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?key,=value,&key1(=value1(")]
    Task UnescapedQuery();

    /// <summary>Gets with a query string containing already-escaped characters.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?key%2C=value%2C&key1%28=value1%28")]
    Task EscapedQuery();

    /// <summary>Gets with a query string whose key and value are mapped from parameters.</summary>
    /// <param name="key">The query key.</param>
    /// <param name="value">The query value.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?{key}={value}")]
    Task ParameterMappedQuery(string key, string value);

    /// <summary>Gets with a nullable integer collection query parameter.</summary>
    /// <param name="values">The nullable integer values.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo")]
    Task NullableIntCollectionQuery([Query] int?[] values);

    /// <summary>Gets with a complex object whose property declares a query prefix and delimiter.</summary>
    /// <param name="query">The complex query object.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo")]
    Task PrefixedQuery(PrefixedQueryObject query);

    /// <summary>Gets with a complex value forced to serialize via ToString using an empty format.</summary>
    /// <param name="size">The value-object query parameter.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/info")]
    Task EmptyFormatComplexQuery([Query(Format = "")] EnumerationQueryValue size);
}
