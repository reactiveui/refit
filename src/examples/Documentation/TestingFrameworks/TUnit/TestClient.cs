// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>Wraps a <see cref="StubHttp"/> in a plain <see cref="HttpClient"/>, for tests that call the API without Refit.</summary>
public static class TestClient
{
    /// <summary>Creates a client that sends every request to <paramref name="http"/> instead of the network.</summary>
    /// <param name="http">The stub handler the test configured with routes and replies.</param>
    /// <returns>A client the caller disposes; disposing it never disposes <paramref name="http"/>.</returns>
    public static HttpClient Create(StubHttp http) => new(http, disposeHandler: false);
}
