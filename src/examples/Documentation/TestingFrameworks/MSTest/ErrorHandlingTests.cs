// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>What happens when the server says no, and how to catch a test that forgot to check its API call.</summary>
[TestClass]
public sealed class ErrorHandlingTests
{
    // Problem: my API can return 404 for a person who doesn't exist. Does my code notice?
    [TestMethod]
    public async Task GetMissingPerson_ReturnsNotFound()
    {
        using StubHttp http = new()
        {
            { Route.Get("/people/{id}"), Reply.Status(HttpStatusCode.NotFound) },
        };
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        ApiException error = await Assert.ThrowsExactlyAsync<ApiException>(() => api.GetPersonAsync(99));

        Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
    }

    // Problem: how do I make sure a test actually called the API it set up?
    [TestMethod]
    public void ForgottenApiCall_FailsVerification()
    {
        using StubHttp http = new()
        {
            { Route.Get("/people/1"), Reply.Status(HttpStatusCode.OK) },
        };

        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(http.VerifyAllCalled);

        StringAssert.Contains(error.Message, "GET /people/1");
    }
}
