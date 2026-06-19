// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit API surface used to exercise reflection-based URL parameter formatting.</summary>
public interface IBasicApi
{
    /// <summary>Gets a value using a simple string path parameter.</summary>
    /// <param name="value">The path parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/{value}")]
    Task<string> GetParam(string value);

    /// <summary>Gets a value using a derived record as a path parameter.</summary>
    /// <param name="value">The record path parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/{value}")]
    Task<string> GetDerivedParam(BaseRecord value);

    /// <summary>Gets a value using a property of a record as a path parameter.</summary>
    /// <param name="value">The record whose property supplies the path value.</param>
    /// <returns>The response body.</returns>
    [Get("/{value.PropValue}")]
    Task<string> GetPropertyParam(MyParams value);

    /// <summary>Gets a value using a generic path parameter.</summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="value">The path parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/{value}")]
    Task<string> GetGenericParam<T>(T value);

    /// <summary>Gets a value using a simple string query parameter.</summary>
    /// <param name="queryKey">The query parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetQuery(string queryKey);

    /// <summary>Gets a value using a generic query parameter.</summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="queryKey">The query parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetGenericQuery<T>(T queryKey);

    /// <summary>Gets a value using a record's properties as query parameters.</summary>
    /// <param name="queryKey">The record whose properties supply the query values.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetPropertyQuery(BaseRecord queryKey);

    /// <summary>Gets a value using an enumerable query parameter.</summary>
    /// <param name="enums">The enumerable query values.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetEnumerableQuery(IEnumerable<string> enums);

    /// <summary>Gets a value using a record's enumerable property as a query parameter.</summary>
    /// <param name="enums">The record whose enumerable property supplies the query values.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetEnumerablePropertyQuery(MyEnumerableParams enums);

    /// <summary>Gets a value using a dictionary as query parameters.</summary>
    /// <param name="dict">The dictionary supplying the query keys and values.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetDictionaryQuery(IDictionary<string, object> dict);
}
