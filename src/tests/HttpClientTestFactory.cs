// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Creates deterministically disposable HTTP clients for isolated test handlers.</summary>
internal static class HttpClientTestFactory
{
    /// <summary>Creates a client using the platform's default handler.</summary>
    /// <returns>A client owned by the caller.</returns>
    internal static HttpClient Create() => new();

    /// <summary>Creates a client over an isolated test handler.</summary>
    /// <param name="handler">The handler owned by the client.</param>
    /// <returns>A client owned by the caller.</returns>
    internal static HttpClient Create(HttpMessageHandler handler) => new(handler);

    /// <summary>Creates a client using the platform's default handler and the supplied base address.</summary>
    /// <param name="baseAddress">The base address assigned to the client.</param>
    /// <returns>A client owned by the caller.</returns>
    internal static HttpClient Create(Uri baseAddress) => new() { BaseAddress = baseAddress };

    /// <summary>Creates a client over an isolated test handler and assigns its base address.</summary>
    /// <param name="handler">The handler owned by the client.</param>
    /// <param name="baseAddress">The base address assigned to the client.</param>
    /// <returns>A client owned by the caller.</returns>
    internal static HttpClient Create(HttpMessageHandler handler, Uri baseAddress) =>
        new(handler) { BaseAddress = baseAddress };
}
