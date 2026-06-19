// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering route parameter handling.</summary>
public class ParameterTests
{
    /// <summary>Verifies generation for a reference-type route parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task RouteParameter() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string user);
            """);

    /// <summary>Verifies generation for a nullable reference-type route parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task NullableRouteParameter() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string? user);
            """);

    /// <summary>Verifies generation for a value-type route parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ValueTypeRouteParameter() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int user);
            """);

    /// <summary>Verifies generation for a nullable value-type route parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task NullableValueTypeRouteParameter() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int? user);
            """);
}
