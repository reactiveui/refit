// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that exposes overloaded generic <c>Get</c> methods.</summary>
/// <typeparam name="TResponse">The response payload type.</typeparam>
/// <typeparam name="TParam">The request parameter type.</typeparam>
/// <typeparam name="THeader">The header value type.</typeparam>
public interface IUseOverloadedGenericMethods<TResponse, in TParam, in THeader>
    where TResponse : class
    where THeader : struct
{
    /// <summary>Performs a GET against the root and returns the body as text.</summary>
    /// <returns>The response body as a string.</returns>
    [Get("")]
    Task<string> Get();

    /// <summary>Performs a GET passing a parameter in the query and a typed header.</summary>
    /// <param name="param">The query parameter value.</param>
    /// <param name="header">The header value.</param>
    /// <returns>The deserialized response payload.</returns>
    [Get("/get")]
    Task<TResponse> Get(TParam param, [Header("X-Refit")] THeader header);

    /// <summary>Performs a GET with the header and parameter types swapped.</summary>
    /// <param name="param">The query parameter value, typed as the header type.</param>
    /// <param name="header">The header value, typed as the parameter type.</param>
    /// <returns>The deserialized response payload.</returns>
    [Get("/get")]
    Task<TResponse> Get(THeader param, [Header("X-Refit")] TParam header);

    /// <summary>Performs a GET against the status endpoint for the supplied status code.</summary>
    /// <param name="httpstatuscode">The HTTP status code to request.</param>
    /// <returns>The raw HTTP response message.</returns>
    [Get("/status/{httpstatuscode}")]
    Task<HttpResponseMessage> Get(int httpstatuscode);

    /// <summary>Performs a GET whose return type is supplied explicitly as a method type argument.</summary>
    /// <typeparam name="TValue">The type the response should be deserialized into.</typeparam>
    /// <param name="someVal">A value passed in the query string.</param>
    /// <returns>The deserialized response payload.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Intentional Refit fixture: the explicit return type argument cannot be inferred and is exercised by the generator tests.")]
    [Get("/get")]
    Task<TValue> Get<TValue>(int someVal);

    /// <summary>Performs a GET with two method type arguments where the return type is not inferable.</summary>
    /// <typeparam name="TValue">The type the response should be deserialized into.</typeparam>
    /// <typeparam name="TInput">The type of the input value.</typeparam>
    /// <param name="input">The input value passed in the query string.</param>
    /// <returns>The deserialized response payload.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Intentional Refit fixture: the explicit return type argument cannot be inferred and is exercised by the generator tests.")]
    [Get("/get")]
    Task<TValue> Get<TValue, TInput>(TInput input);

    /// <summary>Performs a GET with two inferable method type arguments.</summary>
    /// <typeparam name="TInput1">The type of the first input value.</typeparam>
    /// <typeparam name="TInput2">The type of the second input value.</typeparam>
    /// <param name="input1">The first input value passed in the query string.</param>
    /// <param name="input2">The second input value passed in the query string.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/get")]
    Task Get<TInput1, TInput2>(TInput1 input1, TInput2 input2);
}
