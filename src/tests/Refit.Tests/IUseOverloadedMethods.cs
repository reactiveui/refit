// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that exposes overloaded <c>Get</c> methods.</summary>
public interface IUseOverloadedMethods
{
    /// <summary>Performs a GET against the root and returns the body as text.</summary>
    /// <returns>The response body as a string.</returns>
    [Get("")]
    Task<string> Get();

    /// <summary>Performs a GET against the status endpoint for the supplied status code.</summary>
    /// <param name="httpstatuscode">The HTTP status code to request.</param>
    /// <returns>The raw HTTP response message.</returns>
    [Get("/status/{httpstatuscode}")]
    Task<HttpResponseMessage> Get(int httpstatuscode);
}
