// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Declares request policies whose settings and attribute overrides are compared.</summary>
internal interface ISettingsPolicyApi
{
    /// <summary>Sends a body using the client's buffering setting.</summary>
    /// <param name="person">The person to serialize.</param>
    /// <returns>The local handler's reply.</returns>
    [Post("/policy/body")]
    Task<string> InheritedAsync([Body] Person person);

    /// <summary>Sends a body without pre-buffering even when settings enable it.</summary>
    /// <param name="person">The person to serialize.</param>
    /// <returns>The local handler's reply.</returns>
    [Post("/policy/body")]
    Task<string> UnbufferedAsync([Body(false)] Person person);

    /// <summary>Buffers the body even when settings disable it.</summary>
    /// <param name="person">The person to serialize.</param>
    /// <returns>The local handler's reply.</returns>
    [Post("/policy/body")]
    Task<string> BufferedAsync([Body(true)] Person person);

    /// <summary>Builds a path with a placeholder reserved for later rewriting.</summary>
    /// <returns>The unsent request owned by the caller.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Refit",
        "RF015",
        Justification = "The sample demonstrates RefitSettings.AllowUnmatchedRouteParameters, which leaves {tenant} for later rewriting.")]
    [Get("/policy/{tenant}")]
    Task<HttpRequestMessage> UnmatchedAsync();
}
