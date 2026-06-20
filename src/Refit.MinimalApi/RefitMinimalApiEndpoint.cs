// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Refit;

/// <summary>Describes one Minimal API endpoint generated from a Refit method.</summary>
/// <typeparam name="TApi">The Refit interface type.</typeparam>
public sealed class RefitMinimalApiEndpoint<TApi>
    where TApi : class
{
    /// <summary>Initializes a new instance of the <see cref="RefitMinimalApiEndpoint{TApi}"/> class.</summary>
    /// <param name="pattern">The ASP.NET Core route pattern.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="handler">The endpoint handler. The handler is responsible for writing the response.</param>
    public RefitMinimalApiEndpoint(
        string pattern,
        string httpMethod,
        Func<HttpContext, TApi, ValueTask> handler)
        : this(pattern, [httpMethod], handler)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RefitMinimalApiEndpoint{TApi}"/> class.</summary>
    /// <param name="pattern">The ASP.NET Core route pattern.</param>
    /// <param name="httpMethods">The HTTP methods.</param>
    /// <param name="handler">The endpoint handler. The handler is responsible for writing the response.</param>
    public RefitMinimalApiEndpoint(
        string pattern,
        IReadOnlyList<string> httpMethods,
        Func<HttpContext, TApi, ValueTask> handler)
    {
        ArgumentExceptionHelper.ThrowIfNull(pattern);
        ArgumentExceptionHelper.ThrowIfNull(httpMethods);
        ArgumentExceptionHelper.ThrowIfNull(handler);

        Pattern = pattern;
        HttpMethods = httpMethods;
        Handler = handler;
    }

    /// <summary>Gets the ASP.NET Core route pattern.</summary>
    public string Pattern { get; }

    /// <summary>Gets the HTTP methods.</summary>
    public IReadOnlyList<string> HttpMethods { get; }

    /// <summary>Gets the endpoint handler. The handler is responsible for writing the response.</summary>
    public Func<HttpContext, TApi, ValueTask> Handler { get; }
}
