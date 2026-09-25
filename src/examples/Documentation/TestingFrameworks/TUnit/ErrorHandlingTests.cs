// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>What happens when the server says no, and how to catch a test that forgot to check its API call.</summary>
public sealed class ErrorHandlingTests
{
    // Problem: my API can return 404 for a person who doesn't exist. Does my code notice?
    [Test]
    public async Task GetMissingPerson_ReturnsNotFound()
    {
        using StubHttp http = new() { { Route.Get("/people/{id}"), Reply.Status(HttpStatusCode.NotFound) } };
        using HttpClient httpClient = TestClient.Create(http);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://api.example.com/people/99"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // Problem: how do I make sure a test actually called the API it set up?
    [Test]
    public async Task ForgottenApiCall_FailsVerification()
    {
        using StubHttp http = new() { { Route.Get("/people/1"), Reply.Status(HttpStatusCode.OK) } };

        InvalidOperationException? error = await Assert.That(http.VerifyAllCalled).ThrowsExactly<InvalidOperationException>();

        await Assert.That(error?.Message).Contains("GET /people/1");
    }
}
