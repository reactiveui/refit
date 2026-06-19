// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A generic Refit interface modelling the httpbin echo API over response, parameter, and header types.</summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TParam">The query/parameter type.</typeparam>
/// <typeparam name="THeader">The header value type.</typeparam>
public interface IHttpBinApi<TResponse, in TParam, in THeader>
    where TResponse : class
    where THeader : struct
{
    /// <summary>Gets the echo response, sending a parameter and a header.</summary>
    /// <param name="param">The parameter to send.</param>
    /// <param name="header">The header value to send.</param>
    /// <returns>The echo response.</returns>
    [Get("")]
    Task<TResponse> Get(TParam param, [Header("X-Refit")] THeader header);

    /// <summary>Gets the echo response sending the parameter as a query object.</summary>
    /// <param name="param">The parameter to send as a query.</param>
    /// <returns>The echo response.</returns>
    [Get("/get?hardcoded=true")]
    Task<TResponse> GetQuery([Query("_")] TParam param);

    /// <summary>Posts the echo response sending the parameter as a query object.</summary>
    /// <param name="param">The parameter to send as a query.</param>
    /// <returns>The echo response.</returns>
    [Post("/post?hardcoded=true")]
    Task<TResponse> PostQuery([Query("_")] TParam param);

    /// <summary>Gets the echo response sending the parameter with an included parameter name.</summary>
    /// <param name="param">The parameter to send as a query.</param>
    /// <returns>The echo response.</returns>
    [Get("")]
    Task<TResponse> GetQueryWithIncludeParameterName([Query(".", "search")] TParam param);

    /// <summary>Gets the echo response deserialized to an explicit value type.</summary>
    /// <typeparam name="TValue">The value type to deserialize the response into.</typeparam>
    /// <param name="param">The parameter to send as a query.</param>
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The Refit test intentionally exercises a generic method whose type parameter is supplied explicitly at the call site.")]
    [Get("/get?hardcoded=true")]
    Task<TValue> GetQuery1<TValue>([Query("_")] TParam param);
}
